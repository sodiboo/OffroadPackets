using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Steamworks;
using Terrain.Packets.LowLevelNetworking;

namespace Terrain.Packets
{
    /// <summary>
    /// A wrapper object that holds onto a callback and some state for writing Offroad Packets. You should put this in a using statement, in your send method, because when this is disposed, the packet is sent.
    /// </summary>
    public class OffroadPacketWriter : BinaryWriter
    {
        private readonly MemoryStream stream;
        private readonly Action<byte[]> callback;
        private bool sent;
        internal OffroadPacketWriter(Action<byte[]> callback, MemoryStream stream) : base(stream)
        {
            this.callback = callback;
            this.stream = stream;
        }

        /// <summary>
        /// Sends the packet and disposes the stream it wrote to.
        /// </summary>
        public void Send()
        {
            sent = true;
            callback(stream.GetBuffer());
            Dispose();
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (!sent) UnityEngine.Debug.LogWarning($"Packet writer was disposed but not sent\n{new StackTrace()}");
            base.Dispose(disposing);
        }
    }

    public partial class OffroadPackets
    {
        /// <summary>
        /// All the methods that handle packets sent by the server, mapped by their name.
        /// </summary>
        public Dictionary<string, ServerRawPacketHandler> ServerPacketHandlers = new();

        /// <summary>
        /// All the methods that handle packets sent by the client, mapped by their name.
        /// </summary>
        public Dictionary<string, ClientRawPacketHandler> ClientPacketHandlers = new();

        /// <summary>
        /// A method that is ran on the client and handles a packet sent by the server.
        /// </summary>
        /// <param name="reader">The contents of the packet the server sent.</param>
        /// <remarks>
        /// A handler method with this signature will be placed in a wrapper method that creates and manages the reader and its base stream.
        /// </remarks>
        public delegate void ServerPacketHandler(BinaryReader reader);

        /// <summary>
        /// A method that is ran on the server and handles a packet sent by a client.
        /// </summary>
        /// <param name="fromClient">The client ID that sent this packet.</param>
        /// <param name="reader">The contents of the packet the client sent.</param>
        /// <remarks>
        /// A handler method with this signature will be placed in a wrapper method that creates and manages the reader and its base stream.
        /// </remarks>
        public delegate void ClientPacketHandler(int fromClient, BinaryReader reader);

        /// <summary>
        /// A method that is ran on the client and handles a packet sent by the server.
        /// </summary>
        /// <param name="data">The contents of the packet the server sent.</param>
        /// <remarks>
        /// If you send a packet using the <c>WriteTo**</c> methods, the buffer may be bigger than the content actually written, therefore you shouldn't use <c>WriteTo**</c> and raw packet handlers in combination.
        /// </remarks>
        public delegate void ServerRawPacketHandler(byte[] data);

        /// <summary>
        /// A method that is ran on the server and handles a packet sent by a client.
        /// </summary>
        /// <param name="fromClient">The client ID that sent this packet.</param>
        /// <param name="data">The contents of the packet the client sent.</param>
        /// <remarks>
        /// If you send a message using the <see cref="WriteToServer" /> method, the buffer may be bigger than the content actually written, therefore you shouldn't use <see cref="WriteToServer" /> and a raw packet handler in combination.
        /// </remarks>
        public delegate void ClientRawPacketHandler(int fromClient, byte[] data);

        /// <summary>
        /// Registers a method that will run on the client and handle a packet sent by the server.
        /// </summary>
        /// <param name="name">The name of the packet to register.</param>
        /// <param name="handler">The method that handles this packet.</param>
        /// <remarks>
        /// If you register your handler using this method, your handler will be called from a wrapper method that creates and manages the reader and its base stream.
        /// </remarks>
        public void Handle(string name, ServerPacketHandler handler) => Handle(name, WrapServerPacketHandler(handler));

        /// <summary>
        /// Registers a method that will run on the client and handle a packet sent by the server.
        /// </summary>
        /// <param name="name">The name of the packet to register.</param>
        /// <param name="handler">The method that handles this packet.</param>
        public void Handle(string name, ServerRawPacketHandler handler) => ServerPacketHandlers[name] = handler;

        /// <summary>
        /// Registers a method that will run on the server and handle a packet sent by the client.
        /// </summary>
        /// <param name="name">The name of the packet to register.</param>
        /// <param name="handler">The method that handles this packet.</param>
        /// <remarks>
        /// If you register your handler using this method, your handler will be called from a wrapper method that creates and manages the reader and its base stream.
        /// </remarks>
        public void Handle(string name, ClientPacketHandler handler) => Handle(name, WrapClientPacketHandler(handler));

        /// <summary>
        /// Registers a method that will run on the server and handle a packet sent by the client.
        /// </summary>
        /// <param name="name">The name of the packet to register.</param>
        /// <param name="handler">The method that handles this packet.</param>
        public void Handle(string name, ClientRawPacketHandler handler) => ClientPacketHandlers[name] = handler;

        private static ClientRawPacketHandler WrapClientPacketHandler(ClientPacketHandler handler) => (fromClient, data) =>
        {
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);
            handler(fromClient, reader);
        };

