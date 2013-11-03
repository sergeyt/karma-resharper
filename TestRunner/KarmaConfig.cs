using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Karma.TestRunner
{
	internal static class KarmaConfig
	{
		public static void Build(string projectFolder, IEnumerable<string> testFiles)
		{
			// TODO make external
			var config = @"
// Karma configuration for resharper test runner
module.exports = function(config) {

	// apply base config
	require('./karma.conf.js')(config);

	var testFiles = [%testFiles%];
	var files = config.files;

	if (testFiles.length > 0) {
		// remove test patterns
		files = files.filter(function(p){
			return p.indexOf('test') < 0;
		});
	}

	if (config.testLibs && config.testLibs.length > 0) {
		files = files.concat(config.testLibs);
	}

	files = files.concat(testFiles);

	console.log(files);

	config.set({
		files: files
	});
};
";
			config = config.Replace("%testFiles%",
				string.Join(",\n",
					(from f in testFiles select JsPath(projectFolder, f)).ToArray())
				);

			var path = Path.Combine(projectFolder, "karma.conf.resharper.js");
			File.WriteAllText(path, config);
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
	}
}
