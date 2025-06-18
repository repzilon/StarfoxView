using System.IO;

namespace StarFox.Interop
{
    public interface IImporterObject
    {
        string OriginalFilePath { get; }
    }

    public static class ImporteObjectExtension
    {
        /// <summary>
        /// The file name of this file using OriginalFilePath
        /// </summary>
        public static string FileName(this IImporterObject self)
        {
            return Path.GetFileNameWithoutExtension(self.OriginalFilePath);
        }
    }
}