        private static ServerRawPacketHandler WrapServerPacketHandler(ServerPacketHandler handler) => (data) =>
        {
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);
            handler(reader);
        };
        /// <summary>
        /// Starts writing a packet that will be sent to the server.
        /// </summary>
        /// <param name="name">The name of the packet you want to send.</param>
        /// <param name="type">The <see cref="P2PSend" /> to use for this request.</param>
        /// <returns>An <see cref="OffroadPacketWriter" /> that you can use to write your packet.</returns>
        public OffroadPacketWriter WriteToServer(string name, P2PSend type = P2PSend.Reliable) => new(bytes => SendToServer(name, bytes, type), new MemoryStream());

        /// <summary>
        /// Starts writing a packet that will be sent to only the client with id <paramref name="client" />.
        /// </summary>
        /// <param name="name">The name of the packet you want to send.</param>
        /// <param name="client">The client id that will receive this packet.</param>
        /// <param name="type">The <see cref="P2PSend" /> to use for this request.</param>
        /// <returns>An <see cref="OffroadPacketWriter" /> that you can use to write your packet.</returns>
        public OffroadPacketWriter WriteToClient(string name, int client, P2PSend type = P2PSend.Reliable) => new(bytes => SendToClient(name, client, bytes, type), new MemoryStream());

        /// <summary>
        /// Starts writing a packet that will be sent to every connected client (including the local one).
        /// </summary>
        /// <param name="name">The name of the packet you want to send.</param>
        /// <param name="type">The <see cref="P2PSend" /> to use for this request.</param>
        /// <returns>An <see cref="OffroadPacketWriter" /> that you can use to write your packet.</returns>
        public OffroadPacketWriter WriteToAll(string name, P2PSend type = P2PSend.Reliable) => new(bytes => SendToAll(name, bytes, type), new MemoryStream());

        /// <summary>
        /// Starts writing a packet that will be sent to every client in the lobby except for the client with id <paramref name="client" />.
        /// </summary>
        /// <param name="name">The name of the packet you want to send.</param>
        /// <param name="client">The client id that will not receive this packet.</param>
        /// <param name="type">The <see cref="P2PSend" /> to use for this request.</param>
        /// <returns>An <see cref="OffroadPacketWriter" /> that you can use to write your packet.</returns>
        public OffroadPacketWriter WriteToAllExcept(string name, int client, P2PSend type = P2PSend.Reliable) => new(bytes => SendToAllExcept(name, client, bytes, type), new MemoryStream());

        /// <summary>
        /// Starts writing a packet that will be sent to every client in the lobby except for the clients with the ids in <paramref name="clients" />
        /// </summary>
        /// <param name="name">The name of the packet you want to send.</param>
        /// <param name="clients">The client ids that will not receive this packet.</param>
        /// <param name="type">The <see cref="P2PSend" /> to use for this request.</param>
        /// <returns>An <see cref="OffroadPacketWriter" /> that you can use to write your packet.</returns>
        public OffroadPacketWriter WriteToAllExcept(string name, int[] clients, P2PSend type = P2PSend.Reliable) => new(bytes => SendToAllExcept(name, clients, bytes, type), new MemoryStream());

        /// <summary>
        /// Sends a packet to the server with the data in <paramref name="bytes" />.
        /// </summary>
        /// <param name="name">The name of the packet you want to send.</param>
        /// <param name="bytes">The raw buffer of bytes to send.</param>
        /// <param name="type">The <see cref="P2PSend" /> to use for this request.</param>
        public void SendToServer(string name, byte[] bytes, P2PSend type)
        {
            using var packet = new Packet(0);
            packet.Write(guid);
            packet.Write(name);
            packet.Write(bytes);

            Send.ToServer(packet, type);
        }

        /// <summary>
        /// Sends a packet to the specified <paramref name="client" /> with the data in <paramref name="bytes" />.
        /// </summary>
        /// <param name="name">The name of the packet you want to send.</param>
        /// <param name="client">The client id that will receive this packet.</param>
        /// <param name="bytes">The raw buffer of bytes to send.</param>
        /// <param name="type">The <see cref="P2PSend" /> to use for this request.</param>
        public void SendToClient(string name, int client, byte[] bytes, P2PSend type)
        {
            using var packet = new Packet(0);
            packet.Write(guid);
            packet.Write(name);
            packet.Write(bytes);

            Send.To(client, packet, type);
        }

        /// <summary>
        /// Sends a packet to every connected client (including the local one) with the data in <paramref name="bytes" />.
        /// </summary>
        /// <param name="name">The name of the packet you want to send.</param>
        /// <param name="bytes">The raw buffer of bytes to send.</param>
        /// <param name="type">The <see cref="P2PSend" /> to use for this request.</param>
        public void SendToAll(string name, byte[] bytes, P2PSend type)
        {
            using var packet = new Packet(0);
            packet.Write(guid);
            packet.Write(name);
            packet.Write(bytes);

            Send.ToAll(packet, type);
        }

        /// <summary>
        /// Sends a packet to every connected client except for <paramref name="client" /> with the data in <paramref name="bytes" />.
        /// </summary>
        /// <param name="name">The name of the packet you want to send.</param>
        /// <param name="client">The client id that will not receive this packet.</param>
        /// <param name="bytes">The raw buffer of bytes to send.</param>
        /// <param name="type">The <see cref="P2PSend" /> to use for this request.</param>
        public void SendToAllExcept(string name, int client, byte[] bytes, P2PSend type)
        {
            using var packet = new Packet(0);
            packet.Write(guid);
            packet.Write(name);
            packet.Write(bytes);

            Send.ToAllExcept(client, packet, type);
        }

        /// <summary>
        /// Sends a packet to every connected client except for those in <paramref name="clients" /> with the data in <paramref name="bytes" />.
        /// </summary>
        /// <param name="name">The name of the packet you want to send.</param>
        /// <param name="clients">The client ids that will not receive this packet.</param>
        /// <param name="bytes">The raw buffer of bytes to send.</param>
        /// <param name="type">The <see cref="P2PSend" /> to use for this request.</param>
        public void SendToAllExcept(string name, int[] clients, byte[] bytes, P2PSend type)
        {
            using var packet = new Packet(0);
            packet.Write(guid);
            packet.Write(name);
            packet.Write(bytes);

            Send.ToAllExcept(clients, packet, type);
        }
    }
}