using Android.Content;
using Android.Provider;

namespace LicenseVerificationLibrary.Tests
{
    public class ServerManagedPolicyTest : TestCase
    {
        private ServerManagedPolicy policy;

        public ServerManagedPolicyTest(Context context)
            : base(context)
        {
        }

        public override void SetUp()
        {
            var salt = new byte[] { 104, 12, 112, 82, 85, 10, 11, 61, 15, 54, 44, 66, 117, 89, 64, 110, 53, 123, 33 };

            string deviceId = Settings.Secure.GetString(Context.ContentResolver, Settings.Secure.AndroidId);
            this.policy = new ServerManagedPolicy(Context, new AesObfuscator(salt, Context.PackageName, deviceId));
        }

        public override void RunTests()
        {
            this.TestExtraDataParsed();
            this.TestRetryCountsCleared();
            this.TestNoFailureOnEncodedExtras();
        }

        /// <summary>
        /// Verify that extra data is parsed correctly on a LICENSED resopnse..
        /// </summary>
        private void TestExtraDataParsed()
        {
            const string SampleResponse = "0|1579380448|com.example.android.market.licensing|1|ADf8I4ajjgc1P5ZI1S1DN/YIPIUNPECLrg==|1279578835423:VT=11&GT=22&GR=33";

            this.policy.ProcessServerResponse(PolicyServerResponse.Licensed, ResponseData.Parse(SampleResponse));
            AssertEquals(11L, this.policy.ValidityTimestamp);
            AssertEquals(22L, this.policy.RetryUntil);
            AssertEquals(33L, this.policy.MaxRetries);
        }

        /// <summary>
        /// Verify that retry counts are cleared after getting a NOT_LICENSED response.
        /// </summary>
        private void TestRetryCountsCleared()
        {
            const string SampleResponse = "0|1579380448|com.example.android.market.licensing|1|ADf8I4ajjgc1P5ZI1S1DN/YIPIUNPECLrg==|1279578835423:VT=1&GT=2&GR=3";

            this.policy.ProcessServerResponse(PolicyServerResponse.Licensed, ResponseData.Parse(SampleResponse));

            // Sanity test
            AssertTrue(0L != this.policy.ValidityTimestamp);
            AssertTrue(0L != this.policy.RetryUntil);
            AssertTrue(0L != this.policy.MaxRetries);

            // Actual test
            this.policy.ProcessServerResponse(PolicyServerResponse.NotLicensed, null);
            AssertEquals(0L, this.policy.ValidityTimestamp);
            AssertEquals(0L, this.policy.RetryUntil);
            AssertEquals(0L, this.policy.MaxRetries);
        }

        private void TestNoFailureOnEncodedExtras()
        {
            const string SampleResponse = "0|1579380448|com.example.android.market.licensing|1|ADf8I4ajjgc1P5ZI1S1DN/YIPIUNPECLrg==|1279578835423:VT=1&test=hello%20world%20%26%20friends&GT=2&GR=3";

            this.policy.ProcessServerResponse(PolicyServerResponse.Licensed, ResponseData.Parse(SampleResponse));
            AssertEquals(1L, this.policy.ValidityTimestamp);
            AssertEquals(2L, this.policy.RetryUntil);
            AssertEquals(3L, this.policy.MaxRetries);
        }
    }
}