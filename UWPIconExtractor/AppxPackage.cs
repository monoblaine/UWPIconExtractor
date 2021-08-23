//ş
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace UWPIconExtractor {
    public class AppxPackage {
        private readonly List<AppxApp> _apps = new List<AppxApp>();

        private IAppxManifestProperties _properties;

        private AppxPackage () {
        }

        public String FullName { get; private set; }

        public String Path { get; private set; }

        public String Publisher { get; private set; }

        public String PublisherId { get; private set; }

        public String ResourceId { get; private set; }

        public String FamilyName { get; private set; }

        public String ApplicationUserModelId { get; private set; }

        public String Logo { get; private set; }

        public String PublisherDisplayName { get; private set; }

        public String Description { get; private set; }

        public String DisplayName { get; private set; }

        public Boolean IsFramework { get; private set; }

        public Version Version { get; private set; }

        public AppxPackageArchitecture ProcessorArchitecture { get; private set; }

        public IReadOnlyList<AppxApp> Apps {
            get {
                return _apps;
            }
        }

        public IEnumerable<AppxPackage> DependencyGraph {
            get {
                return QueryPackageInfo(FullName, PackageConstants.PACKAGE_FILTER_ALL_LOADED).Where(p => p.FullName != FullName);
            }
        }

        public String FindHighestScaleQualifiedImagePath (String resourceName) {
            if (resourceName == null) {
                throw new ArgumentNullException("resourceName");
            }

            const String scaleToken = ".scale-";
            var sizes = new List<Int32>();
            var name = System.IO.Path.GetFileNameWithoutExtension(resourceName);
            var ext = System.IO.Path.GetExtension(resourceName);
            foreach (var file in Directory.EnumerateFiles(System.IO.Path.Combine(Path, System.IO.Path.GetDirectoryName(resourceName)), name + scaleToken + "*" + ext)) {
                var fileName = System.IO.Path.GetFileNameWithoutExtension(file);
                var pos = fileName.IndexOf(scaleToken) + scaleToken.Length;
                var sizeText = fileName.Substring(pos);
                if (Int32.TryParse(sizeText, out var size)) {
                    sizes.Add(size);
                }
            }
            if (sizes.Count == 0) {
                return null;
            }

            sizes.Sort();
            return System.IO.Path.Combine(Path, System.IO.Path.GetDirectoryName(resourceName), name + scaleToken + sizes.Last() + ext);
        }

        public override String ToString () {
            return FullName;
        }

        public static AppxPackage FromWindow (IntPtr handle) {
            GetWindowThreadProcessId(handle, out var processId);

            if (processId == 0) {
                return null;
            }

            return FromProcess(processId);
        }

        public static AppxPackage FromProcess (Process process) {
            if (process == null) {
                process = Process.GetCurrentProcess();
            }

            try {
                return FromProcess(process.Handle);
            }
            catch {
                // probably access denied on .Handle
                return null;
            }
        }

        public static AppxPackage FromProcess (Int32 processId) {
            const Int32 QueryLimitedInformation = 0x1000;
            var hProcess = OpenProcess(QueryLimitedInformation, false, processId);

            try {
                return FromProcess(hProcess);
            }
            finally {
                if (hProcess != IntPtr.Zero) {
                    CloseHandle(hProcess);
                }
            }
        }

        public static AppxPackage FromProcess (IntPtr hProcess) {
            if (hProcess == IntPtr.Zero) {
                throw new Exception("hProcess == IntPtr.Zero");
            }

            // hprocess must have been opened with QueryLimitedInformation
            var len = 0;
            GetPackageFullName(hProcess, ref len, null);

            if (len == 0) {
                throw new Exception($"len == 0 / {Marshal.GetLastWin32Error()}");
            }

            var sb = new StringBuilder(len);
            var fullName = GetPackageFullName(hProcess, ref len, sb) == 0 ? sb.ToString() : null;

            if (String.IsNullOrEmpty(fullName)) { // not an AppX
                throw new Exception($"not an AppX / {Marshal.GetLastWin32Error()}");
            }

            var package = QueryPackageInfo(fullName, PackageConstants.PACKAGE_FILTER_HEAD).First();

            len = 0;
            GetApplicationUserModelId(hProcess, ref len, null);
            sb = new StringBuilder(len);
            package.ApplicationUserModelId = GetApplicationUserModelId(hProcess, ref len, sb) == 0 ? sb.ToString() : null;

            return package;
        }

        public String GetPropertyStringValue (String name) {
            if (name == null) {
                throw new ArgumentNullException("name");
            }

            return GetStringValue(_properties, name);
        }

        public Boolean GetPropertyBoolValue (String name) {
            if (name == null) {
                throw new ArgumentNullException("name");
            }

            return GetBoolValue(_properties, name);
        }

        public String LoadResourceString (String resource) {
            return LoadResourceString(FullName, resource);
        }

        private static IEnumerable<AppxPackage> QueryPackageInfo (String fullName, PackageConstants flags) {
            OpenPackageInfoByFullName(fullName, 0, out var infoRef);

            if (infoRef == IntPtr.Zero) {
                yield break;
            }

            var infoBuffer = IntPtr.Zero;

            try {
                var len = 0;

                GetPackageInfo(infoRef, flags, ref len, IntPtr.Zero, out var count);

                if (len == 0) {
                    yield break;
                }

                var factory = (IAppxFactory) new AppxFactory();

                infoBuffer = Marshal.AllocHGlobal(len);

                var res = GetPackageInfo(infoRef, flags, ref len, infoBuffer, out count);

                for (var i = 0; i < count; i++) {
                    var info = (PACKAGE_INFO) Marshal.PtrToStructure(infoBuffer + i * Marshal.SizeOf(typeof(PACKAGE_INFO)), typeof(PACKAGE_INFO));
                    var package = new AppxPackage {
                        FamilyName = Marshal.PtrToStringUni(info.packageFamilyName),
                        FullName = Marshal.PtrToStringUni(info.packageFullName),
                        Path = Marshal.PtrToStringUni(info.path),
                        Publisher = Marshal.PtrToStringUni(info.packageId.publisher),
                        PublisherId = Marshal.PtrToStringUni(info.packageId.publisherId),
                        ResourceId = Marshal.PtrToStringUni(info.packageId.resourceId),
                        ProcessorArchitecture = info.packageId.processorArchitecture,
                        Version = new Version(info.packageId.VersionMajor, info.packageId.VersionMinor, info.packageId.VersionBuild, info.packageId.VersionRevision)
                    };

                    // read manifest
                    var manifestPath = System.IO.Path.Combine(package.Path, "AppXManifest.xml");
                    const Int32 STGM_SHARE_DENY_NONE = 0x40;

                    SHCreateStreamOnFileEx(manifestPath, STGM_SHARE_DENY_NONE, 0, false, IntPtr.Zero, out var strm);

                    if (strm == null) {
                        yield break;
                    }

                    var reader = factory.CreateManifestReader(strm);

                    package._properties = reader.GetProperties();
                    package.Description = package.GetPropertyStringValue("Description");
                    package.DisplayName = package.GetPropertyStringValue("DisplayName");
                    package.Logo = package.GetPropertyStringValue("Logo");
                    package.PublisherDisplayName = package.GetPropertyStringValue("PublisherDisplayName");
                    package.IsFramework = package.GetPropertyBoolValue("Framework");

                    var apps = reader.GetApplications();

                    while (apps.GetHasCurrent()) {
                        var app = apps.GetCurrent();
                        var appx = new AppxApp(app) {
                            Description = GetStringValue(app, "Description"),
                            DisplayName = GetStringValue(app, "DisplayName"),
                            EntryPoint = GetStringValue(app, "EntryPoint"),
                            Executable = GetStringValue(app, "Executable"),
                            Id = GetStringValue(app, "Id"),
                            Logo = GetStringValue(app, "Logo"),
                            SmallLogo = GetStringValue(app, "SmallLogo"),
                            StartPage = GetStringValue(app, "StartPage"),
                            Square150x150Logo = GetStringValue(app, "Square150x150Logo"),
                            Square30x30Logo = GetStringValue(app, "Square30x30Logo"),
                            Square44x44Logo = GetStringValue(app, "Square44x44Logo"),
                            BackgroundColor = GetStringValue(app, "BackgroundColor"),
                            ForegroundText = GetStringValue(app, "ForegroundText"),
                            WideLogo = GetStringValue(app, "WideLogo"),
                            Wide310x310Logo = GetStringValue(app, "Wide310x310Logo"),
                            ShortName = GetStringValue(app, "ShortName"),
                            Square310x310Logo = GetStringValue(app, "Square310x310Logo"),
                            Square70x70Logo = GetStringValue(app, "Square70x70Logo"),
                            MinWidth = GetStringValue(app, "MinWidth")
                        };

                        package._apps.Add(appx);
                        apps.MoveNext();
                    }

                    Marshal.ReleaseComObject(strm);

                    yield return package;
                }

                Marshal.ReleaseComObject(factory);
            }
            finally {
                if (infoBuffer != IntPtr.Zero) {
                    Marshal.FreeHGlobal(infoBuffer);
                }

                ClosePackageInfo(infoRef);
            }
        }

        public static String LoadResourceString (String packageFullName, String resource) {
            if (packageFullName == null) {
                throw new ArgumentNullException("packageFullName");
            }

            if (String.IsNullOrWhiteSpace(resource)) {
                return null;
            }

            const String resourceScheme = "ms-resource:";

            if (!resource.StartsWith(resourceScheme)) {
                return null;
            }

            var part = resource.Substring(resourceScheme.Length);
            String url;

            if (part.StartsWith("/")) {
                url = resourceScheme + "//" + part;
            }
            else {
                url = resourceScheme + "///resources/" + part;
            }

            var source = String.Format("@{{{0}? {1}}}", packageFullName, url);
            var sb = new StringBuilder(1024);
            var i = SHLoadIndirectString(source, sb, sb.Capacity, IntPtr.Zero);

            if (i != 0) {
                return null;
            }

            return sb.ToString();
        }

        private static String GetStringValue (IAppxManifestProperties props, String name) {
            if (props == null) {
                return null;
            }

            props.GetStringValue(name, out var value);

            return value;
        }

        private static Boolean GetBoolValue (IAppxManifestProperties props, String name) {
            props.GetBoolValue(name, out var value);

            return value;
        }

        internal static String GetStringValue (IAppxManifestApplication app, String name) {
            app.GetStringValue(name, out var value);

            return value;
        }

        [Guid("5842a140-ff9f-4166-8f5c-62f5b7b0c781"), ComImport]
        private class AppxFactory {
        }

        [Guid("BEB94909-E451-438B-B5A7-D79E767B75D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAppxFactory {
            void _VtblGap0_2 (); // skip 2 methods

            IAppxManifestReader CreateManifestReader (IStream inputStream);
        }

        [Guid("4E1BD148-55A0-4480-A3D1-15544710637C"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAppxManifestReader {
            void _VtblGap0_1 (); // skip 1 method

            IAppxManifestProperties GetProperties ();

            void _VtblGap1_5 (); // skip 5 methods

            IAppxManifestApplicationsEnumerator GetApplications ();
        }

        [Guid("9EB8A55A-F04B-4D0D-808D-686185D4847A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAppxManifestApplicationsEnumerator {
            IAppxManifestApplication GetCurrent ();

            Boolean GetHasCurrent ();

            Boolean MoveNext ();
        }

        [Guid("5DA89BF4-3773-46BE-B650-7E744863B7E8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IAppxManifestApplication {
            [PreserveSig]
            Int32 GetStringValue ([MarshalAs(UnmanagedType.LPWStr)] String name, [MarshalAs(UnmanagedType.LPWStr)] out String vaue);
        }

        [Guid("03FAF64D-F26F-4B2C-AAF7-8FE7789B8BCA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAppxManifestProperties {
            [PreserveSig]
            Int32 GetBoolValue ([MarshalAs(UnmanagedType.LPWStr)] String name, out Boolean value);

            [PreserveSig]
            Int32 GetStringValue ([MarshalAs(UnmanagedType.LPWStr)] String name, [MarshalAs(UnmanagedType.LPWStr)] out String vaue);
        }

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern Int32 SHLoadIndirectString (String pszSource, StringBuilder pszOutBuf, Int32 cchOutBuf, IntPtr ppvReserved);

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern Int32 SHCreateStreamOnFileEx (String fileName, Int32 grfMode, Int32 attributes, Boolean create, IntPtr reserved, out IStream stream);

        [DllImport("user32.dll")]
        private static extern Int32 GetWindowThreadProcessId (IntPtr hWnd, out Int32 lpdwProcessId);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess (Int32 dwDesiredAccess, Boolean bInheritHandle, Int32 dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern Boolean CloseHandle (IntPtr hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern Int32 OpenPackageInfoByFullName (String packageFullName, Int32 reserved, out IntPtr packageInfoReference);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern Int32 GetPackageInfo (IntPtr packageInfoReference, PackageConstants flags, ref Int32 bufferLength, IntPtr buffer, out Int32 count);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern Int32 ClosePackageInfo (IntPtr packageInfoReference);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern Int32 GetPackageFullName (IntPtr hProcess, ref Int32 packageFullNameLength, StringBuilder packageFullName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern Int32 GetApplicationUserModelId (IntPtr hProcess, ref Int32 applicationUserModelIdLength, StringBuilder applicationUserModelId);

        [Flags]
        private enum PackageConstants {
            PACKAGE_FILTER_ALL_LOADED = 0x00000000,
            PACKAGE_PROPERTY_FRAMEWORK = 0x00000001,
            PACKAGE_PROPERTY_RESOURCE = 0x00000002,
            PACKAGE_PROPERTY_BUNDLE = 0x00000004,
            PACKAGE_FILTER_HEAD = 0x00000010,
            PACKAGE_FILTER_DIRECT = 0x00000020,
            PACKAGE_FILTER_RESOURCE = 0x00000040,
            PACKAGE_FILTER_BUNDLE = 0x00000080,
            PACKAGE_INFORMATION_BASIC = 0x00000000,
            PACKAGE_INFORMATION_FULL = 0x00000100,
            PACKAGE_PROPERTY_DEVELOPMENT_MODE = 0x00010000,
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct PACKAGE_INFO {
            public Int32 reserved;
            public Int32 flags;
            public IntPtr path;
            public IntPtr packageFullName;
            public IntPtr packageFamilyName;
            public PACKAGE_ID packageId;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct PACKAGE_ID {
            public Int32 reserved;
            public AppxPackageArchitecture processorArchitecture;
            public UInt16 VersionRevision;
            public UInt16 VersionBuild;
            public UInt16 VersionMinor;
            public UInt16 VersionMajor;
            public IntPtr name;
            public IntPtr publisher;
            public IntPtr resourceId;
            public IntPtr publisherId;
        }
    }
}
