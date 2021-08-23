//ş
using System;
using UWPIconExtractor;

namespace Kabuk {
    internal class Program {
        private static void Main (String[] args) {
            //var processId = Convert.ToInt32(args[0]);

            //var package = AppxPackage.FromProcess(processId);
            //var package = AppxPackage.FromProcess(new IntPtr(0x007112C8));
            //var package = AppxPackage.FromProcess(new IntPtr(0x00480F80));
            //var package = AppxPackage.FromProcess(new IntPtr(0x007112C8));
            //var package = AppxPackage.FromProcess(7216);
            var package = AppxPackage.FromWindow(new IntPtr(0x007112C8));
            //var package = AppxPackage.FromProcess(new IntPtr(198838));
            //var package = AppxPackage.FromProcess(new IntPtr(0x000308B6));
            //var package = AppxPackage.FromProcess(new IntPtr(0x000208A4));
            var ehe = 32;
        }
    }
}
