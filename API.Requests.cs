using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Terrain.Packets
{
    enum RejectionReason : byte
    {
        UnknownPacket,
        InternalException,
        CustomMessage,
    }

    /// <summary>
    /// An exception whose message will be sent across the network if thrown in a request handler.
    /// </summary>
    public class RejectedRequestException : Exception
    {
        /// <summary>
        /// Creates an exception that will be sent across the network if thrown in a request handler. Due to privacy and security concerns, other exceptions are not sent by default.
        /// </summary>
        /// <param name="message">The message that will be sent to the receiver instead of a response.</param>
        public RejectedRequestException(string message) : base(message) { }
    }

    /// <summary>
    /// A helper class to write requests. If you don't call <see cref="Send" />, your request will not be sent.
    /// </summary>
    public class OffroadRequest : BinaryWriter
    {
        internal OffroadRequest(Func<byte[], Task<BinaryReader>> callback, MemoryStream stream) : base(stream)
        {
            this.stream = stream;
            this.callback = callback;
        }

        private readonly MemoryStream stream;
        private readonly Func<byte[], Task<BinaryReader>> callback;

        /// <summary>
        /// Finishes writing this request and sends it. This will also dispose the current instance.
        /// </summary>
        /// <returns>A task that may or may not complete with the response the receiver wrote.</returns>
        public Task<BinaryReader> Send()
        {
            var bytes = stream.GetBuffer();
            Dispose();
            return callback(bytes);
        }
    }

    public partial class OffroadPackets
    {
        /// <summary>
        /// All the methods that handle requests sent by the server, mapped by their name.
        /// </summary>
        public Dictionary<string, ServerRawRequestHandler> ServerRequestHandlers = new();

        /// <summary>
        /// All the methods that handle requests sent by the client, mapped by their name.
        /// </summary>
        public Dictionary<string, ClientRawRequestHandler> ClientRequestHandlers = new();

        /// <summary>
        /// A method that runs on the client and responds to a request sent by the server.
        /// </summary>
        /// <param name="reader">The contents of the request the server sent</param>
        /// <param name="writer">The response that you can write back to the server.</param>
        /// <returns>A task that should complete when the entire response has been written to <paramref name="writer" />, or that throws if the request couldn't be responded to.</returns>
        /// <remarks>
        /// Since requests are asynchronous, the handlers are also permitted to be asynchronous. And since your handler is asynchronous, it may not always run on the main thread!
        /// Take great caution before interacting with Unity, because it doesn't generally like when you do things off the main thread. <br />
        /// If your method is synchronous, you can just return <see cref="Task.CompletedTask" />.
        /// </remarks>
        public delegate Task ServerRequestHandler(BinaryReader reader, BinaryWriter writer);

        /// <summary>
        /// A method that runs on the server and responds to a request sent by a client.
        /// </summary>
        /// <param name="fromClient">The client ID that sent this request.</param>
        /// <param name="reader">The contents of the request the client sent</param>
        /// <param name="writer">The response that you can write back to the client.</param>
        /// <returns>A task that should complete when the entire response has been written to <paramref name="writer" />, or that throws if the request couldn't be responded to.</returns>
        /// <remarks>
        /// Since requests are asynchronous, the handlers are also permitted to be asynchronous. And since your handler is asynchronous, it may not always run on the main thread!
        /// Take great caution before interacting with Unity, because it doesn't generally like when you do things off the main thread. <br />
        /// If your method is synchronous, you can just return <see cref="Task.CompletedTask" />.
        /// </remarks>
        public delegate Task ClientRequestHandler(int fromClient, BinaryReader reader, BinaryWriter writer);

        /// <summary>
        /// A method that runs on the client and responds to a request sent by the server.
        /// </summary>
        /// <param name="data">The contents of the request the server sent.</param>
        /// <returns>A task that should complete with the response, or that throws if the request couldn't be responded to.</returns>
        /// <remarks>
        /// Since requests are asynchronous, the handlers are also permitted to be asynchronous. And since your handler is asynchronous, it may not always run on the main thread!
        /// Take great caution before interacting with Unity, because it doesn't generally like when you do things off the main thread. <br />
        /// If your method is synchronous, you can just return <see cref="Task.FromResult{TResult}(TResult)" />. <br />
        /// If you send a message using the <see cref="WriteToClient"/> method, the buffer may be bigger than the content actually written, therefore you shouldn't use <see cref="WriteToClient"/> and a raw request handler in combination.
        /// </remarks>
        public delegate Task<byte[]> ServerRawRequestHandler(byte[] data);

        /// <summary>
        /// A method that runs on the server and responds to a request sent by a client.
        /// </summary>
        /// <param name="fromClient">The client ID that sent this request.</param>
        /// <param name="data">The contents of the request the client sent.</param>
        /// <returns>A task that should complete with the response, or that throws if the request couldn't be responded to.</returns>
        /// <remarks>
        /// Since requests are asynchronous, the handlers are also permitted to be asynchronous. And since your handler is asynchronous, it may not always run on the main thread!
        /// Take great caution before interacting with Unity, because it doesn't generally like when you do things off the main thread. <br />
        /// If your method is synchronous, you can just return <see cref="Task.FromResult{TResult}(TResult)" />. <br />
        /// If you send a message using the <see cref="WriteToServer"/> method, the buffer may be bigger than the content actually written, therefore you shouldn't use <see cref="WriteToServer"/> and a raw request handler in combination.
        /// </remarks>
        public delegate Task<byte[]> ClientRawRequestHandler(int fromClient, byte[] data);

        private int serverRequestId = 0;
        private int clientRequestId = 0;

        // the key with the client id is redundant (ids are unique), but it prevents a client from answering another client's request
        internal readonly Dictionary<(int client, int id), PendingRequest> PendingServerRequests = new();
        // the same precaution does not need to be taken with the server, because there is only one server
        internal readonly Dictionary<int, PendingRequest> PendingClientRequests = new();

        /// <summary>
        /// Registers a method that will run on the client and respond to a request sent by the server.
        /// </summary>
        /// <param name="name">The name of the request to register.</param>
        /// <param name="handler">The method that handles this request.</param>
        /// <remarks>
        /// If you register your handler using this method, your handler will be called from a wrapper method that creates and manages the reader, writer, and their base streams.
        /// </remarks>
        public void Handle(string name, ServerRequestHandler handler) => Handle(name, WrapServerRequestHandler(handler));

        /// <summary>
        /// Registers a method that will run on the client and respond to a request sent by the server.
        /// </summary>
        /// <param name="name">The name of the request to register.</param>
        /// <param name="handler">The method that handles this request.</param>
        public void Handle(string name, ServerRawRequestHandler handler) => ServerRequestHandlers[name] = handler;

        /// <summary>
        /// Registers a method that will run on the server and respond to a request sent by the client.
        /// </summary>
        /// <param name="name">The name of the request to register.</param>
        /// <param name="handler">The method that handles this request.</param>
        /// <remarks>
        /// If you register your handler using this method, your handler will be called from a wrapper method that creates and manages the reader, writer, and their base streams.
        /// </remarks>
        public void Handle(string name, ClientRequestHandler handler) => Handle(name, WrapClientRequestHandler(handler));

        /// <summary>
        /// Registers a method that will run on the server and respond to a request sent by the client.
        /// </summary>
        /// <param name="name">The name of the request to register.</param>
        /// <param name="handler">The method that handles this request.</param>
        public void Handle(string name, ClientRawRequestHandler handler) => ClientRequestHandlers[name] = handler;

        private static ClientRawRequestHandler WrapClientRequestHandler(ClientRequestHandler handler) => async (fromClient, data) =>
        {
            using var readStream = new MemoryStream(data);
            using var reader = new BinaryReader(readStream);

            using var writeStream = new MemoryStream();
            using var writer = new BinaryWriter(writeStream);

            await handler(fromClient, reader, writer);
            return writeStream.GetBuffer();
        };

        private static ServerRawRequestHandler WrapServerRequestHandler(ServerRequestHandler handler) => async (data) =>
        {
            using var readStream = new MemoryStream(data);
            using var reader = new BinaryReader(readStream);

            using var writeStream = new MemoryStream();
            using var writer = new BinaryWriter(writeStream);

            await handler(reader, writer);
            return writeStream.GetBuffer();
        };

        /// <summary>
        /// Sends a request to the server with the data in <paramref name="request" />. <br />
        /// To save some memory and latency, sending a request as the server will directly call the handler method and skip any packets.
        /// As a side effect, if you call this from the server, the return value will be exactly the <see cref="Task{TResult}" /> the handler method returned.
        /// If your handler uses <see cref="BinaryReader" />s and <see cref="BinaryWriter" />s, it may not be the exact same <see cref="Task{TResult}" />, but it will complete with the same exceptions.
        /// </summary>
        /// <param name="name">The name of the packet you want to send.</param>
        /// <param name="request">The raw buffer of bytes to send.</param>
        /// <returns>The task that may or may not complete with the bytes the server responds with.</returns>
        public Task<byte[]> SendRequestToServer(string name, byte[] request)
        {
            if (LocalClient.instance.serverHost == SteamManager.Instance.PlayerSteamId)
            {
                return ClientRequestHandlers[name](SteamLobby.steamIdToClientId[LocalClient.instance.serverHost], request);
            }

            var id = clientRequestId++;
            var pending = new PendingRequest(new TaskCompletionSource<byte[]>(), name);
            PendingClientRequests[id] = pending;
            ImportantPackets.ClientSend.StartClientRequest(guid, name, id, request);
            return pending.completionSource.Task;
        }

        /// <summary>
        /// Sends a request to the client with the id <paramref name="client" /> with the data in <paramref name="request" />. <br />
        /// To save some memory and latency, sending a request to the local client will directly call the handler method and skip any packets.
        /// As a side effect, if you send a request to the local client, the return value will be exactly the <see cref="Task{TResult}" /> the handler method returned.
        /// If your handler uses <see cref="BinaryReader" />s and <see cref="BinaryWriter" />s, it may not be the exact same <see cref="Task{TResult}" />, but it will complete with the same exceptions.
        /// </summary>
        /// <param name="name">The name of the packet you want to send.</param>
        /// <param name="client">The client id that will receive, handle, and answer this request.</param>
        /// <param name="request">The raw buffer of bytes to send.</param>
        /// <returns>The task that may or may not complete with the bytes the client responds with.</returns>
        public Task<byte[]> SendRequestToClient(string name, int client, byte[] request)
        {
            if (client == LocalClient.instance.myId)
            {
                return ServerRequestHandlers[name](request);
            }

            var id = serverRequestId++;
            var pending = new PendingRequest(new TaskCompletionSource<byte[]>(), name);
            PendingServerRequests[(client, id)] = pending;
            ImportantPackets.ServerSend.StartServerRequest(client, guid, name, id, request);
            return pending.completionSource.Task;
        }

        /// <summary>
        /// Starts writing a request to the server.
        /// </summary>
        /// <param name="name">The name of the packet you want to send.</param>
        /// <returns>An <see cref="OffroadRequest" /> you can use to write and send this request.</returns>
        /// <remarks>
        /// To save some memory and latency, sending a request as the server will directly call the handler method and skip any packets.
        /// As a side effect, if you call this from the server, the final <see cref="Task{TResult}" /> will complete with the same exceptions as the handler method.
        /// </remarks>
        public OffroadRequest WriteRequestToServer(string name) => new(async bytes =>
        {
            var response = await SendRequestToServer(name, bytes);
            return new BinaryReader(new MemoryStream(response));
        }, new MemoryStream());

        /// <summary>
        /// Starts writing a request to the client with id <paramref name="client" />.
        /// </summary>
        /// <param name="name">The name of the packet you want to send.</param>
        /// <param name="client">The client id to send this request to.</param>
        /// <returns>An <see cref="OffroadRequest" /> you can use to write and send this request.</returns>
        /// <remarks>
        /// To save some memory and latency, sending a request to the local client will directly call the handler method and skip any packets.
        /// As a side effect, if you send a request to the local client, the final <see cref="Task{TResult}" /> will complete with the same exceptions as the handler method.
        /// </remarks>
        public OffroadRequest WriteRequestToClient(string name, int client) => new(async bytes =>
        {
            var response = await SendRequestToClient(name, client, bytes);
            return new BinaryReader(new MemoryStream(response));
        }, new MemoryStream());

        internal class PendingRequest
        {
            public PendingRequest(TaskCompletionSource<byte[]> completionSource, string packet)
            {
                this.completionSource = completionSource;
                this.packet = packet;
            }
            public readonly TaskCompletionSource<byte[]> completionSource;
            public readonly string packet;
        }
    }
}