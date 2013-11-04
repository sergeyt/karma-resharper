module.exports = function(config) {

	// apply base config
	require('../karma.conf.e2e.js')(config);

	// this line will be replaced by karma-resharper plugin
	var testFiles = [];
	var files = config.files;

	if (testFiles.length > 0) {
		// remove test patterns
		files = files.filter(function(p) {
			return p.indexOf('test') < 0;
		});
	}

	if (config.testLibs && config.testLibs.length > 0) {
		files = files.concat(config.testLibs);
	}

	files = files.concat(testFiles);

	config.set({
		basePath: '..',
		files: files,
		reporters: ['dots', 'junit'],
		junitReporter: {
			outputFile: '.resharper/e2e-test-results.xml'
		}
	});
};
