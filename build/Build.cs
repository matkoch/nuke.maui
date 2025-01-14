using System;
using AvantiPoint.Nuke.Maui;
using AvantiPoint.Nuke.Maui.CI.AzurePipelines;
using AvantiPoint.Nuke.Maui.CI.GitHubActions;
using AvantiPoint.Nuke.Maui.Windows;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Tools.NerdbankGitVersioning;

[GitHubWorkflow(typeof(CI))]
[GitHubWorkflow(typeof(PR))]
[AzurePipelines(typeof(CI))]
class Build : MauiBuild, ICompileLibrary, IPublishInternal, ICodeSignNuget
{
    public static int Main () => Execute<Build>(x => x.Foo);

    Target Foo => _ => _
        .Executes(() =>
        {
            WinUIAppSigning.AzureKeyVaultSign(null, new string[0]);
        });

    public GitHubActions GitHubActions => GitHubActions.Instance;

    [NerdbankGitVersioning]
    readonly NerdbankGitVersioning NerdbankVersioning;

    public override string ApplicationDisplayVersion => NerdbankVersioning.SimpleVersion;
    public override long ApplicationVersion => IsLocalBuild ?
        (DateTimeOffset.Now.ToUnixTimeSeconds() - new DateTimeOffset(2022, 7, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds()) / 60 :
        GitHubActions.RunNumber;
}
