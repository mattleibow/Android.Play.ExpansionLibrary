namespace LicenseVerificationLibrary
{
    using System;

    /// <summary>
    /// Indicates that an error occurred while validating the integrity 
    /// of data managed by an <see cref="IObfuscator"/>.
    /// </summary>
    public class ValidationException : Exception
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class.
        /// </summary>
        public ValidationException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        public ValidationException(string message)
            : base(message)
        {
        }

        #endregion
    }
}