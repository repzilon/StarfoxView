using System;
using System.Collections.Generic;
using StarFox.Interop.ASM;

namespace StarFox.Interop.MSG
{
	public class TRNFile : ASMFile
	{
		internal TRNFile(string OriginalFilePath) : base(OriginalFilePath)
		{
		}

		internal TRNFile(ASMFile From) : base(From)
		{
		}

		private byte[] m_bytarMoji = new byte[256 - 32];
		private byte m_bytImported;

		public byte TileNumberFor(byte codePoint)
		{
			if (codePoint < 32) {
				throw new ArgumentOutOfRangeException(nameof(codePoint),
					"The first 32 code points are reserved for control characters.");
			}

			return m_bytarMoji[codePoint - 32];
		}

		public byte CodedCharForTile(byte tileNumber)
		{
			var index = new List<byte>(m_bytarMoji).IndexOf(tileNumber);
			return (index > 0) ? (byte)index : (byte)0;
		}

		internal void Clear()
		{
			m_bytImported = 0;
			m_bytarMoji = new byte[256 - 32];
		}

		internal void Append(params byte[] values)
		{
			var c = values.Length;
			int n = m_bytImported;
			for (var i = 0; i < c; i++) {
				m_bytarMoji[n] = values[i];
				n++;
			}

			m_bytImported = (byte)n;
		}

		internal void Append(byte oneValue)
		{
			m_bytarMoji[m_bytImported] = oneValue;
			m_bytImported++;
		}
	}
}
