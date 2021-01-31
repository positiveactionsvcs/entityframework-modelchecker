namespace EfModelChecker.Dto.Internal
{
    /// <summary>
    /// Represents the mapping of an entity "Association" to the tables named in Entity Framework.
    /// </summary>
    internal class EfRelationshipMapping
    {
        /// <summary>
        /// The table for the primary key.
        /// </summary>
        public string FromTable { get; set; }

        /// <summary>
        /// The list of properties that make up the key on the "from" table.
        /// </summary>
        public string[] FromProperties { get; set; }

        /// <summary>
        /// The table for the foreign key.
        /// </summary>
        public string ToTable { get; set; }

        /// <summary>
        /// The list of properties that make up the key on the "to" table.
        /// </summary>
        public string[] ToProperties { get; set; }
    }
}
