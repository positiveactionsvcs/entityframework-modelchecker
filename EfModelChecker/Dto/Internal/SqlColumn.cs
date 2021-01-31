using System;

namespace EfModelChecker.Dto.Internal
{
    /// <summary>
    /// Represents the metadata about a column in the SQL database.
    /// </summary>
    internal class SqlColumn
    {
        /// <summary>
        /// The name of the column.
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
        /// The data type of the column.
        /// </summary>
        public Type DataType { get; set; }
    }
}
