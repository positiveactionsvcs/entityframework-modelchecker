namespace EfModelChecker.Dto.Internal
{
    /// <summary>
    /// Represents the metadata about a table in the SQL database.
    /// </summary>
    internal class SqlTable
    {
        /// <summary>
        /// The name of the table.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// The schema that the table belongs to.
        /// </summary>
        public string SchemaName { get; set; }
    }
}
