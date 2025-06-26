namespace StarFox.Interop.BSP
{
	/// <summary>
	/// Options to use when exporting shapes using the <see cref="BSPExporter"/>
	/// </summary>
	public class BSPExportOptions
	{
		/// <summary>
		/// Process color information on this shape and apply as Vertex Colors?
		/// </summary>
		public bool ColorActivated { get; set; }

		/// <summary>
		/// Process color animations?
		/// </summary>
		public bool ColorAnimationsActivated { get; set; }

		/// <summary>
		/// Process elements of this shape that only are comprised of two 3D positions?
		/// </summary>
		public bool ProcessLines { get; set; }

		public static BSPExportOptions Default = new BSPExportOptions()
		{
			ColorActivated = true,
			ColorAnimationsActivated = true,
			ProcessLines = true
		};
	}
}
