using UnityEngine;

namespace PacketHandlers
{
    public class WelcomeReceivedHandler :MonoBehaviour, IServerHandler
    {
        public ClientPackets ClientPacket => ClientPackets.WelcomeReceived;
        public void Handle(int fromClient, Packet packet)
        {
            var clientId = packet.ReadInt();
            var username = packet.ReadString();
            Debug.Log($"{Server.Clients[clientId].Tcp.Socket.Client.RemoteEndPoint} connected as {username} with Id : {fromClient}");

            if (fromClient != clientId)
            {
                Debug.Log($"Player \"{username}\" (ID: {fromClient} has assumed the wrong client Id ({clientId})");
            }
            Server.Clients[fromClient].SendIntoGame(username);
        }
    }
}