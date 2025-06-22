using System;

namespace WpfPanAndZoom
{
	public class ValueEventArgs<T> : EventArgs
	{
		public T Value { get; set; }

		public ValueEventArgs() { }

		public ValueEventArgs(T newValue)
		{
			this.Value = newValue;
		}
	}
}
