// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using EfModelChecker.Dto;
using EfModelChecker.Dto.Internal;
using EfModelChecker.Helpers;

namespace EfModelChecker
{
    /// <summary>
    /// Class to allow an Entity Framework model to be checked against the schema of a connected database.
    /// </summary>
    public static class EfModelCheck
    {
        /// <summary>
        /// Run the validation.
        /// </summary>
        /// <param name="context">The DbContext (code-first) database context.</param>
        /// <param name="schemaName">The schema (e.g. dbo) where the tables to compare are held.  An empty string means that schema is not checked.</param>
        public static IEnumerable<string> Run(DbContext context, string schemaName = "")
        {
            return Run(context, new EfModelCheckOptions(schemaName));
        }

        /// <summary>
        /// Run the validation.
        /// </summary>
        /// <param name="context">The DbContext (code-first) database context.</param>
        /// <param name="options">A list of options.</param>
        public static IEnumerable<string> Run(DbContext context, EfModelCheckOptions options)
        {
            // Retrieve the model and all of its currently defined mappings etc.
            EfMappingHelper mapping = new EfMappingHelper(context);

            // TABLES...
            //
            // Get all of the table information, both from the database and the model.
            List<SqlTable> allSqlTables;
            List<string> allMappedTables;

            // Apply schema filter if supplied.
            if (!string.IsNullOrEmpty(options.DatabaseSchemaName))
            {
                allSqlTables = SqlSchemaHelper.GetSqlTables(context)
                    .Where(t => t.SchemaName == options.DatabaseSchemaName).ToList();

                allMappedTables = mapping.Tables
                    .Where(tm => tm.SchemaName == options.DatabaseSchemaName)
                    .Select(t => t.SchemaName + "." + t.TableName).ToList();
            }
            else
            {
                allSqlTables = SqlSchemaHelper.GetSqlTables(context).ToList();

                allMappedTables = mapping.Tables.Select(t => t.SchemaName + "." + t.TableName).ToList();
            }

            // Set up list of messages that we will return at the end.
            List<string> errors = new List<string>();

            // ...IN THE DATABASE BUT NOT IN THE MODEL
            if (options.CheckForTablesInDatabaseButNotInModel)
            {
                List<string> tablesInDatabaseButNotInModel = allSqlTables.Select(t => t.SchemaName + "." + t.TableName).Except(allMappedTables).ToList();

                errors.AddRange(tablesInDatabaseButNotInModel
                    .Select(table => $"The table {table} is in the database but not in the entity model."));
            }

            // ...IN THE MODEL BUT NOT IN THE DATABASE
            if (options.CheckForTablesInModelButNotInDatabase)
            {
                List<string> tablesInModelButNotInDatabase = allMappedTables.Except(allSqlTables.Select(t => t.SchemaName + "." + t.TableName)).ToList();

                errors.AddRange(tablesInModelButNotInDatabase.Select(table => $"The table {table} is in the model but not in the database.").ToList());
            }

            // COLUMNS...
            //
            // Now validate all of the columns in all of the entities.
            foreach (EfEntityMapping entityMapping in mapping.EntityMappings)
            {
                IEnumerable<EfTableMapping> tableMappings = options.DatabaseSchemaName != string.Empty ?
                    entityMapping.TableMappings.Where(t => t.SchemaName == options.DatabaseSchemaName) : entityMapping.TableMappings;

                foreach (EfTableMapping tableMapping in tableMappings)
                {
                    // Retrieve the columns for this particular table directly from the database.
                    List<SqlColumn> sqlColumns = SqlSchemaHelper.GetTableSqlColumns(context, tableMapping.TableName, options.DatabaseSchemaName).ToList();

                    if (options.CheckForColumnsInDatabaseButNotInModel)
                    {
                        // ...IN THE DATABASE BUT NOT IN THE MODEL
                        errors.AddRange(from sqlColumn in sqlColumns
                                        let columnNames = (from t in tableMapping.PropertyMappings select t.ColumnName).ToList()
                                        where !columnNames.Contains(sqlColumn.ColumnName)
                                        select
                                            $"The column {sqlColumn.ColumnName} in table {tableMapping.TableName} is not in the entity model.");
                    }

                    if (options.CheckForColumnsInModelButNotInDatabase)
                    {
                        // Look through all of the properties of the entity.
                        foreach (EfPropertyMapping propertyMapping in tableMapping.PropertyMappings)
                        {
                            // ...IN THE MODEL BUT NOT IN THE DATABASE
                            if (sqlColumns.All(c => c.ColumnName != propertyMapping.ColumnName))
                                errors.Add($"The column {propertyMapping.ColumnName} doesn't exist in the table {tableMapping.TableName}.");

                            SqlColumn column = sqlColumns.SingleOrDefault(c => c.ColumnName == propertyMapping.ColumnName);

                            if (column != null)
                            {
                                // Check that the column is the correct data type.
                                if (column.DataType == null)
                                    errors.Add(
                                        $"The column {propertyMapping.ColumnName} in table {tableMapping.TableName} has an unknown data type of {propertyMapping.PropertyType.Name}.");

                                if (column.DataType != null && propertyMapping.PropertyType != column.DataType)
                                    errors.Add(
                                        $"The column {propertyMapping.ColumnName} in table {tableMapping.TableName} has a data type of {propertyMapping.PropertyType.Name} which does not match with the database ({column.DataType.Name}).");

                                // Check that the nullable status of the column agrees with the database.
                                if (column.IsNullable & !propertyMapping.IsNullable)
                                    errors.Add(
                                        $"The column {propertyMapping.ColumnName} in table {tableMapping.TableName} is nullable, but the {propertyMapping.PropertyType.Name} property is not nullable.");

                                if (!column.IsNullable & propertyMapping.IsNullable)
                                    errors.Add(
                                        $"The column {propertyMapping.ColumnName} in table {tableMapping.TableName} is not nullable, but the {propertyMapping.PropertyType.Name} property is nullable.");

                                // Check that "MaxLength" property (where appropriate) of the column agrees with the database.
                                if (column.MaximumLength != propertyMapping.MaximumLength)
                                    errors.Add(
                                        $"The column {propertyMapping.ColumnName} in table {tableMapping.TableName} has a maximum length which does not agree with the {propertyMapping.PropertyType.Name} property.");
                            }
                        }
                    }
                }
            }

            // PRIMARY/FOREIGN KEY RELATIONSHIPS...
            //
            // Retrieve all primary/foreign key relationships directly from the database.
            List<SqlRelationship> sqlRelationships = SqlSchemaHelper.GetSqlRelationships(context).ToList();

            // Group these relationships by the name of the Foreign Key so that composite foreign keys can be grouped together.
            List<IGrouping<string, SqlRelationship>> sqlRelationshipGroups = sqlRelationships.GroupBy(s => s.ForeignKeyName).ToList();

            // ...IN THE DATABASE BUT NOT IN THE MODEL
            if (options.CheckForRelationshipsInDatabaseButNotInModel)
            {
                foreach (IGrouping<string, SqlRelationship> sqlRelationshipGroup in sqlRelationshipGroups)
                {
                    // For this group, retrieve the list of properties of to/from.
                    string[] fromProperties = sqlRelationshipGroup.OrderBy(g => g.OrdinalPosition).Select(g => g.PrimaryKeyColumnName).ToArray();
                    string[] toProperties = sqlRelationshipGroup.OrderBy(g => g.OrdinalPosition).Select(g => g.ForeignKeyColumnName).ToArray();
                    string fromTable = sqlRelationshipGroup.First().PrimaryKeyTableName;
                    string toTable = sqlRelationshipGroup.First().ForeignKeyTableName;

                    if (!mapping.RelationshipMappings.Any(relationship => fromTable == relationship.FromTable &&
                                                                          toTable == relationship.ToTable &&
                                                                          fromProperties.SequenceEqual(relationship.FromProperties) &&
                                                                          toProperties.SequenceEqual(relationship.ToProperties)))
                    {
                        errors.Add(
                            $"The relationship between {fromTable} and {toTable} from keys {string.Join(",", fromProperties)} to {string.Join(",", toProperties)} is in the database but not in the entity model.");
                    }
                }
            }

            if (options.CheckForRelationshipsInModelButNotInDatabase)
            {
                // ...IN THE MODEL BUT NOT IN THE DATABASE
                foreach (EfRelationshipMapping relationshipMapping in mapping.RelationshipMappings)
                {
                    bool match = false;

                    foreach (IGrouping<string, SqlRelationship> sqlRelationshipGroup in sqlRelationshipGroups)
                    {
                        // For this group, retrieve the list of properties of to/from.
                        string[] fromProperties = sqlRelationshipGroup.OrderBy(g => g.OrdinalPosition).Select(g => g.PrimaryKeyColumnName).ToArray();
                        string[] toProperties = sqlRelationshipGroup.OrderBy(g => g.OrdinalPosition).Select(g => g.ForeignKeyColumnName).ToArray();

                        if (sqlRelationshipGroup.Any(sqlRelationship => sqlRelationship.PrimaryKeyTableName == relationshipMapping.FromTable &&
                                                                        sqlRelationship.ForeignKeyTableName == relationshipMapping.ToTable &&
                                                                        fromProperties.SequenceEqual(relationshipMapping.FromProperties) &&
                                                                        toProperties.SequenceEqual(relationshipMapping.ToProperties)))
                        {
                            match = true;
                            break;
                        }
                    }

                    if (!match)
                    {
                        errors.Add(
                            $"The relationship between {relationshipMapping.FromTable} and {relationshipMapping.ToTable} from keys {string.Join(",", relationshipMapping.FromProperties)} to {string.Join(",", relationshipMapping.ToProperties)} is not in the database.");
                    }
                }
            }

            return errors.ToArray();
        }
    }
}
