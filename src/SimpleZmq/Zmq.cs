using SimpleZmq.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SimpleZmq
{
    /// <summary>
    /// Static utility class for various zmq functions.
    /// </summary>
    public static class Zmq
    {
        /// <summary>
        /// The returns value that indicates zmq errors.
        /// </summary>
        public const int ErrorReturnValue = -1;

        /// <summary>
        /// Keeps running the specified delegate until it successfully runs or returns non-EINTR error.
        /// </summary>
        /// <typeparam name="TArg1">The type of the 1. argument.</typeparam>
        /// <typeparam name="TArg2">The type of the 2. argument.</typeparam>
        /// <typeparam name="TArg3">The type of the 3. argument.</typeparam>
        /// <param name="function">The function to run.</param>
        /// <param name="arg1">The 1. argument.</param>
        /// <param name="arg2">The 2. argument.</param>
        /// <param name="arg3">The 3. argument.</param>
        /// <returns>The return value of the function.</returns>
        public static int RetryIfInterrupted<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, int> function, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            int returnValue;
            do
            {
                returnValue = function(arg1, arg2, arg3);
            } while (returnValue == -1 && LibZmq.zmq_errno() == ZmqErrNo.EINTR);
            return returnValue;
        }

        /// <summary>
        /// Keeps running the specified delegate until it successfully runs or returns non-EINTR error.
        /// </summary>
        /// <typeparam name="TArg1">The type of the 1. argument.</typeparam>
        /// <typeparam name="TArg2">The type of the 2. argument.</typeparam>
        /// <typeparam name="TArg3">The type of the 3. argument.</typeparam>
        /// <typeparam name="TArg4">The type of the 3. argument.</typeparam>
        /// <param name="function">The function to run.</param>
        /// <param name="arg1">The 1. argument.</param>
        /// <param name="arg2">The 2. argument.</param>
        /// <param name="arg3">The 3. argument.</param>
        /// <param name="arg4">The 3. argument.</param>
        /// <returns>The return value of the function.</returns>
        public static int RetryIfInterrupted<TArg1, TArg2, TArg3, TArg4>(Func<TArg1, TArg2, TArg3, TArg4, int> function, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            int returnValue;
            do
            {
                returnValue = function(arg1, arg2, arg3, arg4);
            } while (returnValue == -1 && LibZmq.zmq_errno() == ZmqErrNo.EINTR);
            return returnValue;
        }

        /// <summary>
        /// Creates a <see cref="ZmqError"/> from the specified return value. If it means success, it'll return a <see cref="ZmqError"/> instance that indicates success.
        /// </summary>
        /// <param name="returnValue">The return value of the last run zmq function.</param>
        /// <returns>The <see cref="ZmqError"/> describing the details of the error or indicating success.</returns>
        public static ZmqError Error(int returnValue)
        {
            if (returnValue >= 0) return ZmqError.Success();
            if (returnValue == ErrorReturnValue)
            {
                return ZmqError.FromErrNo(LibZmq.zmq_errno());
            }
            throw new ArgumentException(String.Format("returnValue can be either {0} or 0 or greater, but it was {1}.", ErrorReturnValue, returnValue));
        }

        /// <summary>
        /// Creates a <see cref="ZmqError"/> from the specified return value. If it means success, it'll return a <see cref="ZmqError"/> instance that indicates success.
        /// </summary>
        /// <param name="returnValue">The return value of the last run zmq function.</param>
        /// <returns>The <see cref="ZmqError"/> describing the details of the error or indicating success.</returns>
        public static ZmqError Error(IntPtr returnValue)
        {
            if (returnValue != IntPtr.Zero) return ZmqError.Success();
            return ZmqError.FromErrNo(LibZmq.zmq_errno());
        }

        /// <summary>
        /// Throws a <see cref="ZmqException"/> if the specified <see cref="ZmqError"/> instance means an error (not success).
        /// </summary>
        /// <param name="zmqError">The zmq error.</param>
        public static void ThrowIfError(ZmqError zmqError)
        {
            if (zmqError.NoError) return;
            throw new ZmqException(zmqError);
        }

        /// <summary>
        /// Throws a <see cref="ZmqException"/> if the specified return value (and it's error) means an error (not success).
        /// </summary>
        /// <param name="returnValue">The return value of the last run zmq function.</param>
        public static int ThrowIfError(int returnValue)
        {
            var zmqError = Error(returnValue);
            if (zmqError.NoError) return returnValue;
            throw new ZmqException(zmqError);
        }

        /// <summary>
        /// Throws a <see cref="ZmqException"/> if the specified return value (and it's error) means an error (not success).
        /// </summary>
        /// <param name="returnValue">The return value of the last run zmq function.</param>
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
        /// Checks if the specified return value indicates an error, if so throws a <see cref="ZmqException"/>. Context-termination doesn't count as error, it just returns 0 for it, if <paramref name="expectTryAgain"/> is true, EAGAIN means null return value.
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
