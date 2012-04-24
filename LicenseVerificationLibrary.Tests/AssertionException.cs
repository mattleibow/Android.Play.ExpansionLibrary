using System;

namespace LicenseVerificationLibrary.Tests
{
    public class AssertionException : Exception
    {
        public AssertionException()
        {
        }

        public AssertionException(string message)
            : base(message)
        {
        }
    }
}