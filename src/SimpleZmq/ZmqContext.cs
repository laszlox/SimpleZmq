using SimpleZmq.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleZmq
{
    public class ZmqContext : IDisposable
    {
        private const int ZMQ_IO_THREADS = 1;
        private const int ZMQ_MAX_SOCKETS = 2;
        private const int ZMQ_IPV6 = 42;

        private IntPtr _zmqContextPtr;
        private bool   _disposed;

        public const int DefaultNumberOfIoThreads = 1;
        public const int DefaultMaxNumberOfSockets = 1023;

        public ZmqContext()
        {
            _zmqContextPtr = Zmq.ThrowIfError(LibZmq.zmq_ctx_new());
        }

        public int NumberOfIoThreads
        {
            get { return Zmq.ThrowIfError(LibZmq.zmq_ctx_get(_zmqContextPtr, ZMQ_IO_THREADS)); }
            set { Zmq.ThrowIfError(LibZmq.zmq_ctx_set(_zmqContextPtr, ZMQ_IO_THREADS, value)); }
        }

        public int MaxNumberOfSockets
        {
            get { return Zmq.ThrowIfError(LibZmq.zmq_ctx_get(_zmqContextPtr, ZMQ_MAX_SOCKETS)); }
            set { Zmq.ThrowIfError(LibZmq.zmq_ctx_set(_zmqContextPtr, ZMQ_MAX_SOCKETS, value)); }
        }

        public bool IPv6
        {
            get { return Zmq.ThrowIfError(LibZmq.zmq_ctx_get(_zmqContextPtr, ZMQ_IPV6)) == 1; }
            set { Zmq.ThrowIfError(LibZmq.zmq_ctx_set(_zmqContextPtr, ZMQ_IPV6, value ? 1 : 0)); }
        }

        public ZmqSocket CreateSocket(SocketType socketType)
        {
            return new ZmqSocket(Zmq.ThrowIfError(LibZmq.zmq_socket(_zmqContextPtr, (int)socketType)));
        }

        ~ZmqContext()
        {
            Dispose(false);
        }

        public virtual void Dispose(bool isDisposing)
        {
            if (_disposed) return;
            if (_zmqContextPtr == IntPtr.Zero) return;

            int errNo;
            string errString;
            while (Zmq.ErrorNumberWithDescription(LibZmq.zmq_ctx_term(_zmqContextPtr), out errNo, out errString))
            {
                if (errNo != ErrNo.EAGAIN)
                {
                    // it must be EFAULT, we can't do too much about it.
                    // TODO log the error. We can't throw exception because we may be in a finally block.
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
