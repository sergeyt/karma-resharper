using System;
using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.UnitTestFramework;
using Karma.Elements;

namespace Karma.TestProvider
{
	[UnitTestProvider]
    public class KarmaTestProvider : IUnitTestProvider
    {
        internal const string ProviderId = "Karma";
        private readonly UnitTestElementComparer _comparer = new UnitTestElementComparer(new[] { typeof(JasmineSuiteElement), typeof(JasmineSpecificationElement) });

		public int CompareUnitTestElements(IUnitTestElement x, IUnitTestElement y)
        {
            if (!(x is JasmineSuiteElement) || !(y is JasmineSuiteElement))
            {
                return _comparer.Compare(x, y);
            }
            if (x.Id.StartsWith(y.Id))
            {
                return 1;
            }
            if (y.Id.StartsWith(x.Id))
            {
                return -1;
            }
            return string.Compare(x.ShortName, y.ShortName, StringComparison.Ordinal);
        }

        public void ExploreExternal(UnitTestElementConsumer consumer)
        {
        }

        public void ExploreSolution(ISolution solution, UnitTestElementConsumer consumer)
        {
            TestCache component;
            try
            {
                component = solution.GetComponent<TestCache>();
            }
            catch
            {
                return;
            }
            component.ReanalyzeDirtyFiles();
            foreach (var element in from e in component.UnitTestElements
                where e.State.IsValid() && (e.Provider is KarmaTestProvider)
                select e)
            {
                consumer(element);
            }
        }

        public bool IsElementOfKind(IDeclaredElement declaredElement, UnitTestElementKind elementKind)
        {
            return false;
        }

        public bool IsElementOfKind(IUnitTestElement element, UnitTestElementKind elementKind)
        {
            switch (elementKind)
            {
                case UnitTestElementKind.Unknown:
		            return !(element is JasmineSpecificationElement) && !(element is JasmineSuiteElement);

	            case UnitTestElementKind.Test:
                    return element is JasmineSpecificationElement;

                case UnitTestElementKind.TestContainer:
                    return element is JasmineSuiteElement;

                case UnitTestElementKind.TestStuff:
                    return element is JasmineSpecificationElement || element is JasmineSuiteElement;
            }
            throw new ArgumentOutOfRangeException("elementKind");
        }

        public bool IsSupported(IHostProvider hostProvider)
        {
			return hostProvider.ID.Equals(WellKnownHostProvidersIds.DebugProviderId);
        }

        public string ID
        {
			get { return ProviderId; }
        }

        public string Name
        {
			get { return ProviderId; }
        }
    }
}

