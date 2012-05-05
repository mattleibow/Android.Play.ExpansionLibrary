using Android.Content;

namespace LicenseVerificationLibrary.Tests
{
    public abstract class TestCase
    {
        private readonly Context _context;

        protected TestCase(Context context)
        {
            _context = context;
        }

        public Context Context
        {
            get { return _context; }
        }

        public void AssertTrue(bool result)
        {
            if (!result)
                throw new AssertionException();
        }

        public void AssertFalse(bool result)
        {
            if (result)
                throw new AssertionException();
        }

        public void AssertEquals(long expected, long actual)
        {
            if (expected != actual)
                throw new AssertionException("Expected: " + expected + " Actual: " + actual);
        }

        public void AssertEquals(string expected, string actual)
        {
            if (expected != actual)
                throw new AssertionException("Expected: " + expected + " Actual: " + actual);
        }

        public void Execute()
        {
            SetUp();
            RunTests();
            CleanUp();
        }

        public virtual void SetUp()
        {
        }

        public abstract void RunTests();

        public virtual void CleanUp()
        {
        }

        protected void Fail(string message)
        {
            throw new AssertionException(message);
        }
    }
}