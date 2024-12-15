namespace MockMethodCallOrderVerifier
{
    /// <summary>
    /// Exception class for exceptions thrown in the <see cref="Sequence{T}" /> class.
    /// </summary>
    [Serializable]
    public class SequenceException : Exception
    {
        /// <summary>
        /// Create an exception without parameters.
        /// </summary>
        public SequenceException()
        {
        }

        /// <summary>
        /// Create an exception with the given message.
        /// </summary>
        /// <param name="message">
        /// The exception message.
        /// </param>
        public SequenceException(string message) : base(message)
        {
        }

        /// <summary>
        /// Create an exception with the given message and inner exception.
        /// </summary>
        /// <param name="message">
        /// The exception message.
        /// </param>
        /// <param name="innerException">
        /// The inner exception.
        /// </param>
        public SequenceException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}