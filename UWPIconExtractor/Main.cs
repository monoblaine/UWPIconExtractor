//ş
using System;
using RGiesecke.DllExport;
using System.Runtime.InteropServices;
using System.IO;

namespace UWPIconExtractor {
    public class Main {
        [DllExport("getFileName", CallingConvention = CallingConvention.Cdecl)]
        public static String GetFileName (IntPtr id) {
            var package = AppxPackage.FromWindow(id);
            var pathToImage = Path.Combine(
                package.Path,
                package.Apps[0].Square44x44Logo
            );
            var fileInfo = new FileInfo(pathToImage);

            return (
                fileInfo.DirectoryName + "\\" +
                Path.GetFileNameWithoutExtension(pathToImage) +
                ".targetsize-32" +
                fileInfo.Extension
            );
        }
    }
}
