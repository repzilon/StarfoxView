using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if NET46 || NET40
using Newtonsoft.Json;
#else
using System.Text.Json;
#endif
using System.Threading.Tasks;

namespace StarFox.Interop.MISC
{
	public static class StrongTypeSerialization
	{
		public class StrongTypeSerializationObject
		{
			public StrongTypeSerializationObject(string typeName, string serializedData)
			{
				TypeName = typeName;
				SerializedData = serializedData;
			}

			public string TypeName { get; }
			public string SerializedData { get; }
		}

		private static string SerializeToString(object instance)
		{
#if NET46 || NET40
			string text;
			using (var wrtString = new StringWriter()) {
				JsonSerializer.Create().Serialize(wrtString, instance, instance.GetType());
				wrtString.Flush();
				text = wrtString.ToString();
			}
			return text;
#else
            return JsonSerializer.Serialize(instance, instance.GetType());
#endif
		}

		private static async void SerializeToStream(Stream Stream, object obj)
		{
#if NET46 || NET40
			using (var wrtStream = new StreamWriter(Stream)) {
				using (var wrtJson = new JsonTextWriter(wrtStream)) {
					JsonSerializer.Create().Serialize(wrtJson, obj);
				}
			}
#else
            await JsonSerializer.SerializeAsync(Stream, obj);
#endif
		}

		public static async Task SerializeObject(Stream Stream, object Object)
		{
			string text = SerializeToString(Object);
			var obj = new StrongTypeSerializationObject(Object.GetType().AssemblyQualifiedName, text);
			SerializeToStream(Stream, obj);
		}

		public static void SerializeObjects(Stream Stream, params object[] Objects)
		{
			SerializeObjects(Stream, Objects);
		}

		public static async void SerializeObjects(Stream Stream, IEnumerable<object> Objects)
		{
			var sObjs = Objects.Select(x => new StrongTypeSerializationObject(
				x.GetType().AssemblyQualifiedName, SerializeToString(x)));
			SerializeToStream(Stream, sObjs);
		}

		public static async Task<object> DeserializeObject(Stream Stream)
		{
			var sobj = await Deserialize<StrongTypeSerializationObject>(Stream);
			return Deserialize(sobj);
		}

		public static async Task<IEnumerable<object>> DeserializeObjects(Stream Stream)
		{
			var sObjs = await Deserialize<IEnumerable<StrongTypeSerializationObject>>(Stream);
			List<object> resultList = new List<object>();
			foreach (var sobj in sObjs) {
				var result = Deserialize(sobj);
				if (result == null) continue;
				resultList.Add(result);
			}
			return resultList;
		}

		private static async Task<T> Deserialize<T>(Stream stream)
		{
#if NET46 || NET40
			using (var rdrStream = new StreamReader(stream)) {
				using (var rdrJson = new JsonTextReader(rdrStream)) {
					return JsonSerializer.Create().Deserialize<T>(rdrJson);
				}
			}
#else
			return await JsonSerializer.DeserializeAsync<T>(stream);
#endif
		}

		private static object Deserialize(StrongTypeSerializationObject stso)
		{
#if NET46 || NET40
			using (var rdrString = new StringReader(stso.SerializedData)) {
				using (var rdrJson = new JsonTextReader(rdrString)) {
					return JsonSerializer.Create().Deserialize(rdrJson, Type.GetType(stso.TypeName));
				}
			}
#else
			return JsonSerializer.Deserialize(stso.SerializedData, Type.GetType(stso.TypeName));
#endif
		}
	}
}
