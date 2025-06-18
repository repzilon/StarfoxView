using System;
using System.Collections.Generic;
using StarFox.Interop.GFX;
using StarFox.Interop.GFX.DAT;
using StarFox.Interop.GFX.DAT.MSPRITES;

namespace StarFoxMapVisualizer
{
	public struct PaletteTuple
	{
		public string Name;
		public CAD.COL Palette;

		public PaletteTuple(string name, CAD.COL palette)
		{
			this.Name    = name;
			this.Palette = palette;
		}
	}

	internal struct SpriteTuple<T>
	{
		public T Image;
		public MSprite Sprite;

		public SpriteTuple(T image, MSprite sprite)
		{
			this.Image  = image;
			this.Sprite = sprite;
		}
	}

	public sealed class ScrollEventArgs : EventArgs
	{
		public readonly bool Horizontal;
		public readonly double ScrollValue;

		public ScrollEventArgs(bool horizontal, double scrollValue)
		{
			this.Horizontal  = horizontal;
			this.ScrollValue = scrollValue;
		}
	}

	internal struct SpriteRenderTuple
	{
		public MSpritesDefinitionFile SpriteDef;
		public CAD.COL Palette;
		public IEnumerable<FXCGXFile> CGXs;

		public SpriteRenderTuple(MSpritesDefinitionFile definition, CAD.COL palette,
		IEnumerable<FXCGXFile> cgxCollection)
		{
			this.SpriteDef = definition;
			this.Palette   = palette;
			this.CGXs      = cgxCollection;
		}
	}
}
