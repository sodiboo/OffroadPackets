using BepInEx;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Terrain.Packets.LowLevelNetworking;

namespace Terrain.Packets
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class OffroadPacketAttribute : Attribute
    {
        internal string name;
        public OffroadPacketAttribute(string name = null)
        {
            this.name = name;
        }
    }

    public class OffroadPacketWriter : IDisposable
    {
        BinaryWriter writer;
        MemoryStream stream;
        Action<byte[]> callback;
        public OffroadPacketWriter(Action<byte[]> callback, out BinaryWriter writer)
        {
            this.callback = callback;
            this.stream = new MemoryStream();
            this.writer = writer = new BinaryWriter(stream);
        }

        public void Dispose()
        {
            callback(stream.GetBuffer());
            writer.Dispose();
            stream.Dispose();
        }
    }

    public class OffroadPackets
    {
        public static Dictionary<string, OffroadPackets> Instances = new Dictionary<string, OffroadPackets>();

        public readonly string guid;

        public Dictionary<string, ServerPacketHandler> ServerPacketHandlers = new Dictionary<string, ServerPacketHandler>();
        public Dictionary<string, ClientPacketHandler> ClientPacketHandlers = new Dictionary<string, ClientPacketHandler>();

        public delegate void ServerPacketHandler(BinaryReader packet);
        public delegate void ClientPacketHandler(int fromClient, BinaryReader packet);

        public static OffroadPackets Register()
        {
            var plugin = new StackFrame(1, false).GetMethod().DeclaringType;
            foreach (var type in plugin.Assembly.GetTypes())
            {
                var handler = type.GetCustomAttribute<OffroadPacketAttribute>();
                if (handler != null) return Register(handler.name ?? type.FullName, type);
            }
            var guid = plugin.GetCustomAttribute<BepInPlugin>().GUID;
            return Register(guid, plugin);
        }

        public static OffroadPackets Register<Handler>()
        {
            var type = typeof(Handler);
            var handler = type.GetCustomAttribute<OffroadPacketAttribute>();
            if (handler != null) return Register(handler.name ?? type.FullName, type);

            var plugin = new StackFrame(1, false).GetMethod().DeclaringType;
            var guid = plugin.GetCustomAttribute<BepInPlugin>().GUID;
            return Register(guid, type);
        }

        private static OffroadPackets Register(string guid, Type handler)
        {
            var packetHandlers = GetPacketHandlers(handler);
            var packets = new OffroadPackets(guid);
            foreach (var packetHandler in packetHandlers)
            {
                if (packetHandler.server)
                {
                    packets.HandleServer(packetHandler.name, (fromClient, reader) => packetHandler.method.Invoke(null, new object[] { fromClient, reader }));
                }
                else
                {
                    packets.HandleClient(packetHandler.name, reader => packetHandler.method.Invoke(null, new object[] { reader }));
                }
            }
            return packets;
        }

        private static IEnumerable<(string name, MethodInfo method, bool server)> GetPacketHandlers(Type type)
        {
            foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var attr = method.GetCustomAttribute<OffroadPacketAttribute>();
                if (attr == null) continue;
                var name = attr.name ?? method.Name;
                var parameters = method.GetParameters();
                if (parameters.Length == 0 || parameters.Length > 2) continue;
                if (parameters.Length == 1)
                {
                    if (!parameters[0].ParameterType.IsAssignableFrom(typeof(BinaryReader))) continue;
                    yield return (name, method, false);
                }
                else
                {
                    if (!parameters[0].ParameterType.IsAssignableFrom(typeof(int))) continue;
                    if (!parameters[1].ParameterType.IsAssignableFrom(typeof(BinaryReader))) continue;
                    yield return (name, method, true);
                }
            }

            foreach (var child in type.GetNestedTypes())
            {
                foreach (var e in GetPacketHandlers(child)) yield return e;
            }
        }

        public OffroadPackets(string guid)
        {
            this.guid = guid;
            Instances[guid] = this;
        }

        public void HandleClient(string name, ServerPacketHandler handler)
        {
            ServerPacketHandlers[name] = handler;
        }

        public void HandleServer(string name, ClientPacketHandler handler)
        {
            ClientPacketHandlers[name] = handler;
        }

        public OffroadPacketWriter WriteToServer(string name, out BinaryWriter writer) => WriteToServer(name, out writer, P2PSend.Reliable);

        public OffroadPacketWriter WriteToServer(string name, out BinaryWriter writer, P2PSend type)
        {
            return new OffroadPacketWriter(bytes => SendToServer(name, bytes, type), out writer);
        }

        public OffroadPacketWriter WriteToClient(string name, int client, out BinaryWriter writer) => WriteToClient(name, client, out writer, P2PSend.Reliable);

        public OffroadPacketWriter WriteToClient(string name, int client, out BinaryWriter writer, P2PSend type)
        {
            return new OffroadPacketWriter(bytes => SendToClient(name, client, bytes, type), out writer);
        }

        public OffroadPacketWriter WriteToAll(string name, out BinaryWriter writer) => WriteToAll(name, out writer, P2PSend.Reliable);

        public OffroadPacketWriter WriteToAll(string name, out BinaryWriter writer, P2PSend type)
        {
            return new OffroadPacketWriter(bytes => SendToAll(name, bytes, type), out writer);
        }

        public OffroadPacketWriter WriteToAllExcept(string name, int client, out BinaryWriter writer) => WriteToAllExcept(name, client, out writer, P2PSend.Reliable);

        public OffroadPacketWriter WriteToAllExcept(string name, int client, out BinaryWriter writer, P2PSend type)
        {
            return new OffroadPacketWriter(bytes => SendToAllExcept(name, client, bytes, type), out writer);
        }

        public OffroadPacketWriter WriteToAllExcept(string name, int[] clients, out BinaryWriter writer) => WriteToAllExcept(name, clients, out writer, P2PSend.Reliable);

        public OffroadPacketWriter WriteToAllExcept(string name, int[] clients, out BinaryWriter writer, P2PSend type)
        {
            return new OffroadPacketWriter(bytes => SendToAllExcept(name, clients, bytes, type), out writer);
        }

        public void SendToServer(string name, byte[] bytes, P2PSend type)
        {
            using (var packet = new Packet(0))
            {
                packet.Write(guid);
                packet.Write(name);
                packet.Write(bytes);

                Send.ToServer(packet, type);
            }
        }

        public void SendToClient(string name, int client, byte[] bytes, P2PSend type)
        {
            using (var packet = new Packet(0))
            {
                packet.Write(guid);
                packet.Write(name);
                packet.Write(bytes);

                Send.To(client, packet, type);
            }
        }

        public void SendToAll(string name, byte[] bytes, P2PSend type)
        {
            using (var packet = new Packet(0))
            {
                packet.Write(guid);
                packet.Write(name);
                packet.Write(bytes);

                Send.ToAll(packet, type);
            }
        }

        public void SendToAllExcept(string name, int client, byte[] bytes, P2PSend type)
        {
            using (var packet = new Packet(0))
            {
                packet.Write(guid);
                packet.Write(name);
                packet.Write(bytes);

                Send.ToAllExcept(client, packet, type);
            }
        }

        public void SendToAllExcept(string name, int[] clients, byte[] bytes, P2PSend type)
        {
            using (var packet = new Packet(0))
            {
                packet.Write(guid);
                packet.Write(name);
                packet.Write(bytes);

                Send.ToAllExcept(clients, packet, type);
            }
        }
    }
}