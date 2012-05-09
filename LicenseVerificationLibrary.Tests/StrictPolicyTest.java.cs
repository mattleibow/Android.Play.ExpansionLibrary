namespace LicenseVerificationLibrary.Tests
{
    using Android.Content;

    public class StrictPolicyTest : TestCase
    {
        public StrictPolicyTest(Context context)
            : base(context)
        {
        }

        /// <summary>
        /// Verify that initial response is to deny access.
        /// </summary>
        private void TestInitialResponse()
        {
            var p = new StrictPolicy();
            bool result = p.AllowAccess();
            AssertFalse(result);
        }

        /// <summary>
        /// Verify that after receiving a LICENSED response, the policy grants access.
        /// </summary>
        private void TestLicensedResonse()
        {
            var p = new StrictPolicy();
            p.ProcessServerResponse(PolicyServerResponse.Licensed, null);
            bool result = p.AllowAccess();
            AssertTrue(result);
        }

        /// <summary>
        /// Verify that after receiving a NOT_LICENSED response, the policy denies access.
        /// </summary>
        private void TestNotLicensedResponse()
        {
            var p = new StrictPolicy();
            p.ProcessServerResponse(PolicyServerResponse.NotLicensed, null);
            bool result = p.AllowAccess();
            AssertFalse(result);
        }

        /// <summary>
        /// Verify that after receiving a RETRY response, the policy denies access.
        /// </summary>
        private void TestRetryResponse()
        {
            var p = new StrictPolicy();
            p.ProcessServerResponse(PolicyServerResponse.Retry, null);
            bool result = p.AllowAccess();
            AssertFalse(result);
        }

        public override void RunTests()
        {
            this.TestInitialResponse();
            this.TestLicensedResonse();
            this.TestNotLicensedResponse();
            this.TestRetryResponse();
        }
    }
}