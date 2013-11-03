using System.Collections.Generic;
using System.Linq;
using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.JavaScript.LanguageImpl;
using JetBrains.ReSharper.Psi.JavaScript.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.Util;
using Karma.Elements;

namespace Karma.TestProvider
{
	public class JasmineFileExplorer : IRecursiveElementProcessor
    {
        private readonly UnitTestElementLocationConsumer _consumer;
        private JasmineSuiteElement _currentSuite;
        private int _disabledEndOffset = -1;
        private readonly JasmineElementFactory _factory;
        private readonly IFile _file;
        private readonly string _filename;
        private readonly CheckForInterrupt _interrupted;
        private readonly IProject _project;
        private readonly ProjectModelElementEnvoy _projectFileEnvoy;
        private readonly string[] _referencedFiles;

        public JasmineFileExplorer(JasmineElementFactory factory, UnitTestElementLocationConsumer consumer, IFile file, CheckForInterrupt interrupted, IEnumerable<IPsiSourceFile> referencedFiles)
        {
            _factory = factory;
            _consumer = consumer;
            _file = file;
            _interrupted = interrupted;

            _referencedFiles = (referencedFiles != null) ? (from f in referencedFiles
                where f.PrimaryPsiLanguage.IsExactly<JavaScriptLanguage>()
                select f.GetLocation().FullPath).ToArray<string>() : EmptyArray<string>.Instance;

            var element = file.GetSourceFile().ToProjectFile();
            if (element != null)
            {
                _filename = element.Location.FullPath;
            }

            _project = _file.GetProject();
            _projectFileEnvoy = ProjectModelElementEnvoy.Create(element);
        }

		public bool InteriorShouldBeProcessed(ITreeNode element)
        {
            if (_disabledEndOffset != -1)
            {
                if (element.GetDocumentRange().TextRange.EndOffset <= _disabledEndOffset)
                {
                    return false;
                }
                _disabledEndOffset = -1;
            }
            return true;
        }

		public void ProcessAfterInterior(ITreeNode element)
        {
        }

		public void ProcessBeforeInterior(ITreeNode element)
        {
            var invocationExpression = element as IInvocationExpression;
            if (invocationExpression != null)
            {
                var invokedExpression = invocationExpression.InvokedExpression as IReferenceExpression;
                if ((invokedExpression != null) && (invokedExpression.Qualifier == null))
                {
                    var name = invokedExpression.Name;
	                switch (name)
	                {
						case "it":
							ExploreTest(invokedExpression.GetDocumentRange().TextRange, invocationExpression);
							break;
						case "describe":
							ExploreSuite(invocationExpression);
							break;
						case "xdescribe":
							_disabledEndOffset = invocationExpression.GetDocumentRange().TextRange.EndOffset;
			                break;
	                }
                }
            }
        }

		public bool ProcessingIsFinished
        {
            get { return _interrupted(); }
        }

		private void ExploreTest(TextRange textRange, IInvocationExpression invocationExpression)
		{
			var arguments = invocationExpression.Arguments;
			switch (arguments.Count)
			{
				case 2:
				case 3:
				{
					var expression = arguments[0] as IJavaScriptLiteralExpression;
					if (expression != null)
					{
						var range = invocationExpression.GetDocumentRange().TextRange;
						FinishCurrentSuite(range.StartOffset);
						var stringValue = expression.GetStringValue();
						var unitTestElement = _factory.GetOrCreateSpecification(stringValue, _project, _projectFileEnvoy, _filename, range, _referencedFiles, _currentSuite);
						_consumer(new UnitTestElementDisposition(unitTestElement, _file.GetSourceFile().ToProjectFile(), textRange, range));
					}
					break;
				}
			}
		}

		private void ExploreSuite(IInvocationExpression invocationExpression)
		{
			var arguments = invocationExpression.Arguments;
			if (arguments.Count == 2)
			{
				var expression = arguments[0] as IJavaScriptLiteralExpression;
				if ((expression != null) && (arguments[1] is IFunctionExpression))
				{
					var stringValue = expression.GetStringValue();
					FinishCurrentSuite(invocationExpression.GetDocumentRange().TextRange.StartOffset);
					_currentSuite = _factory.GetOrCreateSuite(_project, _projectFileEnvoy, stringValue, _currentSuite, invocationExpression.GetDocumentRange().TextRange, _referencedFiles);
					foreach (var element in _currentSuite.Children.ToList())
					{
						element.State = UnitTestElementState.Pending;
					}
					_consumer(new UnitTestElementDisposition(_currentSuite, _file.GetSourceFile().ToProjectFile(), expression.GetDocumentRange().TextRange, invocationExpression.GetDocumentRange().TextRange));
				}
			}
		}

		private void FinishCurrentSuite(int offset)
		{
			if ((_currentSuite != null) && (_currentSuite.TextRange.EndOffset < offset))
			{
				_currentSuite = _currentSuite.Parent as JasmineSuiteElement;
				FinishCurrentSuite(offset);
			}
		}
    }
}

