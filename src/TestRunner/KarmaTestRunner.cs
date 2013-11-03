using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using JetBrains.ReSharper.TaskRunnerFramework;
using JetBrains.ReSharper.UnitTestFramework;
using Karma.Elements;
using Karma.Tasks;
using Karma.TestProvider;

namespace Karma.TestRunner
{
	// TODO make async starting web server and custom karma reporter

	public class KarmaTestRunner : RecursiveRemoteTaskRunner
	{
		public const string ID = KarmaTestProvider.ProviderId;

		public KarmaTestRunner(IRemoteTaskServer server) : base(server)
		{
		}

		public override void ExecuteRecursive(TaskExecutionNode node)
		{
			var task = node.RemoteTask as TestTask;
			if (task == null) return;

			var projectFolder = task.ProjectFolder;
			Action run = () =>
			{
				KarmaConfig.Build(projectFolder, Enumerable.Empty<string>());
				if (!StartKarma(projectFolder)) return;

				// notify task server
				var doc = XDocument.Load(Path.Combine(projectFolder, "test-results.xml"));
				JUnitReporter.Report(doc, Server, node);
			};

			run.BeginInvoke(ar => { }, null);
		}

		public static void Run(IUnitTestLaunch launch, string projectFolder, IEnumerable<Element> elements)
		{
			Action run = () =>
			{
				var elementList = elements.ToList();
				var testFiles = (from e in elementList.Flatten(x => x.Children.OfType<Element>())
					let pf = e.GetProjectFile()
					where pf != null
					select pf.Location.FullPath)
					.Distinct(StringComparer.CurrentCultureIgnoreCase);

				KarmaConfig.Build(projectFolder, testFiles);
				if (!StartKarma(projectFolder)) return;

				// notify task server
				var resultsFile = Path.Combine(projectFolder, "test-results.xml");
				if (File.Exists(resultsFile))
				{
					var doc = XDocument.Load(resultsFile);
					JUnitReporter.Report(doc, launch, elementList);
				}
			};

			run.BeginInvoke(ar => { }, null);
		}

		private static bool StartKarma(string projectFolder)
		{
			try
			{
				Exec("cmd.exe", "/k karma start karma.conf.resharper.js --single-run --reporters dots,junit", projectFolder);
				// Npm(projectFolder, "run resharper");
				return true;
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return false;
			}
		}

		private static void Npm(string workingDir, string npmcl)
		{
			var nodeDir = GetNodejsDir();
			var npmScript = Path.Combine(nodeDir, @"node_modules\npm\bin\npm-cli.js");
			var nodeCl = string.Format("\"{0}\" {1}", npmScript, npmcl);

			Exec(Path.Combine(nodeDir, "node.exe"), nodeCl, workingDir);
		}

		private static string GetNodejsDir()
		{
			// TODO get from config/settings or registry
			return @"C:\Program Files\nodejs";
		}

		private static void Exec(string program, string cl, string workingDir)
		{
			var process = new Process
			{
				StartInfo =
				{
					FileName = program,
					Arguments = cl,
					CreateNoWindow = true,
					UseShellExecute = false,
					WorkingDirectory = workingDir,
					RedirectStandardOutput = true,
					RedirectStandardError = true
				}
			};

			process.OutputDataReceived += (sender, args) =>
			{
				using (process.StandardOutput)
				{
					var s = process.StandardOutput.ReadToEnd();
					Console.WriteLine(s);
				}
				using (process.StandardError)
				{
					var s = process.StandardError.ReadToEnd();
					Console.WriteLine(s);
				}
			};

			process.Start();
			process.WaitForExit();
		}
	}
}

