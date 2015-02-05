using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SimpleZmq
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct ZmqPollItem
    {
        public IntPtr Socket;
        public IntPtr FileDescriptor;
        public short Events;
        public short ReadyEvents;
    }
}
