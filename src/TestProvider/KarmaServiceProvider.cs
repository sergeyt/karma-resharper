using JetBrains.DataFlow;
using JetBrains.ProjectModel;
#if RESHARPER_8
using JetBrains.ReSharper.UnitTestFramework.Strategy;
using Karma.TestRunner;
#endif

namespace Karma.TestProvider
{
	[SolutionComponent]
    public class KarmaServiceProvider
    {
#if RESHARPER_8
		private readonly IUnitTestRunStrategy _strategy = new KarmaTestRunStrategy();
#endif

        public KarmaServiceProvider(Lifetime lifetime, ISolution solution, KarmaTestProvider testProvider)
        {
            TestProvider = testProvider;
        }

#if RESHARPER_8
        public IUnitTestRunStrategy GetRunStrategy()
        {
            return _strategy;
        }
#endif

	    public KarmaTestProvider TestProvider { get; private set; }
    }
}

