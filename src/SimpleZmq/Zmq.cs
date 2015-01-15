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
        public const int ErrorReturnValue = -1;

        public static ZmqError ErrorNumberWithDescription(int returnValue)
        {
            if (returnValue >= 0) return null;
            if (returnValue == ErrorReturnValue)
            {
                return new ZmqError(LibZmq.zmq_errno());
            }
            throw new ArgumentException(String.Format("returnValue can be either {0} or 0 or greater, but it was {1}.", ErrorReturnValue, returnValue));
        }

        public static int ThrowIfError(int returnValue)
        {
            if (returnValue >= 0) return returnValue;
            if (returnValue == ErrorReturnValue)
            {
                var errNo = LibZmq.zmq_errno();
                var errStrPtr = LibZmq.zmq_strerror(errNo);
                throw new ZmqException(errNo, Marshal.PtrToStringAnsi(errStrPtr));
            }
            throw new ArgumentException(String.Format("returnValue can be either {0} or 0 or greater, but it was {1}.", ErrorReturnValue, returnValue));
        }

        public static IntPtr ThrowIfError(IntPtr pointer)
        {
            if (pointer != IntPtr.Zero) return pointer;
            var errNo = LibZmq.zmq_errno();
            var errStrPtr = LibZmq.zmq_strerror(errNo);
            throw new ZmqException(errNo, Marshal.PtrToStringAnsi(errStrPtr));
        }
    }
}
