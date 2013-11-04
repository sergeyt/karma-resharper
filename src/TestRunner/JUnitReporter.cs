using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using JetBrains.ReSharper.TaskRunnerFramework;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.Util.Special;
using Karma.Elements;
using Karma.Tasks;

namespace Karma.TestRunner
{
	internal sealed class JUnitReporter
	{
		public static void Report(XDocument doc, IRemoteTaskServer server, TaskExecutionNode node)
		{
			var testCases = Parse(doc);
			if (testCases == null) return;

			var testNodes = from n in node.Flatten(x => x.Children)
				where n.RemoteTask is TestTask
				select n;

			foreach (var testNode in testNodes)
			{
				var key = GetFullName(testNode);
				if (!testCases.ContainsKey(key)) continue;

				var testCase = testCases[key];
				var task = testNode.RemoteTask;

				if (testCase.Duration != null)
				{
					server.TaskDuration(task, testCase.Duration.Value);
				}
				server.TaskFinished(task, testCase.Error, testCase.TaskResult);
			}
		}

		public static void Report(XDocument doc, IUnitTestLaunch launch, IEnumerable<Element> elements)
		{
			var testCases = Parse(doc);
			if (testCases == null) return;

			foreach (var element in elements.Flatten(x => x.Children.OfType<Element>()).OfType<JasmineSpecificationElement>())
			{
				var key = element.Id;
				if (!testCases.ContainsKey(key)) continue;

				var testCase = testCases[key];
				var task = element.RemoteTask;

				if (testCase.Duration != null)
				{
					launch.TaskDuration(task, testCase.Duration.Value);
				}
				launch.TaskFinished(task, testCase.Error, testCase.TaskResult);
			}
		}

		private static string GetFullName(TaskExecutionNode node)
		{
			// using karma-junit-reporter convention
			var fullname = "";

			while (node != null)
			{
				var name = GetName(node);
				if (string.IsNullOrEmpty(name)) break;
				fullname = string.IsNullOrEmpty(fullname) ? name : name + " " + fullname;
				node = node.Parent;
			}

			return fullname.Replace('.', '_');
		}

		private static string GetName(TaskExecutionNode node)
		{
			var test = node.RemoteTask as TestTask;
			if (test != null) return test.Name;

			return null;
		}

		private static IDictionary<string, TestCase> Parse(XDocument doc)
		{
			if (doc == null || doc.Root == null) return null;

			var testSuite = doc.Root.Element("testsuite");
			if (testSuite == null) return null;

			var suiteName = testSuite.Attribute("name").IfNotNull(x => x.Value) ?? "";

			return (from e in testSuite.Elements("testcase")
							 select new TestCase
							 {
								 Name = e.Attribute("name").IfNotNull(x => x.Value),
								 Suite = e.Attribute("classname")
									 .IfNotNull(x => x.Value.StartsWith(suiteName + ".") ? x.Value.Substring(suiteName.Length + 1) : x.Value),
								 Error = ParseErrorMessage(e),
								 Duration = e.Attribute("time").IfNotNull(x =>
								 {
									 double time;
									 if (double.TryParse(x.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out time))
										 return (TimeSpan?)TimeSpan.FromSeconds(time);
									 return null;
								 })
							 })
				.Where(x => !string.IsNullOrEmpty(x.Name))
				.ToDictionary(x => string.IsNullOrEmpty(x.Suite) ? x.Name : x.Suite + " " + x.Name, x => x);
		}

		private static string ParseErrorMessage(XElement testCase)
		{
			var message = testCase.Element("error").IfNotNull(x => x.Attribute("message").IfNotNull(a => a.Value));
			if (!string.IsNullOrEmpty(message))
			{
				return message;
			}

			message = testCase.Element("failure").IfNotNull(x => x.Value);
			return string.IsNullOrEmpty(message) ? null : message;
		}

		private class TestCase
		{
			public string Name;
			public string Suite;
			public string Error;
			public TimeSpan? Duration;

			public TaskResult TaskResult
			{
				get { return string.IsNullOrEmpty(Error) ? TaskResult.Success : TaskResult.Error; }
			}
		}
	}
}
