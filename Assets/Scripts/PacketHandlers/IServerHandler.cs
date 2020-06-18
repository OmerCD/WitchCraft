namespace PacketHandlers
{
    public interface IServerHandler
    {
        ClientPackets ClientPacket { get; }
        void Handle(int fromClient, Packet packet);
    }
}