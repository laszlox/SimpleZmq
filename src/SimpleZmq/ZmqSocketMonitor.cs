using SimpleZmq.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleZmq
{
    /// <summary>
    /// The available zmq monitor event flags.
    /// </summary>
    [Flags]
    public enum ZmqSocketMonitorEvent
    {
        /// <summary>
        /// The socket was connected.
        /// </summary>
        Connected = 1,

        /// <summary>
        /// The connection to the other endpoint was delayed.
        /// </summary>
        ConnectDelayed = 2,

        /// <summary>
        /// The socket retried connecting to the other endpoint.
        /// </summary>
        ConnectRetried = 4,

        /// <summary>
        /// The socket is listening.
        /// </summary>
        Listening = 8,

        /// <summary>
        /// The socket's binding failed.
        /// </summary>
        BindFailed = 16,

        /// <summary>
        /// The socket successfully accepted a new connection.
        /// </summary>
        Accepted = 32,

        /// <summary>
        /// The socket failed to accept a new connection.
        /// </summary>
        AcceptFailed = 64,

        /// <summary>
        /// The socket was closed.
        /// </summary>
        Closed = 128,

        /// <summary>
        /// The socket failed to close.
        /// </summary>
        CloseFailed = 256,

        /// <summary>
        /// The socket was disconnected from the other endpoint.
        /// </summary>
        Disconnected = 512,

        /// <summary>
        /// The monitor is stopped. This is the last event the monitor receives.
        /// </summary>
        MonitorStopped = 1024,

        /// <summary>
        /// The combination of all possible monitor events.
        /// </summary>
        All = Connected | ConnectDelayed | ConnectRetried | Listening | BindFailed | Accepted | AcceptFailed | Closed | CloseFailed | Disconnected | MonitorStopped
    }

    /// <summary>
    /// The event arguments for zmq monitor event occurances.
    /// </summary>
    public class ZmqSocketMonitorEventArgs
    {
        private ZmqSocketMonitorEvent   _monitorEvent;
        private int                     _eventValue;
        private ZmqError                _error;
        private string                  _endpoint;

        internal ZmqSocketMonitorEventArgs ReInitialize(ZmqSocketMonitorEvent monitorEvent, int eventValue, ZmqError error, string endpoint)
        {
            _monitorEvent = monitorEvent;
            _eventValue = eventValue;
            _error = error;
            _endpoint = endpoint;
            return this;
        }

        /// <summary>
        /// Gets the monitor event type.
        /// </summary>
        public ZmqSocketMonitorEvent Event
        {
            get { return _monitorEvent; }
        }

        /// <summary>
        /// Gets the event value. It means different things for different events (sometimes nothing).
        /// </summary>
        public int EventValue
        {
            get { return _eventValue; }
        }

        /// <summary>
        /// The zmq error if the event meant a failure.
        /// </summary>
        public ZmqError Error
        {
            get { return _error; }
        }

        /// <summary>
        /// The endpoint string. As of libzmq 4.0.4 it's not guaranteed to be a meaningful string.
        /// </summary>
        public string Endpoint
        {
            get { return _endpoint; }
        }

        /// <summary>
        /// Returns the string representation of this class.
        /// </summary>
        /// <returns>The string representation of this class.</returns>
        public override string ToString()
        {
            // event-value is not that meaningful, endpoint is not always a valid string, so we just print the event and the error, if there was any.
            return String.Format("{0}{1}", _monitorEvent, (_error.IsError ? String.Format(" ({0})", _error) : String.Empty));
        }
    }

    /// <summary>
    /// Class to monitor a zmq socket.
    /// </summary>
    public class ZmqSocketMonitor : IDisposable
    {
        private readonly Action<string>             _logError;
        private readonly string                     _monitorEndpoint;
        private readonly ZmqSocket                  _monitorSocket;
        private readonly byte[]                     _monitorEventBuffer = new byte[256];
        private readonly ZmqSocketMonitorEventArgs  _monitorEventArgs = new ZmqSocketMonitorEventArgs();

        private bool                                _isStopped;

        internal ZmqSocketMonitor(ZmqSocket socketToMonitor, ZmqSocketMonitorEvent eventsToMonitor, Action<string> logError)
        {
            Argument.ExpectNonNull(socketToMonitor, "socketToMonitor");
            Argument.ExpectNonNull(logError, "logError");

            _logError = logError;
            _monitorEndpoint = String.Format("inproc://{0}", Guid.NewGuid().ToString("D"));
            Zmq.ThrowIfError(LibZmq.zmq_socket_monitor(socketToMonitor.NativePtr, _monitorEndpoint, (int)eventsToMonitor));
            _monitorSocket = socketToMonitor.Context.CreateSocket(ZmqSocketType.Pair);
            _monitorSocket.Connect(_monitorEndpoint);
        }

        /// <summary>
        /// The zmq socket of the monitor. This inproc PAIR socket received all the monitoring events. It's not supposed to be used directly.
        /// </summary>
        internal ZmqSocket Socket
        {
            get { return _monitorSocket; }
        }

        /// <summary>
        /// Gets a value indicating whether the zmq monitor was stopped or not. The zmq monitor's inproc PAIR socket must be polled until it receives the last message, which is the stop.
        /// </summary>
        public bool IsStopped
        {
            get { return _isStopped; }
        }

        /// <summary>
        /// Receives a monitoring event from the inproc PAIR socket.
        /// </summary>
        /// <returns>The monitor event arguments.</returns>
        internal ZmqSocketMonitorEventArgs ReceiveMonitorEvent()
        {
            int msgframeSize;
            var msgFrameBuffer = _monitorSocket.Receive(_monitorEventBuffer, out msgframeSize);
            if (msgframeSize != 6)
            {
                _logError(String.Format("ZmqSocketMonitor.ReceiveMonitorEvent(): received only {0} bytes, expecting 6 in the 1. frame.", msgframeSize));
                return null;
            }
            var monitorEvent = (ZmqSocketMonitorEvent)(BitConverter.ToInt16(msgFrameBuffer, 0));
            var eventValue = BitConverter.ToInt32(msgFrameBuffer, sizeof(Int16));
            var error = ZmqError.Success();
            string endpoint = null;

            if (monitorEvent == ZmqSocketMonitorEvent.MonitorStopped)
            {
                _isStopped = true;
                // receive the second, empty frame, and just ignore it.
                _monitorSocket.Receive(_monitorEventBuffer, out msgframeSize);
            }
            else
            {
                msgFrameBuffer = _monitorSocket.Receive(_monitorEventBuffer, out msgframeSize);
                endpoint = Encoding.ASCII.GetString(msgFrameBuffer, 0, msgframeSize);
            }

            if (monitorEvent == ZmqSocketMonitorEvent.BindFailed || monitorEvent == ZmqSocketMonitorEvent.AcceptFailed || monitorEvent == ZmqSocketMonitorEvent.CloseFailed)
            {
                error = ZmqError.FromErrNo(eventValue);
            }

            return _monitorEventArgs.ReInitialize(monitorEvent, eventValue, error, endpoint);
        }

        /// <summary>
        /// Disposes the zmq monitor with its inproc PAIR socket.
        /// </summary>
        public void Dispose()
        {
            _monitorSocket.Dispose();
        }
    }
}
