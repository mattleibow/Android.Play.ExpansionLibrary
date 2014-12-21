using System.Reflection;
using System.Runtime.InteropServices;
using Android;
using Android.App;
using LicenseVerificationLibrary;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

[assembly: AssemblyCopyright("Copyright © .NET Development Addict 2014")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("en-US")]
[assembly: AssemblyCompany(".NET Development Addict")]
[assembly: AssemblyTitle("ExpansionDownloader.Sample")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyProduct("ExpansionDownloader.Sample")]
[assembly: Guid("a557ce8c-9dbe-4b93-8fc4-95ffc126cf14")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]

[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]


// Add some common permissions, these can be removed if not needed

[assembly: UsesPermission(Manifest.Permission.Internet)]
[assembly: UsesPermission(Manifest.Permission.WriteExternalStorage)]
[assembly: UsesPermission(Manifest.Permission.AccessNetworkState)]
[assembly: UsesPermission(Manifest.Permission.WakeLock)]
[assembly: UsesPermission(Manifest.Permission.AccessWifiState)]
[assembly: UsesPermission(LicenseChecker.Manifest.Permission.CheckLicense)]
