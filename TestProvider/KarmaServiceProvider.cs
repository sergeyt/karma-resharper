using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.UnitTestFramework.Strategy;
using Karma.TestRunner;

namespace Karma.TestProvider
{
	[SolutionComponent]
    public class KarmaServiceProvider
    {
		private readonly IUnitTestRunStrategy _strategy = new KarmaTestRunStrategy();

        public KarmaServiceProvider(Lifetime lifetime, ISolution solution, KarmaTestProvider testProvider)
        {
            TestProvider = testProvider;
        }

        public IUnitTestRunStrategy GetRunStrategy()
        {
            return _strategy;
        }

	    public KarmaTestProvider TestProvider { get; private set; }
    }
}

