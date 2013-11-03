using System;
using System.Linq;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.TaskRunnerFramework;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.ReSharper.UnitTestFramework.Strategy;
using Karma.Elements;

namespace Karma.TestRunner
{
	public class KarmaTestRunStrategy : IUnitTestRunStrategy
    {
        public RuntimeEnvironment GetRuntimeEnvironment(IUnitTestElement element, RuntimeEnvironment projectRuntimeEnvironment, IUnitTestLaunch launch)
		{
			return RuntimeEnvironment.Automatic;
		}

		public void Run(Lifetime lifetime, ITaskRunnerHostController runController, IUnitTestRun run, IUnitTestLaunch launch,
			Action continuation)
		{
			foreach (var project in run.Elements.OfType<Element>().GroupBy(x => x.ProjectFolder))
			{
				KarmaTestRunner.Run(launch, project.Key, project);
			}
			launch.Finished();
		}

		public void Cancel(ITaskRunnerHostController runController, IUnitTestRun run)
		{
		}

		public void Abort(ITaskRunnerHostController runController, IUnitTestRun run)
		{
		}

		public bool NeedProjectBuild(IProject project)
        {
            return false;
        }
    }
}

