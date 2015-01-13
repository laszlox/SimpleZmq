using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SimpleZmq.Native
{
    internal static class Win32
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string library);
    }
}
