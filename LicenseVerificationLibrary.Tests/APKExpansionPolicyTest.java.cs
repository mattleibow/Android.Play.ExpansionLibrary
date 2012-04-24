using Android.Content;
using Android.Provider;
using Java.Net;
using LicenseVerificationLibrary;

public class APKExpansionPolicyTest : TestCase
{
    private APKExpansionPolicy p;

    public APKExpansionPolicyTest(Context context)
        : base(context)
    {
    }

    public override void RunTests()
    {
        testExtraDataParsed();
        testNoFailureOnEncodedExtras();
        testRetryCountsCleared();
    }

    public override void SetUp()
    {
        var SALT = new byte[] {104, 12, 112, 82, 85, 10, 11, 61, 15, 54, 44, 66, 117, 89, 64, 110, 53, 123, 33};

        string deviceId = Settings.Secure.GetString(Context.ContentResolver, Settings.Secure.AndroidId);
        p = new APKExpansionPolicy(Context, new AESObfuscator(SALT, Context.PackageName, deviceId));
    }

    /**
     * Verify that extra data is parsed correctly on a LICENSED resopnse..
     */

    public void testExtraDataParsed()
    {
        string sampleResponse = "0|1579380448|com.example.android.market.licensing|1|" +
                                "ADf8I4ajjgc1P5ZI1S1DN/YIPIUNPECLrg==|1279578835423:VT=11&GT=22&GR=33" +
                                "&FILE_URL1=http://jmt17.google.com/vending_kila/download/AppDownload?packageName%3Dcom.example.android.market.licensing%26versionCode%3D3%26ft%3Do%26token%3DAOTCm0RwlzqFYylBNSCTLJApGH0cYtm9g8mGMdUhKLSLJW4v9VM8GLj4GVlGU5oyW6y3FsXrJiQqMunTGw9B" +
                                "&FILE_NAME1=main.3.com.example.android.market.licensing.obb" +
                                "&FILE_SIZE1=687801613" +
                                "&FILE_URL2=http://jmt17.google.com/vending_kila/download/AppDownload?packageName%3Dcom.example.android.market.licensing%26versionCode%3D3%26ft%3Do%26token%3DAOTCm0RwlzqFYylBNSCTLJApGH0cYtm9g8mGMdUhKLSLJW4v9VM8GLsdSDjefsdfEKdVaseEsfaMeifTek9B" +
                                "&FILE_NAME2=patch.3.com.example.android.market.licensing.obb" +
                                "&FILE_SIZE2=204233";
        p.ProcessServerResponse(PolicyServerResponse.Licensed, ResponseData.parse(sampleResponse));
        AssertEquals(11L, p.getValidityTimestamp());
        AssertEquals(22L, p.getRetryUntil());
        AssertEquals(33L, p.getMaxRetries());
        AssertEquals(2, p.getExpansionURLCount());
        AssertEquals("main.3.com.example.android.market.licensing.obb", p.getExpansionFileName(0));
        AssertEquals(687801613L, p.getExpansionFileSize(0));
        AssertEquals(
            URLDecoder.Decode(
                "http://jmt17.google.com/vending_kila/download/AppDownload?packageName%3Dcom.example.android.market.licensing%26versionCode%3D3%26ft%3Do%26token%3DAOTCm0RwlzqFYylBNSCTLJApGH0cYtm9g8mGMdUhKLSLJW4v9VM8GLj4GVlGU5oyW6y3FsXrJiQqMunTGw9B"),
            p.getExpansionURL(0));
        AssertEquals("patch.3.com.example.android.market.licensing.obb", p.getExpansionFileName(1));
        AssertEquals(204233, p.getExpansionFileSize(1));
        AssertEquals(
            URLDecoder.Decode(
                "http://jmt17.google.com/vending_kila/download/AppDownload?packageName%3Dcom.example.android.market.licensing%26versionCode%3D3%26ft%3Do%26token%3DAOTCm0RwlzqFYylBNSCTLJApGH0cYtm9g8mGMdUhKLSLJW4v9VM8GLsdSDjefsdfEKdVaseEsfaMeifTek9B"),
            p.getExpansionURL(1));
    }

    /**
     * Verify that retry counts are cleared after getting a NOT_LICENSED response.
     */

    public void testRetryCountsCleared()
    {
        string sampleResponse = "0|1579380448|com.example.android.market.licensing|1|ADf8I4ajjgc1P5ZI1S1DN/YIPIUNPECLrg==|1279578835423:VT=1&GT=2&GR=3";
        p.ProcessServerResponse(PolicyServerResponse.Licensed, ResponseData.parse(sampleResponse));
        // Sanity test
        AssertTrue(0L != p.getValidityTimestamp());
        AssertTrue(0L != p.getRetryUntil());
        AssertTrue(0L != p.getMaxRetries());

        // Actual test
        p.ProcessServerResponse(PolicyServerResponse.NotLicensed, null);
        AssertEquals(0L, p.getValidityTimestamp());
        AssertEquals(0L, p.getRetryUntil());
        AssertEquals(0L, p.getMaxRetries());
    }

    public void testNoFailureOnEncodedExtras()
    {
        string sampleResponse =
            "0|1579380448|com.example.android.market.licensing|1|ADf8I4ajjgc1P5ZI1S1DN/YIPIUNPECLrg==|1279578835423:VT=1&test=hello%20world%20%26%20friends&GT=2&GR=3";
        p.ProcessServerResponse(PolicyServerResponse.Licensed, ResponseData.parse(sampleResponse));
        AssertEquals(1L, p.getValidityTimestamp());
        AssertEquals(2L, p.getRetryUntil());
        AssertEquals(3L, p.getMaxRetries());
    }
}