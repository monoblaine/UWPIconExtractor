//ş
using System;

namespace UWPIconExtractor {
    public class AppxApp {
        // we code well-known but there are others (like Square71x71Logo, Square44x44Logo, whatever ...)
        // https://msdn.microsoft.com/en-us/library/windows/desktop/hh446703.aspx

        public String Id { get; internal set; }

        public String Square44x44Logo { get; internal set; }
    }
}
