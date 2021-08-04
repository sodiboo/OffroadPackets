# Offroad Packets

This is a library for sending and receiving custom packets that won't ever conflict. The names of packets are transmitted as strings, which is less efficient, but guarantees you'll have no conflicting IDs and also that the order you register them in doesn't matter.

This library only registers the packet ID ``0``, because it's not used by the base game. Unless a mod GUID only contains the characters ``%*/49>@EJOTY^`ejoty~`` after a space anywhere, this also won't conflict with iiVeil's PacketHelper. It should actually be completely cross-compatible and you should be able to use both at once if you really wanna. If your GUID's offset evaluates to 0, you have bigger problems such as conflicting with the game if you register so much as 2 packets anyways, and that's why i made this library, as it avoids such issues altogether.

## Not on windows?

This library is built against the windows version of Facepunch.Steamworks. If you're not on Windows, install [UnifiedSteamworks](https://muck.thunderstore.io/package/Terrain/UnifiedSteamworks/).

## XML Documentation

The download includes an ``.xml`` documentation file. As an end user, you can just delete this. As a developer, it provides documentation as you're writing your code.

## Terminology

- A "Server Packet" is a packet sent by the server, to a client (or multiple clients).
- A "Client Packet" is a packet sent by a client, to the server.
- A "Server Packet Handler" is a method on the client that handles a Server Packet.
- A "Client Packet Handler" is a method on the server that handles a Client Packet.
- A "Request" is a special type of packet which also has a response.
- A "Request Handler" is a special type of packet handler which writes a response to the Request.

## Registering packet handlers

There are 2 main ways you can register Offroad Packets. Either you can do it the good-looking way with attributes, or you can register everything manually. Make sure you're ``using Terrain.Packets;``

You don't have to patch any methods to register packets. You should add them only once when your mod initializes and it'll be persistent even if the network objects are destroyed.

## Handler methods

For both methods of registering packet handlers, every handler must have one of the following signatures:

- ``void ServerPacketHandler(BinaryReader reader)``
- ``void ClientPacketHandler(int fromClient, BinaryReader reader)``

- ``void ServerRawPacketHandler(byte[] data)``
- ``void ClientRawPacketHandler(int fromClient, byte[] data)``

- ``Task ServerRequestHandler(BinaryReader reader, BinaryWriter writer)``
- ``Task ClientRequestHandler(int fromClient, BinaryReader reader, BinaryWriter writer)``

- ``Task<byte[]> ServerRawRequestHandler(byte[] data)``
- ``Task<byte[]> ClientRawRequestHandler(int fromClient, byte[] data)``

The raw handlers get a byte array which is exactly the data received. The non-raw handlers are wrapped in a method that manages their readers and writers.

A request handler can be asynchronous.

## The manual way

First, you need to create a ``new OffroadPackets(string guid)`` with your mod's GUID. This doesn't have to be your mod's GUID, but generally that's what you'll likely want, as it should be unique anyways and this ID also should be unique. You should keep track of this object in your code, but if you don't, you *can* access it through ``OffroadPackets.Instances[guid]``. For the remainder of this document, i'll assume you assigned this to a variable named ``packets``.

To register a packet or request handler on the server or the client, call ``packets.Handle(name, handler)``, where ``name`` is a string (usually ``nameof`` your handler method) and ``handler`` fits any of the signatures described in [Handler methods](#handler-methods).

## The Beautiful™ way

Create a new class with an ``OffroadPacketAttribute``. Or don't, i don't care. Fuck it, if you don't wanna create a new class, that's cool too, add your packet handlers in your plugin, why not? If you do add an attribute, the GUID of your mod's packets will be the ``name`` field, or a default value that depends on how exactly you register it.

In the plugin's constructor (or initializer, whichever you prefer) call ``OffroadPackets.Create()``. This will cause it to search through your whole assembly, looking for any types with the ``OffroadPacketAttribute``. If it does find one, the packet GUID is its ``name`` field, or the type's fully qualified name if you don't specify one. If it cannot find a type with this attribute, it defaults to the calling type which must then have a ``BepInPlugin`` attribute (hence, call it from your plugin's constructor) and its GUID is used for the packet GUID. This is the most convenient way for quickly getting set up.

If you want to save some time on initialization, or want it to use the plugin GUID instead of the attribute, call ``OffroadPackets.Create<T>()`` instead, and directly specify the handler type as ``T``. If the type has an ``OffroadPacketAttribute``, its ``name`` field is used for the packet GUID, defaulting to the fully qualified name. If the type does *not* have an ``OffroadPacketAttribute``, the current ``BepInPlugin``'s guid is used instead. This is usually the prettiest way, and is a bit less confusing. As mentioned at the start, it is also faster, even if probably not by a lot.

