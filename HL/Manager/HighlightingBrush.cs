namespace HL.Manager
{
	using System;
	using System.Diagnostics;
	using System.Reflection;
	using System.Runtime.Serialization;
#if Avalonia
	using Avalonia.Media;
	using AvaloniaEdit.Highlighting;
    using AvaloniaEdit.Rendering;
#else
	using System.Windows;
	using ICSharpCode.AvalonEdit.Highlighting;
	using ICSharpCode.AvalonEdit.Rendering;
	using System.Windows.Media;
#endif

	/// <summary>
	/// HighlightingBrush implementation that finds a brush using a resource.
	/// </summary>
	[Serializable]
	sealed class SystemColorHighlightingBrush : HighlightingBrush, ISerializable
	{
		readonly PropertyInfo property;

		public SystemColorHighlightingBrush(PropertyInfo property)
		{
			Debug.Assert(property.ReflectedType == typeof(SystemColors));
			Debug.Assert(typeof(Brush).IsAssignableFrom(property.PropertyType));
			this.property = property;
		}

#if Avalonia
		public override IBrush GetBrush(ITextRunConstructionContext context)
#else
		public override Brush GetBrush(ITextRunConstructionContext context)
#endif
		{
			return (Brush)property.GetValue(null, null);
		}

		public override string ToString()
		{
			return property.Name;
		}

		SystemColorHighlightingBrush(SerializationInfo info, StreamingContext context)
		{
			property = typeof(SystemColors).GetProperty(info.GetString("propertyName"));
			if (property == null)
				throw new ArgumentException("Error deserializing SystemColorHighlightingBrush");
		}

		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("propertyName", property.Name);
		}

		public override bool Equals(object obj)
		{
			SystemColorHighlightingBrush other = obj as SystemColorHighlightingBrush;
			return other != null && object.Equals(this.property, other.property);
		}

		public override int GetHashCode()
		{
			return property.GetHashCode();
		}
	}
}
