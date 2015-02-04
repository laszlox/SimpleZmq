using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleZmq.Test
{
    [TestClass]
    public class ZmqSocketTest
    {
        [TestMethod]
        public void Socket_Cannot_Be_Bound_With_Null_EndPoint()
        {
            using (var zmqContext = new ZmqContext())
            {
                using (ZmqSocket socket = zmqContext.CreateSocket(ZmqSocketType.Push))
                {
                    ExceptionAssert.Throws<ArgumentNullException>(() => socket.Bind(null));
                }
            }
        }

        [TestMethod]
        public void Socket_Cannot_Be_Bound_With_Empty_EndPoint()
        {
            using (var zmqContext = new ZmqContext())
            {
                using (ZmqSocket socket = zmqContext.CreateSocket(ZmqSocketType.Push))
                {
                    ExceptionAssert.Throws<ArgumentException>(() => socket.Bind(""));
                }
            }
        }

        [TestMethod]
        public void Socket_Cannot_Be_Bound_With_WhiteSpace_EndPoint()
        {
            using (var zmqContext = new ZmqContext())
            {
                using (ZmqSocket socket = zmqContext.CreateSocket(ZmqSocketType.Push))
                {
                    ExceptionAssert.Throws<ArgumentException>(() => socket.Bind("    "));
                }
            }
        }

        [TestMethod]
        public void Socket_Cannot_Be_Bound_With_Invalid_Protocol()
        {
            using (var zmqContext = new ZmqContext())
            {
                using (ZmqSocket socket = zmqContext.CreateSocket(ZmqSocketType.Push))
                {
                    ExceptionAssert.Throws<ZmqException>(() => socket.Bind("invalid://test"), "Protocol not supported (135)");
                }
            }
        }

        [TestMethod]
        public void Get_And_Set_Socket_Options()
        {
            using (var zmqContext = new ZmqContext())
            {
                using (ZmqSocket socket = zmqContext.CreateSocket(ZmqSocketType.Push))
                {
                    Assert.AreEqual(socket.SocketType, ZmqSocketType.Push);
                    Assert.IsFalse(socket.HasMoreToReceive);

                    Assert.AreEqual(socket.SendHWM, 1000);
                    socket.SendHWM = 0;
                    Assert.AreEqual(socket.SendHWM, 0);
                    socket.SendHWM = 10;
                    Assert.AreEqual(socket.SendHWM, 10);

                    Assert.AreEqual(socket.ReceiveHWM, 1000);
                    socket.ReceiveHWM = 0;
                    Assert.AreEqual(socket.ReceiveHWM, 0);
                    socket.ReceiveHWM = 10;
                    Assert.AreEqual(socket.ReceiveHWM, 10);

                    Assert.AreEqual(socket.Affinity, 0ul);
                    socket.Affinity = 1;
                    Assert.AreEqual(socket.Affinity, 1ul);

                    CollectionAssert.AreEqual(socket.Identity, new byte[0]);
                    socket.Identity = new byte[] { 1, 2, 3 };
                    CollectionAssert.AreEqual(socket.Identity, new byte[] { 1, 2, 3 });

                    Assert.AreEqual(socket.Rate, 100);
                    socket.Rate = 150;
                    Assert.AreEqual(socket.Rate, 150);
                    socket.Rate = 500;
                    Assert.AreEqual(socket.Rate, 500);

                    Assert.AreEqual(socket.RecoveryInterval, 10000);
                    socket.RecoveryInterval = 15000;
                    Assert.AreEqual(socket.RecoveryInterval, 15000);
                    socket.RecoveryInterval = 50000;
                    Assert.AreEqual(socket.RecoveryInterval, 50000);

                    Assert.AreEqual(socket.SendBufferSize, 0);
                    socket.SendBufferSize = 1024;
                    Assert.AreEqual(socket.SendBufferSize, 1024);
                    socket.SendBufferSize = 4096;
                    Assert.AreEqual(socket.SendBufferSize, 4096);

                    Assert.AreEqual(socket.ReceiveBufferSize, 0);
                    socket.ReceiveBufferSize = 1024;
                    Assert.AreEqual(socket.ReceiveBufferSize, 1024);
                    socket.ReceiveBufferSize = 4096;
                    Assert.AreEqual(socket.ReceiveBufferSize, 4096);

                    Assert.AreEqual(socket.Linger, -1);
                    socket.Linger = 0;
                    Assert.AreEqual(socket.Linger, 0);
                    socket.Linger = 500;
                    Assert.AreEqual(socket.Linger, 500);

                    Assert.AreEqual(socket.ReconnectInterval, 100);
                    socket.ReconnectInterval = 150;
                    Assert.AreEqual(socket.ReconnectInterval, 150);
                    socket.ReconnectInterval = 500;
                    Assert.AreEqual(socket.ReconnectInterval, 500);

                    Assert.AreEqual(socket.MaxReconnectInterval, 0);
                    socket.MaxReconnectInterval = 1000;
                    Assert.AreEqual(socket.MaxReconnectInterval, 1000);
                    socket.MaxReconnectInterval = 5000;
                    Assert.AreEqual(socket.MaxReconnectInterval, 5000);

                    Assert.AreEqual(socket.BackLog, 100);
                    socket.BackLog = 1000;
                    Assert.AreEqual(socket.BackLog, 1000);
                    socket.BackLog = 2000;
                    Assert.AreEqual(socket.BackLog, 2000);

                    Assert.AreEqual(socket.MaxMessageSize, -1L);
                    socket.MaxMessageSize = 1000L;
                    Assert.AreEqual(socket.MaxMessageSize, 1000);
                    socket.MaxMessageSize = 2000;
                    Assert.AreEqual(socket.MaxMessageSize, 2000);

                    Assert.AreEqual(socket.MulticastHops, 1);
                    socket.MulticastHops = 2;
                    Assert.AreEqual(socket.MulticastHops, 2);
                    socket.MulticastHops = 10;
                    Assert.AreEqual(socket.MulticastHops, 10);

                    Assert.AreEqual(socket.ReceiveTimeOut, -1);
                    socket.ReceiveTimeOut = 100;
                    Assert.AreEqual(socket.ReceiveTimeOut, 100);
                    socket.ReceiveTimeOut = 210;
                    Assert.AreEqual(socket.ReceiveTimeOut, 210);

                    Assert.AreEqual(socket.SendTimeOut, -1);
                    socket.SendTimeOut = 100;
                    Assert.AreEqual(socket.SendTimeOut, 100);
                    socket.SendTimeOut = 210;
                    Assert.AreEqual(socket.SendTimeOut, 210);

                    Assert.IsFalse(socket.IPv6Enabled);
                    socket.IPv6Enabled = true;
                    Assert.IsTrue(socket.IPv6Enabled);
                    socket.IPv6Enabled = false;
                    Assert.IsFalse(socket.IPv6Enabled);

                    Assert.IsFalse(socket.Immediate);
                    socket.Immediate = true;
                    Assert.IsTrue(socket.Immediate);
                    socket.Immediate = false;
                    Assert.IsFalse(socket.Immediate);

                    Assert.AreEqual(socket.LastEndPoint, String.Empty);
                    socket.Bind("tcp://127.0.0.1:5555");
                    Assert.AreEqual(socket.LastEndPoint, "tcp://127.0.0.1:5555");

                    Assert.AreEqual(socket.TcpKeepAlive, -1);
                    socket.TcpKeepAlive = 0;
                    Assert.AreEqual(socket.TcpKeepAlive, 0);
                    socket.TcpKeepAlive = 1;
                    Assert.AreEqual(socket.TcpKeepAlive, 1);

                    Assert.AreEqual(socket.TcpKeepAliveIdle, -1);
                    socket.TcpKeepAliveIdle = 1;
                    Assert.AreEqual(socket.TcpKeepAliveIdle, 1);
                    socket.TcpKeepAliveIdle = 10;
                    Assert.AreEqual(socket.TcpKeepAliveIdle, 10);

                    Assert.AreEqual(socket.TcpKeepAliveCnt, -1);
                    socket.TcpKeepAliveCnt = 1;
                    Assert.AreEqual(socket.TcpKeepAliveCnt, 1);
                    socket.TcpKeepAliveCnt = 10;
                    Assert.AreEqual(socket.TcpKeepAliveCnt, 10);

                    Assert.AreEqual(socket.TcpKeepAliveIntVl, -1);
                    socket.TcpKeepAliveIntVl = 1;
                    Assert.AreEqual(socket.TcpKeepAliveIntVl, 1);
                    socket.TcpKeepAliveIntVl = 10;
                    Assert.AreEqual(socket.TcpKeepAliveIntVl, 10);

                    Assert.AreEqual(socket.SecurityMechanism, ZmqSocketSecurityMechanism.Null);
                    // These don't work. Not sure why, maybe it's read-only or needs some setup for security?
                    //socket.SecurityMechanism = SocketSecurityMechanism.Plain;
                    //Assert.AreEqual(socket.SecurityMechanism, SocketSecurityMechanism.Plain);
                    //socket.SecurityMechanism = SocketSecurityMechanism.Curve;
                    //Assert.AreEqual(socket.SecurityMechanism, SocketSecurityMechanism.Curve);

                    Assert.AreEqual(socket.PlainServer, 0);
                    socket.PlainServer = 1;
                    Assert.AreEqual(socket.PlainServer, 1);

                    Assert.AreEqual(socket.PlainUserName, String.Empty);
                    socket.PlainUserName = "userName";
                    Assert.AreEqual(socket.PlainUserName, "userName");

                    Assert.AreEqual(socket.PlainPassword, String.Empty);
                    socket.PlainPassword = "password";
                    Assert.AreEqual(socket.PlainPassword, "password");

                    // getting the CurvePublicKey doesn't work.
                    //Assert.AreEqual(socket.CurvePublicKey, new byte[0]);
                    //socket.CurvePublicKey = new byte[32];
                    //Assert.AreEqual(socket.CurvePublicKey, new byte[32]);
                    //Assert.AreEqual(socket.CurvePublicKeyString, String.Empty);
                    //socket.CurvePublicKeyString = "abcdeabcdeabcdeabcdeabcdeabcdeabcdeabcde";
                    //Assert.AreEqual(socket.CurvePublicKeyString, "abcdeabcdeabcdeabcdeabcdeabcdeabcdeabcde");

                    // getting the CurveSecretKey doesn't work.
                    //Assert.AreEqual(socket.CurveSecretKey, new byte[0]);
                    //socket.CurveSecretKey = new byte[32];
                    //Assert.AreEqual(socket.CurveSecretKey, new byte[32]);
                    //Assert.AreEqual(socket.CurveSecretKeyString, String.Empty);
                    //socket.CurveSecretKeyString = "abcdeabcdeabcdeabcdeabcdeabcdeabcdeabcde";
                    //Assert.AreEqual(socket.CurveSecretKeyString, "abcdeabcdeabcdeabcdeabcdeabcdeabcdeabcde");

                    // getting the CurveServerKey doesn't work.
                    //Assert.AreEqual(socket.CurveServerKey, new byte[0]);
                    //socket.CurveServerKey = new byte[32];
                    //Assert.AreEqual(socket.CurveServerKey, new byte[32]);
                    //Assert.AreEqual(socket.CurveServerKeyString, String.Empty);
                    //socket.CurveServerKeyString = "abcdeabcdeabcdeabcdeabcdeabcdeabcdeabcde";
                    //Assert.AreEqual(socket.CurveServerKeyString, "abcdeabcdeabcdeabcdeabcdeabcdeabcdeabcde");
                }
            }
        }

        [TestMethod]
        public void Socket_Connect_To_Bound_Socket_Then_Send_And_Receive()
        {
            using (var zmqContext = new ZmqContext())
            {
                using (ZmqSocket pushSocket = zmqContext.CreateSocket(ZmqSocketType.Push))
                using (ZmqSocket pullSocket = zmqContext.CreateSocket(ZmqSocketType.Pull))
                {
                    pushSocket.Bind("tcp://127.0.0.1:5555");
                    pullSocket.Connect("tcp://127.0.0.1:5555");

                    Assert.IsTrue(pushSocket.Send(new byte[] { 1,2,3,4 }, 4));
                    var buffer = new byte[4];
                    int receivedLength;
                    var receivedBuffer = pullSocket.Receive(buffer, out receivedLength);
                    Assert.IsNotNull(receivedBuffer);
                    Assert.AreSame(buffer, receivedBuffer);
                    Assert.AreEqual(receivedLength, 4);
                    CollectionAssert.AreEqual(receivedBuffer, new byte[] { 1,2,3,4 });
                }
            }
        }

        [TestMethod]
        public void Pub_Socket_Sends_To_Multiple_Subscribers()
        {
            using (var zmqPubContext = new ZmqContext())
            using (var zmqSubContext1 = new ZmqContext())
            using (var zmqSubContext2 = new ZmqContext())
            {
                using (var pubSocket = zmqPubContext.CreateSocket(ZmqSocketType.Pub))
                {
                    pubSocket.Bind("tcp://127.0.0.1:5555");

                    Action<byte[]> subscriberPolling = topic =>
                    {
                        using (var subSocket = zmqSubContext1.CreateSocket(ZmqSocketType.Sub))
                        {
                            subSocket.Connect("tcp://127.0.0.1:5555");
                            subSocket.Subscribe(topic);

                            int numberOfExpectedMessages = 10;

                            var poller = ZmqPoller.New()
                                .With(
                                    subSocket,
                                    s =>
                                    {
                                        var buffer = new byte[10];
                                        int receivedLength;
                                        var receivedBuffer = s.Receive(buffer, out receivedLength, doNotWait: true);
                                        Assert.IsNotNull(receivedBuffer);
                                        Assert.AreSame(buffer, receivedBuffer);
                                        Assert.AreEqual(receivedLength, 10);
                                        for (int i = 0; i < topic.Length; i++)
                                        {
                                            Assert.AreEqual(receivedBuffer[i], topic[i]);
                                        }
                                        numberOfExpectedMessages--;
                                    }
                                 )
                                .Build();

                            while (numberOfExpectedMessages > 0)
                            {
                                poller.Poll(500);
                            }
                        }
                    };

                    var subscriberToAll = Task.Run(() => subscriberPolling(new byte[0]));
                    var subscriber1 = Task.Run(() => subscriberPolling(new byte[] { 1 }));
                    var subscriber2 = Task.Run(() => subscriberPolling(new byte[] { 2 }));
                    var subscribers = new[] { subscriberToAll, subscriber1, subscriber2 };

                    Thread.Sleep(2000);  // so that it's more likely that subscribers are receiving

                    var message = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
                    int numberOfSentMessages = 0;
                    while (subscribers.Any(s => !s.IsCompleted))
                    {
                        Assert.IsTrue(pubSocket.Send(message, message.Length));
                        message[0] = (byte)((message[0] + 1) % 10);
                        message[1] = (byte)((message[0] * 2) % 20);
                        numberOfSentMessages++;
                    }

                    Task.WaitAll(subscribers);

                    Console.WriteLine("Number of sent messasge: {0}", numberOfSentMessages);
                }
            }
        }
    }
}
