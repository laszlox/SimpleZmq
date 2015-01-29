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

        private struct ZmqPollItemWithHandlers
        {
            private readonly ZmqSocket          _zmqSocket;
            private readonly Action<ZmqSocket>  _handleReceive;
            private readonly Action<ZmqSocket>  _handleSend;

            public ZmqPollItemWithHandlers(ZmqSocket zmqSocket, Action<ZmqSocket> handleReceive, Action<ZmqSocket> handleSend = null)
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

        public class ZmqPollerBuilder
        {
            private List<ZmqPollItemWithHandlers> _pollItemsWithHandlers = new List<ZmqPollItemWithHandlers>();

            public ZmqPollerBuilder With(ZmqSocket[] zmqSockets, Action<ZmqSocket> handleReceive, Action<ZmqSocket> handleSend = null)
            {
                if (_pollItemsWithHandlers == null) throw new InvalidOperationException("ZmqPollerBuilder has already built a poller.");
                for (int i = 0; i < zmqSockets.Length; i++)
                {
                    _pollItemsWithHandlers.Add(new ZmqPollItemWithHandlers(zmqSockets[i], handleReceive, handleSend));
                }
                return this;
            }

            public ZmqPollerBuilder With(ZmqSocket zmqSocket, Action<ZmqSocket> handleReceive, Action<ZmqSocket> handleSend = null)
            {
                if (_pollItemsWithHandlers == null) throw new InvalidOperationException("ZmqPollerBuilder has already built a poller.");
                _pollItemsWithHandlers.Add(new ZmqPollItemWithHandlers(zmqSocket, handleReceive, handleSend));
                return this;
            }

            public ZmqPoller Build()
            {
                var pollItemsWithHandlers = _pollItemsWithHandlers;
                _pollItemsWithHandlers = null;
                return new ZmqPoller(pollItemsWithHandlers.ToArray());
            }
        }


        private readonly ZmqPollItemWithHandlers[] _pollItemsWithHandlers;
        private readonly ZmqPollItem[]             _pollItems;

        private ZmqPoller(ZmqPollItemWithHandlers[] pollItemsWithHandlers)
        {
            Argument.ExpectNonNull(pollItemsWithHandlers, "pollItemsWithHandlers");

            _pollItemsWithHandlers = pollItemsWithHandlers;
            _pollItems = new ZmqPollItem[pollItemsWithHandlers.Length];
            for (int i = 0; i < pollItemsWithHandlers.Length; i++)
            {
                _pollItems[i] = new ZmqPollItem { Socket = pollItemsWithHandlers[i].ZmqSocket.NativePtr, Events = ZMQ_POLLIN | ZMQ_POLLOUT };
            }
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
                for (int i = 0; i < _pollItems.Length; i++)
                {
                    var pollItem = _pollItems[i];
                    var pollItemWithHandler = _pollItemsWithHandlers[i];

                    if (pollItem.ReadyEvents != 0)
                    {
                        if (pollItemWithHandler.HandleReceive != null && (pollItem.ReadyEvents & ZMQ_POLLIN) == ZMQ_POLLIN)
                        {
                            pollItemWithHandler.HandleReceive(pollItemWithHandler.ZmqSocket);
                        }
                        if (pollItemWithHandler.HandleSend != null && (pollItem.ReadyEvents & ZMQ_POLLOUT) == ZMQ_POLLOUT)
                        {
                            pollItemWithHandler.HandleSend(pollItemWithHandler.ZmqSocket);
                        }
                    }
                }
            }

            return numberOfReadySockets > 0;
        }
    }
}
