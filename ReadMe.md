# Nuke MAUI

The AvantiPoint Nuke Maui library is an extension library for [Nuke Build](https://www.nuke.build/) for developers writing DotNet Maui applications. Out of the box it's meant to simplify the process of generating a fully functional CI build for your target platforms. Two extremely attractive features of Nuke Build are that it removes some of the complexity of CI as many of the various tasks are removed from the CI Platform and become agnostic of where they are run. The other attractive feature of Nuke Build is that it moves your build process out of hard to understand YAML to C# you're already extremely familiar with.

| Platform | Status |
| -------- | ------ |
| Android | Supported |
| iOS | In Progress\* |
| macOS | Supported |
| Windows | Supported |
| Tizen | Planned |

\* The iOS Build is technically feature complete and is passing in local builds, however we are still trying to determine why it is hanging and ultimately times out in CI builds.

## Application Versioning

.NET MAUI makes Application Versioning a bit more uniform with an easy to use Build Property to set the App Version / Display Version. These ultimately must follow the requirements of the underlying platforms. For instance iOS/Android might allow a Display version of `1.0-beta`, while Windows would require it to be `1.0.0`. The Nuke Maui library makes it easy to set these properties during the build, but ultimately we have no idea what your version should be. As a result we take an approach that makes it dead simple to version your app but we do not actually take it to the next level where we make assumptions around how you want to handle versioning. You can easily apply any sort of GitVersioning supported by Nuke, or add a completely custom versioning system by simply providing values for the ApplicationDisplayVersion or ApplicationVersion properties in the MauiBuild. If you do not want Nuke Maui to set the version simply return an empty string and we will not pass these values in the build.

## Getting Started

To get started you will need to setup the Nuke Build CLI tool on your system and initialize a new Nuke Build project in your repo. This will update your solution file to include the build project and add a few helpful scripts and other resources needed by Nuke Build. Next update the Build class to inherit from `MauiBuild`. This will automatically add all of the supported targets to build the platforms listed above. The `MauiBuild` class is an abstract class and you will need to implement the ApplicationDisplayVersion and ApplicationVersion which will map to the MSBuild properties that MAUI uses.

By default if these properties are null or empty we will not specify them as command line arguments and it will be assumed that you are managing these values outside of the Nuke Build.

```bash
nuke :setup
```

```cs
public class Build : MauiBuild
{
    // This has no default targets and will display available targets when you run `nuke`
    public static int Main () => Execute<Build>();

    public GitHubActions GitHubActions => GitHubActions.Instance;

    [NerdbankGitVersioning]
    readonly NerdbankGitVersioning NerdbankVersioning;

    public override string ApplicationDisplayVersion => NerdbankVersioning.NuGetPackageVersion;
    public override long ApplicationVersion => GitHubActions.RunId;
}
```

### Parameters and Secrets

The MauiBuild introduces a number of parameters that need to be provided, most of which tend to be more sensitive and are handled as secrets. After setting up your Nuke Project you'll notice a file at `./.nuke/parameters.json` which contains the values of parameters that should be passed in. Be careful with this file as when you set secrets it will add the encrypted values to the file. It's best not to check changes to this file into source control which contain secrets, however parameters such as your Solution name or the Project Name are perfectly safe to add to the file and check into source control. If you used `nuke :setup` the parameters.json should already exist with the schema and solution parameters. Be sure to add one with the name of the MAUI Single Project that you are building. This should ONLY be the name of the project not file name or path.

```json
{
  "$schema": "./build.schema.json",
  "Solution": "Demo.sln",
  "ProjectName": "MauiCIDemo"
}
```

To test this locally you will likely need to set at least a few of the secrets. To do this run `nuke :secrets`, this will prompt you to set a password. I would suggest that you set a password on Mac and do not use an autogenerated password on the keychain. As you prepare to build your app you will need to get a Keystore for your Android App, an Apple Developer Certificate for iOS & MacCatalyst, and a Code Signing Certificate for Windows. In order to handle these files they must first be base64 encoded so that you can save it as a Secret for GitHub Actions, or whatever CI Platform you're using.

> **NOTE** For Windows Builds you can optionally supply parameters to sign the MSIX using Azure KeyVault. If you use the Azure KeyVault be sure to run `nuke :add-package AzureSignTool` this will ensure that the CLI Tool is available for Nuke to use.

#### Converting files to Base64

In order to convert files such as your Keystore, or other code signing certificates to a base 64 string you will need to open a terminal. From PowerShell you can run:

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes(".\Path\WindowsCert.pfx"))
```

From the Mac Terminal you can simply run:

```bash
base64 -i your_file_path
```

Alternatively you can invoke the EncodeFile target in the MauiBuild. This is provided to make it easy out of the box to get the base64 encoded certificates when setting up your pipelines/workflows.

```bash
nuke EncodeFile --input-file-path your_file_path
```

### Running the Build

The Build can easily be run by running the `nuke` command along with the desired build target.

```bash
nuke CompileAndroid
```

## Additional Considerations

Currently the MAUI templates do not include any additional parameters which are typically required for building, particularly on iOS & MacCatalyst. Be sure that your csproj has been updated to include the following:

```xml
<PropertyGroup Condition="$(TargetFramework.Contains('-ios')) AND '$(Configuration)' == 'Release'">
  <RuntimeIdentifier>ios-arm64</RuntimeIdentifier>
