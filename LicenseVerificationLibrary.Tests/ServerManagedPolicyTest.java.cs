using Android.Content;
using Android.Provider;
using LicenseVerificationLibrary;

public class ServerManagedPolicyTest : TestCase
{
    private ServerManagedPolicy p;

    public ServerManagedPolicyTest(Context context)
        : base(context)
    {
    }

    public override void SetUp()
    {
        var SALT = new byte[] {104, 12, 112, 82, 85, 10, 11, 61, 15, 54, 44, 66, 117, 89, 64, 110, 53, 123, 33};

        string deviceId = Settings.Secure.GetString(Context.ContentResolver, Settings.Secure.AndroidId);
        p = new ServerManagedPolicy(Context, new AESObfuscator(SALT, Context.PackageName, deviceId));
    }

    public override void RunTests()
    {
        testExtraDataParsed();
        testRetryCountsCleared();
        testNoFailureOnEncodedExtras();
    }

    /**
     * Verify that extra data is parsed correctly on a LICENSED resopnse..
     */

    public void testExtraDataParsed()
    {
        string sampleResponse =
            "0|1579380448|com.example.android.market.licensing|1|ADf8I4ajjgc1P5ZI1S1DN/YIPIUNPECLrg==|1279578835423:VT=11&GT=22&GR=33";

        p.ProcessServerResponse(PolicyServerResponse.Licensed, ResponseData.parse(sampleResponse));
        AssertEquals(11L, p.getValidityTimestamp());
        AssertEquals(22L, p.getRetryUntil());
        AssertEquals(33L, p.getMaxRetries());
    }

    /**
     * Verify that retry counts are cleared after getting a NOT_LICENSED response.
     */

    public void testRetryCountsCleared()
    {
        string sampleResponse =
            "0|1579380448|com.example.android.market.licensing|1|ADf8I4ajjgc1P5ZI1S1DN/YIPIUNPECLrg==|1279578835423:VT=1&GT=2&GR=3";

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