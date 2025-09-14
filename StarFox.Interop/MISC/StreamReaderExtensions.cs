using System;
using System.IO;
using System.Reflection;

namespace StarFox.Interop.MISC
{
	internal enum TriState : sbyte
	{
		Unknown = -1,
		False = 0,
		True = 1
	}

	public static class StreamReaderExtensions
	{
		private static TriState underScoredPrivateFields = TriState.Unknown;

		// Field name may change between .NET implementations
		private static T InvokeTo<T>(this Type objectType, string name, object instance)
		{
			if (underScoredPrivateFields == TriState.True) {
				return CoreInvokeTo<T>(objectType, "_" + name, instance);
			} else if (underScoredPrivateFields == TriState.False) {
				return CoreInvokeTo<T>(objectType, name, instance);
			} else {
				T value;
				try {
					value = CoreInvokeTo<T>(objectType, name, instance);
					underScoredPrivateFields = TriState.False;
				} catch (MissingFieldException) {
					value = CoreInvokeTo<T>(objectType, "_" + name, instance);
					underScoredPrivateFields = TriState.True;
				}
				return value;
			}
		}

		private static T CoreInvokeTo<T>(this Type objectType, string name, object instance)
		{
			const BindingFlags kPrivateField = BindingFlags.DeclaredOnly | BindingFlags.Public |
			                                   BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField;
			return (T)objectType.InvokeMember(name, kPrivateField, null, instance, null);
		}

		public static long GetActualPosition(this StreamReader reader)
		{
			var typSmr = reader.GetType();

			// The current buffer of decoded characters
			var charBuffer = typSmr.InvokeTo<char[]>("charBuffer", reader);

			// The index of the next char to be read from charBuffer
			var charPos = typSmr.InvokeTo<int>("charPos", reader);

			// The number of decoded chars presently used in charBuffer
			var charLen = typSmr.InvokeTo<int>("charLen", reader);

			// The current buffer of read bytes (byteBuffer.Length = 1024; this is critical).
			var byteBuffer = typSmr.InvokeTo<byte[]>("byteBuffer", reader);

			// The number of bytes read while advancing reader.BaseStream.Position to (re)fill charBuffer
			var byteLen = typSmr.InvokeTo<int>("byteLen", reader);

			// The number of bytes the remaining chars use in the original encoding.
			int numBytesLeft = reader.CurrentEncoding.GetByteCount(charBuffer, charPos, charLen - charPos);

			// For variable-byte encodings, deal with partial chars at the end of the buffer
			int numFragments = 0;
			if (byteLen > 0 && !reader.CurrentEncoding.IsSingleByte) {
				if (reader.CurrentEncoding.CodePage == 65001) { // UTF-8
					byte byteCountMask = 0;
					while ((byteBuffer[byteLen - numFragments - 1] >> 6) == 2) // if the byte is "10xx xxxx", it's a continuation-byte
						byteCountMask |= (byte)(1 << ++numFragments); // count bytes & build the "complete char" mask
					if ((byteBuffer[byteLen - numFragments - 1] >> 6) == 3) // if the byte is "11xx xxxx", it starts a multibyte char.
						byteCountMask |= (byte)(1 << ++numFragments); // count bytes & build the "complete char" mask
																	  // see if we found as many bytes as the leading-byte says to expect
					if (numFragments > 1 && ((byteBuffer[byteLen - numFragments] >> 7 - numFragments) == byteCountMask))
						numFragments = 0; // no partial-char in the byte-buffer to account for
				} else if (reader.CurrentEncoding.CodePage == 1200) { // UTF-16LE
					if (byteBuffer[byteLen - 1] >= 0xd8) // high-surrogate
						numFragments = 2; // account for the partial character
				} else if (reader.CurrentEncoding.CodePage == 1201) { // UTF-16BE
					if (byteBuffer[byteLen - 2] >= 0xd8) // high-surrogate
						numFragments = 2; // account for the partial character
				}
			}
			return reader.BaseStream.Position - numBytesLeft - numFragments;
		}
	}
}
