using System;

/**
 * Indicates that an error occurred while validating the integrity of data
 * managed by an {@link Obfuscator}.}
 */

namespace LicenseVerificationLibrary
{
    public class ValidationException : Exception
    {
        private static long serialVersionUID = 1L;

        public ValidationException()
        {
        }

        public ValidationException(string s)
            : base(s)
        {
        }
    }
}