using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;

namespace EfModelChecker.Helpers
{
    /// <summary>
    /// Access to the database for running SQL queries directly.
    /// </summary>
    internal static class SqlQueryHelper
    {
        /// <summary>
        /// Get a DataTable of results.
        /// </summary>
        /// <param name="sqlConnection">The SQL Server connection.</param>
        /// <param name="commandText">The SQL to execute.</param>
        /// <returns>DataTable containing query results.</returns>
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public static DataSet GetData(DbConnection sqlConnection, string commandText)
        {
            DataSet dataSet = new DataSet();

            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(commandText, (SqlConnection)sqlConnection))
                    adapter.Fill(dataSet);

                return dataSet;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving the data: {ex.Message}", ex);
            }
        }
    }
}
