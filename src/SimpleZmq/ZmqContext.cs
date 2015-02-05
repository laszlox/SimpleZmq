using SimpleZmq.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleZmq
{
    /// <summary>
    /// Class wrapping a native libzmq context.
    /// </summary>
    public class ZmqContext : IDisposable
    {
        private const int ZMQ_IO_THREADS = 1;
        private const int ZMQ_MAX_SOCKETS = 2;
        private const int ZMQ_IPV6 = 42;

        private readonly Action<string> _logError;
        private IntPtr                  _zmqContextPtr;
        private bool                    _disposed;

        /// <summary>
        /// The default number of the zmq io threads.
        /// </summary>
        public const int DefaultNumberOfIoThreads = 1;

        /// <summary>
        /// The default maximum number of zmq sockets.
        /// </summary>
        public const int DefaultMaxNumberOfSockets = 1023;

        /// <summary>
        /// Disposes the zmq context.
        /// </summary>
        /// <param name="isDisposing">True if it's called from Dispose(), false if it's called from the finalizer.</param>
        protected virtual void Dispose(bool isDisposing)
        {
            if (_disposed) return;
            if (_zmqContextPtr == IntPtr.Zero) return;

            ZmqError zmqError;
            while ((zmqError = Zmq.Error(LibZmq.zmq_ctx_term(_zmqContextPtr))).IsError)
            {
                if (zmqError.Number != ZmqErrNo.EAGAIN)
                {
                    // it must be EFAULT, we can't do too much about it. We can't throw exception because we may be in a finally block.
                    _logError(String.Format("ZmqContext.Dispose(): {0}", zmqError));
                    return;
                }
                // if EAGAIN, just retry.
            }

            _zmqContextPtr = IntPtr.Zero;
            _disposed = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZmqContext"/> class.
        /// </summary>
        /// <param name="logError">The optional error logging delegate. It's used to log errors, when throwing exception is not possible.</param>
        public ZmqContext(Action<string> logError = null)
        {
            _logError = logError ?? (s => {});
            _zmqContextPtr = Zmq.ThrowIfError(LibZmq.zmq_ctx_new());
        }

        /// <summary>
        /// Gets or sets the number of zmq io threads.
        /// </summary>
        public int NumberOfIoThreads
        {
            get { return Zmq.ThrowIfError(LibZmq.zmq_ctx_get(_zmqContextPtr, ZMQ_IO_THREADS)); }
            set { Zmq.ThrowIfError(LibZmq.zmq_ctx_set(_zmqContextPtr, ZMQ_IO_THREADS, value)); }
        }

        /// <summary>
        /// Gets or sets the maximum number of zmq sockets.
        /// </summary>
        public int MaxNumberOfSockets
        {
            get { return Zmq.ThrowIfError(LibZmq.zmq_ctx_get(_zmqContextPtr, ZMQ_MAX_SOCKETS)); }
            set { Zmq.ThrowIfError(LibZmq.zmq_ctx_set(_zmqContextPtr, ZMQ_MAX_SOCKETS, value)); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether IPv6 is enabled for the sockets created afterwards or not.
        /// </summary>
        public bool IPv6
        {
            get { return Zmq.ThrowIfError(LibZmq.zmq_ctx_get(_zmqContextPtr, ZMQ_IPV6)) == 1; }
            set { Zmq.ThrowIfError(LibZmq.zmq_ctx_set(_zmqContextPtr, ZMQ_IPV6, value ? 1 : 0)); }
        }

        /// <summary>
        /// Creates a socket of the specified type.
        /// </summary>
        /// <param name="socketType">The type of the socket.</param>
        /// <returns>The socket of the specified type.</returns>
        public ZmqSocket CreateSocket(ZmqSocketType socketType)
        {
            return new ZmqSocket(this, Zmq.ThrowIfError(LibZmq.zmq_socket(_zmqContextPtr, (int)socketType)), _logError);
        }

        /// <summary>
        /// Finalizes (disposes) the zmq context.
        /// </summary>
        ~ZmqContext()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disposes the zmq context.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
