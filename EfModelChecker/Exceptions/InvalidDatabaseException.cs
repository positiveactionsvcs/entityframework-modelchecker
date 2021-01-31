using System;

namespace EfModelChecker.Exceptions
{
    /// <summary>
    /// For the raising of exceptions specific to the database.
    /// </summary>
    [Serializable]
    public class InvalidDatabaseException : Exception  
    {
        /// <summary>
        /// Creates an Exception with a message.
        /// </summary>
        /// <param name="message">The error message.</param>
        public InvalidDatabaseException(string message)
            : base(message)
        {
        }
    }
}
