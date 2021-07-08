using System.IO;
using Terrain.Packets.Plugin;

namespace Terrain.Packets.Compatibility
{
    public class ImportantPackets
    {
        public class ServerSend
        {
            public static void BadPacket(int fromClient, string guid, string name)
            {
                using (Main.packets.WriteToClient(nameof(ClientHandle.BadPacket), fromClient, out var writer))
                {
                    writer.Write(guid);
                    writer.Write(name != null);
                    if (name != null) writer.Write(name);
                }
            }
        }

        public class ClientSend
        {
            public static void BadPacket(string guid, string name)
            {
                using (Main.packets.WriteToServer(nameof(ServerHandle.BadPacket), out var writer))
                {
                    writer.Write(guid);
                    writer.Write(name != null);
                    if (name != null) writer.Write(name);
                }
            }
        }
        
        public class ServerHandle
        {
            [OffroadPacket]
            public static void BadPacket(int fromClient, BinaryReader reader)
            {
                var guid = reader.ReadString();
                var hasName = reader.ReadBoolean();
                var name = hasName ? reader.ReadString() : null;
                var username = Server.clients[fromClient].player.username;
                if (hasName) {
                    Main.Error($"{username} doesn't have packet {guid}/{name}.");
                }
                else
                {
                    Main.Error($"{username} doesn't have packet {guid}");
                }
            }
        }

        static class ClientHandle
        {
            [OffroadPacket]
            public static void BadPacket(BinaryReader reader) {
                var guid = reader.ReadString();
                var hasName = reader.ReadBoolean();
                var name = hasName ? reader.ReadString() : null;
                if (hasName) {
                    Main.Error($"Server doesn't have packet {guid}/{name}.");
                }
                else
                {
                    Main.Error($"Server doesn't have packet {guid}");
                }
            }
        }
    }
}