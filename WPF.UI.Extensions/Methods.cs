#if NET40
using System.Reflection;
#endif

namespace WPF.UI.Extensions
{
	internal static class Methods
	{
#if NET40
		public static object GetValue(this PropertyInfo self, object instance)
		{
			return self.GetValue(instance, null);
		}

		public static void SetValue(this PropertyInfo self, object instance, object newValue)
		{
			self.SetValue(instance, newValue, null);
		}
#endif
	}
}
