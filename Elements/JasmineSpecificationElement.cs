using System;
using System.Collections.Generic;
using System.Xml;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.TaskRunnerFramework;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.Util;
using JetBrains.Util.Special;
using Karma.Tasks;
using Karma.TestProvider;

namespace Karma.Elements
{
	public class JasmineSpecificationElement : JasmineElement, IEquatable<JasmineSpecificationElement>
	{
		private const string SpecNameAttribute = "SpecName";
		private RemoteTask _task;
		
		public JasmineSpecificationElement(KarmaServiceProvider provider, string name,
			ProjectModelElementEnvoy projectFileEnvoy,
			TextRange textRange, IList<string> referencedFiles)
			: base(provider, name, projectFileEnvoy, textRange, referencedFiles)
		{
		}

		public override string Kind
		{
			get { return "Jasmine Specification"; }
		}

		public bool Equals(JasmineSpecificationElement other)
		{
			return base.Equals(other);
		}

		public RemoteTask RemoteTask
		{
			get
			{
				if (GetProject() == null)
				{
					return null;
				}
				return _task ?? (_task = new TestTask(ShortName, ProjectFolder, false));
			}
		}

		public override IList<UnitTestTask> GetTaskSequence(ICollection<IUnitTestElement> explicitElements,
			IUnitTestLaunch launch)
		{
			return RemoteTask.IfNotNull(x => new List<UnitTestTask>
			{
				new UnitTestTask(this, x)
			});
		}

		public static IUnitTestElement ReadFromXml(XmlElement parent, IUnitTestElement parentElement,
			JasmineElementFactory factory, ISolution solution)
		{
			var p = ReadFromXml(parent, solution, SpecNameAttribute);
			if (p == null) return null;

			var suite = parentElement as JasmineSuiteElement;
			return factory.GetOrCreateSpecification(p.Name, p.ProjectFolder, p.ProjectFileEnvoy,
				p.FileName, p.TextRange, p.ReferencedFiles, suite);
		}

		public void WriteToXml(XmlElement parent)
		{
			WriteToXml(parent, SpecNameAttribute);
		}
	}
}

