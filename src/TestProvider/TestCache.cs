using System.Collections.Generic;
using System.Linq;
using System.Xml;
using JetBrains.Application;
using JetBrains.Application.Progress;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Store.Implementation;
using JetBrains.DataFlow;
using JetBrains.DocumentManagers.impl;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.JavaScript.LanguageImpl;
using JetBrains.ReSharper.Psi.JavaScript.Services;
#if RESHARPER_8
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Modules;
#endif
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util.Caches;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.Util;
using JetBrains.Util.DataStructures;
using Karma.Elements;

namespace Karma.TestProvider
{
	/// <summary>
	/// Cache for JavaScript tests.
	/// </summary>
	[PsiComponent]
	public partial class TestCache
#if RESHARPER_8
		: ISwitchingCache
#endif
	{
		private readonly JetHashSet<IPsiSourceFile> _dirtyFiles = new JetHashSet<IPsiSourceFile>();

		private readonly CompactOneToListMap<IPsiSourceFile, IUnitTestElement> _elementsInFiles =
			new CompactOneToListMap<IPsiSourceFile, IUnitTestElement>(null);

		private readonly JasmineElementFactory myJasmineFactory;
		private readonly KarmaTestProvider _testProvider;
		private readonly IJavaScriptDependencyManager _javaScriptDependencyManager;
		private readonly Lifetime _lifetime;
		private readonly object _lock = new object();
		private SimplePersistentCache<string> _persistentCache;
		private readonly IPersistentIndexManager _persistentIndexManager;
		private readonly JetHashSet<IPsiSourceFile> _processedFiles = new JetHashSet<IPsiSourceFile>();
		private readonly IPsiConfiguration _psiConfiguration;
		private readonly IUnitTestingSettingsAccessor _settingsAccessor;
		private readonly IContextBoundSettingsStoreLive _settingsStore;
		private readonly IShellLocks _shellLocks;
		private readonly ISolution _solution;

		public TestCache(Lifetime lifetime, ISolution solution, IUnitTestingSettingsAccessor settingsAccessor,
			IShellLocks shellLocks, IPsiConfiguration psiConfiguration, ISettingsStore settingsStore,
			KarmaTestProvider testProvider, JasmineElementFactory jasmineFactory,
			IPersistentIndexManager persistentIndexManager, IJavaScriptDependencyManager javaScriptDependencyManager)
		{
			_lifetime = lifetime;
			_solution = solution;
			_settingsAccessor = settingsAccessor;
			_shellLocks = shellLocks;
			_psiConfiguration = psiConfiguration;
			_testProvider = testProvider;
			myJasmineFactory = jasmineFactory;
			_persistentIndexManager = persistentIndexManager;
			_javaScriptDependencyManager = javaScriptDependencyManager;
			_settingsStore = settingsStore.BindToContextLive(lifetime, ContextRange.ManuallyRestrictWritesToOneContext(solution.ToDataContext()), BindToContextFlags.Normal);
			_settingsStore.Changed.Advise(lifetime, OnSettingsChange);
			Active = new Property<bool>(lifetime, "KarmaTestCache", true);
		}

		private static bool CanHandle(IPsiSourceFile sourceFile)
		{
			if (!sourceFile.PrimaryPsiLanguage.Is<JavaScriptLanguage>())
			{
				return false;
			}
			if (!(sourceFile.GetPsiModule() is IProjectPsiModule))
			{
				return false;
			}
			var properties = sourceFile.Properties;
			return (properties.ProvidesCodeModel && properties.ShouldBuildPsi);
		}

		private void ExploreJasmine(IEnumerable<IPsiSourceFile> referencedFiles, IFile psiFile,
			ICollection<IUnitTestElement> explored)
		{
			psiFile.ProcessDescendants(new JasmineFileExplorer(myJasmineFactory,
				d => explored.Add(d.UnitTestElement), psiFile,
				InterruptableReadActivity.Empty, referencedFiles));
		}

		public void Initialize()
		{
		}

		object ICache.Build(IPsiSourceFile sourceFile, bool isStartup)
		{
			if (!CanHandle(sourceFile))
			{
				return null;
			}
			var dominantPsiFile = sourceFile.GetTheOnlyPsiFile(JavaScriptLanguage.Instance);
			if (dominantPsiFile == null)
			{
				return null;
			}
			var explored = new List<IUnitTestElement>();
			var transitiveDependencies = _javaScriptDependencyManager.GetTransitiveDependencies(sourceFile);
			ExploreJasmine(transitiveDependencies, dominantPsiFile, explored);
			return explored;
		}

		public void Drop(IPsiSourceFile sourceFile)
		{
			lock (_lock)
			{
				_elementsInFiles.RemoveKey(sourceFile);
				if (_persistentCache != null)
				{
					_persistentCache.MarkDataToDelete(sourceFile);
				}
			}
		}

		object ICache.Load(IProgressIndicator progress, bool enablePersistence)
		{
			if (enablePersistence)
			{
				_persistentCache = new SimplePersistentCache<string>(_shellLocks, 1, "KarmaTest", _psiConfiguration);
				if (_persistentCache.Load(progress, _persistentIndexManager,
					(file, reader) => reader.ReadString(),
					(file, serialized) =>
					{
						lock (_lock)
						{
							var info = new PersistentUnitTestSessionInfo(_solution, serialized);
							foreach (var element in info.Elements)
							{
								_elementsInFiles.AddValue(file, element);
							}
							_processedFiles.Add(file);
							_persistentCache.AddDataToSave(file, serialized);
						}
					}) != LoadResult.OK)
				{
					lock (_lock)
					{
						_elementsInFiles.Clear();
						_processedFiles.Clear();
					}
				}
			}
			return null;
		}

		void ICache.Merge(IPsiSourceFile sourceFile, object builtPart)
		{
			var values = builtPart as List<IUnitTestElement>;
			if (values != null)
			{
				lock (_lock)
				{
					_elementsInFiles.RemoveKey(sourceFile);
					_elementsInFiles.AddValueRange(sourceFile, values);
				}

				var xml = "<Serialized></Serialized>";
				if (_elementsInFiles[sourceFile].Any())
				{
					var document = new XmlDocument();
					document.LoadXml(xml);
					var info = new PersistentUnitTestSessionInfo(sourceFile.GetSolution(), null)
					{
						Elements = _elementsInFiles[sourceFile]
					};
					info.WriteToXml(document.DocumentElement);
					xml = document.OuterXml;
				}

				if (_persistentCache != null)
				{
					_persistentCache.AddDataToSave(sourceFile, xml);
				}
			}
		}

		void ICache.MergeLoaded(object data)
		{
		}

#if RESHARPER_8
		void ICache.OnDocumentChange(IPsiSourceFile sourceFile, ProjectFileDocumentCopyChange change)
		{
			MarkAsDirty(sourceFile);
		}
#endif
#if RESHARPER_7
		void ICache.OnDocumentChange(ProjectFileDocumentCopyChange change)
		{
			// TODO
			// MarkAsDirty(change.);
		}
#endif

		void ICache.OnPsiChange(ITreeNode elementContainingChanges, PsiChangedElementType type)
		{
			if (elementContainingChanges == null) return;

			var sourceFile = elementContainingChanges.GetSourceFile();
			if ((sourceFile != null) && sourceFile.PrimaryPsiLanguage.Is<JavaScriptLanguage>())
			{
				lock (_lock)
				{
					_dirtyFiles.Add(sourceFile);
				}
			}
		}

		void ICache.Save(IProgressIndicator progress, bool enablePersistence)
		{
			if (enablePersistence)
			{
				lock (_lock)
				{
					_persistentCache.Save(progress, _persistentIndexManager, (writer, file, data) => writer.Write(data));
				}
				_persistentCache.Dispose();
				_persistentCache = null;
			}
		}

		void ICache.SyncUpdate(bool underTransaction)
		{
		}

		bool ICache.UpToDate(IPsiSourceFile sourceFile)
		{
			if (!CanHandle(sourceFile))
			{
				return true;
			}
			lock (_lock)
			{
				return (!_dirtyFiles.Contains(sourceFile) && _processedFiles.Contains(sourceFile));
			}
		}

		public void MarkAsDirty(IPsiSourceFile sourceFile)
		{
			if (sourceFile.PrimaryPsiLanguage.Is<JavaScriptLanguage>())
			{
				lock (_lock)
				{
					_dirtyFiles.Add(sourceFile);
				}
			}
		}

		private void OnSettingsChange(SettingsStoreChangeArgs settingsStoreChangeArgs)
		{
		}

		public void ReanalyzeDirtyFiles()
		{
			lock (_lock)
			{
				foreach (var file in _dirtyFiles.Where(x => x.IsValid()))
				{
					using (_shellLocks.UsingReadLock())
					{
						ICache cache = this;
						cache.Merge(file, cache.Build(file, false));
					}
				}
				_dirtyFiles.Clear();
			}
		}

		public void Release()
		{
			lock (_lock)
			{
				_elementsInFiles.Clear();
				_processedFiles.Clear();
				_dirtyFiles.Clear();
			}
		}

		public IProperty<bool> Active { get; private set; }

		bool ICache.HasDirtyFiles
		{
			get { return false; }
		}

		public IEnumerable<IUnitTestElement> UnitTestElements
		{
			get
			{
				lock (_lock)
				{
					return _elementsInFiles.AllValues.ToList();
				}
			}
		}
	}
}

