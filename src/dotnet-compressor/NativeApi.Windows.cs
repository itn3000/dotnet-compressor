using System;
using System.Runtime.InteropServices;

namespace dotnet_compressor
{
    static partial class NativeApi
    {
        /// <summary>The link target is a directory.</summary>
        public const uint SYMBOLIC_LINK_FLAG_DIRECTORY = 1;
        /// <summary>Specify this flag to allow creation of symbolic links when the process is not elevated. </summary>
        /// <remarks>Developer Mode must first be enabled on the machine before this option will function. </remarks>
        public const uint SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE = 2;

        /// <summary>creating symbolic link(including junction) in windows</summary>
        /// <remarks>see https://docs.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-createsymboliclinkw for more details</remarks>
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CreateSymbolicLinkW(string symlinkFileName, string targetFileName, uint flags);
        [DllImport("kernel32")]
        public static extern uint GetLastError();
    }

}