namespace LicenseVerificationLibrary.Tests
{
    using Android.Content;

    public abstract class TestCase
    {
        private readonly Context context;

        protected TestCase(Context context)
        {
            this.context = context;
        }

        protected Context Context
        {
            get { return this.context; }
        }

        protected void AssertTrue(bool result)
        {
            if (!result)
                throw new AssertionException();
        }

        protected void AssertFalse(bool result)
        {
            if (result)
                throw new AssertionException();
        }

        protected void AssertEquals(long expected, long actual)
        {
            if (expected != actual)
                throw new AssertionException("Expected: " + expected + " Actual: " + actual);
        }

        protected void AssertEquals(string expected, string actual)
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