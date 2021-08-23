//ş
using System;

namespace UWPIconExtractor {
    public class AppxApp {
        private readonly AppxPackage.IAppxManifestApplication _app;

        internal AppxApp (AppxPackage.IAppxManifestApplication app) {
            _app = app;
        }

        public String GetStringValue (String name) {
            if (name == null) {
                throw new ArgumentNullException("name");
            }

            return AppxPackage.GetStringValue(_app, name);
        }

        // we code well-known but there are others (like Square71x71Logo, Square44x44Logo, whatever ...)
        // https://msdn.microsoft.com/en-us/library/windows/desktop/hh446703.aspx
        public String Description { get; internal set; }

        public String DisplayName { get; internal set; }

        public String EntryPoint { get; internal set; }

        public String Executable { get; internal set; }

        public String Id { get; internal set; }

        public String Logo { get; internal set; }

        public String SmallLogo { get; internal set; }

        public String StartPage { get; internal set; }

        public String Square150x150Logo { get; internal set; }

        public String Square30x30Logo { get; internal set; }

        public String Square44x44Logo { get; internal set; }

        public String BackgroundColor { get; internal set; }

        public String ForegroundText { get; internal set; }

        public String WideLogo { get; internal set; }

        public String Wide310x310Logo { get; internal set; }

        public String ShortName { get; internal set; }

        public String Square310x310Logo { get; internal set; }

        public String Square70x70Logo { get; internal set; }

        public String MinWidth { get; internal set; }
    }
}
