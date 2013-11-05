#if RESHARPER_7
using System.Linq;
using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace Karma.TestProvider
{
	/// <summary>
	/// Cache for JavaScript tests.
	/// </summary>
	partial class TestCache : ICache
	{
		public void OnFileRemoved(IPsiSourceFile sourceFile)
		{
			Drop(sourceFile);
		}

		public object Build(IPsiAssembly assembly)
		{
			return null;
		}

		public void Merge(IPsiAssembly assembly, object part)
		{
		}

		public void OnAssemblyRemoved(IPsiAssembly assembly)
		{
		}

		public void OnSandBoxCreated(SandBox sandBox)
		{
		}

		public void OnSandBoxPsiChange(ITreeNode elementContainingChanges)
		{
		}

		public IEnumerable<IPsiSourceFile> OnProjectModelChange(ProjectModelChange change)
		{
			// TODO
			return Enumerable.Empty<IPsiSourceFile>();
		}

		public IEnumerable<IPsiSourceFile> OnPsiModulePropertiesChange(IPsiModule module)
		{
			// TODO
			return Enumerable.Empty<IPsiSourceFile>();
		}
	}
}
#endif