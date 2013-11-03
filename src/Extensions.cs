using System;
using System.Collections.Generic;

namespace Karma
{
	internal static class Extensions
	{
		public static IEnumerable<T> Flatten<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> childSelector)
		{
			foreach (var item in source)
			{
				yield return item;

				foreach (var d in childSelector(item).Flatten(childSelector))
				{
					yield return d;
				}
			}
		}

		public static IEnumerable<T> Flatten<T>(this T source, Func<T, IEnumerable<T>> childSelector)
		{
			yield return source;

			foreach (var d in childSelector(source).Flatten(childSelector))
			{
				yield return d;
			}
		}
	}
}
