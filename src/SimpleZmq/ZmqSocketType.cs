using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleZmq
{
    /// <summary>
    /// The available zmq socket types.
    /// </summary>
    public enum ZmqSocketType
    {
        /// <summary>
        /// Pair.
        /// </summary>
        Pair = 0,

        /// <summary>
        /// Pub.
        /// </summary>
        Pub = 1,

        /// <summary>
        /// Sub.
        /// </summary>
        Sub = 2,

        /// <summary>
        /// Req.
        /// </summary>
        Req = 3,

        /// <summary>
        /// Rep.
        /// </summary>
        Rep = 4,

        /// <summary>
        /// Dealer.
        /// </summary>
        Dealer = 5,

        /// <summary>
        /// Router.
        /// </summary>
        Router = 6,

        /// <summary>
        /// Pull.
        /// </summary>
        Pull = 7,

        /// <summary>
        /// Push.
        /// </summary>
        Push = 8,

        /// <summary>
        /// XPub.
        /// </summary>
        XPub = 9,

        /// <summary>
        /// XSub.
        /// </summary>
        XSub = 10,

        /// <summary>
        /// Stream.
        /// </summary>
        Stream = 11,

        /// <summary>
        /// XReq.
        /// </summary>
        XReq = Dealer,

        /// <summary>
        /// XRep.
        /// </summary>
        XRep = Router
    }
}
