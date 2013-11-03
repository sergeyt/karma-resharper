using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using JetBrains.ProjectModel;
using JetBrains.Util;
using Karma.TestProvider;

namespace Karma.Elements
{
	public abstract class JasmineElement : Element
	{
		private const string FileNameAttribute = "FileName";
		private const string ProjectAttribute = "Project";
		private const string ReferencedFilePathAttribute = "Path";
		private const string ReferencedFileTag = "Reference";
		private const string TextRangeEndAttribute = "RangeEnd";
		private const string TextRangeStartAttribute = "RangeStart";

		private IList<string> _referencedFiles;

		protected JasmineElement(KarmaServiceProvider provider, string name,
			ProjectModelElementEnvoy projectFileEnvoy,
			TextRange textRange, IList<string> referencedFiles)
			: base(provider, name, projectFileEnvoy, textRange)
		{
			_referencedFiles = referencedFiles ?? new List<string>().AsReadOnly();
		}

		public IList<string> ReferencedFiles
		{
			get { return _referencedFiles; }
			set { _referencedFiles = value ?? new List<string>().AsReadOnly(); }
		}

		protected class ElementProperties
		{
			public string Name;
			public string FileName;
			public IProject ProjectFolder;
			public ProjectModelElementEnvoy ProjectFileEnvoy;
			public TextRange TextRange;
			public IList<string> ReferencedFiles;
		}

		protected static ElementProperties ReadFromXml(XmlElement parent, ISolution solution, string nameAttribute)
		{
			int rangeStart;
			int rangeEnd;
			var fileName = parent.GetAttribute(FileNameAttribute);
			var name = parent.GetAttribute(nameAttribute);
			var id = parent.GetAttribute(ProjectAttribute);
			var textRange = (!int.TryParse(parent.GetAttribute(TextRangeStartAttribute), out rangeStart) ||
			                 !int.TryParse(parent.GetAttribute(TextRangeEndAttribute), out rangeEnd))
				? TextRange.InvalidRange
				: new TextRange(rangeStart, rangeEnd);
			var projectFolder = (IProject)ProjectUtil.FindProjectElementByPersistentID(solution, id);

			var referencedFiles = (from element in parent.ChildNodes.OfType<XmlElement>()
				where Equals(element.Name, ReferencedFileTag)
				select element.GetAttribute(ReferencedFilePathAttribute)).ToList();

			var file = projectFolder.GetAllProjectFiles(f => Equals(f.Location.FullPath, fileName)).FirstOrDefault();
			if (file == null)
			{
				return null;
			}

			return new ElementProperties
			{
				Name = name,
				FileName = fileName,
				ProjectFolder = projectFolder,
				ProjectFileEnvoy = ProjectModelElementEnvoy.Create(file),
				TextRange = textRange,
				ReferencedFiles = referencedFiles
			};
		}

		protected void WriteToXml(XmlElement parent, string nameAttribute)
		{
			var doc = parent.OwnerDocument;
			if (doc == null) return;

			var projectFile = GetProjectFile();
			if (projectFile == null) return;

			parent.SetAttribute(FileNameAttribute, projectFile.Location.FullPath);
			parent.SetAttribute(nameAttribute, ShortName);
			parent.SetAttribute(TextRangeStartAttribute, TextRange.StartOffset.ToString(CultureInfo.InvariantCulture));
			parent.SetAttribute(TextRangeEndAttribute, TextRange.EndOffset.ToString(CultureInfo.InvariantCulture));

			var project = GetProject();
			if (project != null)
			{
				parent.SetAttribute(ProjectAttribute, project.GetPersistentID());
			}

			foreach (var path in _referencedFiles)
			{
				var element = doc.CreateElement(ReferencedFileTag);
				element.SetAttribute(ReferencedFilePathAttribute, path);
				parent.AppendChild(element);
			}
		}
	}
}

