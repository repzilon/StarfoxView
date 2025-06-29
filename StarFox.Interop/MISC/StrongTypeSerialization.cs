using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if NET46
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

		private static async void SerializeToStream(Stream Stream, object obj)
		{
#if NET46
			using (var wrtStream = new StreamWriter(Stream)) {
				using (var wrtJson = new JsonTextWriter(wrtStream)) {
					JsonSerializer.Create().Serialize(wrtJson, obj);
				}
			}
#else
            await JsonSerializer.SerializeAsync(Stream, obj);
#endif
		}

		public static async void SerializeObjects(Stream Stream, IEnumerable<object> Objects)
		{
			var sObjs = Objects.Select(x => new StrongTypeSerializationObject(
				x.GetType().AssemblyQualifiedName, JsonImportExport.ToString(x)));
			SerializeToStream(Stream, sObjs);
		}

		public static async Task<IEnumerable<object>> DeserializeObjects(Stream Stream)
		{
			var sObjs = await JsonImportExport.LoadTo<IEnumerable<StrongTypeSerializationObject>>(Stream);
			List<object> resultList = new List<object>();
			foreach (var sobj in sObjs) {
				var result = Deserialize(sobj);
				if (result == null) continue;
				resultList.Add(result);
			}
			return resultList;
		}

		private static object Deserialize(StrongTypeSerializationObject stso)
		{
#if NET46
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
