using System.IO;

namespace DocHound.Utilities
{
    public class KavaUtils
    {
        /// <summary>
        /// Copies directories using either top level only or deep merge copy.
        /// 
        /// Copies a directory by copying files from source folder to target folder.
        /// If folder(s) don't exist they are created.
        /// 
        /// deepCopy copies files in sub-folders and merges them into the target
        /// folder. Unless you specify deleteFirst, files are copied and overwrite or add to
        /// existing structure, leaving old files in place. Use deleteFirst if you
        /// want a new structure with only the source files.
        /// </summary>
        /// <param name="sourcePath">Path to copy from</param>
        /// <param name="targetPath">Path to copy to</param>
        /// <param name="deleteFirst">If true deletes target folder before copying. Otherwise files are merged from source into target.</param>
        public static void CopyDirectory(string sourcePath, string targetPath, bool deleteFirst = false, bool deepCopy = true)
        {
            if (deleteFirst && Directory.Exists(targetPath))
                Directory.Delete(targetPath, true);

            if (!Directory.Exists(targetPath))
                Directory.CreateDirectory(targetPath);

            var searchOption = SearchOption.TopDirectoryOnly;
            if (deepCopy)
                searchOption = SearchOption.AllDirectories;

            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", searchOption))
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));

            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", searchOption))
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
        }
    }
}