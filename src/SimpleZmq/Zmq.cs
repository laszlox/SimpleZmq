using SimpleZmq.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SimpleZmq
{
    /// <summary>
    /// Static utility class for various functions.
    /// </summary>
    public static class Zmq
    {
        public static int ThrowIfError(int returnValue)
        {
            if (returnValue >= 0) return returnValue;
            if (returnValue == -1)
            {
                var errNo = LibZmq.zmq_errno();
                var errStrPtr = LibZmq.zmq_strerror(errNo);
                throw new ZmqException(errNo, Marshal.PtrToStringAnsi(errStrPtr));
            }
            throw new ArgumentException(String.Format("returnValue can be either -1 or 0 or greater, but it was {0}.", returnValue));
        }
    }
}
