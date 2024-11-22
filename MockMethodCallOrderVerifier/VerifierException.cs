namespace MockMethodCallOrderVerifier
{
#if DEBUG
    /// <summary>
    /// Exception class for exceptions thrown in the <see cref="MethodCallOrderVerifier" /> class.
    /// </summary>
    [Serializable]
    public class VerifierException : Exception
    {
        /// <summary>
        /// Create an exception without parameters.
        /// </summary>
        public VerifierException()
        {
        }

        /// <summary>
        /// Create an exception with the given message.
        /// </summary>
        /// <param name="message">
        /// The exception message.
        /// </param>
        public VerifierException(string message) : base(message)
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
        public VerifierException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
#endif
}