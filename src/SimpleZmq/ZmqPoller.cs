using SimpleZmq.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleZmq
{
    public class ZmqPoller
    {
        private const int ZMQ_POLLIN = 1;
        private const int ZMQ_POLLOUT = 2;
        private const int ZMQ_POLLERR = 4;

        private struct ZmqSocketWithHandlers
        {
            private readonly ZmqSocket          _zmqSocket;
            private readonly Action<ZmqSocket>  _handleReceive;
            private readonly Action<ZmqSocket>  _handleSend;

            public ZmqSocketWithHandlers(ZmqSocket zmqSocket, Action<ZmqSocket> handleReceive, Action<ZmqSocket> handleSend = null)
            {
                _zmqSocket = zmqSocket;
                _handleReceive = handleReceive;
                _handleSend = handleSend;
            }

            public ZmqSocket ZmqSocket
            {
                get { return _zmqSocket; }
            }

            public Action<ZmqSocket> HandleReceive
            {
                get { return _handleReceive; }
            }

            public Action<ZmqSocket> HandleSend
            {
                get { return _handleSend; }
            }
        }

        private struct ZmqSocketMonitorWithHandler
        {
            private readonly ZmqSocketMonitor                   _zmqSocketMonitor;
            private readonly Action<ZmqSocketMonitorEventArgs>  _handleEvent;

            public ZmqSocketMonitorWithHandler(ZmqSocketMonitor zmqSocketMonitor, Action<ZmqSocketMonitorEventArgs> handleEvent)
            {
                _zmqSocketMonitor = zmqSocketMonitor;
                _handleEvent = handleEvent;
            }

            public ZmqSocketMonitor ZmqSocketMonitor
            {
                get { return _zmqSocketMonitor; }
            }

            public Action<ZmqSocketMonitorEventArgs> HandleEvent
            {
                get { return _handleEvent; }
            }
        }

        public class ZmqPollerBuilder
        {
            private List<ZmqSocketWithHandlers>         _socketsWithHandlers = new List<ZmqSocketWithHandlers>();
            private List<ZmqSocketMonitorWithHandler>   _socketMonitorsWithHandler = new List<ZmqSocketMonitorWithHandler>();

            public ZmqPollerBuilder With(ZmqSocket[] zmqSockets, Action<ZmqSocket> handleReceive, Action<ZmqSocket> handleSend = null)
            {
                if (_socketsWithHandlers == null) throw new InvalidOperationException("ZmqPollerBuilder has already built a poller.");
                for (int i = 0; i < zmqSockets.Length; i++)
                {
                    _socketsWithHandlers.Add(new ZmqSocketWithHandlers(zmqSockets[i], handleReceive, handleSend));
                }
                return this;
            }

            public ZmqPollerBuilder With(ZmqSocket zmqSocket, Action<ZmqSocket> handleReceive, Action<ZmqSocket> handleSend = null)
            {
                if (_socketsWithHandlers == null) throw new InvalidOperationException("ZmqPollerBuilder has already built a poller.");
                _socketsWithHandlers.Add(new ZmqSocketWithHandlers(zmqSocket, handleReceive, handleSend));
                return this;
            }

            public ZmqPollerBuilder With(ZmqSocketMonitor zmqSocketMonitor, Action<ZmqSocketMonitorEventArgs> handleEvent)
            {
                if (_socketMonitorsWithHandler == null) throw new InvalidOperationException("ZmqPollerBuilder has already built a poller.");
                _socketMonitorsWithHandler.Add(new ZmqSocketMonitorWithHandler(zmqSocketMonitor, handleEvent));
                return this;
            }

            public ZmqPoller Build()
            {
                var pollItemsWithHandlers = _socketsWithHandlers;
                var socketMonitorsWithHandler = _socketMonitorsWithHandler;
                _socketsWithHandlers = null;
                _socketMonitorsWithHandler = null;
                return new ZmqPoller(pollItemsWithHandlers.ToArray(), socketMonitorsWithHandler.ToArray());
            }
        }


        private readonly ZmqSocketWithHandlers[]        _socketsWithHandlers;
        private readonly ZmqSocketMonitorWithHandler[]  _socketMonitorsWithHandler;
        private readonly ZmqPollItem[]                  _pollItems;
        private readonly ZmqPollItem[]                  _socketMonitorPollItems;

        private ZmqPoller(ZmqSocketWithHandlers[] socketsWithHandlers, ZmqSocketMonitorWithHandler[] socketMonitorsWithHandler)
        {
            Argument.ExpectNonNull(socketsWithHandlers, "socketsWithHandlers");
            Argument.ExpectNonNull(socketMonitorsWithHandler, "socketMonitorsWithHandler");

            _socketsWithHandlers = socketsWithHandlers;
            _socketMonitorsWithHandler = socketMonitorsWithHandler;
            _pollItems = new ZmqPollItem[socketsWithHandlers.Length + socketMonitorsWithHandler.Length];
            _socketMonitorPollItems = new ZmqPollItem[socketMonitorsWithHandler.Length];
            int pollItemIndex = 0;
            // creating poll-items for the sockets to be polled
            for (int i = 0; i < socketsWithHandlers.Length; i++, pollItemIndex++)
            {
                _pollItems[pollItemIndex] = new ZmqPollItem { Socket = socketsWithHandlers[i].ZmqSocket.NativePtr, Events = ZMQ_POLLIN | ZMQ_POLLOUT };
            }
            // creating poll-items for the socket-monitors to be polled
            for (int i = 0; i < socketMonitorsWithHandler.Length; i++, pollItemIndex++)
            {
                _pollItems[pollItemIndex] = new ZmqPollItem { Socket = socketMonitorsWithHandler[i].ZmqSocketMonitor.Socket.NativePtr, Events = ZMQ_POLLIN };
                // storing the socket monitors' poll-items separately to be able to poll only the monitors
                _socketMonitorPollItems[i] = _pollItems[pollItemIndex];
            }
        }

        private int ProcessSocketEvents()
        {
            var pollItemIndex = 0;
            for (int i = 0; i < _socketsWithHandlers.Length; i++, pollItemIndex++)
            {
                var pollItem = _pollItems[pollItemIndex];
                var socketsWithHandler = _socketsWithHandlers[i];

                if (pollItem.ReadyEvents != 0)
                {
                    if (socketsWithHandler.HandleReceive != null && (pollItem.ReadyEvents & ZMQ_POLLIN) == ZMQ_POLLIN)
                    {
                        socketsWithHandler.HandleReceive(socketsWithHandler.ZmqSocket);
                    }
                    if (socketsWithHandler.HandleSend != null && (pollItem.ReadyEvents & ZMQ_POLLOUT) == ZMQ_POLLOUT)
                    {
                        socketsWithHandler.HandleSend(socketsWithHandler.ZmqSocket);
                    }
                }
            }
            return pollItemIndex;
        }

        private int ProcessSocketMonitorEvents(int pollItemIndex, ZmqPollItem[] pollItems)
        {
            for (int i = 0; i < _socketMonitorsWithHandler.Length; i++, pollItemIndex++)
            {
                var pollItem = pollItems[pollItemIndex];
                var socketMonitorWithHandler = _socketMonitorsWithHandler[i];

                if (pollItem.ReadyEvents != 0)
                {
                    if ((pollItem.ReadyEvents & ZMQ_POLLIN) == ZMQ_POLLIN)
                    {
                        var socketMonitorEventArgs = socketMonitorWithHandler.ZmqSocketMonitor.ReceiveMonitorEvent();
                        if (socketMonitorEventArgs != null)
                        {
                            socketMonitorWithHandler.HandleEvent(socketMonitorEventArgs);
                        }
                    }
                }
            }
            return pollItemIndex;
        }

        private bool AreAllMonitorSocketsStopped()
        {
            // not using linq to avoid managed heap usage (GetEnumerator())
            for (int i = 0; i < _socketMonitorsWithHandler.Length; i++)
            {
                if (!_socketMonitorsWithHandler[i].ZmqSocketMonitor.IsStopped) return false;
            }
            return true;
        }

        public static ZmqPollerBuilder New()
        {
            return new ZmqPollerBuilder();
        }

        public bool Poll(int timeOutMilliseconds)
        {
            var numberOfReadySockets = Zmq.ThrowIfError_IgnoreContextTerminated(
                Zmq.RetryIfInterrupted(LibZmq.zmq_poll_func, _pollItems, _pollItems.Length, timeOutMilliseconds)
            );

            if (numberOfReadySockets > 0)
            {
                var pollItemIndex = ProcessSocketEvents();
                ProcessSocketMonitorEvents(pollItemIndex, _pollItems);
            }

            return numberOfReadySockets > 0;
        }

        public void PollMonitorSocketsUntilTheyStop()
        {
            while (!AreAllMonitorSocketsStopped())
            {
                var numberOfReadySockets = Zmq.ThrowIfError_IgnoreContextTerminated(
                    Zmq.RetryIfInterrupted(LibZmq.zmq_poll_func, _socketMonitorPollItems, _socketMonitorPollItems.Length, 100)
                );

                if (numberOfReadySockets > 0)
                {
                    ProcessSocketMonitorEvents(0, _socketMonitorPollItems);
                }
            }
        }
    }
}
