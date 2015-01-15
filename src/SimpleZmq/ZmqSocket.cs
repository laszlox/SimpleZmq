using SimpleZmq.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleZmq
{
    public class ZmqSocket : IDisposable
    {
        private IntPtr  _zmqSocketPtr;
        private bool    _disposed;

        internal ZmqSocket(IntPtr zmqSocketPtr)
        {
            _zmqSocketPtr = zmqSocketPtr;
        }

        ~ZmqSocket()
        {
            Dispose(false);
        }

        public virtual void Dispose(bool isDisposing)
        {
            if (_disposed) return;
            if (_zmqSocketPtr == IntPtr.Zero) return;

            int errNo;
            string errString;
            if (Zmq.ErrorNumberWithDescription(LibZmq.zmq_close(_zmqSocketPtr), out errNo, out errString))
            {
                // TODO log the error. We can't throw exception because we may be in a finally block.
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
