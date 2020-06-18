namespace PacketHandlers
{
    public class PlayerShootHandler : IServerHandler
    {
        public ClientPackets ClientPacket => ClientPackets.PlayerShoot;
        public void Handle(int fromClient, Packet packet)
        {
            var shootDirection = packet.ReadVector3();
            Server.Clients[fromClient].Player.Shoot(shootDirection);
        }
    }
}