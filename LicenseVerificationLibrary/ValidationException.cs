using System;

namespace LicenseVerificationLibrary
{
    /// <summary>
    /// Indicates that an error occurred while validating the integrity 
    /// of data managed by an <see cref="IObfuscator"/>.
    /// </summary>
    public class ValidationException : Exception
    {
        public ValidationException()
        {
        }

        public ValidationException(string message)
            : base(message)
        {
        }
    }
}