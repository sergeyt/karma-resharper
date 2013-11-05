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

#if RESHARPER_7
		public override TaskResult Start(TaskExecutionNode node)
		{
			throw new NotImplementedException();
		}

		public override TaskResult Execute(TaskExecutionNode node)
		{
			throw new NotImplementedException();
		}

		public override TaskResult Finish(TaskExecutionNode node)
		{
			throw new NotImplementedException();
		}
#endif

		public override void ExecuteRecursive(TaskExecutionNode node)
		{
			var task = node.RemoteTask as TestTask;
			if (task == null) return;

			var projectFolder = task.ProjectFolder;
			Action job = () =>
			{
				// TODO split tasks by project and e2e
				var confFile = KarmaConfig.Build(projectFolder, Enumerable.Empty<string>(), false);
				if (!StartKarma(projectFolder, confFile)) return;

				// notify task server
				var doc = LoadResults(projectFolder, false);
				JUnitReporter.Report(doc, Server, node);
			};

			job.BeginInvoke(null, null);
		}

		public static void Run(IUnitTestLaunch launch, string projectFolder, IEnumerable<Element> elements, bool isE2E)
		{
			Action job = () =>
			{
				var elementList = elements.ToList();
				var testFiles = (from e in elementList.Flatten(x => x.Children.OfType<Element>())
					let pf = e.GetProjectFile()
					where pf != null
					select pf.Location.FullPath)
					.Distinct(StringComparer.CurrentCultureIgnoreCase);

				var confFile = KarmaConfig.Build(projectFolder, testFiles, isE2E);
				if (!StartKarma(projectFolder, confFile)) return;

				// notify task server
				var doc = LoadResults(projectFolder, isE2E);
				JUnitReporter.Report(doc, launch, elementList);
			};

			job.BeginInvoke(null, null);
		}

		private static bool StartKarma(string projectFolder, string confFile)
		{
			try
			{
				Exec("cmd.exe",
					string.Format("/k karma start {0} --single-run", confFile),
					projectFolder);
				return true;
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return false;
			}
		}

		private static XDocument LoadResults(string projectFolder, bool isE2E)
		{
			var path = Path.Combine(projectFolder, isE2E ? @".resharper\e2e-test-results.xml" : @".resharper\test-results.xml");
			if (!File.Exists(path)) return null;
			return XDocument.Load(path);
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

