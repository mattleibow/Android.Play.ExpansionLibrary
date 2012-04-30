using Android.Content;
using Android.Provider;

namespace LicenseVerificationLibrary.Tests
{
    public class ServerManagedPolicyTest : TestCase
    {
        private ServerManagedPolicy _policy;

        public ServerManagedPolicyTest(Context context)
            : base(context)
        {
        }

        public override void SetUp()
        {
            var salt = new byte[] {104, 12, 112, 82, 85, 10, 11, 61, 15, 54, 44, 66, 117, 89, 64, 110, 53, 123, 33};

            string deviceId = Settings.Secure.GetString(Context.ContentResolver, Settings.Secure.AndroidId);
            _policy = new ServerManagedPolicy(Context, new AesObfuscator(salt, Context.PackageName, deviceId));
        }

        public override void RunTests()
        {
            TestExtraDataParsed();
            TestRetryCountsCleared();
            TestNoFailureOnEncodedExtras();
        }

        /**
     * Verify that extra data is parsed correctly on a LICENSED resopnse..
     */

        public void TestExtraDataParsed()
        {
            const string sampleResponse = "0|1579380448|com.example.android.market.licensing|1|ADf8I4ajjgc1P5ZI1S1DN/YIPIUNPECLrg==|1279578835423:VT=11&GT=22&GR=33";

            _policy.ProcessServerResponse(PolicyServerResponse.Licensed, ResponseData.Parse(sampleResponse));
            AssertEquals(11L, _policy.ValidityTimestamp);
            AssertEquals(22L, _policy.RetryUntil);
            AssertEquals(33L, _policy.MaxRetries);
        }

        /**
     * Verify that retry counts are cleared after getting a NOT_LICENSED response.
     */

        public void TestRetryCountsCleared()
        {
            const string sampleResponse = "0|1579380448|com.example.android.market.licensing|1|ADf8I4ajjgc1P5ZI1S1DN/YIPIUNPECLrg==|1279578835423:VT=1&GT=2&GR=3";

            _policy.ProcessServerResponse(PolicyServerResponse.Licensed, ResponseData.Parse(sampleResponse));
            // Sanity test
            AssertTrue(0L != _policy.ValidityTimestamp);
            AssertTrue(0L != _policy.RetryUntil);
            AssertTrue(0L != _policy.MaxRetries);

            // Actual test
            _policy.ProcessServerResponse(PolicyServerResponse.NotLicensed, null);
            AssertEquals(0L, _policy.ValidityTimestamp);
            AssertEquals(0L, _policy.RetryUntil);
            AssertEquals(0L, _policy.MaxRetries);
        }

        public void TestNoFailureOnEncodedExtras()
        {
            const string sampleResponse = "0|1579380448|com.example.android.market.licensing|1|ADf8I4ajjgc1P5ZI1S1DN/YIPIUNPECLrg==|1279578835423:VT=1&test=hello%20world%20%26%20friends&GT=2&GR=3";

            _policy.ProcessServerResponse(PolicyServerResponse.Licensed, ResponseData.Parse(sampleResponse));
            AssertEquals(1L, _policy.ValidityTimestamp);
            AssertEquals(2L, _policy.RetryUntil);
            AssertEquals(3L, _policy.MaxRetries);
        }
    }
}