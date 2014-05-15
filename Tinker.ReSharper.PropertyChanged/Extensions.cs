using System;
using System.Collections.Generic;

namespace Tinker.ReSharper.PropertyChanged
{
	public static class Extensions
	{
		public static IEnumerable<T> Chain<T>(this T item, Func<T, T> getNext, bool includeSelf = true)
		{
			if (!includeSelf)
			{
				item = getNext(item);
			}

			while (!Equals(item, default(T)))
			{
				yield return item;
				item = getNext(item);
			}
		}
	}
}