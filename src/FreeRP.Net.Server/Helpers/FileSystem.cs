using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Net.Server.Helpers
{
    public static class FileSystem
    {
        public static bool CopyAll(string sourcePath, string targetPath)
        {
            try
            {
                foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
                }

                foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                {
                    File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
