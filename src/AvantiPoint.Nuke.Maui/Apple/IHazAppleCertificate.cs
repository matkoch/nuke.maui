﻿using AvantiPoint.Nuke.Maui.Extensions;
using AvantiPoint.Nuke.Maui.Tools.Security;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Components;
using Serilog;
using static AvantiPoint.Nuke.Maui.Tools.Security.SecurityTasks;

namespace AvantiPoint.Nuke.Maui.Apple;

[PublicAPI]
public interface IHazAppleCertificate : IHazGitRepository, INukeBuild
{
    [Parameter("P12 Certificate must be Base64 Encoded"), Secret]
    string P12B64 => TryGetValue(() => P12B64);

    [Parameter("P12 Certificate must be provided"), Secret]
    string P12Password => TryGetValue(() => P12Password);

    AbsolutePath P12CertifiatePath => TemporaryDirectory / "apple.p12";

    AbsolutePath KeychainPath => TemporaryDirectory / "signing_temp.keychain";

    Target RestoreIOSCertificate => _ => _
        .OnlyOnMacHost()
        .TryBefore<IDotNetRestore>()
        .BeforeMauiWorkload()
        .Unlisted()
        .Executes(() =>
        {
            Log.Debug("Restoring Apple Developer Certificate.");
            var data = Convert.FromBase64String(P12B64);
            File.WriteAllBytes(P12CertifiatePath, data);
            var relativePath = EnvironmentInfo.WorkingDirectory.GetRelativePathTo(P12CertifiatePath);
            Assert.True(P12CertifiatePath.FileExists(), "Something went wrong, the Apple Developer Certificate was not restored and does not exist at the expected path.");
            Log.Debug("Apple Developer Certificate restored to path '{relativePath}'.", relativePath);

            try
            {
                if(!KeychainPath.Exists())
                {
                    Log.Debug("Creating Temporary Signing Keychain.");
                    SecurityCreateKeychain(settings => settings
                        .SetPassword(P12Password)
                        .SetKeychain(KeychainPath));
                    Security($"set-keychain-settings -lut 21600 {KeychainPath}");
                }

                // Unlock Keychain
                Log.Debug("Unlocking Temporary Signing Keychain.");
                SecurityUnlockKeychain(_ => _
                    .SetPassword(P12Password)
                    .SetKeychain(KeychainPath));
                // Import Pkcs12
                Log.Debug("Importing Apple Developer Certificate from: {relativePath}", relativePath);
                SecurityImport(_ => _
                    .SetCertificatePath(P12CertifiatePath)
                    .SetPassword(P12Password)
                    .EnableAllowAny()
                    .SetType(AppleCertificateType.cert)
                    .SetFormat(AppleCertificateFormat.pkcs12)
                    .SetKeychainPath(KeychainPath)
                    .SetProcessArgumentConfigurator(_ => _
                        .Add("-T /usr/bin/codesign")
                        .Add("-T /usr/bin/security")));
                // SetPartitionList
                Log.Debug("Setting the Keychain Partition List");
                SecuritySetPartitionList(_ => _
                    .SetAllowedList("apple-tool:,apple:")
                    .SetPassword(P12Password)
                    .SetKeychain(KeychainPath));
                // Update Keychain list
                Log.Debug("Updating the Keychain List");
                Security($"list-keychain -d user -s {KeychainPath} login.keychain");
            }
            catch
            {
                Log.Error("Error Encountered by Security Tool");
                Assert.Fail("Unable to import p12 into the keychain");
            }
        });
}
