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

        public static int RetryIfInterrupted<TArg1, TArg2, TArg3, TArg4>(Func<TArg1, TArg2, TArg3, TArg4, int> function, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            int returnValue;
            do
            {
                returnValue = function(arg1, arg2, arg3, arg4);
            } while (returnValue == -1 && LibZmq.zmq_errno() == ErrNo.EINTR);
            return returnValue;
        }

        public static ZmqError Error(int returnValue)
        {
            if (returnValue >= 0) return null;
            if (returnValue == ErrorReturnValue)
            {
                return new ZmqError(LibZmq.zmq_errno());
            }
            throw new ArgumentException(String.Format("returnValue can be either {0} or 0 or greater, but it was {1}.", ErrorReturnValue, returnValue));
        }

        public static ZmqError Error(IntPtr returnValue)
        {
            if (returnValue != IntPtr.Zero) return null;
            return new ZmqError(LibZmq.zmq_errno());
        }

        public static void ThrowIfError(ZmqError zmqError)
        {
            if (zmqError == null) return;
            throw new ZmqException(zmqError);
        }

        public static int ThrowIfError(int returnValue)
        {
            var zmqError = Error(returnValue);
            if (zmqError == null) return returnValue;
            throw new ZmqException(zmqError);
        }

        public static IntPtr ThrowIfError(IntPtr returnValue)
        {
            var zmqError = Error(returnValue);
            if (zmqError == null) return returnValue;
            throw new ZmqException(zmqError);
        }
    }
}
