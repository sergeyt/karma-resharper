﻿using System.Reflection;
using JetBrains.Application.PluginSupport;
using Karma;

[assembly: AssemblyTitle("Karma.ReSharper")]
[assembly: AssemblyDescription("Runs jasmine tests with karma runner")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyProduct("Karma.ReSharper")]
[assembly: AssemblyCompany("Sergey Todyshev")]
[assembly: AssemblyCopyright("Copyright © 2013 Sergey Todyshev, Inc. All rights reserved.")]
[assembly: AssemblyVersion(VersionInfo.Version)]
[assembly: AssemblyFileVersion(VersionInfo.Version)]
[assembly: AssemblyInformationalVersion(VersionInfo.Version)]

[assembly: PluginTitle("Karma Test Runner")]
[assembly: PluginDescription("Runs jasmine tests with karma runner")]
[assembly: PluginVendor("Sergey Todyshev")]

namespace Karma
{
	internal static class VersionInfo
	{
		public const string Version = "0.5.0.0";
	}
}