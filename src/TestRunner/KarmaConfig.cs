using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Karma.TestRunner
{
	/// <summary>
	/// Builds karma configuration file.
	/// </summary>
	internal static class KarmaConfig
	{
		public static string Build(string projectFolder, IEnumerable<string> testFiles)
		{
			const string confName = "karma.conf.js";
			var assembly = typeof(KarmaConfig).Assembly;
			var resName = assembly.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith(confName));
			var resStream = assembly.GetManifestResourceStream(resName);
			if (resStream == null)
				throw new InvalidOperationException("Unable to find '" + confName + "' embedded resource");
			
			var config = resStream.ReadAllText();

			var filesString = string.Join(",\n", (from f in testFiles select JsPath(projectFolder, f)).ToArray());
			config = config.Replace("var testFiles = [];", string.Format("var testFiles = [{0}]", filesString));

			var confFile = Path.Combine(".resharper", confName);
			var path = Path.Combine(projectFolder, confFile);
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllText(path, config);

			return confFile;
		}

		private static string JsPath(string root, string path)
		{
			if (path.StartsWith(root))
			{
				path = path.Substring(root.Length);
			}
			if (path.StartsWith("\\") || path.StartsWith("/"))
			{
				path = path.Substring(1);
			}
			return "'" + string.Join("", (from c in path select Escape(c)).ToArray()) + "'";
		}

		private static string Escape(char c)
		{
			switch (c)
			{
				case '\'':
					return "\\'";
				case '\\':
					return "\\\\";
				default:
					return c.ToString(CultureInfo.InvariantCulture);
			}
		}

		private static string ReadAllText(this Stream stream)
		{
			using (var reader = new StreamReader(stream))
			{
				return reader.ReadToEnd();
			}
		}
	}
}
