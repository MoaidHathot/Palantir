using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Palantir;

/// <summary>
/// Creates Start Menu shortcuts (.lnk) with an AppUserModelID property set,
/// which is what Windows uses to brand toast notifications (corner icon + app name).
/// </summary>
internal static class ShellLink
{
    /// <summary>System.AppUserModel.ID property key.</summary>
    private static readonly PropertyKey PKEY_AppUserModel_ID = new(
        new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 5);

    /// <summary>
    /// System.AppUserModel.ToastActivatorCLSID property key. REQUIRED on the
    /// shortcut for Windows 10 1607+ to display toasts at all when using a
    /// custom AUMID via <c>ToastNotificationManager.CreateToastNotifier(aumid)</c>.
    /// Without it, <c>Show()</c> succeeds silently but no banner / Action Center
    /// entry is produced.
    /// </summary>
    private static readonly PropertyKey PKEY_AppUserModel_ToastActivatorCLSID = new(
        new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 26);

    /// <summary>
    /// Create or overwrite a Start Menu shortcut at <paramref name="shortcutPath"/>
    /// pointing to <paramref name="targetExe"/>, with the given AUMID and icon.
    /// </summary>
    public static void Create(
        string shortcutPath,
        string targetExe,
        string aumid,
        string iconPath,
        Guid toastActivatorClsid,
        string? arguments = null,
        string? description = null)
    {
        var dir = Path.GetDirectoryName(shortcutPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var link = (IShellLinkW)new CShellLink();
        try
        {
            link.SetPath(targetExe);
            if (!string.IsNullOrWhiteSpace(arguments))
                link.SetArguments(arguments);
            if (!string.IsNullOrWhiteSpace(description))
                link.SetDescription(description);
            link.SetIconLocation(iconPath, 0);
            link.SetWorkingDirectory(Path.GetDirectoryName(targetExe) ?? "");

            var store = (IPropertyStore)link;
            using (var pv = PropVariant.FromString(aumid))
            {
                store.SetValue(ref Unsafe(PKEY_AppUserModel_ID), pv.Variant);
            }
            using (var pv = PropVariant.FromClsid(toastActivatorClsid))
            {
                store.SetValue(ref Unsafe(PKEY_AppUserModel_ToastActivatorCLSID), pv.Variant);
            }
            store.Commit();

            var file = (IPersistFile)link;
            file.Save(shortcutPath, fRemember: true);
        }
        finally
        {
            Marshal.ReleaseComObject(link);
        }
    }

    /// <summary>Delete a shortcut file if it exists. Returns true if deleted.</summary>
    public static bool Delete(string shortcutPath)
    {
        if (!File.Exists(shortcutPath)) return false;
        File.Delete(shortcutPath);
        return true;
    }

    /// <summary>
    /// Read the AUMID property from a shortcut, or null if absent / unreadable.
    /// </summary>
    public static string? ReadAumid(string shortcutPath)
    {
        if (!File.Exists(shortcutPath)) return null;

        var link = (IShellLinkW)new CShellLink();
        try
        {
            ((IPersistFile)link).Load(shortcutPath, 0);
            var store = (IPropertyStore)link;
            store.GetValue(ref Unsafe(PKEY_AppUserModel_ID), out var pv);
            try
            {
                return PropVariant.ToString(pv);
            }
            finally
            {
                PropVariant.Clear(ref pv);
            }
        }
        catch
        {
            return null;
        }
        finally
        {
            Marshal.ReleaseComObject(link);
        }
    }

    // Helper to get a writable ref to a static readonly value (pass-by-ref to COM).
    private static ref T Unsafe<T>(in T value) =>
        ref System.Runtime.CompilerServices.Unsafe.AsRef(in value);

    // ── COM Interop ─────────────────────────────────────────────────

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class CShellLink { }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszFile,
            int cchMaxPath, IntPtr pfd, int fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        void Resolve(IntPtr hwnd, int fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    private interface IPropertyStore
    {
        void GetCount(out uint cProps);
        void GetAt(uint iProp, out PropertyKey pkey);
        void GetValue(ref PropertyKey key, out PropVariant.PROPVARIANT pv);
        void SetValue(ref PropertyKey key, [In] PropVariant.PROPVARIANT pv);
        void Commit();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct PropertyKey
    {
        public Guid FormatId;
        public int PropertyId;

        public PropertyKey(Guid formatId, int propertyId)
        {
            FormatId = formatId;
            PropertyId = propertyId;
        }
    }

    /// <summary>
    /// Minimal PROPVARIANT helpers — only string (VT_LPWSTR) is needed for AUMID.
    /// </summary>
    private sealed class PropVariant : IDisposable
    {
        public PROPVARIANT Variant;
        private IntPtr _allocated;

        [DllImport("ole32.dll")]
        private static extern int PropVariantClear(ref PROPVARIANT pvar);

        public static PropVariant FromString(string s)
        {
            var pv = new PropVariant
            {
                _allocated = Marshal.StringToCoTaskMemUni(s),
            };
            pv.Variant.vt = VarEnum.VT_LPWSTR;
            pv.Variant.unionmember = pv._allocated;
            return pv;
        }

        public static PropVariant FromClsid(Guid clsid)
        {
            // VT_CLSID requires the GUID itself heap-allocated; PROPVARIANT
            // owns the memory and PropVariantClear frees it.
            var pv = new PropVariant
            {
                _allocated = Marshal.AllocCoTaskMem(16),
            };
            Marshal.StructureToPtr(clsid, pv._allocated, fDeleteOld: false);
            pv.Variant.vt = VarEnum.VT_CLSID;
            pv.Variant.unionmember = pv._allocated;
            return pv;
        }

        public static string? ToString(PROPVARIANT pv)
        {
            if (pv.vt == VarEnum.VT_LPWSTR && pv.unionmember != IntPtr.Zero)
                return Marshal.PtrToStringUni(pv.unionmember);
            return null;
        }

        public static void Clear(ref PROPVARIANT pv) => PropVariantClear(ref pv);

        public void Dispose()
        {
            PropVariantClear(ref Variant);
            // PropVariantClear frees the allocated string; don't double-free.
            _allocated = IntPtr.Zero;
            GC.SuppressFinalize(this);
        }

        ~PropVariant() => Dispose();

        [StructLayout(LayoutKind.Sequential)]
        public struct PROPVARIANT
        {
            public VarEnum vt;
            public ushort wReserved1;
            public ushort wReserved2;
            public ushort wReserved3;
            public IntPtr unionmember;
            public IntPtr unionmember2;
        }
    }
}
