// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
namespace EfModelChecker.Dto
{
    /// <summary>
    /// Options to configure the behaviour of the Entity Framework model validator.
    /// </summary>
    /// <remarks>
    /// By default, check that the all of the entities and relationships in the conceptual model are present in the database.  
    /// However, can optionally make the checking stricter to check for objects in the database that are not in the model.
    /// </remarks>
    public class EfModelCheckOptions
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public EfModelCheckOptions()
        {
            DatabaseSchemaName = string.Empty;
            CheckForTablesInDatabaseButNotInModel = false;
            CheckForTablesInModelButNotInDatabase = true;
            CheckForColumnsInDatabaseButNotInModel = false;
            CheckForColumnsInModelButNotInDatabase = true;
            CheckForRelationshipsInDatabaseButNotInModel = false;
            CheckForRelationshipsInModelButNotInDatabase = true;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public EfModelCheckOptions(string databaseSchemaName)
        {
            DatabaseSchemaName = databaseSchemaName;
            CheckForTablesInDatabaseButNotInModel = false;
            CheckForTablesInModelButNotInDatabase = true;
            CheckForColumnsInDatabaseButNotInModel = false;
            CheckForColumnsInModelButNotInDatabase = true;
            CheckForRelationshipsInDatabaseButNotInModel = false;
            CheckForRelationshipsInModelButNotInDatabase = true;
        }

        /// <summary>
        /// The schema (e.g. dbo) where the tables to compare are held.
        /// </summary>
        public string DatabaseSchemaName { get; set; }

        /// <summary>
        /// Check for tables in the database which are not in the model.
        /// </summary>
        public bool CheckForTablesInDatabaseButNotInModel { get; set; }

        /// <summary>
        /// Check for tables in the model which are not in the database.
        /// </summary>
        public bool CheckForTablesInModelButNotInDatabase { get; set; }

        /// <summary>
        /// Check for columns which are in the database but not in the model.
        /// </summary>
        public bool CheckForColumnsInDatabaseButNotInModel { get; set; }

        /// <summary>
        /// Check for columns in the model which are not in the database.
        /// </summary>
        public bool CheckForColumnsInModelButNotInDatabase { get; set; }

        /// <summary>
        /// Check for relationships in the database which are not in the model.
        /// </summary>
        public bool CheckForRelationshipsInDatabaseButNotInModel { get; set; }

        /// <summary>
        /// Check for relationships which are in the model but not in the database.
        /// </summary>
        public bool CheckForRelationshipsInModelButNotInDatabase { get; set; }
    }
}
