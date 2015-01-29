﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SimpleZmq.Test
{
    [TestClass]
    public class ZmqMonitorSocketTest
    {
        [TestMethod]
        public void Monitor_Tcp_Bound_And_Connected_Sockets()
        {
            using (var zmqServerContext = new ZmqContext())
            using (var zmqClientContext = new ZmqContext())
            {
                using (var zmqServerSocket = zmqServerContext.CreateSocket(ZmqSocketType.Push))
                using (var zmqServerSocketMonitor = zmqServerSocket.Monitor(ZmqSocketMonitorEvent.All))
                {
                    zmqServerSocket.Bind("tcp://127.0.0.1:5555");

                    int numberOfMessages = 10;

                    var clientPolling = Task.Run(() =>
                    {
                        using (var zmqClientSocket = zmqClientContext.CreateSocket(ZmqSocketType.Pull))
                        using (var zmqClientSocketMonitor = zmqClientSocket.Monitor())
                        {
                            zmqClientSocket.Connect("tcp://127.0.0.1:5555");

                            int numberOfReceivedMessages = 0;

                            var clientPoller = ZmqPoller.New()
                                .With(
                                    zmqClientSocket,
                                    s =>
                                    {
                                        var buffer = new byte[4];
                                        int receivedLength;
                                        var receivedBuffer = s.Receive(buffer, out receivedLength, doNotWait: true);
                                        Assert.IsNotNull(receivedBuffer);
                                        Assert.AreSame(buffer, receivedBuffer);
                                        Assert.AreEqual(receivedLength, 4);
                                        CollectionAssert.AreEqual(receivedBuffer, new byte[] { 1, 2, 3, 4 });
                                        numberOfReceivedMessages++;
                                    }
                                 )
                                .With(
                                    zmqClientSocketMonitor.Socket,
                                    s =>
                                    {
                                        var monitoringEventArgs = zmqClientSocketMonitor.ReceiveMonitorEvent();
                                        Assert.IsNotNull(monitoringEventArgs);
                                        Console.WriteLine("Client socket monitoring event: {0}", monitoringEventArgs);
                                    }
                                 )
                                .Build();
                            while (numberOfReceivedMessages < numberOfMessages)
                            {
                                clientPoller.Poll(500);
                            }
                        }
                    });

                    var serverPoller = ZmqPoller.New()
                        .With(
                            zmqServerSocketMonitor.Socket,
                            s =>
                            {
                                var monitoringEventArgs = zmqServerSocketMonitor.ReceiveMonitorEvent();
                                Assert.IsNotNull(monitoringEventArgs);
                                Console.WriteLine("Server socket monitoring event: {0}", monitoringEventArgs);
                            }
                         )
                        .Build();
                    int numberOfSentMessages = 0;
                    while (numberOfSentMessages < numberOfMessages)
                    {
                        serverPoller.Poll(10);
                        Assert.IsTrue(zmqServerSocket.Send(new byte[] { 1, 2, 3, 4}, 4));
                        numberOfSentMessages++;
                    }

                    clientPolling.Wait();
                }
            }
        }
    }
}