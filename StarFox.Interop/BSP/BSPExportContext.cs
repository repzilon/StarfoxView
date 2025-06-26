using StarFox.Interop.BSP.SHAPE;
using StarFox.Interop.GFX;
using StarFox.Interop.GFX.COLTAB;

namespace StarFox.Interop.BSP
{
	/// <summary>
	/// The context used to complete an export operation, encompassing palettes, options, etc.
	/// </summary>
	public class BSPExportContext
	{
		private BSPExportOptions m_objOptions;

		/// <summary>
		/// The options for this export to change certain behaviors to mitigate issues involving certain <see cref="BSPShape"/>s
		/// </summary>
		public BSPExportOptions Options
		{
			get { return m_objOptions ?? BSPExportOptions.Default; }
			set { m_objOptions = value; }
		}

		public string FileName { get; set; }

		public BSPShape Shape { get; set; }

		public COLGroup Group { get; set; }

		public SFPalette Palette { get; set; }

		public int Frame { get; set; }

		public COLTABFile ColorTable { get; set; }

		public CAD.COL Palt { get; set; }

		/// <summary>
		/// Creates a new instance of the <see cref="BSPExportContext"/> with the given formal parameters
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="shape"></param>
		/// <param name="group"></param>
		/// <param name="palette"></param>
		/// <param name="frame"></param>
		/// <param name="colorTable"></param>
		/// <param name="palt"></param>
		/// <param name="options">Optional, will evaluate to <see cref="BSPExportOptions.Default"/> if left <see langword="default"/></param>
		public BSPExportContext(string fileName, BSPShape shape, COLGroup group, SFPalette palette, int frame,
		COLTABFile colorTable, CAD.COL palt, BSPExportOptions options = null)
		{
			FileName   = fileName;
			Shape      = shape;
			Group      = group;
			Palette    = palette;
			Frame      = frame;
			ColorTable = colorTable;
			Palt       = palt;
			Options    = options;
		}
	}
}
