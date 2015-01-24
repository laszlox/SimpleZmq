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

        private readonly Action<ZmqSocket> _handleReceive;
        private readonly Action<ZmqSocket> _handleSend;
        private readonly ZmqSocket[]       _sockets;
        private readonly ZmqPollItem[]        _pollItems;

        public ZmqPoller(ZmqSocket[] socketsToPoll, Action<ZmqSocket> handleReceive, Action<ZmqSocket> handleSend = null)
        {
            _handleReceive = handleReceive;
            _handleSend = handleSend;
            _sockets = socketsToPoll;
            _pollItems = new ZmqPollItem[socketsToPoll.Length];
            for (int i = 0; i < socketsToPoll.Length; i++)
            {
                _pollItems[i] = new ZmqPollItem { Socket = socketsToPoll[i].NativePtr, Events = ZMQ_POLLIN | ZMQ_POLLOUT };
            }
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

                    if (pollItem.ReadyEvents != 0)
                    {
                        if (_handleReceive != null && (pollItem.ReadyEvents & ZMQ_POLLIN) == ZMQ_POLLIN)
                        {
                            _handleReceive(_sockets[i]);
                        }
                        if (_handleSend != null && (pollItem.ReadyEvents & ZMQ_POLLOUT) == ZMQ_POLLOUT)
                        {
                            _handleSend(_sockets[i]);
                        }
                    }
                }
            }

            return numberOfReadySockets > 0;
        }
    }
}
