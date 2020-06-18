namespace PacketHandlers
{
    public class PlayerThrowItemHandler : IServerHandler
    {
        public ClientPackets ClientPacket => ClientPackets.PlayerThrowItem;
        public void Handle(int fromClient, Packet packet)
        {
            var throwDirection = packet.ReadVector3();
            Server.Clients[fromClient].Player.ThrowItem(throwDirection);
        }
    }
}