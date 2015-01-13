using SimpleZmq.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleZmq
{
    public class ZmqContext : IDisposable
    {
        private IntPtr _zmqContextPtr;
        private bool   _disposed;

        public ZmqContext()
        {
            _zmqContextPtr = LibZmq.zmq_ctx_new();
        }

        ~ZmqContext()
        {
            Dispose(false);
        }

        public virtual void Dispose(bool isDisposing)
        {
            if (_disposed) return;
            if (_zmqContextPtr == IntPtr.Zero) return;

            while (LibZmq.zmq_ctx_term(_zmqContextPtr) != 0)
            {
                int errNo = LibZmq.zmq_errno();
                if (errNo != LibZmq.ErrNo.EAGAIN)
                {
                    // it must be EFAULT, we can't do too much about it.
                    return;
                }
                // if EAGAIN, just retry.
            }

            _zmqContextPtr = IntPtr.Zero;
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
