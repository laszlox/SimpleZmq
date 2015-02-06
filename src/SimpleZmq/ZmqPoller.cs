using SimpleZmq.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleZmq
{
    /// <summary>
    /// Class helping zmq polling.
    /// </summary>
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

            public short ZmqEventsFlags
            {
                get { return (short)((_handleReceive != null ? ZMQ_POLLIN : 0) | (_handleSend != null ? ZMQ_POLLOUT : 0)); }
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

            public short ZmqEventsFlags
            {
                get { return ZMQ_POLLIN; }
            }
        }

        /// <summary>
        /// Builder class to build a <see cref="ZmqPoller"/> instance.
        /// </summary>
        public class Builder
        {
            private List<ZmqSocketWithHandlers>         _socketsWithHandlers = new List<ZmqSocketWithHandlers>();
            private List<ZmqSocketMonitorWithHandler>   _socketMonitorsWithHandler = new List<ZmqSocketMonitorWithHandler>();

            /// <summary>
            /// Adds the specified sockets to the polling with the receive and optional send handlers.
            /// </summary>
            /// <param name="zmqSockets">The sockets to poll.</param>
            /// <param name="handleReceive">The receive handler.</param>
            /// <param name="handleSend">The send handler.</param>
            /// <returns>This builder instance to support fluent calls.</returns>
            public Builder HandleEventsOf(ZmqSocket[] zmqSockets, Action<ZmqSocket> handleReceive, Action<ZmqSocket> handleSend = null)
            {
                Argument.ExpectNonNull(zmqSockets, "zmqSockets");
                if (_socketsWithHandlers == null) throw new InvalidOperationException("ZmqPoller.Builder has already built a poller.");

                for (int i = 0; i < zmqSockets.Length; i++)
                {
                    _socketsWithHandlers.Add(new ZmqSocketWithHandlers(zmqSockets[i], handleReceive, handleSend));
                }
                return this;
            }

            /// <summary>
            /// Adds the specified socket to the polling with the receive and optional send handlers.
            /// </summary>
            /// <param name="zmqSocket">The socket to poll.</param>
            /// <param name="handleReceive">The receive handler.</param>
            /// <param name="handleSend">The send handler.</param>
            /// <returns>This builder instance to support fluent calls.</returns>
            public Builder HandleEventsOf(ZmqSocket zmqSocket, Action<ZmqSocket> handleReceive, Action<ZmqSocket> handleSend = null)
            {
                Argument.ExpectNonNull(zmqSocket, "zmqSocket");
                if (_socketsWithHandlers == null) throw new InvalidOperationException("ZmqPoller.Builder has already built a poller.");

                _socketsWithHandlers.Add(new ZmqSocketWithHandlers(zmqSocket, handleReceive, handleSend));
                return this;
            }

            /// <summary>
            /// Adds the specified socket monitor to the polling with the monitor event handler.
            /// </summary>
            /// <param name="zmqSocketMonitor">The socket monitor to poll.</param>
            /// <param name="handleEvent">The monitor event handler.</param>
            /// <returns>This builder instance to support fluent calls.</returns>
            public Builder HandleEventsOf(ZmqSocketMonitor zmqSocketMonitor, Action<ZmqSocketMonitorEventArgs> handleEvent)
            {
                Argument.ExpectNonNull(zmqSocketMonitor, "zmqSocketMonitor");
                if (_socketMonitorsWithHandler == null) throw new InvalidOperationException("ZmqPoller.Builder has already built a poller.");

                _socketMonitorsWithHandler.Add(new ZmqSocketMonitorWithHandler(zmqSocketMonitor, handleEvent));
                return this;
            }

            /// <summary>
            /// Finishes building up the <see cref="ZmqPoller"/> and returns the ready-to-use poller.
            /// </summary>
            /// <returns>The ready-to-use poller.</returns>
            public ZmqPoller Build()
            {
                if (_socketsWithHandlers == null || _socketMonitorsWithHandler == null) throw new InvalidOperationException("ZmqPoller.Builder has already built a poller.");
                if (_socketsWithHandlers.Count == 0 && _socketMonitorsWithHandler.Count == 0) throw new InvalidOperationException("ZmqPoller.Builder needs at least one socket or socket monitor to be polled.");

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
                var socketWithHandlers = socketsWithHandlers[i];
                _pollItems[pollItemIndex] = new ZmqPollItem { Socket = socketWithHandlers.ZmqSocket.NativePtr, Events = socketWithHandlers.ZmqEventsFlags };
            }
            // creating poll-items for the socket-monitors to be polled
            for (int i = 0; i < socketMonitorsWithHandler.Length; i++, pollItemIndex++)
            {
                var socketMonitorWithHandler = socketMonitorsWithHandler[i];
                _pollItems[pollItemIndex] = new ZmqPollItem { Socket = socketMonitorWithHandler.ZmqSocketMonitor.Socket.NativePtr, Events = socketMonitorWithHandler.ZmqEventsFlags };
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

        /// <summary>
        /// Creates a new builder to builder up a <see cref="ZmqPoller"/> instance.
        /// </summary>
        /// <returns>A new builder to builder up a <see cref="ZmqPoller"/> instance.</returns>
        public static Builder New()
        {
            return new Builder();
        }

        /// <summary>
        /// Polls the sockets and monitors until the specified time-out.
        /// </summary>
        /// <param name="timeOutMilliseconds">The time-out in milliseconds.</param>
        /// <returns>True if the polling received any event to be processed.</returns>
        public bool Poll(int timeOutMilliseconds)
        {
            Argument.ExpectGreaterThan(-1, timeOutMilliseconds, "timeOutMilliseconds");

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

        /// <summary>
        /// Polls only the socket monitors until they are stopped.
        /// </summary>
        /// <remarks>
        /// The monitoring sockets must not be disposed until they received the last message, the stop. That's why
        /// the monitored sockets must be disposed first, then with this method all remaining monitoring events can be processed and once this returns
        /// the socket monitors also can be disposed.
        /// </remarks>
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
