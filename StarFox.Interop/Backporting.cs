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

#if NETFRAMEWORK || NETSTANDARD || NET5_0
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
	}

#if NET40
	internal struct Vector3
	{
		public float X;
		public float Y;
		public float Z;

		public Vector3(float x, float y, float z) : this()
		{
			X = x;
			Y = y;
			Z = z;
		}

		public static Vector3 operator +(Vector3 u, Vector3 v)
		{
			return new Vector3(u.X + v.X, u.Y + v.Y, u.Z + v.Z);
		}

		public static Vector3 operator -(Vector3 u, Vector3 v)
		{
			return new Vector3(u.X - v.X, u.Y - v.Y, u.Z - v.Z);
		}
	}
#endif
}
