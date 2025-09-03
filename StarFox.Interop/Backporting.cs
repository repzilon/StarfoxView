using System;
using System.Collections.Generic;
using System.Linq;

namespace StarFox.Interop
{
	public static class Backporting
	{
#if NETFRAMEWORK || NETSTANDARD
		internal static bool Contains(this string text, char singleCharacter)
		{
			return text.Contains(singleCharacter.ToString());
		}

		internal static bool StartsWith(this string text, char singleCharacter)
		{
			return text.StartsWith(singleCharacter.ToString());
		}

		public static bool TryAdd<K, V>(this IDictionary<K, V> self, K key, V value)
		{
			if ((self == null) || self.ContainsKey(key)) {
				return false;
			} else {
				try {
					self.Add(key, value);
					return true;
				} catch (Exception) {
					return false;
				}
			}
		}
#endif

#if NETFRAMEWORK || NETSTANDARD || NET5_0 || NETCOREAPP3_0 || NETCOREAPP3_1 || NETCOREAPP2_1
		internal static IEnumerable<T> DistinctBy<T, K>(this IEnumerable<T> self, Func<T, K> criterion)
		{
			if (criterion == null) {
				throw new ArgumentNullException(nameof(criterion));
			}

			var dicUnique = new Dictionary<K, T>();
			foreach (var x in self) {
				dicUnique.TryAdd(criterion(x), x);
			}
			return dicUnique.Values;
		}
#endif

		internal static T[] Slice<T>(this T[] array, int first, int last)
		{
			var x = (first > 0) ? array.Skip(checked(first - 1)) : array;
			return x.Take(checked(last - first + 1)).ToArray();
		}

#if NETSTANDARD2_0 || NETCOREAPP2_1
		internal static Color FromHtml(string htmlCode)
		{
			if (String.IsNullOrEmpty(htmlCode)) {
				throw new ArgumentNullException(nameof(htmlCode));
			}
			if ((htmlCode[0] != '#') || (htmlCode.Length != 4) || (htmlCode.Length != 7)) {
				throw new ArgumentException("Code does not start with '#' or is not 4 or 7 chars long.", nameof(htmlCode));
			}

			var triplet = Int32.Parse("0x" + htmlCode.Substring(1), NumberStyles.HexNumber,
				CultureInfo.InvariantCulture);
			return htmlCode.Length == 4
				? Color.FromArgb(((triplet & 0xf00) >> 8) * 17, ((triplet & 0xf0) >> 4) * 17, (triplet & 0x00f) * 17)
				: Color.FromArgb((triplet & 0xff0000) >> 16, (triplet & 0x00ff00) >> 8, triplet & 0xff);
		}
#endif
	}
}
