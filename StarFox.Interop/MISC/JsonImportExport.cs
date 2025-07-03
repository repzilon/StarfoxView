using System.IO;
using System.Threading.Tasks;
using fastJSON;

namespace StarFox.Interop.MISC
{
	/// <summary>
	/// Methods to serialize and deserialize JSON, hopefully independent of the JSON library.
	/// </summary>
	/// <remarks>
	/// By default, System.Text.Json serializes public get-set properties, and none of the fields, public or not.
	/// </remarks>
	public static class JsonImportExport
	{
		public static string ToString<T>(T toSerialize)
		{
			return JSON.ToJSON(toSerialize);
		}

		public static T LoadTo<T>(string fromFilePath)
		{
			return JSON.ToObject<T>(File.ReadAllText(fromFilePath));
		}

		public static async Task<T> LoadTo<T>(Stream fromJson)
		{
			using (TextReader rdrStream = new StreamReader(fromJson)) {
				return JSON.ToObject<T>(await rdrStream.ReadToEndAsync());
			}
		}

		public static void Serialize<T>(T what, Stream destination)
		{
			using (var wrtStream = new StreamWriter(destination)) {
				wrtStream.WriteLine(ToString(what));
			}
		}
	}
}
