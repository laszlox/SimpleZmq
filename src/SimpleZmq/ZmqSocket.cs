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
        private readonly Action<string> _logError;
        private IntPtr                  _zmqSocketPtr;
        private bool                    _disposed;

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
            if (zmqSocketPtr == IntPtr.Zero) throw new ArgumentNullException("zmqSocketPtr");
            if (logError == null) throw new ArgumentNullException("logError");

            _logError = logError;
            _zmqSocketPtr = zmqSocketPtr;
        }

        public void Bind(string endPoint)
        {
            ThrowIfSocketError(LibZmq.zmq_bind(_zmqSocketPtr, endPoint));
        }

        public void Connect(string endPoint)
        {
            ThrowIfSocketError(LibZmq.zmq_connect(_zmqSocketPtr, endPoint));
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
