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

            /*
             * TODO: Sonradan sıçış olursa şurayı incele:
             * https://stackoverflow.com/questions/37417757/extract-icon-from-uwp-application
             * https://stackoverflow.com/questions/37686916/how-do-i-retrieve-a-windows-store-apps-icon-from-a-c-sharp-desktop-app
             */

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
