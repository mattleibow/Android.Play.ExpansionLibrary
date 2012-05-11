namespace LicenseVerificationLibrary.Tests
{
    using Android.Content;
    using Android.Provider;

    using Java.Net;
    
    public class ApkExpansionPolicyTest : TestCase
    {
        private ApkExpansionPolicy policy;

        public ApkExpansionPolicyTest(Context context)
            : base(context)
        {
        }

        public override void SetUp()
        {
            var salt = new byte[] { 104, 12, 112, 82, 85, 10, 11, 61, 15, 54, 44, 66, 117, 89, 64, 110, 53, 123, 33 };

            string deviceId = Settings.Secure.GetString(Context.ContentResolver, Settings.Secure.AndroidId);
            this.policy = new ApkExpansionPolicy(Context, new AesObfuscator(salt, Context.PackageName, deviceId));
        }

        /// <summary>
        /// Verify that extra data is parsed correctly on a LICENSED resopnse..
        /// </summary>
        public void TestExtraDataParsed()
        {
            const string SampleResponse = "0|1579380448|com.example.android.market.licensing|1|" +
                                          "ADf8I4ajjgc1P5ZI1S1DN/YIPIUNPECLrg==|1279578835423:VT=11&GT=22&GR=33" +
                                          "&FILE_URL1=http://jmt17.google.com/vending_kila/download/AppDownload?packageName%3Dcom.example.android.market.licensing%26versionCode%3D3%26ft%3Do%26token%3DAOTCm0RwlzqFYylBNSCTLJApGH0cYtm9g8mGMdUhKLSLJW4v9VM8GLj4GVlGU5oyW6y3FsXrJiQqMunTGw9B" +
                                          "&FILE_NAME1=main.3.com.example.android.market.licensing.obb" +
                                          "&FILE_SIZE1=687801613" +
                                          "&FILE_URL2=http://jmt17.google.com/vending_kila/download/AppDownload?packageName%3Dcom.example.android.market.licensing%26versionCode%3D3%26ft%3Do%26token%3DAOTCm0RwlzqFYylBNSCTLJApGH0cYtm9g8mGMdUhKLSLJW4v9VM8GLsdSDjefsdfEKdVaseEsfaMeifTek9B" +
                                          "&FILE_NAME2=patch.3.com.example.android.market.licensing.obb" +
                                          "&FILE_SIZE2=204233";

            this.policy.ProcessServerResponse(PolicyServerResponse.Licensed, ResponseData.Parse(SampleResponse));
            AssertEquals(11L, this.policy.ValidityTimestamp);
            AssertEquals(22L, this.policy.RetryUntil);
            AssertEquals(33L, this.policy.MaxRetries);
            AssertEquals(2, this.policy.GetExpansionUrlCount());
            AssertEquals("main.3.com.example.android.market.licensing.obb", this.policy.GetExpansionFileName(ApkExpansionPolicy.ExpansionFileType.MainFile));
            AssertEquals(687801613L, this.policy.GetExpansionFileSize(ApkExpansionPolicy.ExpansionFileType.MainFile));
            AssertEquals(
                URLDecoder.Decode(
                    "http://jmt17.google.com/vending_kila/download/AppDownload?packageName%3Dcom.example.android.market.licensing%26versionCode%3D3%26ft%3Do%26token%3DAOTCm0RwlzqFYylBNSCTLJApGH0cYtm9g8mGMdUhKLSLJW4v9VM8GLj4GVlGU5oyW6y3FsXrJiQqMunTGw9B"),
                this.policy.GetExpansionUrl(0));
            AssertEquals("patch.3.com.example.android.market.licensing.obb", this.policy.GetExpansionFileName(ApkExpansionPolicy.ExpansionFileType.PatchFile));
            AssertEquals(204233, this.policy.GetExpansionFileSize(ApkExpansionPolicy.ExpansionFileType.PatchFile));
            AssertEquals(
                URLDecoder.Decode(
                    "http://jmt17.google.com/vending_kila/download/AppDownload?packageName%3Dcom.example.android.market.licensing%26versionCode%3D3%26ft%3Do%26token%3DAOTCm0RwlzqFYylBNSCTLJApGH0cYtm9g8mGMdUhKLSLJW4v9VM8GLsdSDjefsdfEKdVaseEsfaMeifTek9B"),
                this.policy.GetExpansionUrl(ApkExpansionPolicy.ExpansionFileType.PatchFile));
        }

        /// <summary>
        /// Verify that retry counts are cleared after getting a NOT_LICENSED response.
        /// </summary>
        public void TestRetryCountsCleared()
        {
            const string SampleResponse =
                "0|1579380448|com.example.android.market.licensing|1|ADf8I4ajjgc1P5ZI1S1DN/YIPIUNPECLrg==|1279578835423:VT=1&GT=2&GR=3";

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

        public void TestNoFailureOnEncodedExtras()
        {
            const string SampleResponse =
                "0|1579380448|com.example.android.market.licensing|1|ADf8I4ajjgc1P5ZI1S1DN/YIPIUNPECLrg==|1279578835423:VT=1&test=hello%20world%20%26%20friends&GT=2&GR=3";

            this.policy.ProcessServerResponse(PolicyServerResponse.Licensed, ResponseData.Parse(SampleResponse));
            AssertEquals(1L, this.policy.ValidityTimestamp);
            AssertEquals(2L, this.policy.RetryUntil);
            AssertEquals(3L, this.policy.MaxRetries);
        }
    }
}