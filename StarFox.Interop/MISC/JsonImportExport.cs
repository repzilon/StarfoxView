using System.IO;
using System.Threading.Tasks;
#if NET46
using Newtonsoft.Json;
#else
using System.Text.Json;
#endif

namespace StarFox.Interop.MISC
{
	public static class JsonImportExport
	{
		public static string ToString<T>(T toSerialize)
		{
#if NET46
			string json;
			using (var wrtString = new StringWriter()) {
				using (var wrtJson = new JsonTextWriter(wrtString)) {
					JsonSerializer.Create().Serialize(wrtJson, toSerialize);
					wrtJson.Flush();
					json = wrtString.ToString();
				}
			}
			return json;
#else
			return JsonSerializer.Serialize(toSerialize);
#endif
		}

		public static T LoadTo<T>(string fromFilePath)
		{
#if NET46
			using (var rdrStream = new StreamReader(fromFilePath)) {
				using (var rdrJson = new JsonTextReader(rdrStream)) {
					return JsonSerializer.Create().Deserialize<T>(rdrJson);
				}
			}
#else
			var text = File.ReadAllText(fromFilePath);
			return JsonSerializer.Deserialize<T>(text);
#endif
		}

		public static async Task<T> LoadTo<T>(Stream fromJson)
		{
#if NET46
			using (TextReader rdrStream = new StreamReader(fromJson)) {
				using (JsonReader rdrJson = new JsonTextReader(rdrStream)) {
					return JsonSerializer.Create().Deserialize<T>(rdrJson);
				}
			}
#else
			return await JsonSerializer.DeserializeAsync<T>(fromJson);
#endif
		}

		public static void Serialize<T>(T what, Stream destination)
		{
#if NET46
			using (var wrtStream = new StreamWriter(destination)) {
				using (var wrtJson = new JsonTextWriter(wrtStream)) {
					JsonSerializer.Create(new JsonSerializerSettings() { Formatting = Formatting.Indented }).Serialize(wrtJson, what);
				}
			}
#else
			using (var writer = new Utf8JsonWriter(destination)) {
				using (var doc = JsonSerializer.SerializeToDocument(what, new JsonSerializerOptions()
				{
					WriteIndented = true,
				})) {
					doc.WriteTo(writer);
				}
			}
#endif
		}
	}
}
