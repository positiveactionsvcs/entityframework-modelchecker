// ReSharper disable LoopCanBeConvertedToQuery
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using EfModelChecker.Dto.Internal;

namespace EfModelChecker.Helpers
{
    /// <summary>
    /// Represents the current mapping between entities and tables in an Entity Framework model.
    /// </summary>
    internal class EfMappingHelper
    {
        /// <summary>
        /// Mapping information for each entity in the conceptual model.
        /// </summary>
        public EfEntityMapping[] EntityMappings { get; }

        /// <summary>
        /// Mapping information for each entity in the conceptual model.
        /// </summary>
        public EfRelationshipMapping[] RelationshipMappings { get; }

        /// <summary>
        /// Tables in the storage part of the model.
        /// </summary>
        public EfTable[] Tables { get; }

        /// <summary>
        /// Initializes an instance of the EfMappingHelper class.
        /// </summary>
        /// <param name="db">The Entity Framework DbContext to get the mapping from.</param>
        public EfMappingHelper(DbContext db)
        {
            // Get a reference to the metadata workspace, from where we can retrieve information about all parts of the model.
            MetadataWorkspace metadata = ((IObjectContextAdapter)db).ObjectContext.MetadataWorkspace;

            // Conceptual part of the model has info about the shape of our entity classes.
            ReadOnlyCollection<EntityContainer> conceptualEntityContainers = metadata.GetItems<EntityContainer>(DataSpace.CSpace);
            EntityContainer conceptualContainer = conceptualEntityContainers.SingleOrDefault();

            if (conceptualContainer == null)
                return;

            // Storage part of the model has info about the shape of our tables.
            ReadOnlyCollection<EntityContainer> storeContainers = metadata.GetItems<EntityContainer>(DataSpace.SSpace);
            EntityContainer storeContainer = storeContainers.SingleOrDefault();

            if (storeContainer == null)
                return;

            // Object part of the model that contains info about the actual CLR types.
            ObjectItemCollection objectItemCollection = (ObjectItemCollection)metadata.GetItemCollection(DataSpace.OSpace);
            ReadOnlyCollection<EntityType> objectContainers = metadata.GetItems<EntityType>(DataSpace.OSpace);

            // Mapping part of model is not public, so we need to write to XML.
            XDocument edmx = GetEdmx(db);

            // Set up results.
            List<EfEntityMapping> entityMappings = new List<EfEntityMapping>();
            List<EfRelationshipMapping> relationshipMappings = new List<EfRelationshipMapping>();
            List<EfTable> tables = new List<EfTable>();

            // Look through each entity in the conceptual model, and retrieve how each entity is mapped to the tables in the 
            // storage part of the model.
            //
            // Remember that it is theoretically possible that multiple entities could be mapped to the same database table, 
            // i.e. Table Splitting, or that one entity could be mapped to multiple database tables, i.e. "Entity Splitting".  
            // Therefore when it comes to checking against the database, if multiple entities are mapped to the same table, 
            // we can make sure that each of those entities have the correct data types mapped in all cases.
            //
            // Exclude the EdmMetadata table which is a table that EF may have created itself.
            foreach (EntitySet set in conceptualContainer.BaseEntitySets.OfType<EntitySet>().Where(e => e.Name != "EdmMetadatas"))
            {
                EfEntityMapping entityMapping = new EfEntityMapping
                {
                    TableMappings = new List<EfTableMapping>()
                };
                entityMappings.Add(entityMapping);

                if (objectContainers != null)
                {
                    // Get the CLR type of the entity.
                    entityMapping.EntityType = objectContainers.Select(objectItemCollection.GetClrType)
                        .Single(e => e.Name == set.ElementType.Name);

                    // Get the mapping fragments for this type.  NOTE: Types may have multiple fragments if Entity Splitting is used.
                    XElement element = edmx.Descendants()
                        .SingleOrDefault(e => e.Name.LocalName == "EntityTypeMapping"
                                && e.Attribute("TypeName")?.Value == set.ElementType.FullName);

                    if (element != null)
                    {
                        IEnumerable<XElement> mappingFragments = element.Descendants()
                            .Where(e => e.Name.LocalName == "MappingFragment");

                        foreach (XElement mapping in mappingFragments)
                        {
                            EfTableMapping tableMapping = new EfTableMapping
                            {
                                PropertyMappings = new List<EfPropertyMapping>()
                            };
                            entityMapping.TableMappings.Add(tableMapping);

                            // Find the table and schema that this fragment maps to.
                            string storeSet = mapping.Attribute("StoreEntitySet")?.Value;

                            tableMapping.TableName = (string)storeContainer
                                .BaseEntitySets.OfType<EntitySet>()
                                .Single(s => s.Name == storeSet)
                                .MetadataProperties["Table"].Value;

                            tableMapping.SchemaName = (string)storeContainer
                                .BaseEntitySets.OfType<EntitySet>()
                                .Single(s => s.Name == storeSet)
                                .MetadataProperties["Schema"].Value;

                            // Find the property-to-column mappings.
                            IEnumerable<XElement> propertyMappings = mapping
                                .Descendants()
                                .Where(e => e.Name.LocalName == "ScalarProperty");

                            foreach (XElement propertyMapping in propertyMappings)
                            {
                                // Find the property and column being mapped.
                                string propertyName = propertyMapping.Attribute("Name")?.Value ?? string.Empty;
                                string columnName = propertyMapping.Attribute("ColumnName")?.Value ?? string.Empty;

                                // Get the information about the property from the actual CLR class.
                                PropertyInfo propertyInfo = entityMapping.EntityType.GetProperty(propertyName);

                                if (propertyInfo != null)
                                {
                                    // Get the infromation about the property from the entity model metadata.
                                    EdmProperty edmProperty = set.ElementType.Properties.Single(e => e.Name == propertyName);
                                    Facet maxLengthFacet = edmProperty.TypeUsage.Facets.SingleOrDefault(f => f.Name == "MaxLength");
                                    int maxLength;

                                    // The only meaningful "max lengths" are from strings and binary values.
                                    //
                                    // If the property on the entity model is an enumeration, then check that the underlying type of 
                                    // the enumeration is what is compared with the database.
                                    //
                                    // Use reflection to retrieve the data type defined for the property on the entity itself.
                                    PrimitiveTypeKind primitiveTypeKind = PrimitiveTypeKind.Binary;
                                    Type primitivePropertyType = null;

                                    if (edmProperty.TypeUsage.EdmType.BuiltInTypeKind == BuiltInTypeKind.EnumType)
                                    {
                                        primitiveTypeKind = edmProperty.UnderlyingPrimitiveType.PrimitiveTypeKind;

                                        // Some special handling here if there is a Nullable enumeration.  If the enumeration
                                        // is nullable then the nullableEnum will be populated.
                                        Type nullableEnum = Nullable.GetUnderlyingType(propertyInfo.PropertyType);

                                        // If we're dealing with a nullable enumeration, we then have to create a nullable version of the underlying type
                                        // to compare the database schema with.  Otherwise we can just call Enum.GetUnderlyingType to get the non-nullable version.
                                        primitivePropertyType = nullableEnum != null ?
                                            typeof(Nullable<>).MakeGenericType(Enum.GetUnderlyingType(nullableEnum)) :
                                            Enum.GetUnderlyingType(propertyInfo.PropertyType);
                                    }
                                    else if (edmProperty.TypeUsage.EdmType.BuiltInTypeKind == BuiltInTypeKind.PrimitiveType)
                                    {
                                        primitiveTypeKind = ((PrimitiveType)edmProperty.TypeUsage.EdmType).PrimitiveTypeKind;
                                        primitivePropertyType = propertyInfo.PropertyType;
                                    }

                                    if ((primitiveTypeKind == PrimitiveTypeKind.Binary || primitiveTypeKind == PrimitiveTypeKind.String) && maxLengthFacet != null)
                                    {
                                        // The contents of MaxLength might be the string "Max" for columns in SQL Server like
                                        // varchar(max).  Therefore return -1 in these cases because this is also how SQL Server reports
                                        // the column length in its own metadata.  All other values for strings with maximum lengths 
                                        // should just return a numeric value.
                                        if (edmProperty.TypeUsage.Facets["MaxLength"].Value.ToString() == "Max")
                                            maxLength = -1;
                                        else
                                            maxLength = (int)edmProperty.TypeUsage.Facets["MaxLength"].Value;
                                    }
                                    else
                                    {
                                        maxLength = 0;
                                    }

                                    tableMapping.PropertyMappings.Add(new EfPropertyMapping
                                    {
                                        PropertyType = primitivePropertyType,
                                        ColumnName = columnName,
                                        IsNullable = edmProperty.Nullable,
                                        MaximumLength = maxLength
                                    });
                                }
                            }
                        }
                    }
                }
            }

            // At this point, we have retrieved all of the tables that our conceptual model has mapped to, however this won't necessarily
            // include all of the "join" tables that are part of many-many relationships.  The conceptual mapping doesn't list these tables,
            // therefore we have to look in the storage part of the model to actually get the list of tables that EF "knows" about.
            foreach (EntitySet set in storeContainer.BaseEntitySets.OfType<EntitySet>().Where(e => e.Name != "EdmMetadatas"))
            {
                EfTable table = new EfTable
                {
                    TableName = set.MetadataProperties["Table"].Value.ToString(),
                    SchemaName = set.MetadataProperties["Schema"].Value.ToString()
                };

                tables.Add(table);
            }

            // Look through all associations configured in the "store" part of the model.  These should map to the primary and foreign keys in the database.
            foreach (AssociationSet set in storeContainer.BaseEntitySets.OfType<AssociationSet>())
            {
                foreach (ReferentialConstraint constraint in set.ElementType.ReferentialConstraints)
                {
                    EfRelationshipMapping relationshipMapping = new EfRelationshipMapping
                    {
                        FromProperties = constraint.FromProperties.Select(x => x.Name).ToArray(),
                        FromTable = storeContainer.BaseEntitySets.OfType<EntitySet>().Single(e => e.Name == constraint.FromRole.Name).Table,
                        ToProperties = constraint.ToProperties.Select(x => x.Name).ToArray(),
                        ToTable = storeContainer.BaseEntitySets.OfType<EntitySet>().Single(e => e.Name == constraint.ToRole.Name).Table
                    };

                    relationshipMappings.Add(relationshipMapping);
                }
            }

            // Return results.
            EntityMappings = entityMappings.ToArray();
            Tables = tables.ToArray();
            RelationshipMappings = relationshipMappings.ToArray();
        }

        /// <summary>
        /// Write the database context to an EDMX file.
        /// </summary>
        /// <param name="db"></param>
        private static XDocument GetEdmx(DbContext db)
        {
            MemoryStream memoryStream = new MemoryStream();

            using (XmlWriter xmlWriter = XmlWriter.Create(memoryStream, new XmlWriterSettings { Indent = true }))
            {
                EdmxWriter.WriteEdmx(db, xmlWriter);
            }

            memoryStream.Position = 0;

            return XDocument.Load(memoryStream);
        }
    }
}
