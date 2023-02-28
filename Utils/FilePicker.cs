using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Progrimage.Utils
{
    public class FilePicker
    {
        public virtual string ResultPath { get; protected set; }
        public virtual string ResultName { get; protected set; }
        public virtual string InputPath { get; set; }
        public virtual bool ForceFileSystem { get; set; }
        public virtual string Title { get; set; }
        public virtual string OkButtonLabel { get; set; }
        public virtual string FileNameLabel { get; set; }
        public bool PickFolders;
        public string? Filter = null;

        public virtual int SetOptions(int options)
        {
            if (ForceFileSystem) options |= (int)FOS.FORCEFILESYSTEM;
            return options;
        }

        // for all .NET
        public virtual bool ShowDialog()
        {
            var dialog = (IFileOpenDialog)new FileOpenDialog();
            if (!string.IsNullOrEmpty(InputPath))
            {
                if (SHCreateItemFromParsingName(InputPath, null, typeof(IShellItem).GUID, out var item) != 0)
                    return false;

                dialog.SetFolder(item);
            }

            //var options = FOS.FOS_PICKFOLDERS;
            int options = PickFolders ? (int)FOS.PICKFOLDERS : 0;
            options = SetOptions(options);
            dialog.SetOptions((FOS)options);

            if (Filter is not null) dialog.SetFilter(Filter);
            if (Title != null) dialog.SetTitle(Title);
            if (OkButtonLabel != null) dialog.SetOkButtonLabel(OkButtonLabel);
            if (FileNameLabel != null) dialog.SetFileName(FileNameLabel);

            //    IntPtr owner = Process.GetCurrentProcess().MainWindowHandle;
            //if (owner == IntPtr.Zero)
            //{
                IntPtr owner = GetDesktopWindow();
            //}

            var hr = dialog.Show(owner);
            if (hr == ERROR_CANCELLED) return false;
            if (hr != 0) return false;
            if (dialog.GetResult(out var result) != 0) return false;
            if (result.GetDisplayName(SIGDN.DESKTOPABSOLUTEPARSING, out var path) != 0) return false;

            ResultPath = path;
            if (PickFolders) ResultPath += '\\';

            if (result.GetDisplayName(SIGDN.DESKTOPABSOLUTEEDITING, out path) == 0)
                ResultName = path;

            return true;
        }

        [DllImport("shell32")]
        private static extern int SHCreateItemFromParsingName([MarshalAs(UnmanagedType.LPWStr)] string pszPath, IBindCtx pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IShellItem ppv);

        [DllImport("user32")]
        private static extern IntPtr GetDesktopWindow();

#pragma warning disable IDE1006 // Naming Styles
        private const int ERROR_CANCELLED = unchecked((int)0x800704C7);
#pragma warning restore IDE1006 // Naming Styles

        [ComImport, Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")] // CLSID_FileOpenDialog
        private class FileOpenDialog
        {
        }

        [ComImport, Guid("42f85136-db7e-439c-85f1-e4075d135fc8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog
        {
            [PreserveSig] int Show(IntPtr parent); // IModalWindow
            [PreserveSig] int SetFileTypes();  // not fully defined
            [PreserveSig] int SetFileTypeIndex(int iFileType);
            [PreserveSig] int GetFileTypeIndex(out int piFileType);
            [PreserveSig] int Advise(); // not fully defined
            [PreserveSig] int Unadvise();
            [PreserveSig] int SetOptions(FOS fos);
            [PreserveSig] int GetOptions(out FOS pfos);
            [PreserveSig] int SetDefaultFolder(IShellItem psi);
            [PreserveSig] int SetFolder(IShellItem psi);
            [PreserveSig] int GetFolder(out IShellItem ppsi);
            [PreserveSig] int GetCurrentSelection(out IShellItem ppsi);
            [PreserveSig] int SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            [PreserveSig] int GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
            [PreserveSig] int SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            [PreserveSig] int SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
            [PreserveSig] int SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
            [PreserveSig] int GetResult(out IShellItem ppsi);
            [PreserveSig] int AddPlace(IShellItem psi, int alignment);
            [PreserveSig] int SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
            [PreserveSig] int Close(int hr);
            [PreserveSig] int SetClientGuid();  // not fully defined
            [PreserveSig] int ClearClientData();
            [PreserveSig] int SetFilter([MarshalAs(UnmanagedType.IUnknown)] object pFilter);
            //[PreserveSig] int SetFilter([MarshalAs(UnmanagedType.LPWStr)] string pFilter);
            [PreserveSig] int GetResults([MarshalAs(UnmanagedType.IUnknown)] out object ppenum);
            [PreserveSig] int GetSelectedItems([MarshalAs(UnmanagedType.IUnknown)] out object ppsai);
        }

        [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            [PreserveSig] int BindToHandler(); // not fully defined
            [PreserveSig] int GetParent(); // not fully defined
            [PreserveSig] int GetDisplayName(SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
            [PreserveSig] int GetAttributes();  // not fully defined
            [PreserveSig] int Compare();  // not fully defined
        }

#pragma warning disable CA1712 // Do not prefix enum values with type name
        private enum SIGDN : uint
        {
            DESKTOPABSOLUTEEDITING = 0x8004c000,
            DESKTOPABSOLUTEPARSING = 0x80028000,
            FILESYSPATH = 0x80058000,
            NORMALDISPLAY = 0,
            PARENTRELATIVE = 0x80080001,
            PARENTRELATIVEEDITING = 0x80031001,
            PARENTRELATIVEFORADDRESSBAR = 0x8007c001,
            PARENTRELATIVEPARSING = 0x80018001,
            URL = 0x80068000
        }

        [Flags]
        public enum FOS
        {
            OVERWRITEPROMPT = 0x2,
            STRICTFILETYPES = 0x4,
            NOCHANGEDIR = 0x8,
            PICKFOLDERS = 0x20,
            FORCEFILESYSTEM = 0x40,
            ALLNONSTORAGEITEMS = 0x80,
            NOVALIDATE = 0x100,
            ALLOWMULTISELECT = 0x200,
            PATHMUSTEXIST = 0x800,
            FILEMUSTEXIST = 0x1000,
            CREATEPROMPT = 0x2000,
            SHAREAWARE = 0x4000,
            NOREADONLYRETURN = 0x8000,
            NOTESTFILECREATE = 0x10000,
            HIDEMRUPLACES = 0x20000,
            HIDEPINNEDPLACES = 0x40000,
            NODEREFERENCELINKS = 0x100000,
            OKBUTTONNEEDSINTERACTION = 0x200000,
            DONTADDTORECENT = 0x2000000,
            FORCESHOWHIDDEN = 0x10000000,
            DEFAULTNOMINIMODE = 0x20000000,
            FORCEPREVIEWPANEON = 0x40000000,
            SUPPORTSTREAMABLEITEMS = unchecked((int)0x80000000)
        }
#pragma warning restore CA1712 // Do not prefix enum values with type name
    }
}
