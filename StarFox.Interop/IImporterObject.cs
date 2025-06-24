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
    }
}
