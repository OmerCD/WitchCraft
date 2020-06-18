using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using PacketHandlers;
using UnityEngine;

 public class Server
    {
        public static int MaxPlayer { get; set; }
        public static int Port { get; set; }
        public static Dictionary<int, Client> Clients { get; set; } = new Dictionary<int, Client>();

        public static Dictionary<ClientPackets, IServerHandler> PacketHandlers;
        
        private static TcpListener _tcpListener;
        private static UdpClient _udpListener;

        public static void Start(int maxPlayer, int port)
        {
            MaxPlayer = maxPlayer;
            Port = port;
            Debug.Log("Starting Server...");
            InitializeServerData();
            _tcpListener = new TcpListener(IPAddress.Parse("192.168.1.67"),port );
            _tcpListener.Start();
            _tcpListener.BeginAcceptTcpClient(TcpConnectCallback, null);
            _udpListener = new UdpClient(Port);
            _udpListener.BeginReceive(UdpReceiveCallback, null);
            Debug.Log($"Server started on {Port}");
        }

        private static void UdpReceiveCallback(IAsyncResult ar)
        {
            try
            {
                var clientEndpoint = new IPEndPoint(IPAddress.Any, 0);
                var data = _udpListener.EndReceive(ar, ref clientEndpoint);
                _udpListener.BeginReceive(UdpReceiveCallback, null);

                if (data.Length < 4)
                {
                    return;
                }

                using (var packet = new Packet(data))
                {
                    var clientId = packet.ReadInt();
                    if (clientId == 0)
                    {
                        return;
                    }

                    if (Clients[clientId].Udp.EndPoint == null)
                    {
                        Clients[clientId].Udp.Connect(clientEndpoint);
                        return;
                    }

                    if (Clients[clientId].Udp.EndPoint.ToString() == clientEndpoint.ToString())
                    {
                        Clients[clientId].Udp.HandleData(packet);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }

        public static void SendUdpData(IPEndPoint clientEndPoint, Packet packet)
        {
            try
            {
                if (clientEndPoint != null)
                {
                    _udpListener.BeginSend(packet.ToArray(), packet.Length(), clientEndPoint, null, null);
                }
            }
            catch (Exception e)
            {
                // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                Debug.Log(e);
            }
        }
        private static void TcpConnectCallback(IAsyncResult result)
        {
            try
            {
                var client = _tcpListener.EndAcceptTcpClient(result);
                _tcpListener.BeginAcceptTcpClient(TcpConnectCallback, null);
                Debug.Log($"incoming connection from {client.Client.RemoteEndPoint}");
                for (int i = 1; i <= MaxPlayer; i++)
                {
                    if (Clients[i].Tcp.Socket == null)
                    {
                        Clients[i].Tcp.Connect(client);
                        return;
                    }
                }

                Debug.Log($"{client.Client.RemoteEndPoint} failed to connect. Server is full.");
            }
            catch (System.ObjectDisposedException exception)
            {
                Debug.Log("Client disconnected");
            }
        }

        private static void InitializeServerData()
        {
            for (int i = 1; i <= MaxPlayer; i++)
            {
                Clients.Add(i, new Client(i));
            }
            
            var baseAssembly = typeof(IServerHandler).GetTypeInfo().Assembly;

            var types = baseAssembly.DefinedTypes
                .Where(typeInfo => typeInfo.ImplementedInterfaces.Any(inter => inter == typeof(IServerHandler)))
                .Select(x=>(IServerHandler)Activator.CreateInstance(x.AsType()));

            PacketHandlers = types.ToDictionary(x => x.ClientPacket);
            
            Debug.Log("Packet Handlers Initialized");
        }

        public static void Stop()
        {
            _tcpListener.Stop();
            _udpListener.Close();
        }
    }