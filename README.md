# Offroad Packets

This is a library for sending and receiving custom packets that won't ever conflict. The names of packets are transmitted as strings, which is less efficient, but guarantees you'll have no conflicting IDs and also that the order you register them in doesn't matter.

This library only registers the packet ID ``0``, because it's not used by the base game. Unless a mod GUID only contains the characters ``%*/49>@EJOTY^`ejoty~`` after a space anywhere, this also won't conflict with iiVeil's PacketHelper. It should actually be completely cross-compatible and you should be able to use both at once if you really wanna. If your GUID's offset evaluates to 0, you have bigger problems such as conflicting with the game if you register so much as 2 packets anyways, and that's why i made this library, as it avoids such issues altogether.

## Not on windows?

This library is built against the windows version of Facepunch.Steamworks. If you're not on Windows, install [UnifiedSteamworks](https://muck.thunderstore.io/package/Terrain/UnifiedSteamworks/).

## Terminology

- A "Server Packet" is a packet sent by the server, to a client (or multiple clients).
- A "Client Packet" is a packet sent by a client, to the server.
- A "Server Handler" is a method on the server that handles a client packet.
- A "Client Handler" is a method on the client that handles a server packet.

## Registering packet handlers

There are 2 main ways you can register Offroad Packets. Either you can do it the good-looking way with attributes, or you can register everything manually. Make sure you're ``using Terrain.Packets;``

You don't have to patch any methods to register packets. You should add them only once when your mod initializes and it'll be persistent even if the network objects are destroyed.

## The manual way

First, you need to create a ``new OffroadPackets(string guid)`` with your mod's GUID. This doesn't have to be your mod's GUID, but generally that's what you'll likely want, as it should be unique anyways and this ID also should be unique. You should keep track of this object in your code, but if you don't, you *can* access it through ``OffroadPackets.Instances[guid]``. For the remainder of this document, i'll assume you assigned this to a variable named ``packets``.

To register a server packet handler, call ``packets.HandleClient(string name, ServerPacketHandler handler)``. This method is called ``HandleClient`` because its handler is called from the client, when trying to handle a packet received from the server. ``ServerPacketHandler`` is a delegate type, which represents a function that returns void and takes a ``BinaryReader`` argument.

To register a client packet handler, call ``packets.HandleServer(string name, ClientPacketHandler handler)``. This method is called ``HandleServer`` because its handler is called from the server, when trying to handle a packet received from the client. ``ClientPacketHandler`` is almost like ``ServerPacketHandler``, except it first takes an ``int`` argument that represents which client sent the packet.

## The Beautiful™ way

Create a new class with an ``OffroadPacketAttribute``. Or don't, i don't care. Fuck it, if you don't wanna create a new class, that's cool too, add your packet handlers in your plugin, why not? If you do add an attribute, the GUID of your mod's packets will be the ``name`` field, or a default value that depends on how exactly you register it.

In the plugin's constructor (or initializer, whichever you prefer) call ``OffroadPackets.Register()``. This will cause it to search through your whole assembly, looking for any types with the ``OffroadPacketAttribute``. If it does find one, the packet GUID is its ``name`` field, or the type's fully qualified name if you don't specify one. If it cannot find a type with this attribute, it defaults to the calling type which must then have a ``BepInPlugin`` attribute (hence, call it from your plugin's constructor) and its GUID is used for the packet GUID. This is the most convenient way for quickly getting set up.

If you want to save some time on initialization, or want it to use the plugin GUID instead of the attribute, call ``OffroadPackets.Register<T>()`` instead, and directly specify the handler type as ``T``. If the type has an ``OffroadPacketAttribute``, its ``name`` field is used for the packet GUID, defaulting to the fully qualified name. If the type does *not* have an ``OffroadPacketAttribute``, the current ``BepInPlugin``'s guid is used instead. This is usually the prettiest way, and is a bit less confusing. As mentioned at the start, it is also faster, even if probably not by a lot.

``OffroadPackets.Register()`` and ``OffroadPackets.Register<T>()`` both return the instance of ``OffroadPackets`` it created. You should keep track of this object in your code, but if you don't, you *can* access it through ``OffroadPackets.Instances[guid]``. For the remainder of this document, i'll assume you assigned this to a variable named ``packets``.

To register a packet handler, simply add a **__static__** method with an ``OffroadPacketAttribute`` on it. The name parameter is the packet name (and what you'd need to send that packet), defaulting to the method name. This doesn't have to be directly in the handler type, it can be in nested types down and below forever. It's a recursive search, so you can split up your packet code however you want, be that nested types, regions, partial classes and multiple files, or not at all, it just works. If the method parameter signature is just ``BinaryReader``, it will be a client handler for a server packet. If the method parameter signature is ``int, BinaryReader``, it will be a server handler for a client packet. If a method is not static, doesn't have the attribute, or doesn't fit either of these 2 overloads, it will be ignored.

Does all this sound way too confusing to you? Check out how i handle unknown packets by registering my own packet handler in the ``ImportantPackets.cs`` file. It's also calling ``OffroadPackets.Register<ImportantPackets>()`` in ``Main.cs``.

## Sending packets

There are 5 methods for sending packets. All these methods return an ``IDisposable`` object, which when disposed, actually sends the damn packet and disposes the ``BinaryWriter`` and its underlying ``MemoryStream``. You don't need to ever use this object directly, but you should put it within a ``using()`` statement, as it makes sure it is disposed correctly.

- ``packets.WriteToServer(string name, out BinaryWriter writer, P2PSend type)`` sends a packet from the local client to the server
- ``packets.WriteToClient(string name, int client, out BinaryWriter writer, P2PSend type)`` sends a packet from the server to ``client``
- ``packets.WriteToAll(string name, out BinaryWriter writer, P2PSend type)`` send a packet from the server to every client
- ``packets.WriteToAllExcept(string name, int client, out BinaryWriter writer, P2PSend type)`` sends a packet from the server to every client except ``client``
- ``packets.WriteToAllExcept(string name, int[] clients, out BinaryWriter writer, P2PSend type)`` sends a packet from the server to every client except those in ``clients``

All these methods have an equivalent overload without the last ``P2PSend`` argument, which defaults to ``P2PSend.Reliable`` (TCP).

All of these methods take an ``out BinaryWriter writer``, which is the writer you'll be using to write the packet with. Just in case you don't know, you would specify this parameter as ``out var writer``, and then ``writer`` is the ``BinaryWriter`` for the packet, assigned by the method.

If you don't wanna use these methods and their provided writer (or like, no ``BinaryWriter`` at all), you can also directly send a byte array using any of the following methods:

- ``packets.SendToServer(string name, byte[] bytes, P2PSend type)``
- ``packets.SendToClient(string name, int client, byte[] bytes, P2PSend type)``
- ``packets.SendToAll(string name, byte[] bytes, P2PSend type)``
- ``packets.SendToAllExcept(string name, int client, byte[] bytes, P2PSend type)``
- ``packets.SendToAllExcept(string name, int[] clients, byte[] bytes, P2PSend type)``

These have no equivalent without ``P2PSend``. You will still read the message using a ``BinaryReader``.

## Extension methods

Because ``BinaryWriter`` and ``BinaryReader`` don't have any Unity types, and (surprisingly enough) i couldn't find any implementations of extension methods from that online easily, i've implemented my own extension methods for a lot of Unity types. This project is under the MIT License, feel free to use those extension methods in whatever other projects you want.

---

Icon by [SBTS](https://thenounproject.com/search/?q=file&i=4041625). I haven't modified the icon outside of the preview options The Noun Project provides.