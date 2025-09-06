using System;
using System.Reflection;
using Avalonia.Markup.Xaml;

namespace TextEditLib.Themes
{
	public sealed class ComponentResourceKey : MarkupExtension
	{
		public Type TypeInTargetAssembly { get; set; }

		public string ResourceId { get; set; }

		public Assembly Assembly
		{
			get { return this.TypeInTargetAssembly.Assembly; }
		}

		public ComponentResourceKey() { }

		public ComponentResourceKey(Type typeInTargetAssembly, string resourceId)
		{
			this.TypeInTargetAssembly = typeInTargetAssembly;
			this.ResourceId           = resourceId;
		}

		internal ComponentResourceKey(string resourceId) : this(typeof(ResourceKeys), resourceId) { }

		public static ComponentResourceKey New<T>(string resourceId)
		{
			return new ComponentResourceKey(typeof(T), resourceId);
		}

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}

	public static class ResourceKeys
	{
		#region Accent Keys
		/// <summary>
		/// Accent Color Key - This Color key is used to accent elements in the UI
		/// (e.g.: Color of Activated Normal Window Frame, ResizeGrip, Focus or MouseOver input elements)
		/// </summary>
		public static readonly ComponentResourceKey ControlAccentColorKey =
			new ComponentResourceKey("ControlAccentColorKey");

		/// <summary>
		/// Accent Brush Key - This Brush key is used to accent elements in the UI
		/// (e.g.: Color of Activated Normal Window Frame, ResizeGrip, Focus or MouseOver input elements)
		/// </summary>
		public static readonly ComponentResourceKey ControlAccentBrushKey =
			new ComponentResourceKey("ControlAccentBrushKey");
		#endregion Accent Keys

		#region TextEditor BrushKeys
		public static readonly ComponentResourceKey EditorBackground = new ComponentResourceKey("EditorBackground");

		public static readonly ComponentResourceKey EditorForeground = new ComponentResourceKey("EditorForeground");

		public static readonly ComponentResourceKey EditorLineNumbersForeground =
			new ComponentResourceKey("EditorLineNumbersForeground");

		public static readonly ComponentResourceKey EditorSelectionBrush =
			new ComponentResourceKey("EditorSelectionBrush");

		public static readonly ComponentResourceKey EditorSelectionBorder =
			new ComponentResourceKey("EditorSelectionBorder");

		public static readonly ComponentResourceKey EditorNonPrintableCharacterBrush =
			new ComponentResourceKey("EditorNonPrintableCharacterBrush");

		public static readonly ComponentResourceKey EditorLinkTextForegroundBrush =
			new ComponentResourceKey("EditorLinkTextForegroundBrush");

		public static readonly ComponentResourceKey EditorLinkTextBackgroundBrush =
			new ComponentResourceKey("EditorLinkTextBackgroundBrush");

		#region DiffView Currentline Keys
		/// <summary>
		/// Gets the background color for highlighting for the currently highlighted line.
		/// </summary>
		public static readonly ComponentResourceKey EditorCurrentLineBackgroundBrushKey =
			new ComponentResourceKey("EditorCurrentLineBackgroundBrushKey");

		/// <summary>
		/// Gets the border color for highlighting for the currently highlighted line.
		/// </summary>
		public static readonly ComponentResourceKey EditorCurrentLineBorderBrushKey =
			new ComponentResourceKey("EditorCurrentLineBorderBrushKey");

		/// <summary>
		/// Gets the border thickness for highlighting for the currently highlighted line.
		/// </summary>
		public static readonly ComponentResourceKey EditorCurrentLineBorderThicknessKey =
			new ComponentResourceKey("EditorCurrentLineBorderThicknessKey");
		#endregion DiffView Currentline Keys
		#endregion TextEditor BrushKeys
	}
}
