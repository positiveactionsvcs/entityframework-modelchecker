using System.Collections.Generic;

namespace EfModelChecker.Dto.Internal
{
    /// <summary>
    /// Represents the mapping of an entity to the table named in Entity Framework.
    /// </summary>
    internal class EfTableMapping
    {
        /// <summary>
        /// The name of the table the entity is mapped to.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// The schema that the table belongs to.
        /// </summary>
        public string SchemaName { get; set; }

        /// <summary>
        /// Details of the property-to-column mapping.
        /// </summary>
        public List<EfPropertyMapping> PropertyMappings { get; set; }
    }
}