``OffroadPackets.Create()`` and ``OffroadPackets.Create<T>()`` both return the instance of ``OffroadPackets`` it created. You should keep track of this object in your code, but if you don't, you *can* access it through ``OffroadPackets.Instances[guid]``. For the remainder of this document, i'll assume you assigned this to a variable named ``packets``.

Alternatively, you can create your own ``new OffroadPackets(guid)`` and then call ``packets.Register<T>()`` to manually add annotated handlers from a type. This way you can also add multiple types.

To create a packet handler, simply add a **__static__** method with an ``OffroadPacketAttribute`` on it. The name parameter is the packet name (and what you'd need to send that packet), defaulting to the method name. This doesn't have to be directly in the handler type, it can be in nested types down and below forever. It's a recursive search, so you can split up your packet code however you want, be that nested types, regions, partial classes and multiple files, or not at all, it just works. A method with this attribute must have one of the signatures described in [Handler methods](#handler-methods)

Does all this sound way too confusing to you? Check out how i handle unknown packets by registering my own packet handler in the ``ImportantPackets.cs`` file. It's also calling ``OffroadPackets.Create<ImportantPackets>()`` in ``Main.cs``.

## Sending packets

There are 5 methods for sending packets. All these methods return an ``OffroadPacketWriter`` object, which you can write to (since it inherits from ``BinaryWriter``), and finally call ``OffroadPacketWriter.Send()``, which actually sends the damn packet and disposes the underlying ``MemoryStream``.

- ``packets.WriteToServer(string name, P2PSend type)`` sends a packet from the local client to the server
- ``packets.WriteToClient(string name, int client, P2PSend type)`` sends a packet from the server to ``client``
- ``packets.WriteToAll(string name, P2PSend type)`` send a packet from the server to every client
- ``packets.WriteToAllExcept(string name, int client, P2PSend type)`` sends a packet from the server to every client except ``client``
- ``packets.WriteToAllExcept(string name, int[] clients, P2PSend type)`` sends a packet from the server to every client except those in ``clients``

If you don't wanna use these methods and their provided writer (or like, no ``BinaryWriter`` at all), you can also directly send a byte array using any of the following methods:

- ``packets.SendToServer(string name, byte[] bytes, P2PSend type)``
- ``packets.SendToClient(string name, int client, byte[] bytes, P2PSend type)``
- ``packets.SendToAll(string name, byte[] bytes, P2PSend type)``
- ``packets.SendToAllExcept(string name, int client, byte[] bytes, P2PSend type)``
- ``packets.SendToAllExcept(string name, int[] clients, byte[] bytes, P2PSend type)``

And of course, if you're using these methods in your code, you'll probably want to use raw packet handlers as well.

## Sending requests

There are only two methods for writing and sending requests.

- ``packets.WriteRequestToServer(string name)``
- ``packets.WriteRequestToClient(string name, int client)``

Since requests need a response, it doesn't make sense to send them to multiple clients. These methods return an ``OffroadRequest``, which you can use to write the request body. And once you're done, you can call ``OffroadRequest.Send()``, which returns a ``Task<BinaryReader>``. If you ``await`` this ``Task<BinaryReader>``, you can read the response immediately.

### Caution! Make sure to put the response in a ``using`` statement, and not just the request

Due to complications with async/await, this ``BinaryReader`` and its underlying ``MemoryStream`` **cannot be managed by OffroadPackets**. You need to dispose of it yourself, and the easiest way to do that is with a ``using`` statement.

You can then read the response as you normally would with a ``BinaryReader``, just as in a packet handler. If a ``RejectedRequestException`` is thrown in the request handler, its ``Exception.Message`` will be send across the network and an equal exception will be thrown in the place you sent the request. Due to privacy and security concerns, i'm not gonna automatically send any other kinds of exceptions.

### Caution! Local requests' exceptions *are* forwarded directly

That doesn't mean you should only expect a ``RejectedRequestException`` when sending a request. If a request should be handled and responded to by the local system (i.e. host client sends a request to server), it is treated specially and goes through direct function calls and returns the ``Task<byte[]>`` the request handler returned (or if you're using ``BinaryReader``s/``BinaryWriter``s, some wrapper around it which is in the proper format). Point is, exceptions are bubbled up to the place you send requests directly. OffroadPackets only catches and handles exceptions specially if the requests are handled on a different location than they were sent from.

### Caution! Asynchronous methods may not be executed on the main thread.

Async methods (such as request handlers, and the methods that send requests) will not always be executed on the main thread. **Unity does not like you doing things off the main thread** from what i've heard.

## Extension methods

Because ``BinaryWriter`` and ``BinaryReader`` don't have any Unity types, and (surprisingly enough) i couldn't find any implementations of extension methods from that online easily, i've implemented my own extension methods for a lot of Unity types. This project is under the MIT License, feel free to use those extension methods in whatever other projects you want.

---

Icon by [SBTS](https://thenounproject.com/search/?q=file&i=4041625). I haven't modified the icon outside of the preview options The Noun Project provides.