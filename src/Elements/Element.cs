using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.ReSharper.UnitTestFramework.Strategy;
using JetBrains.Util;
using JetBrains.Util.Special;
using Karma.TestProvider;

namespace Karma.Elements
{
	public abstract class Element : IUnitTestElement
    {
        private readonly List<IUnitTestElement> _children = new List<IUnitTestElement>();
        private IUnitTestElement _parent;
		protected readonly KarmaServiceProvider ServiceProvider;
		protected readonly ProjectModelElementEnvoy ProjectFileEnvoy;

		protected Element(KarmaServiceProvider serviceProvider, string name, ProjectModelElementEnvoy projectFileEnvoy, TextRange textRange)
        {
			ServiceProvider = serviceProvider;
			ShortName = name;
			ProjectFileEnvoy = projectFileEnvoy;
            TextRange = textRange;
            State = UnitTestElementState.Valid;
        }

		public bool IsE2E
		{
			get
			{
				var folder = ProjectFolder;
				var file = GetProjectFile().IfNotNull(f => f.Location.FullPath) ?? "";
				if (file.StartsWith(folder))
				{
					file = file.Substring(folder.Length);
				}
				if (file.StartsWith("\\") || file.StartsWith("/"))
				{
					file = file.Substring(1);
				}
				var dir = file.Split(new[] {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar}).FirstOrDefault() ?? "";
				return dir.StartsWith("e2e", StringComparison.OrdinalIgnoreCase) ||
				       dir.EndsWith("e2e", StringComparison.OrdinalIgnoreCase);
			}
		}

        public IDeclaredElement GetDeclaredElement()
        {
            return null;
        }

        public UnitTestElementDisposition GetDisposition()
        {
            return new UnitTestElementDisposition(new[] { new UnitTestElementLocation(GetProjectFile(), TextRange, TextRange) }, this);
        }

        public UnitTestNamespace GetNamespace()
        {
            return new UnitTestNamespace("");
        }

        public virtual string GetPresentation(IUnitTestElement element)
        {
            return ShortName;
        }

        public IProject GetProject()
        {
            return GetProjectFile().IfNotNull(x => x.GetProject());
        }

        public IProjectFile GetProjectFile()
        {
            return ProjectFileEnvoy.GetValidProjectElement() as IProjectFile;
        }

		public string ProjectFolder
		{
			get { return GetProject().IfNotNull(p => p.ProjectFile).IfNotNull(x => x.Location.Directory.FullPath) ?? ""; }
		}

        public IEnumerable<IProjectFile> GetProjectFiles()
        {
            return new[] { GetProjectFile() }.Where(x => x != null).ToArray();
        }

		public IUnitTestRunStrategy GetRunStrategy(IHostProvider hostProvider)
		{
			return ServiceProvider.GetRunStrategy();
		}

        public abstract IList<UnitTestTask> GetTaskSequence(ICollection<IUnitTestElement> explicitElements, IUnitTestLaunch launch);

        public IEnumerable<UnitTestElementCategory> Categories
        {
            get { return UnitTestElementCategory.Uncategorized; }
        }

        public ICollection<IUnitTestElement> Children
        {
            get { return _children; }
        }

        public bool Explicit
        {
            get { return false; }
        }

        public string ExplicitReason
        {
            get { return string.Empty; }
        }

		public string Id
		{
			get
			{
				// using karma-junit-reporter convention, i.e. space as separator
				string id = ShortName ?? "";
				if (Parent != null)
				{
					var parentId = Parent.Id;
					if (!string.IsNullOrEmpty(parentId))
					{
						id = parentId + " " + id;
					} 
				}
				return id.Replace('.', '_');
			}
		}

        public abstract string Kind { get; }

        public IUnitTestElement Parent
        {
            get
            {
                return _parent;
            }
            set
            {
	            if (ReferenceEquals(_parent, value)) return;

	            if (_parent != null)
	            {
		            _parent.Children.Remove(this);
	            }

	            _parent = value;

	            if (_parent != null)
	            {
		            _parent.Children.Add(this);
	            }
            }
        }

		public IUnitTestProvider Provider
		{
			get { return ServiceProvider.TestProvider; }
		}

		public string ShortName { get; private set; }
		public UnitTestElementState State { get; set; }
		public TextRange TextRange { get; set; }

		public virtual bool Equals(IUnitTestElement other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}
			if (ReferenceEquals(this, other))
			{
				return true;
			}
			if (other.GetType() != GetType())
			{
				return false;
			}

			return Equals(ProjectFileEnvoy, ((Element)other).ProjectFileEnvoy) &&
			       string.Equals(ShortName, other.ShortName) &&
			       Equals(Parent, other.Parent);
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as IUnitTestElement);
		}

		public override int GetHashCode()
		{
			return (ProjectFileEnvoy.IfNotNull(x => x.GetHashCode()) * 0x18d)
				   ^ (ShortName.IfNotNull(x => x.GetHashCode()) * 0x18d)
				   ^ (Parent.IfNotNull(x => x.GetHashCode()) * 0x18d);
		}

		public override string ToString()
		{
			return string.Format("{0}:{1}", Kind, ShortName);
		}
    }
}

