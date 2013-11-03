using System;
using System.Collections.Generic;
using System.Xml;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.Util;
using Karma.Tasks;
using Karma.TestProvider;

namespace Karma.Elements
{
	public class JasmineSuiteElement : JasmineElement, IEquatable<JasmineSuiteElement>
	{
		private const string SuiteNameAttribute = "SuiteName";

		public JasmineSuiteElement(KarmaServiceProvider provider, string name,
			IUnitTestElement parentSuite, ProjectModelElementEnvoy projectFileEnvoy,
			TextRange textRange, IList<string> referencedFiles)
			: base(provider, name, projectFileEnvoy, textRange, referencedFiles)
		{
			Parent = parentSuite;
		}

		public override string Kind
		{
			get { return "Jasmine Suite"; }
		}

		public bool Equals(JasmineSuiteElement other)
		{
			return base.Equals(other);
		}

		public override IList<UnitTestTask> GetTaskSequence(ICollection<IUnitTestElement> explicitElements,
			IUnitTestLaunch launch)
		{
			var project = GetProject();
			if (project == null)
			{
				return null;
			}

			return new List<UnitTestTask>
			{
				new UnitTestTask(this, new TestTask(ShortName, ProjectFolder, true))
			};
		}

		public static IUnitTestElement ReadFromXml(XmlElement parent, IUnitTestElement parentElement,
			JasmineElementFactory factory, ISolution solution)
		{
			var p = ReadFromXml(parent, solution, SuiteNameAttribute);
			if (p == null) return null;

			var parentSuite = parentElement as JasmineSuiteElement;
			return factory.GetOrCreateSuite(p.ProjectFolder, p.ProjectFileEnvoy, p.Name,
				parentSuite, p.TextRange, p.ReferencedFiles);
		}

		public void WriteToXml(XmlElement parent)
		{
			WriteToXml(parent, SuiteNameAttribute);
		}
	}
}

