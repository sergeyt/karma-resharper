using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.ReSharper.UnitTestFramework.Elements;
using JetBrains.Util;
using Karma.TestProvider;

namespace Karma.Elements
{
	[SolutionComponent]
	public class JasmineElementFactory
	{
		private readonly KarmaServiceProvider _provider;
		private readonly IUnitTestElementManager _unitTestElementManager;

		public JasmineElementFactory(KarmaServiceProvider provider, IUnitTestElementManager unitTestElementManager)
		{
			_provider = provider;
			_unitTestElementManager = unitTestElementManager;
		}

		public JasmineSpecificationElement GetOrCreateSpecification(string specificationName, IProject project,
			ProjectModelElementEnvoy projectFileEnvoy, string filename, TextRange textRange, IList<string> referencedFiles,
			IUnitTestElement suite)
		{
			var id = string.Format("{0}:{1}", (suite != null) ? suite.Id : _provider.TestProvider.ID, specificationName);
			var elementById = _unitTestElementManager.GetElementById(project, id) as JasmineSpecificationElement;
			if (elementById != null)
			{
				elementById.Parent = suite;
				elementById.ReferencedFiles = referencedFiles;
				elementById.TextRange = textRange;
				elementById.State = UnitTestElementState.Valid;
				return elementById;
			}
			return new JasmineSpecificationElement(_provider, specificationName, projectFileEnvoy, textRange, referencedFiles)
			{
				Parent = suite
			};
		}

		public JasmineSuiteElement GetOrCreateSuite(IProject project, ProjectModelElementEnvoy projectFileEnvoy,
			string suiteName, JasmineSuiteElement parentSuite, TextRange textRange, IList<string> referencedFiles)
		{
			var id = string.Format("{0}:{1}", (parentSuite != null) ? parentSuite.Id : _provider.TestProvider.ID, suiteName);
			var elementById = _unitTestElementManager.GetElementById(project, id) as JasmineSuiteElement;
			if (elementById != null)
			{
				elementById.ReferencedFiles = referencedFiles;
				elementById.TextRange = textRange;
				elementById.State = UnitTestElementState.Valid;
				elementById.Parent = parentSuite;
				return elementById;
			}
			return new JasmineSuiteElement(_provider, suiteName, parentSuite, projectFileEnvoy, textRange, referencedFiles);
		}
	}
}

