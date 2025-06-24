using System.IO;

namespace StarFox.Interop
{
    public interface IImporterObject
    {
        string OriginalFilePath { get; }
    }

    public static class ImporterObjectExtension
    {
        /// <summary>
        /// The file name of this file using OriginalFilePath
        /// </summary>
        public static string FileTitle(this IImporterObject self)
        {
            return Path.GetFileNameWithoutExtension(self.OriginalFilePath);
        }

        public static string ToString(this IImporterObject self)
        {
	        var strText = self.ToString();
	        if ((strText == null) || strText.Contains(self.GetType().Name)) {
		        strText = Path.GetFileName(self.OriginalFilePath);
	        }
	        return strText;
        }
    }
}
