using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

 public class Client
    {
        public const int DataBufferSize = 4096;

        public int Id { get; set; }
        public TCP Tcp { get; set; }
        public UDP Udp { get; set; }
        public Player Player { get; set; }

        public Client(int id)
        {
            Id = id;
            Tcp = new TCP(Id);
            Udp = new UDP(Id);
        }

        public void SendIntoGame(string playerName)
        {
            Player = NetworkManager.Instance.InstantiatePlayer();
            Player.Initialize(Id, playerName);
            foreach (Client client in Server.Clients.Values)
            {
                if (client.Player != null && client.Player.Id != Id)
                {
                    ServerSend.SpawnPlayer(Id, client.Player);
                }
            }
            foreach (Client client in Server.Clients.Values)
            {
                if (client.Player != null)
                {
                    ServerSend.SpawnPlayer(client.Id, Player);
                }
            }

            foreach (var itemSpawner in ItemSpawner.Spawners.Values)
            {
                ServerSend.CreateItemSpawner(Id, itemSpawner.spawnerId, itemSpawner.transform.position, itemSpawner.hasItem);
            }

            foreach (var enemy in Enemy.Enemies.Values)
            {
                ServerSend.SpawnEnemy(Id, enemy);
            }
        }

       
        public class TCP
        {
            public TcpClient Socket { get; set; }
            private readonly int _id;

            private NetworkStream _stream;
            private Packet _receiveData;
            private byte[] _receiveBuffer;

            public TCP(int id)
            {
                _id = id;
            }

            public void Connect(TcpClient socket)
            {
                Socket = socket;
                Socket.ReceiveBufferSize = DataBufferSize;
                Socket.SendBufferSize = DataBufferSize;

                _stream = socket.GetStream();
                _receiveData = new Packet();
                _receiveBuffer = new byte[DataBufferSize];

                _stream.BeginRead(_receiveBuffer, 0, DataBufferSize, ReceiveCallback, null);

                ServerSend.Welcome(_id, "Welcome to the server!");
            }


            private void ReceiveCallback(IAsyncResult result)
            {
                try
                {
                    int byteLength = _stream.EndRead(result);
                    if (byteLength <= 0)
                    {
                        Server.Clients[_id].Disconnect();
                        return;
                    }

                    var data = new byte[byteLength];
                    Array.Copy(_receiveBuffer, data, byteLength);

                    _receiveData.Reset(HandleData(data));

                    _stream.BeginRead(_receiveBuffer, 0, DataBufferSize, ReceiveCallback, null);
                }
                catch (Exception e)
                {
                    Debug.Log("Error Receiving TCP Data :" + e);
                    Server.Clients[_id].Disconnect();
                }
            }

            private bool HandleData(byte[] data)
            {
                bool PacketLengthCheck(ref int i)
                {
                    if (_receiveData.UnreadLength() < 4) return false;
                    i = _receiveData.ReadInt();
                    return i <= 0;
                }

                var packetLength = 0;
                _receiveData.SetBytes(data);
                if (PacketLengthCheck(ref packetLength)) return true;

                while (packetLength > 0 && packetLength <= _receiveData.UnreadLength())
                {
                    var packetBytes = _receiveData.ReadBytes(packetLength);
                    ThreadManager.ExecuteOnMainThread(() =>
                    {
                        using (var packet = new Packet(packetBytes))
                        {
                            var packetId = packet.ReadInt();
                            Server.PacketHandlers[(ClientPackets)packetId].Handle(_id, packet);
                        }
                    });
                    packetLength = 0;
                    if (PacketLengthCheck(ref packetLength)) return true;
                }

                if (packetLength <= 1)
                {
                    return true;
                }

                return false;
            }

            public void SendData(Packet packet)
            {
                try
                {
                    if (Socket != null)
                    {
                        _stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
                    }
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                }
            }
            public void Disconnect()
            {
                Socket.Close();
                _stream = null;
                _receiveData = null;
                Socket = null;
            }
        }
        public class UDP
        {
            private readonly int _id;
            public IPEndPoint EndPoint;

            public UDP(int id)
            {
                _id = id;
            }


            public void Connect(IPEndPoint endPoint)
            {
                EndPoint = endPoint;
            }

            public void SendData(Packet packet)
            {
                try
                {
                    Server.SendUdpData(EndPoint, packet);
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                }
            }
            
            public void HandleData(Packet packetData)
            {
                var packetLength = packetData.ReadInt();
                var packetBytes = packetData.ReadBytes(packetLength);
                
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (var packet = new Packet(packetBytes))
                    {
                        var packetId = packet.ReadInt();
                        Server.PacketHandlers[(ClientPackets)packetId].Handle(_id, packet);
                    }
                });
            }
            public void Disconnect()
            {
                EndPoint = null;
            }
        }

        private void Disconnect()
        {
            Debug.Log($"{Tcp.Socket.Client.RemoteEndPoint} has disconnected");
            ThreadManager.ExecuteOnMainThread(DestroyPlayer);
            Tcp.Disconnect();
            Udp.Disconnect();
            
            ServerSend.PlayerDisconnected(Id);
        }

        private void DestroyPlayer()
        {
            UnityEngine.Object.Destroy(Player.gameObject);
            Player = null;
        }
    }
