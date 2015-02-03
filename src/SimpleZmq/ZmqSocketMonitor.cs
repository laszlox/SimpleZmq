using SimpleZmq.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleZmq
{
    [Flags]
    public enum ZmqSocketMonitorEvent
    {
        Connected = 1,
        ConnectDelayed = 2,
        ConnectRetried = 4,
        Listening = 8,
        BindFailed = 16,
        Accepted = 32,
        AcceptFailed = 64,
        Closed = 128,
        CloseFailed = 256,
        Disconnected = 512,
        MonitorStopped = 1024,

        All = Connected | ConnectDelayed | ConnectRetried | Listening | BindFailed | Accepted | AcceptFailed | Closed | CloseFailed | Disconnected | MonitorStopped
    }

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

        public ZmqSocketMonitorEvent Event
        {
            get { return _monitorEvent; }
        }

        public int EventValue
        {
            get { return _eventValue; }
        }

        public ZmqError Error
        {
            get { return _error; }
        }

        public string Endpoint
        {
            get { return _endpoint; }
        }

        public override string ToString()
        {
            // event-value is not that meaningful, endpoint is not always a valid string, so we just print the event and the error, if there was any.
            return String.Format("{0}{1}", _monitorEvent, (_error.IsError ? String.Format(" ({0})", _error) : String.Empty));
        }
    }

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

        public ZmqSocket Socket
        {
            get { return _monitorSocket; }
        }

        public bool IsStopped
        {
            get { return _isStopped; }
        }

        public ZmqSocketMonitorEventArgs ReceiveMonitorEvent()
        {
            int msgframeSize;
            var msgFrameBuffer = _monitorSocket.Receive(_monitorEventBuffer, out msgframeSize);
            if (msgframeSize != 6)
            {
                _logError(String.Format("ZmqMonitorSocket.ReceiveMonitorEvent(): received only {0} bytes, expecting 6 in the 1. frame.", msgframeSize));
                return null;
            }
            var monitorEvent = (ZmqSocketMonitorEvent)(BitConverter.ToInt16(msgFrameBuffer, 0));
            var eventValue = BitConverter.ToInt32(msgFrameBuffer, sizeof(Int16));
            var error = ZmqError.Success();
            string endpoint = null;

            if (monitorEvent == ZmqSocketMonitorEvent.MonitorStopped)
            {
                _isStopped = true;
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

        public void Dispose()
        {
            _monitorSocket.Dispose();
        }
    }
}
