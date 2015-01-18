using SimpleZmq.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SimpleZmq
{
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

        private void SetBufferOption(int optionType, byte[] value, int bufferSize = 0)
        {
            if (bufferSize > 0 && value.Length != bufferSize) throw new ArgumentException(String.Format("value's size is {0}, but it must be {1}.", value.Length, bufferSize));
            if (value.Length > MaxBinaryOptionBufferSize) throw new ArgumentException(String.Format("value's size is {0}, but it cannot be larger than {1}.", value.Length, MaxBinaryOptionBufferSize));

            using (var valueBuffer = NativeAllocatedMemory.Create(value.Length))
            {
                Marshal.Copy(value, 0, valueBuffer.Pointer, value.Length);
                ThrowIfSocketError(Zmq.RetryIfInterrupted(LibZmq.zmq_setsockopt_func, _zmqSocketPtr, optionType, valueBuffer.Pointer, valueBuffer.Size));
            }
        }

        private byte[] GetBufferOption(int optionType, int bufferSize = 0)
        {
            using (var sizeBuffer = NativeAllocatedMemory.Create(IntPtr.Size))
            using (var valueBuffer = NativeAllocatedMemory.Create(bufferSize == 0 ? MaxBinaryOptionBufferSize : bufferSize))
            {
                // TODO is it 32 for 64bit?
                Marshal.WriteInt32(sizeBuffer.Pointer, valueBuffer.Size);

                ThrowIfSocketError(Zmq.RetryIfInterrupted(LibZmq.zmq_getsockopt_func, _zmqSocketPtr, optionType, valueBuffer.Pointer, sizeBuffer.Pointer));
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
                ThrowIfSocketError(Zmq.RetryIfInterrupted(LibZmq.zmq_setsockopt_func, _zmqSocketPtr, optionType, valueBuffer.Pointer, valueBuffer.Size));
            }
        }

        private int GetInt32Option(int optionType)
        {
            using (var sizeBuffer = NativeAllocatedMemory.Create(IntPtr.Size))
            using (var valueBuffer = NativeAllocatedMemory.Create(Marshal.SizeOf(typeof(Int32))))
            {
                // TODO is it 32 for 64bit?
                Marshal.WriteInt32(sizeBuffer.Pointer, valueBuffer.Size);

                ThrowIfSocketError(Zmq.RetryIfInterrupted(LibZmq.zmq_getsockopt_func, _zmqSocketPtr, optionType, valueBuffer.Pointer, sizeBuffer.Pointer));
                return Marshal.ReadInt32(valueBuffer.Pointer);
            }
        }

        private void SetLongOption(int optionType, long value)
        {
            using (var valueBuffer = NativeAllocatedMemory.Create(Marshal.SizeOf(typeof(long))))
            {
                Marshal.WriteInt64(valueBuffer.Pointer, value);
                ThrowIfSocketError(Zmq.RetryIfInterrupted(LibZmq.zmq_setsockopt_func, _zmqSocketPtr, optionType, valueBuffer.Pointer, valueBuffer.Size));
            }
        }

        private long GetLongOption(int optionType)
        {
            using (var sizeBuffer = NativeAllocatedMemory.Create(IntPtr.Size))
            using (var valueBuffer = NativeAllocatedMemory.Create(Marshal.SizeOf(typeof(long))))
            {
                // TODO is it 32 for 64bit?
                Marshal.WriteInt32(sizeBuffer.Pointer, valueBuffer.Size);

                ThrowIfSocketError(Zmq.RetryIfInterrupted(LibZmq.zmq_getsockopt_func, _zmqSocketPtr, optionType, valueBuffer.Pointer, sizeBuffer.Pointer));
                return Marshal.ReadInt64(valueBuffer.Pointer);
            }
        }

        private void SetUlongOption(int optionType, ulong value)
        {
            using (var valueBuffer = NativeAllocatedMemory.Create(Marshal.SizeOf(typeof(ulong))))
            {
                Marshal.WriteInt64(valueBuffer.Pointer, unchecked(Convert.ToInt64(value)));
                ThrowIfSocketError(Zmq.RetryIfInterrupted(LibZmq.zmq_setsockopt_func, _zmqSocketPtr, optionType, valueBuffer.Pointer, valueBuffer.Size));
            }
        }

        private ulong GetUlongOption(int optionType)
        {
            using (var sizeBuffer = NativeAllocatedMemory.Create(IntPtr.Size))
            using (var valueBuffer = NativeAllocatedMemory.Create(Marshal.SizeOf(typeof(ulong))))
            {
                // TODO is it 32 for 64bit?
                Marshal.WriteInt32(sizeBuffer.Pointer, valueBuffer.Size);

                ThrowIfSocketError(Zmq.RetryIfInterrupted(LibZmq.zmq_getsockopt_func, _zmqSocketPtr, optionType, valueBuffer.Pointer, sizeBuffer.Pointer));
                return unchecked(Convert.ToUInt64(Marshal.ReadInt64(valueBuffer.Pointer)));
            }
        }

        private void SetStringOption(int optionType, string value, int bufferSize = 0)
        {
            if (value == null)
            {
                ThrowIfSocketError(Zmq.RetryIfInterrupted(LibZmq.zmq_setsockopt_func, _zmqSocketPtr, optionType, IntPtr.Zero, 0));
            }
            else
            {
                if (bufferSize > 0 && value.Length != bufferSize) throw new ArgumentException(String.Format("value's length is {0}, but it must be a {1} long string.", value.Length, bufferSize));

                // TODO heap-allocation: it's OK, because it can happen only at getting binary option values, which are rare and definitely not typical at the message sending/receiving.
                var encodedString = Encoding.ASCII.GetBytes(value + "\x0");
                using (var valueBuffer = NativeAllocatedMemory.Create(encodedString.Length))
                {
                    Marshal.Copy(encodedString, 0, valueBuffer.Pointer, encodedString.Length);
                    ThrowIfSocketError(Zmq.RetryIfInterrupted(LibZmq.zmq_setsockopt_func, _zmqSocketPtr, optionType, valueBuffer.Pointer, valueBuffer.Size));
                }
            }
        }

        private string GetStringOption(int optionType, int bufferSize = 0)
        {
            using (var sizeBuffer = NativeAllocatedMemory.Create(IntPtr.Size))
            using (var valueBuffer = NativeAllocatedMemory.Create(bufferSize == 0 ? MaxBinaryOptionBufferSize : bufferSize))
            {
                // TODO is it 32 for 64bit?
                Marshal.WriteInt32(sizeBuffer.Pointer, valueBuffer.Size);

                ThrowIfSocketError(Zmq.RetryIfInterrupted(LibZmq.zmq_getsockopt_func, _zmqSocketPtr, optionType, valueBuffer.Pointer, sizeBuffer.Pointer));
                return Marshal.PtrToStringAnsi(valueBuffer.Pointer);
            }
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
            if (zmqError != null && zmqError.ShouldTryAgain) return false;
            Zmq.ThrowIfError(zmqError);
            return true;
        }

        #region Socket Option Properties
        public SocketType SocketType
        {
            get { return (SocketType)GetInt32Option(ZMQ_TYPE); }
        }

        public bool HasMoreToReceive
        {
            get { return GetInt32Option(ZMQ_RCVMORE) == 1; }
        }

        public int SendHWM
        {
            get { return GetInt32Option(ZMQ_SNDHWM); }
            set { SetInt32Option(ZMQ_SNDHWM, value); }
        }

        public int ReceiveHWM
        {
            get { return GetInt32Option(ZMQ_RCVHWM); }
            set { SetInt32Option(ZMQ_RCVHWM, value); }
        }

        public ulong Affinity
        {
            get { return GetUlongOption(ZMQ_AFFINITY); }
            set { SetUlongOption(ZMQ_AFFINITY, value); }
        }

        public byte[] Identity
        {
            get { return GetBufferOption(ZMQ_IDENTITY); }
            set { SetBufferOption(ZMQ_IDENTITY, value); }
        }

        public int Rate
        {
            get { return GetInt32Option(ZMQ_RATE); }
            set { SetInt32Option(ZMQ_RATE, value); }
        }

        public int RecoveryInterval
        {
            get { return GetInt32Option(ZMQ_RECOVERY_IVL); }
            set { SetInt32Option(ZMQ_RECOVERY_IVL, value); }
        }

        public int SendBufferSize
        {
            get { return GetInt32Option(ZMQ_SNDBUF); }
            set { SetInt32Option(ZMQ_SNDBUF, value); }
        }

        public int ReceiveBufferSize
        {
            get { return GetInt32Option(ZMQ_RCVBUF); }
            set { SetInt32Option(ZMQ_RCVBUF, value); }
        }

        public int Linger
        {
            get { return GetInt32Option(ZMQ_LINGER); }
            set { SetInt32Option(ZMQ_LINGER, value); }
        }

        public int ReconnectInterval
        {
            get { return GetInt32Option(ZMQ_RECONNECT_IVL); }
            set { SetInt32Option(ZMQ_RECONNECT_IVL, value); }
        }

        public int MaxReconnectInterval
        {
            get { return GetInt32Option(ZMQ_RECONNECT_IVL_MAX); }
            set { SetInt32Option(ZMQ_RECONNECT_IVL_MAX, value); }
        }

        public int BackLog
        {
            get { return GetInt32Option(ZMQ_BACKLOG); }
            set { SetInt32Option(ZMQ_BACKLOG, value); }
        }

        public long MaxMessageSize
        {
            get { return GetLongOption(ZMQ_MAXMSGSIZE); }
            set { SetLongOption(ZMQ_MAXMSGSIZE, value); }
        }

        public int MulticastHops
        {
            get { return GetInt32Option(ZMQ_MULTICAST_HOPS); }
            set { SetInt32Option(ZMQ_MULTICAST_HOPS, value); }
        }

        public int ReceiveTimeOut
        {
            get { return GetInt32Option(ZMQ_RCVTIMEO); }
            set { SetInt32Option(ZMQ_RCVTIMEO, value); }
        }

        public int SendTimeOut
        {
            get { return GetInt32Option(ZMQ_SNDTIMEO); }
            set { SetInt32Option(ZMQ_SNDTIMEO, value); }
        }

        public bool IPv6Enabled
        {
            get { return GetInt32Option(ZMQ_IPV6) == 1; }
            set { SetInt32Option(ZMQ_IPV6, value ? 1 : 0); }
        }

        public bool Immediate
        {
            get { return GetInt32Option(ZMQ_IMMEDIATE) == 1; }
            set { SetInt32Option(ZMQ_IMMEDIATE, value ? 1 : 0); }
        }

        public string LastEndPoint
        {
            get { return GetStringOption(ZMQ_LAST_ENDPOINT); }
        }

        public int TcpKeepAlive
        {
            get { return GetInt32Option(ZMQ_TCP_KEEPALIVE); }
            set { SetInt32Option(ZMQ_TCP_KEEPALIVE, value); }
        }

        public int TcpKeepAliveIdle
        {
            get { return GetInt32Option(ZMQ_TCP_KEEPALIVE_IDLE); }
            set { SetInt32Option(ZMQ_TCP_KEEPALIVE_IDLE, value); }
        }

        public int TcpKeepAliveCnt
        {
            get { return GetInt32Option(ZMQ_TCP_KEEPALIVE_CNT); }
            set { SetInt32Option(ZMQ_TCP_KEEPALIVE_CNT, value); }
        }

        public int TcpKeepAliveIntVl
        {
            get { return GetInt32Option(ZMQ_TCP_KEEPALIVE_INTVL); }
            set { SetInt32Option(ZMQ_TCP_KEEPALIVE_INTVL, value); }
        }

        public SocketSecurityMechanism SecurityMechanism
        {
            get { return (SocketSecurityMechanism)GetInt32Option(ZMQ_MECHANISM); }
            // TODO does it need a setter for ZMQ_MECHANISM?
            set { SetInt32Option(ZMQ_MECHANISM, (int)value); }
        }

        public int PlainServer
        {
            get { return GetInt32Option(ZMQ_PLAIN_SERVER); }
            set { SetInt32Option(ZMQ_PLAIN_SERVER, value); }
        }

        public string PlainUserName
        {
            get { return GetStringOption(ZMQ_PLAIN_USERNAME); }
            set { SetStringOption(ZMQ_PLAIN_USERNAME, value); }
        }

        public string PlainPassword
        {
            get { return GetStringOption(ZMQ_PLAIN_PASSWORD); }
            set { SetStringOption(ZMQ_PLAIN_PASSWORD, value); }
        }

        public byte[] CurvePublicKey
        {
            get { return GetBufferOption(ZMQ_CURVE_PUBLICKEY, 32); }
            set { SetBufferOption(ZMQ_CURVE_PUBLICKEY, value, 32); }
        }

        public string CurvePublicKeyString
        {
            get { return GetStringOption(ZMQ_CURVE_PUBLICKEY, 41); }
            set { SetStringOption(ZMQ_CURVE_PUBLICKEY, value, 41); }
        }

        public byte[] CurveSecretKey
        {
            get { return GetBufferOption(ZMQ_CURVE_SECRETKEY, 32); }
            set { SetBufferOption(ZMQ_CURVE_SECRETKEY, value, 32); }
        }

        public string CurveSecretKeyString
        {
            get { return GetStringOption(ZMQ_CURVE_SECRETKEY, 41); }
            set { SetStringOption(ZMQ_CURVE_SECRETKEY, value, 41); }
        }

        public byte[] CurveServerKey
        {
            get { return GetBufferOption(ZMQ_CURVE_SERVERKEY, 32); }
            set { SetBufferOption(ZMQ_CURVE_SERVERKEY, value, 32); }
        }

        public string CurveServerKeyString
        {
            get { return GetStringOption(ZMQ_CURVE_SERVERKEY, 41); }
            set { SetStringOption(ZMQ_CURVE_SERVERKEY, value, 41); }
        }

        public string ZAPDomain
        {
            get { return GetStringOption(ZMQ_ZAP_DOMAIN); }
            // TODO do we need a setter for ZAPDomain?
            set { SetStringOption(ZMQ_ZAP_DOMAIN, value); }
        }

        #endregion

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