</PropertyGroup>

<PropertyGroup Condition="$(TargetFramework.Contains('-maccatalyst')) AND '$(Configuration)' == 'Release'">
  <RuntimeIdentifiers>maccatalyst-x64;maccatalyst-arm64</RuntimeIdentifiers>
</PropertyGroup>
```

The P12 Certificate used to sign iOS and macCatalyst apps along with the P8 Auth Key for connecting to AppStoreConnect, as well as the Android Keystore must be provided as Base64 encoded strings. The P12 & Android Keystore files will be decoded restored on the local filesystem in the Nuke temp directory. The P12 for iOS & macCatalyst will be added to a temporary Key Chain for use during the build.

As installing workloads requires sudo access which can be a bit of a pain when running this locally, the Install Maui Workload target will first determine if the MAUI workload is installed. If it is already installed it will return eliminating any issues with requiring sudo access. This shouldn't affect your CI builds as you will already have the necessary permissions.

### Apple Provisioning Profiles

The Nuke Targets include a target that will reach out to the Apple AppStore Connect API to retrieve a specified Provisioning Profile. This is particularly useful for CI Builds as it ensures that as long as your provisioning profile is active you will always have the latest valid profile. This can really save time when you need to regenerate the provisioning profile for new team members, add new devices, or renew expiring profiles.

## Creating Workflows

The AvantiPoint.Nuke.Maui library includes some custom attributes that can be used to create custom GitHub Workflows with multiple jobs per workflow. This can be done by defining WorkflowJobs and GitHubWorkflows. The Workflow can define as many Job Names as are required. **NOTE**: Currently Nuke.Maui contains these specialized attributes for automatically creating the workflow YAML, however you can ultimately run this on any CI Server that you want.

```cs
[GitHubWorkflow("maui-build",
    FetchDepth = 0,
    AutoGenerate = true,
    OnPushBranches = new[] { MasterBranch },
    JobNames = new[] { "android-build", "ios-build" } )]
[WorkflowJob(
    Name = "android-build",
    //ArtifactName = "android",
    Image = HostedAgent.Windows,
    InvokedTargets = new[] { nameof(IHazAndroidBuild.CompileAndroid) },
    ImportSecrets = new[]
    {
        nameof(IHazAndroidKeystore.AndroidKeystoreName),
        nameof(IHazAndroidKeystore.AndroidKeystoreB64),
        nameof(IHazAndroidKeystore.AndroidKeystorePassword)
    })]

[WorkflowJob(
    Name = "ios-build",
    //ArtifactName = "ios",
    Image = HostedAgent.Mac,
    InvokedTargets = new[] { nameof(IHazIOSBuild.CompileIos) },
    ImportSecrets = new[]
    {
        nameof(IHazAppleCertificate.P12B64),
        nameof(IHazAppleCertificate.P12Password),
        nameof(IRestoreAppleProvisioningProfile.AppleIssuerId),
        nameof(IRestoreAppleProvisioningProfile.AppleKeyId),
        nameof(IRestoreAppleProvisioningProfile.AppleAuthKeyP8),
        $"{nameof(IRestoreAppleProvisioningProfile.AppleProfileId)}=IOS_PROVISIONING_PROFILE_ID"
    })]
public class Build : MauiBuild
{
    public static int Main () => Execute<Build>();

    const string MasterBranch = "master";

    public GitHubActions GitHubActions => GitHubActions.Instance;

    [NerdbankGitVersioning]
    readonly NerdbankGitVersioning NerdbankVersioning;

    public override string ApplicationDisplayVersion => NerdbankVersioning.NuGetPackageVersion;
    public override long ApplicationVersion => GitHubActions.RunId;
}
```

## Running Locally

To run locally choose you will need to ensure that your environment has been configured with the secrets required to sign your app. Start by running `nuke :secrets` to add the values of the secrets you will need for the iOS or Android Build. Next pick the target you want to run and run `nuke` with the target name.

```bash
nuke CompileAndroid

nuke CompileIos
```
