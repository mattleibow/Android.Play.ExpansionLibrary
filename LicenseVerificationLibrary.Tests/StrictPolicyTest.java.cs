namespace LicenseVerificationLibrary.Tests
{
    using Android.Content;

    using LicenseVerificationLibrary.Policy;

    public class StrictPolicyTest : TestCase
    {
        public StrictPolicyTest(Context context)
            : base(context)
        {
        }

        /// <summary>
        /// Verify that initial response is to deny access.
        /// </summary>
        public void TestInitialResponse()
        {
            var p = new StrictPolicy();
            bool result = p.AllowAccess();
            AssertFalse(result);
        }

        /// <summary>
        /// Verify that after receiving a LICENSED response, the policy grants access.
        /// </summary>
        public void TestLicensedResonse()
        {
            var p = new StrictPolicy();
            p.ProcessServerResponse(PolicyServerResponse.Licensed, null);
            bool result = p.AllowAccess();
            AssertTrue(result);
        }

        /// <summary>
        /// Verify that after receiving a NOT_LICENSED response, the policy denies access.
        /// </summary>
        public void TestNotLicensedResponse()
        {
            var p = new StrictPolicy();
            p.ProcessServerResponse(PolicyServerResponse.NotLicensed, null);
            bool result = p.AllowAccess();
            AssertFalse(result);
        }

        /// <summary>
        /// Verify that after receiving a RETRY response, the policy denies access.
        /// </summary>
        public void TestRetryResponse()
        {
            var p = new StrictPolicy();
            p.ProcessServerResponse(PolicyServerResponse.Retry, null);
            bool result = p.AllowAccess();
            AssertFalse(result);
        }
    }
}