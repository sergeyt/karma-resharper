using JetBrains.Application;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.JavaScript.LanguageImpl;
using JetBrains.ReSharper.Psi.JavaScript.Services;
using JetBrains.ReSharper.Psi.JavaScript.WinRT.LanguageImpl;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.UnitTestFramework;
using Karma.Elements;

namespace Karma.TestProvider
{
	[FileUnitTestExplorer]
    public class JasmineTestFileExplorer : IUnitTestFileExplorer
    {
        private readonly JasmineElementFactory myFactory;
        private readonly IJavaScriptDependencyManager myJavaScriptDependencyManager;
        private readonly KarmaTestProvider myProvider;

        public JasmineTestFileExplorer(KarmaTestProvider provider, JasmineElementFactory factory, IJavaScriptDependencyManager javaScriptDependencyManager)
        {
            myProvider = provider;
            myFactory = factory;
            myJavaScriptDependencyManager = javaScriptDependencyManager;
        }

        public void ExploreFile(IFile psiFile, UnitTestElementLocationConsumer consumer, CheckForInterrupt interrupted)
        {
            if ((psiFile.Language.Is<JavaScriptLanguage>() && !psiFile.Language.Is<JavaScriptWinRTLanguage>()) && (psiFile.GetProject() != null))
            {
                psiFile.ProcessDescendants(new JasmineFileExplorer(myFactory, consumer, psiFile, interrupted, myJavaScriptDependencyManager.GetTransitiveDependencies(psiFile.GetSourceFile())));
            }
        }

        public IUnitTestProvider Provider
        {
            get
            {
                return myProvider;
            }
        }
    }
}

