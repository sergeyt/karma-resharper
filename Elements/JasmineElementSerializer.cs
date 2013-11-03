using System;
using System.Xml;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.UnitTestFramework;
using Karma.TestProvider;

namespace Karma.Elements
{
	[SolutionComponent]
	public class JasmineElementSerializer : IUnitTestElementSerializer
	{
		private readonly JasmineElementFactory _factory;
		private readonly KarmaServiceProvider _serviceProvider;
		private readonly ISolution _solution;
		private const string TypeAttribute = "type";

		public JasmineElementSerializer(KarmaServiceProvider serviceProvider, ISolution solution, JasmineElementFactory factory)
		{
			_serviceProvider = serviceProvider;
			_solution = solution;
			_factory = factory;
		}

		public IUnitTestElement DeserializeElement(XmlElement parent, IUnitTestElement parentElement)
		{
			if (!parent.HasAttribute(TypeAttribute))
			{
				throw new ArgumentException("Element is not Jasmine");
			}
			switch (parent.GetAttribute(TypeAttribute))
			{
				case "JasmineSuiteElement":
					return JasmineSuiteElement.ReadFromXml(parent, parentElement, _factory, _solution);

				case "JasmineSpecificationElement":
					return JasmineSpecificationElement.ReadFromXml(parent, parentElement, _factory, _solution);
			}
			throw new ArgumentException("Element is not Jasmine");
		}

		public void SerializeElement(XmlElement parent, IUnitTestElement element)
		{
			parent.SetAttribute(TypeAttribute, element.GetType().Name);

			var specificationElement = element as JasmineSpecificationElement;
			if (specificationElement != null)
			{
				specificationElement.WriteToXml(parent);
				return;
			}

			if (!(element is JasmineSuiteElement))
			{
				throw new ArgumentException(string.Format("Element {0} is not Jasmine", element.GetType()));
			}

			((JasmineSuiteElement)element).WriteToXml(parent);
		}

		public IUnitTestProvider Provider
		{
			get { return _serviceProvider.TestProvider; }
		}
	}
}

