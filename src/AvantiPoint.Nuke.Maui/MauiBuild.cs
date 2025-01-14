﻿using System.Reflection;
using AvantiPoint.Nuke.Maui.Android;
using AvantiPoint.Nuke.Maui.Apple;
using AvantiPoint.Nuke.Maui.Windows;
using Nuke.Common;
using Nuke.Common.Utilities.Collections;

namespace AvantiPoint.Nuke.Maui;

[AndroidKeystoreCleanup]
[AppleCertificateCleanup]
[WinUICodeSignCleanup]
public abstract class MauiBuild : NukeBuild,
    IHazAndroidBuild,
    IHazIOSBuild,
    IHazMacCatalystBuild,
    IHazWinUIBuild,
    IEncodeFile
{
    public abstract string ApplicationDisplayVersion { get; }
    public abstract long ApplicationVersion { get; }

    public TimeSpan CompileTimeout { get; protected set; } = TimeSpan.FromMinutes(15);

    protected sealed override void WriteLogo()
    {
        Debug();
        GetAsciiArt().ForEach(x => Debug(x));
        Debug();
    }

    protected virtual string[] GetAsciiArt() =>
        new[]
        {
            "██████╗░░█████╗░░██╗░░░░░░░██╗███████╗██████╗░███████╗██████╗░  ██████╗░██╗░░░██╗",
            "██╔══██╗██╔══██╗░██║░░██╗░░██║██╔════╝██╔══██╗██╔════╝██╔══██╗  ██╔══██╗╚██╗░██╔╝",
            "██████╔╝██║░░██║░╚██╗████╗██╔╝█████╗░░██████╔╝█████╗░░██║░░██║  ██████╦╝░╚████╔╝░",
            "██╔═══╝░██║░░██║░░████╔═████║░██╔══╝░░██╔══██╗██╔══╝░░██║░░██║  ██╔══██╗░░╚██╔╝░░",
            "██║░░░░░╚█████╔╝░░╚██╔╝░╚██╔╝░███████╗██║░░██║███████╗██████╔╝  ██████╦╝░░░██║░░░",
            "╚═╝░░░░░░╚════╝░░░░╚═╝░░░╚═╝░░╚══════╝╚═╝░░╚═╝╚══════╝╚═════╝░  ╚═════╝░░░░╚═╝░░░",
            string.Empty,
            string.Empty,
            "░█████╗░██╗░░░██╗░█████╗░███╗░░██╗████████╗██╗██████╗░░█████╗░██╗███╗░░██╗████████╗",
            "██╔══██╗██║░░░██║██╔══██╗████╗░██║╚══██╔══╝██║██╔══██╗██╔══██╗██║████╗░██║╚══██╔══╝",
            "███████║╚██╗░██╔╝███████║██╔██╗██║░░░██║░░░██║██████╔╝██║░░██║██║██╔██╗██║░░░██║░░░",
            "██╔══██║░╚████╔╝░██╔══██║██║╚████║░░░██║░░░██║██╔═══╝░██║░░██║██║██║╚████║░░░██║░░░",
            "██║░░██║░░╚██╔╝░░██║░░██║██║░╚███║░░░██║░░░██║██║░░░░░╚█████╔╝██║██║░╚███║░░░██║░░░",
            "╚═╝░░╚═╝░░░╚═╝░░░╚═╝░░╚═╝╚═╝░░╚══╝░░░╚═╝░░░╚═╝╚═╝░░░░░░╚════╝░╚═╝╚═╝░░╚══╝░░░╚═╝░░░",
        };

    private static void Debug(string? text = null)
    {
        var hostType = typeof(Host);
        var method = hostType.GetMethod("Debug", BindingFlags.Static | BindingFlags.NonPublic);
        method?.Invoke(null, new[] { text });
    }
}
