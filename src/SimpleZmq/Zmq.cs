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

        public static int RetryIfInterrupted<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, int> function, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            int returnValue;
            do
            {
                returnValue = function(arg1, arg2, arg3);
            } while (returnValue == -1 && LibZmq.zmq_errno() == ZmqErrNo.EINTR);
            return returnValue;
        }

        public static int RetryIfInterrupted<TArg1, TArg2, TArg3, TArg4>(Func<TArg1, TArg2, TArg3, TArg4, int> function, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            int returnValue;
            do
            {
                returnValue = function(arg1, arg2, arg3, arg4);
            } while (returnValue == -1 && LibZmq.zmq_errno() == ZmqErrNo.EINTR);
            return returnValue;
        }

        public static ZmqError Error(int returnValue)
        {
            if (returnValue >= 0) return ZmqError.Success();
            if (returnValue == ErrorReturnValue)
            {
                return ZmqError.FromErrNo(LibZmq.zmq_errno());
            }
            throw new ArgumentException(String.Format("returnValue can be either {0} or 0 or greater, but it was {1}.", ErrorReturnValue, returnValue));
        }

        public static ZmqError Error(IntPtr returnValue)
        {
            if (returnValue != IntPtr.Zero) return ZmqError.Success();
            return ZmqError.FromErrNo(LibZmq.zmq_errno());
        }

        public static void ThrowIfError(ZmqError zmqError)
        {
            if (zmqError.NoError) return;
            throw new ZmqException(zmqError);
        }

        public static int ThrowIfError(int returnValue)
        {
            var zmqError = Error(returnValue);
            if (zmqError.NoError) return returnValue;
            throw new ZmqException(zmqError);
        }

        public static IntPtr ThrowIfError(IntPtr returnValue)
        {
            var zmqError = Error(returnValue);
            if (zmqError.NoError) return returnValue;
            throw new ZmqException(zmqError);
        }

        /// <summary>
        /// Checks if the specified return value indicates an error, if so throws a <see cref="ZmqException"/>. Context-termination doesn't count as error, it just returns 0 for it.
        /// </summary>
        /// <param name="returnValue">The return value to process.</param>
        /// <returns>The return value.</returns>
        /// <remarks>
        /// It's useful when we want to get back the return value in case of success, otherwise we want throw <see cref="ZmqException"/>.
        /// </remarks>
        public static int ThrowIfError_IgnoreContextTerminated(int returnValue)
        {
            var zmqError = Zmq.Error(returnValue);
            if (zmqError.NoError) return returnValue;
            // we can safely ignore context-termination at socket operatons
            if (zmqError.ContextTerminated) return 0;

            // it's a real error
            throw new ZmqException(zmqError);
        }

        /// <summary>
        /// Checks if the specified return value indicates an error, if so throws a <see cref="ZmqException"/>. Context-termination doesn't count as error, it just returns 0 for it, if <paramref="expectTryAgain"/> is true, EAGAIN means null return value.
        /// </summary>
        /// <param name="returnValue">The return value to process.</param>
        /// <param name="expectTryAgain">Optional parameter. If it's true, EAGAIN errors don't count as errors, it just returns null to indicate it.</param>
        /// <returns>The return value or null if <paramref name="expectTryAgain"/> is true and the error was EAGAIN.</returns>
        /// <remarks>
        /// It's useful when we want to get back the return value in case of success, otherwise we want throw <see cref="ZmqException"/>.
        /// </remarks>
        public static int? ThrowIfError_IgnoreContextTerminated(int returnValue, bool expectTryAgain = false)
        {
            var zmqError = Zmq.Error(returnValue);
            if (zmqError.NoError) return returnValue;
            // we can safely ignore context-termination at socket operatons
            if (zmqError.ContextTerminated) return 0;
            // ...and try-again (if expectTryAgain is true). The return value indicates that it should be retried.
            if (expectTryAgain && zmqError.ShouldTryAgain) return null;

            // it's a real error
            throw new ZmqException(zmqError);
        }
    }
}
