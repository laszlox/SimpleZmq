using SimpleZmq.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SimpleZmq
{
    /// <summary>
    /// Class wrapping a zmq socket.
    /// </summary>
    public class ZmqSocket : IDisposable
    {
        // send options
        private const int ZMQ_DONTWAIT = 1;
        private const int ZMQ_SNDMORE = 2;

        // socket options
        private const int ZMQ_AFFINITY = 4;
        private const int ZMQ_IDENTITY = 5;
        private const int ZMQ_SUBSCRIBE = 6;
        private const int ZMQ_UNSUBSCRIBE = 7;
        private const int ZMQ_RATE = 8;
        private const int ZMQ_RECOVERY_IVL = 9;
        private const int ZMQ_SNDBUF = 11;
        private const int ZMQ_RCVBUF = 12;
        private const int ZMQ_RCVMORE = 13;
        private const int ZMQ_FD = 14;
        private const int ZMQ_EVENTS = 15;
        private const int ZMQ_TYPE = 16;
        private const int ZMQ_LINGER = 17;
        private const int ZMQ_RECONNECT_IVL = 18;
        private const int ZMQ_BACKLOG = 19;
        private const int ZMQ_RECONNECT_IVL_MAX = 21;
        private const int ZMQ_MAXMSGSIZE = 22;
        private const int ZMQ_SNDHWM = 23;
        private const int ZMQ_RCVHWM = 24;
        private const int ZMQ_MULTICAST_HOPS = 25;
        private const int ZMQ_RCVTIMEO = 27;
        private const int ZMQ_SNDTIMEO = 28;
        private const int ZMQ_LAST_ENDPOINT = 32;
        private const int ZMQ_ROUTER_MANDATORY = 33;
        private const int ZMQ_TCP_KEEPALIVE = 34;
        private const int ZMQ_TCP_KEEPALIVE_CNT = 35;
        private const int ZMQ_TCP_KEEPALIVE_IDLE = 36;
        private const int ZMQ_TCP_KEEPALIVE_INTVL = 37;
        private const int ZMQ_TCP_ACCEPT_FILTER = 38;
        private const int ZMQ_IMMEDIATE = 39;
        private const int ZMQ_XPUB_VERBOSE = 40;
        private const int ZMQ_ROUTER_RAW = 41;
        private const int ZMQ_IPV6 = 42;
        private const int ZMQ_MECHANISM = 43;
        private const int ZMQ_PLAIN_SERVER = 44;
        private const int ZMQ_PLAIN_USERNAME = 45;
        private const int ZMQ_PLAIN_PASSWORD = 46;
        private const int ZMQ_CURVE_SERVER = 47;
        private const int ZMQ_CURVE_PUBLICKEY = 48;
        private const int ZMQ_CURVE_SECRETKEY = 49;
        private const int ZMQ_CURVE_SERVERKEY = 50;
        private const int ZMQ_PROBE_ROUTER = 51;
        private const int ZMQ_REQ_CORRELATE = 52;
        private const int ZMQ_REQ_RELAXED = 53;
        private const int ZMQ_CONFLATE = 54;
        private const int ZMQ_ZAP_DOMAIN = 55;

        private const int MaxBinaryOptionBufferSize = 255;

        private const int SizeOfMsgT = 32;

        private struct NativeAllocatedMemory : IDisposable
        {
            public static NativeAllocatedMemory Create(int size)
            {
                return new NativeAllocatedMemory { Pointer = Marshal.AllocHGlobal(size), Size = size };
            }

            public IntPtr Pointer { get; private set; }
            public int Size { get; private set; }

            public void Dispose()
            {
                if (Pointer != default(IntPtr)) Marshal.FreeHGlobal(Pointer);
            }
        }

        private static readonly byte[]          _allSubscription = new byte[0];

        private readonly ZmqContext             _zmqContext;
        private readonly NativeAllocatedMemory  _msg;
        private readonly Action<string>         _logError;
        private IntPtr                          _zmqSocketPtr;
        private bool                            _disposed;

        /// <summary>
        /// Finalizes (disposes) the zmq socket.
        /// </summary>
        ~ZmqSocket()
        {
            Dispose(false);
        }

        #region Private option setting/getting
        private NativeAllocatedMemory AllocateNativeMemoryForSizeBuffer(int valueBufferSize)
        {
            var sizeBuffer = NativeAllocatedMemory.Create(IntPtr.Size);
            if (sizeBuffer.Size == 4)
            {
                // 32 bit size
                Marshal.WriteInt32(sizeBuffer.Pointer, valueBufferSize);
            }
            else
            {
                // 64 bit size
                Marshal.WriteInt64(sizeBuffer.Pointer, valueBufferSize);
            }
            return sizeBuffer;
        }

        private void SetBufferOption(int optionType, byte[] value, int bufferSize = 0)
        {
            if (bufferSize > 0 && value.Length != bufferSize) throw new ArgumentException(String.Format("value's size is {0}, but it must be {1}.", value.Length, bufferSize));
            if (value.Length > MaxBinaryOptionBufferSize) throw new ArgumentException(String.Format("value's size is {0}, but it cannot be larger than {1}.", value.Length, MaxBinaryOptionBufferSize));

            using (var valueBuffer = NativeAllocatedMemory.Create(value.Length))
            {
                Marshal.Copy(value, 0, valueBuffer.Pointer, value.Length);
                Zmq.ThrowIfError_IgnoreContextTerminated(Zmq.RetryIfInterrupted(LibZmq.zmq_setsockopt_func, _zmqSocketPtr, optionType, valueBuffer.Pointer, valueBuffer.Size));
            }
        }

        private byte[] GetBufferOption(int optionType, int bufferSize = 0)
        {
            using (var valueBuffer = NativeAllocatedMemory.Create(bufferSize == 0 ? MaxBinaryOptionBufferSize : bufferSize))
            using (var sizeBuffer = AllocateNativeMemoryForSizeBuffer(valueBuffer.Size))
            {
                Zmq.ThrowIfError_IgnoreContextTerminated(Zmq.RetryIfInterrupted(LibZmq.zmq_getsockopt_func, _zmqSocketPtr, optionType, valueBuffer.Pointer, sizeBuffer.Pointer));
                // TODO heap-allocation: it's OK, because it can happen only at getting binary option values, which are rare and definitely not typical at the message sending/receiving.
                var value = new byte[Marshal.ReadInt32(sizeBuffer.Pointer)];
                Marshal.Copy(valueBuffer.Pointer, value, 0, value.Length);
                return value;
            }
        }

        private void SetInt32Option(int optionType, int value)
        {
            using (var valueBuffer = NativeAllocatedMemory.Create(Marshal.SizeOf(typeof(Int32))))
            {
                Marshal.WriteInt32(valueBuffer.Pointer, value);
                Zmq.ThrowIfError_IgnoreContextTerminated(Zmq.RetryIfInterrupted(LibZmq.zmq_setsockopt_func, _zmqSocketPtr, optionType, valueBuffer.Pointer, valueBuffer.Size));
            }
        }

        private int GetInt32Option(int optionType)
        {
            using (var valueBuffer = NativeAllocatedMemory.Create(Marshal.SizeOf(typeof(Int32))))
            using (var sizeBuffer = AllocateNativeMemoryForSizeBuffer(valueBuffer.Size))
            {
                Zmq.ThrowIfError_IgnoreContextTerminated(Zmq.RetryIfInterrupted(LibZmq.zmq_getsockopt_func, _zmqSocketPtr, optionType, valueBuffer.Pointer, sizeBuffer.Pointer));
                return Marshal.ReadInt32(valueBuffer.Pointer);
            }
        }

        private void SetLongOption(int optionType, long value)
        {
            using (var valueBuffer = NativeAllocatedMemory.Create(Marshal.SizeOf(typeof(long))))
            {
                Marshal.WriteInt64(valueBuffer.Pointer, value);
                Zmq.ThrowIfError_IgnoreContextTerminated(Zmq.RetryIfInterrupted(LibZmq.zmq_setsockopt_func, _zmqSocketPtr, optionType, valueBuffer.Pointer, valueBuffer.Size));
            }
        }

        private long GetLongOption(int optionType)
        {
            using (var valueBuffer = NativeAllocatedMemory.Create(Marshal.SizeOf(typeof(long))))
            using (var sizeBuffer = AllocateNativeMemoryForSizeBuffer(valueBuffer.Size))
            {
                Zmq.ThrowIfError_IgnoreContextTerminated(Zmq.RetryIfInterrupted(LibZmq.zmq_getsockopt_func, _zmqSocketPtr, optionType, valueBuffer.Pointer, sizeBuffer.Pointer));
                return Marshal.ReadInt64(valueBuffer.Pointer);
            }
        }

        private void SetUlongOption(int optionType, ulong value)
        {
            using (var valueBuffer = NativeAllocatedMemory.Create(Marshal.SizeOf(typeof(ulong))))
            {
                Marshal.WriteInt64(valueBuffer.Pointer, unchecked(Convert.ToInt64(value)));
                Zmq.ThrowIfError_IgnoreContextTerminated(Zmq.RetryIfInterrupted(LibZmq.zmq_setsockopt_func, _zmqSocketPtr, optionType, valueBuffer.Pointer, valueBuffer.Size));
            }
        }

        private ulong GetUlongOption(int optionType)
        {
            using (var valueBuffer = NativeAllocatedMemory.Create(Marshal.SizeOf(typeof(ulong))))
            using (var sizeBuffer = AllocateNativeMemoryForSizeBuffer(valueBuffer.Size))
            {
                Zmq.ThrowIfError_IgnoreContextTerminated(Zmq.RetryIfInterrupted(LibZmq.zmq_getsockopt_func, _zmqSocketPtr, optionType, valueBuffer.Pointer, sizeBuffer.Pointer));
                return unchecked(Convert.ToUInt64(Marshal.ReadInt64(valueBuffer.Pointer)));
            }
        }

        private void SetStringOption(int optionType, string value, int bufferSize = 0)
        {
            if (value == null)
            {
                Zmq.ThrowIfError_IgnoreContextTerminated(Zmq.RetryIfInterrupted(LibZmq.zmq_setsockopt_func, _zmqSocketPtr, optionType, IntPtr.Zero, 0));
            }
            else
            {
                if (bufferSize > 0 && value.Length != bufferSize) throw new ArgumentException(String.Format("value's length is {0}, but it must be a {1} long string.", value.Length, bufferSize));

                // TODO heap-allocation: it's OK, because it can happen only at getting binary option values, which are rare and definitely not typical at the message sending/receiving.
                var encodedString = Encoding.ASCII.GetBytes(value + "\x0");
                using (var valueBuffer = NativeAllocatedMemory.Create(encodedString.Length))
                {
                    Marshal.Copy(encodedString, 0, valueBuffer.Pointer, encodedString.Length);
                    Zmq.ThrowIfError_IgnoreContextTerminated(Zmq.RetryIfInterrupted(LibZmq.zmq_setsockopt_func, _zmqSocketPtr, optionType, valueBuffer.Pointer, valueBuffer.Size));
                }
            }
        }

        private string GetStringOption(int optionType, int bufferSize = 0)
        {
            using (var valueBuffer = NativeAllocatedMemory.Create(bufferSize == 0 ? MaxBinaryOptionBufferSize : bufferSize))
            using (var sizeBuffer = AllocateNativeMemoryForSizeBuffer(valueBuffer.Size))
            {
                Zmq.ThrowIfError_IgnoreContextTerminated(Zmq.RetryIfInterrupted(LibZmq.zmq_getsockopt_func, _zmqSocketPtr, optionType, valueBuffer.Pointer, sizeBuffer.Pointer));
                return Marshal.PtrToStringAnsi(valueBuffer.Pointer);
            }
        }
        #endregion

        /// <summary>
        /// Disposes the zmq socket.
        /// </summary>
        /// <param name="isDisposing">True if it's called from Dispose(), false if it's called from the finalizer.</param>
        protected virtual void Dispose(bool isDisposing)
        {
            if (_disposed) return;

            if (_zmqSocketPtr != IntPtr.Zero)
            {
                ZmqError zmqError;
                if ((zmqError = Zmq.Error(LibZmq.zmq_close(_zmqSocketPtr)).IgnoreContextTerminated()).IsError)
                {
                    // We can't throw exception because we may be in a finally block.
                    _logError(String.Format("ZmqSocket.Dispose(): {0}", zmqError));
                }
                _zmqSocketPtr = IntPtr.Zero;
            }

            _msg.Dispose();

            _disposed = true;
        }

        internal ZmqSocket(ZmqContext zmqContext, IntPtr zmqSocketPtr, Action<string> logError)
        {
            Argument.ExpectNonNull(zmqContext, "zmqContext");
            Argument.ExpectNonZero(zmqSocketPtr, "zmqSocketPtr");
            Argument.ExpectNonNull(logError, "logError");

            _zmqContext = zmqContext;
            _logError = logError;
            _msg = NativeAllocatedMemory.Create(SizeOfMsgT);
            _zmqSocketPtr = zmqSocketPtr;
        }

        internal IntPtr NativePtr
        {
            get { return _zmqSocketPtr; }
        }

        internal ZmqContext Context
        {
            get { return _zmqContext; }
        }

        /// <summary>
        /// Binds the socket to the specified endpoint.
        /// </summary>
        /// <param name="endPoint">The endpoint.</param>
        /// <remarks>
        /// Throws <see cref="ZmqException"/> in case of any non-context termination errors.
        /// </remarks>
        public void Bind(string endPoint)
        {
            Argument.ExpectNonNullAndWhiteSpace(endPoint, "endPoint");

            Zmq.ThrowIfError_IgnoreContextTerminated(LibZmq.zmq_bind(_zmqSocketPtr, endPoint));
        }

        /// <summary>
        /// Connects the socket to the specified endpoint.
        /// </summary>
        /// <param name="endPoint">The endpoint.</param>
        /// <remarks>
        /// Throws <see cref="ZmqException"/> in case of any non-context termination errors.
        /// </remarks>
        public void Connect(string endPoint)
        {
            Argument.ExpectNonNullAndWhiteSpace(endPoint, "endPoint");

            Zmq.ThrowIfError_IgnoreContextTerminated(LibZmq.zmq_connect(_zmqSocketPtr, endPoint));
        }

        /// <summary>
        /// Subscribes the SUB socket to the specified message-prefix.
        /// </summary>
        /// <param name="messagePrefix">The message-prefix to subscribe to.</param>
        /// <remarks>
        /// Throws <see cref="ZmqException"/> in case of any non-context termination errors.
        /// </remarks>
        public void Subscribe(byte[] messagePrefix)
        {
            Argument.ExpectNonNull(messagePrefix, "messagePrefix");

            SetBufferOption(ZMQ_SUBSCRIBE, messagePrefix);
        }

        /// <summary>
        /// Subscribes the SUB socket to all messages.
        /// </summary>
        /// <remarks>
        /// Throws <see cref="ZmqException"/> in case of any non-context termination errors.
        /// </remarks>
        public void SubscribeToAll()
        {
            SetBufferOption(ZMQ_SUBSCRIBE, _allSubscription);
        }

        /// <summary>
        /// Unsubscribes the SUB socket from the specified message-prefix.
        /// </summary>
        /// <param name="messagePrefix">The message-prefix to unsubscribe from.</param>
        /// <remarks>
        /// Throws <see cref="ZmqException"/> in case of any non-context termination errors.
        /// </remarks>
        public void Unsubscribe(byte[] messagePrefix)
        {
            Argument.ExpectNonNull(messagePrefix, "messagePrefix");

            SetBufferOption(ZMQ_UNSUBSCRIBE, messagePrefix);
        }

        /// <summary>
        /// Unsubscribes the SUB socket from all messages.
        /// </summary>
        /// <remarks>
        /// Throws <see cref="ZmqException"/> in case of any non-context termination errors.
        /// </remarks>
        public void UnsubscribeFromAll()
        {
            SetBufferOption(ZMQ_UNSUBSCRIBE, _allSubscription);
        }

        /// <summary>
        /// Starts monitoring the socket and returns the <see cref="ZmqSocketMonitor"/>. The returned monitor must be polled until it's stopped (even after the socket is disposed).
        /// </summary>
        /// <param name="eventsToMonitor">The events to monitor.</param>
        /// <returns>The monitor that must be polled until it's stopped.</returns>
        /// <remarks>
        /// Throws <see cref="ZmqException"/> in case of any errors.
        /// </remarks>
        public ZmqSocketMonitor Monitor(ZmqSocketMonitorEvent eventsToMonitor = ZmqSocketMonitorEvent.All)
        {
            return new ZmqSocketMonitor(this, eventsToMonitor, _logError);
        }

        /// <summary>
        /// Sends the specified byte buffer into the socket.
        /// </summary>
        /// <param name="buffer">The bytes to send.</param>
        /// <param name="length">The length of the data to send.</param>
        /// <param name="hasMore">True if the message has more frames to send.</param>
        /// <param name="doNotWait">If true, the send operation won't block for DEALER and PUSH sockets (it would normally block if there is no peer at the other end). In that case it'll return false instead of blocking.</param>
        /// <returns>True if the buffer was sent successfully, false if it couldn't queue it in the socket and should be retried (it can happen only when doNotWait is true).</returns>
        /// <remarks>
        /// Throws <see cref="ZmqException"/> in case of any non-context termination errors.
        /// </remarks>
        public bool Send(byte[] buffer, int length, bool hasMore = false, bool doNotWait = false)
        {
            Argument.ExpectNonNull(buffer, "buffer");
            Argument.ExpectGreaterOrEqualThanZero(length, "length");

            int sendFlags = (doNotWait ? ZMQ_DONTWAIT : 0) | (hasMore ? ZMQ_SNDMORE : 0);
            return Zmq.ThrowIfError_IgnoreContextTerminated(
                Zmq.RetryIfInterrupted(LibZmq.zmq_send_func, _zmqSocketPtr, buffer, length, sendFlags),
                expectTryAgain: true
            ) != null;
        }

        /// <summary>
        /// Receives a message frame from this socket.
        /// </summary>
        /// <param name="buffer">The buffer to copy the message content into.</param>
        /// <param name="length">Out: the length of the received data.</param>
        /// <param name="doNotWait">If true, the receive won't block if there is nothing to receive, but return null. Otherwise it blocks.</param>
        /// <returns>The byte[] that contains the resulting message. If the input byte[] wasn't large enough, a new one is allocated and returned.</returns>
        /// <remarks>
        /// Throws <see cref="ZmqException"/> in case of any non-context termination errors.
        /// </remarks>
        public byte[] Receive(byte[] buffer, out int length, bool doNotWait = false)
        {
            Zmq.ThrowIfError(LibZmq.zmq_msg_init(_msg.Pointer));
            try
            {
                var lengthOrRetry = Zmq.ThrowIfError_IgnoreContextTerminated(
                    Zmq.RetryIfInterrupted(LibZmq.zmq_msg_recv_func, _msg.Pointer, _zmqSocketPtr, doNotWait ? ZMQ_DONTWAIT : 0),
                    expectTryAgain: true
                );

                length = 0;
                // if we should retry, just return null.
                if (lengthOrRetry == null) return null;
                length = lengthOrRetry.Value;

                // recreating the buffer if it's not large enough
                if (buffer == null || buffer.Length < length)
                {
                    buffer = new byte[length];
                }

                // finally copying the content from the msg structure into the byte buffer
                Marshal.Copy(LibZmq.zmq_msg_data(_msg.Pointer), buffer, 0, length);
            }
            finally
            {
                Zmq.ThrowIfError(LibZmq.zmq_msg_close(_msg.Pointer));
            }

            return buffer;
        }

        #region Socket Option Properties
        /// <summary>
        /// Gets the socket type.
        /// </summary>
        public ZmqSocketType SocketType
        {
            get { return (ZmqSocketType)GetInt32Option(ZMQ_TYPE); }
        }

        /// <summary>
        /// Gets a value indicating whether the socket has anything more to receive without blocking.
        /// </summary>
        public bool HasMoreToReceive
        {
            get { return GetInt32Option(ZMQ_RCVMORE) == 1; }
        }

        /// <summary>
        /// Gets or sets the ZMQ_SNDHWM option.
        /// </summary>
        public int SendHWM
        {
            get { return GetInt32Option(ZMQ_SNDHWM); }
            set { SetInt32Option(ZMQ_SNDHWM, value); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_RCVHWM option.
        /// </summary>
        public int ReceiveHWM
        {
            get { return GetInt32Option(ZMQ_RCVHWM); }
            set { SetInt32Option(ZMQ_RCVHWM, value); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_AFFINITY option.
        /// </summary>
        public ulong Affinity
        {
            get { return GetUlongOption(ZMQ_AFFINITY); }
            set { SetUlongOption(ZMQ_AFFINITY, value); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_IDENTITY option.
        /// </summary>
        public byte[] Identity
        {
            get { return GetBufferOption(ZMQ_IDENTITY); }
            set { SetBufferOption(ZMQ_IDENTITY, value); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_RATE option.
        /// </summary>
        public int Rate
        {
            get { return GetInt32Option(ZMQ_RATE); }
            set { SetInt32Option(ZMQ_RATE, value); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_RECOVERY_IVL option.
        /// </summary>
        public int RecoveryInterval
        {
            get { return GetInt32Option(ZMQ_RECOVERY_IVL); }
            set { SetInt32Option(ZMQ_RECOVERY_IVL, value); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_SNDBUF option.
        /// </summary>
        public int SendBufferSize
        {
            get { return GetInt32Option(ZMQ_SNDBUF); }
            set { SetInt32Option(ZMQ_SNDBUF, value); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_RCVBUF option.
        /// </summary>
        public int ReceiveBufferSize
        {
            get { return GetInt32Option(ZMQ_RCVBUF); }
            set { SetInt32Option(ZMQ_RCVBUF, value); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_LINGER option.
        /// </summary>
        public int Linger
        {
            get { return GetInt32Option(ZMQ_LINGER); }
            set { SetInt32Option(ZMQ_LINGER, value); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_RECONNECT_IVL option.
        /// </summary>
        public int ReconnectInterval
        {
            get { return GetInt32Option(ZMQ_RECONNECT_IVL); }
            set { SetInt32Option(ZMQ_RECONNECT_IVL, value); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_RECONNECT_IVL_MAX option.
        /// </summary>
        public int MaxReconnectInterval
        {
            get { return GetInt32Option(ZMQ_RECONNECT_IVL_MAX); }
            set { SetInt32Option(ZMQ_RECONNECT_IVL_MAX, value); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_BACKLOG option.
        /// </summary>
        public int BackLog
        {
            get { return GetInt32Option(ZMQ_BACKLOG); }
            set { SetInt32Option(ZMQ_BACKLOG, value); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_MAXMSGSIZE option.
        /// </summary>
        public long MaxMessageSize
        {
            get { return GetLongOption(ZMQ_MAXMSGSIZE); }
            set { SetLongOption(ZMQ_MAXMSGSIZE, value); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_MULTICAST_HOPS option.
        /// </summary>
        public int MulticastHops
        {
            get { return GetInt32Option(ZMQ_MULTICAST_HOPS); }
            set { SetInt32Option(ZMQ_MULTICAST_HOPS, value); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_RCVTIMEO option.
        /// </summary>
        public int ReceiveTimeOut
        {
            get { return GetInt32Option(ZMQ_RCVTIMEO); }
            set { SetInt32Option(ZMQ_RCVTIMEO, value); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_SNDTIMEO option.
        /// </summary>
        public int SendTimeOut
        {
            get { return GetInt32Option(ZMQ_SNDTIMEO); }
            set { SetInt32Option(ZMQ_SNDTIMEO, value); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_IPV6 option.
        /// </summary>
        public bool IPv6Enabled
        {
            get { return GetInt32Option(ZMQ_IPV6) == 1; }
            set { SetInt32Option(ZMQ_IPV6, value ? 1 : 0); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_IMMEDIATE option.
        /// </summary>
        public bool Immediate
        {
            get { return GetInt32Option(ZMQ_IMMEDIATE) == 1; }
            set { SetInt32Option(ZMQ_IMMEDIATE, value ? 1 : 0); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_LAST_ENDPOINT option.
        /// </summary>
        public string LastEndPoint
        {
            get { return GetStringOption(ZMQ_LAST_ENDPOINT); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_TCP_KEEPALIVE option.
        /// </summary>
        public int TcpKeepAlive
        {
            get { return GetInt32Option(ZMQ_TCP_KEEPALIVE); }
            set { SetInt32Option(ZMQ_TCP_KEEPALIVE, value); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_TCP_KEEPALIVE_IDLE option.
        /// </summary>
        public int TcpKeepAliveIdle
        {
            get { return GetInt32Option(ZMQ_TCP_KEEPALIVE_IDLE); }
            set { SetInt32Option(ZMQ_TCP_KEEPALIVE_IDLE, value); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_TCP_KEEPALIVE_CNT option.
        /// </summary>
        public int TcpKeepAliveCnt
        {
            get { return GetInt32Option(ZMQ_TCP_KEEPALIVE_CNT); }
            set { SetInt32Option(ZMQ_TCP_KEEPALIVE_CNT, value); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_TCP_KEEPALIVE_INTVL option.
        /// </summary>
        public int TcpKeepAliveIntVl
        {
            get { return GetInt32Option(ZMQ_TCP_KEEPALIVE_INTVL); }
            set { SetInt32Option(ZMQ_TCP_KEEPALIVE_INTVL, value); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_MECHANISM option.
        /// </summary>
        public ZmqSocketSecurityMechanism SecurityMechanism
        {
            get { return (ZmqSocketSecurityMechanism)GetInt32Option(ZMQ_MECHANISM); }
            // TODO does it need a setter for ZMQ_MECHANISM?
            set { SetInt32Option(ZMQ_MECHANISM, (int)value); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_PLAIN_SERVER option.
        /// </summary>
        public int PlainServer
        {
            get { return GetInt32Option(ZMQ_PLAIN_SERVER); }
            set { SetInt32Option(ZMQ_PLAIN_SERVER, value); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_PLAIN_USERNAME option.
        /// </summary>
        public string PlainUserName
        {
            get { return GetStringOption(ZMQ_PLAIN_USERNAME); }
            set { SetStringOption(ZMQ_PLAIN_USERNAME, value); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_PLAIN_PASSWORD option.
        /// </summary>
        public string PlainPassword
        {
            get { return GetStringOption(ZMQ_PLAIN_PASSWORD); }
            set { SetStringOption(ZMQ_PLAIN_PASSWORD, value); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_CURVE_PUBLICKEY option.
        /// </summary>
        public byte[] CurvePublicKey
        {
            get { return GetBufferOption(ZMQ_CURVE_PUBLICKEY, 32); }
            set { SetBufferOption(ZMQ_CURVE_PUBLICKEY, value, 32); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_CURVE_PUBLICKEY option.
        /// </summary>
        public string CurvePublicKeyString
        {
            get { return GetStringOption(ZMQ_CURVE_PUBLICKEY, 41); }
            set { SetStringOption(ZMQ_CURVE_PUBLICKEY, value, 41); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_CURVE_SECRETKEY option.
        /// </summary>
        public byte[] CurveSecretKey
        {
            get { return GetBufferOption(ZMQ_CURVE_SECRETKEY, 32); }
            set { SetBufferOption(ZMQ_CURVE_SECRETKEY, value, 32); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_CURVE_SECRETKEY option.
        /// </summary>
        public string CurveSecretKeyString
        {
            get { return GetStringOption(ZMQ_CURVE_SECRETKEY, 41); }
            set { SetStringOption(ZMQ_CURVE_SECRETKEY, value, 41); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_CURVE_SERVERKEY option.
        /// </summary>
        public byte[] CurveServerKey
        {
            get { return GetBufferOption(ZMQ_CURVE_SERVERKEY, 32); }
            set { SetBufferOption(ZMQ_CURVE_SERVERKEY, value, 32); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_CURVE_SERVERKEY option.
        /// </summary>
        public string CurveServerKeyString
        {
            get { return GetStringOption(ZMQ_CURVE_SERVERKEY, 41); }
            set { SetStringOption(ZMQ_CURVE_SERVERKEY, value, 41); }
        }

        /// <summary>
        /// Gets or sets the ZMQ_ZAP_DOMAIN option.
        /// </summary>
        public string ZAPDomain
        {
            get { return GetStringOption(ZMQ_ZAP_DOMAIN); }
            // TODO do we need a setter for ZAPDomain?
            set { SetStringOption(ZMQ_ZAP_DOMAIN, value); }
        }

        #endregion

        /// <summary>
        /// Disposes the zmq socket.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
