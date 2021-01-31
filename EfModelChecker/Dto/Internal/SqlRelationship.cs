namespace EfModelChecker.Dto.Internal
{
    /// <summary>
    /// Represents the metadata about a primary/foreign key pairing in the SQL database.
    /// </summary>
    internal class SqlRelationship
    {
        /// <summary>
        /// The table containing the primary key.
        /// </summary>
        public string PrimaryKeyTableName { get; set; }

        /// <summary>
        /// The column name of the primary key.
        /// </summary>
        public string PrimaryKeyColumnName { get; set; }

        /// <summary>
        /// Name of the foreign key constraint.
        /// </summary>
        public string ForeignKeyName { get; set; }

        /// <summary>
        /// The table containing the foreign key.
        /// </summary>
        public string ForeignKeyTableName { get; set; }

        /// <summary>
        /// The column name of the foreign key.
        /// </summary>
        public string ForeignKeyColumnName { get; set; }
        
        /// <summary>
        /// Position of the column.
        /// </summary>
        /// <remarks>
        /// Normally is "1" but could be 2 or more with a composite key.
        /// </remarks>
        public int OrdinalPosition { get; set; }
    }
}
