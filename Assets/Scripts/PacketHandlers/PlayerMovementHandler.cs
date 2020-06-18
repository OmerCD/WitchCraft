namespace PacketHandlers
{
    public class PlayerMovementHandler : IServerHandler
    {
        public ClientPackets ClientPacket => ClientPackets.PlayerMovement;
        public void Handle(int fromClient, Packet packet)
        {
            var inputs = new bool[packet.ReadInt()];
            for (int i = 0; i < inputs.Length; i++)
            {
                inputs[i] = packet.ReadBool();
            }

            var rotation = packet.ReadQuaternion();
            Server.Clients[fromClient].Player.SetInput(inputs, rotation);
        }
    }
}