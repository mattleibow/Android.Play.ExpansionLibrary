using Android.Content;

namespace LicenseVerificationLibrary.Tests
{
    public class StrictPolicyTest : TestCase
    {
        /**
     * Verify that initial response is to deny access.
     */

        public StrictPolicyTest(Context context)
            : base(context)
        {
        }

        public void testInitialResponse()
        {
            var p = new StrictPolicy();
            bool result = p.AllowAccess();
            AssertFalse(result);
        }

        /**
     * Verify that after receiving a LICENSED response, the policy grants
     * access.
     */

        public void testLicensedResonse()
        {
            var p = new StrictPolicy();
            p.ProcessServerResponse(PolicyServerResponse.Licensed, null);
            bool result = p.AllowAccess();
            AssertTrue(result);
        }

        /**
     * Verify that after receiving a NOT_LICENSED response, the policy denies
     * access.
     */

        public void testNotLicensedResponse()
        {
            var p = new StrictPolicy();
            p.ProcessServerResponse(PolicyServerResponse.NotLicensed, null);
            bool result = p.AllowAccess();
            AssertFalse(result);
        }

        /**
     * Verify that after receiving a RETRY response, the policy denies
     * access.
     */

        public void testRetryResponse()
        {
            var p = new StrictPolicy();
            p.ProcessServerResponse(PolicyServerResponse.Retry, null);
            bool result = p.AllowAccess();
            AssertFalse(result);
        }

        public override void RunTests()
        {
            testInitialResponse();
            testLicensedResonse();
            testNotLicensedResponse();
            testRetryResponse();
        }
    }
}