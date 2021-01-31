namespace EfModelChecker.Dto.Internal
{
    /// <summary>
    /// Represents a table named in Entity Framework.
    /// </summary>
    internal class EfTable
    {
        /// <summary>
        /// The name of the table the entity is mapped to.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// The schema that the table belongs to.
        /// </summary>
        public string SchemaName { get; set; }
    }
}
