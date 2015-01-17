using SimpleZmq.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SimpleZmq
{
    public class ZmqSocket : IDisposable
    {
        private const int ZMQ_DONTWAIT = 1;
        private const int ZMQ_SNDMORE = 2;

        private readonly Action<string>     _logError;
        private IntPtr                      _zmqSocketPtr;
        private bool                        _disposed;

        ~ZmqSocket()
        {
            Dispose(false);
        }

        private ZmqError SocketError(int returnValue)
        {
            var zmqError = Zmq.Error(returnValue);

            // we can safely ignore context-termination at socket operatons
            if (zmqError == null || zmqError.ContextTerminated) return null;

            // otherwise keep the error
            return zmqError;
        }

        private void ThrowIfSocketError(int returnValue)
        {
            Zmq.ThrowIfError(SocketError(returnValue));
        }

        internal ZmqSocket(IntPtr zmqSocketPtr, Action<string> logError)
        {
            Argument.ExpectNonZero(zmqSocketPtr, "zmqSocketPtr");
            Argument.ExpectNonNull(logError, "logError");

            _logError = logError;
            _zmqSocketPtr = zmqSocketPtr;
        }

        public void Bind(string endPoint)
        {
            Argument.ExpectNonNullAndWhiteSpace(endPoint, "endPoint");

            ThrowIfSocketError(LibZmq.zmq_bind(_zmqSocketPtr, endPoint));
        }

        public void Connect(string endPoint)
        {
            Argument.ExpectNonNullAndWhiteSpace(endPoint, "endPoint");

            ThrowIfSocketError(LibZmq.zmq_connect(_zmqSocketPtr, endPoint));
        }

        /// <summary>
        /// Sends the specified byte buffer into the socket.
        /// </summary>
        /// <param name="buffer">The bytes to send.</param>
        /// <param name="length">The length of the data to send.</param>
        /// <param name="doNotWait">If true, the send operation won't block for DEALER and PUSH sockets (it would normally block if there is no peer at the other end). In that case it'll return false instead of blocking.</param>
        /// <param name="hasMore">True if the message has more frames to send.</param>
        /// <returns>True if the buffer was sent successfully, false if it couldn't queue it in the socket and should be retried (it can happen only when doNotWait is true).</returns>
        public bool Send(byte[] buffer, int length, bool doNotWait = false, bool hasMore = false)
        {
            int sendFlags = (doNotWait ? ZMQ_DONTWAIT : 0) | (hasMore ? ZMQ_SNDMORE : 0);
            var zmqError = SocketError(Zmq.RetryIfInterrupted(LibZmq.zmq_send_func, _zmqSocketPtr, buffer, length, sendFlags));
            if (zmqError.ShouldTryAgain) return false;
            Zmq.ThrowIfError(zmqError);
            return true;
        }

        public virtual void Dispose(bool isDisposing)
        {
            if (_disposed) return;
            if (_zmqSocketPtr == IntPtr.Zero) return;

            ZmqError zmqError;
            if ((zmqError = SocketError(LibZmq.zmq_close(_zmqSocketPtr))) != null)
            {
                // We can't throw exception because we may be in a finally block.
                _logError(String.Format("ZmqSocket.Dispose(): {0}", zmqError));
            }

            _zmqSocketPtr = IntPtr.Zero;
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
