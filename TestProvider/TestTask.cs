using System;
using System.Xml;
using JetBrains.ReSharper.TaskRunnerFramework;
using JetBrains.Util.Special;
using Karma.TestRunner;

namespace Karma.Tasks
{
    [Serializable]
    public class TestTask : RemoteTask, IEquatable<TestTask>
    {
	    private const string NameAttribute = "name";
	    private const string ProjectFolderAttribute = "project-folder";

        public TestTask(XmlElement element) : base(element)
        {
			Name = GetXmlAttribute(element, NameAttribute);
			ProjectFolder = GetXmlAttribute(element, ProjectFolderAttribute);
        }

        public TestTask(string name, string projectFolder, bool isSuite) : base(KarmaTestRunner.ID)
        {
            Name = name;
	        ProjectFolder = projectFolder;
	        IsSuite = isSuite;
        }

		public string Name { get; private set; }
		public string ProjectFolder { get; private set; }
		public bool IsSuite { get; private set; }
		public override bool IsMeaningfulTask { get { return true; } }

        public override bool Equals(RemoteTask other)
        {
            return Equals(other as TestTask);
        }

        public bool Equals(TestTask other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            return (ReferenceEquals(this, other) || string.Equals(other.Name, Name, StringComparison.Ordinal));
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TestTask);
        }

        public override int GetHashCode()
        {
	        return Name.IfNotNull(x => x.GetHashCode());
        }

	    public override void SaveXml(XmlElement element)
        {
            base.SaveXml(element);

			SetXmlAttribute(element, NameAttribute, Name);
			SetXmlAttribute(element, ProjectFolderAttribute, ProjectFolder);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}

