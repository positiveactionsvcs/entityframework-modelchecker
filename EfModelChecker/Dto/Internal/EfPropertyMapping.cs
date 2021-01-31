using System;

namespace EfModelChecker.Dto.Internal
{
    /// <summary>
    /// Represents the mapping of a property to a column named in Entity Framework.
    /// </summary>
    internal class EfPropertyMapping
    {
        /// <summary>
        /// The column that property is mapped to.
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// Is the column nullable?
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// The maximum length (where appropriate) of the column.
        /// </summary>
        public int MaximumLength { get; set; }

        /// <summary>
        /// The property type from the entity type.
        /// </summary>
        public Type PropertyType { get; set; }
    }
}
