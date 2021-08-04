using System;
using System.IO;
using Terrain.Packets.Plugin;
using UnityEngine;

namespace Terrain.Packets
{
    class ImportantPackets
    {
        public static class ServerSend
        {
            public static void BadPacket(int fromClient, string guid, string name)
            {
                using var packet = Main.packets.WriteToClient(nameof(ClientHandle.BadPacket), fromClient);
                packet.Write(guid);
                packet.Write(name != null);
                if (name != null) packet.Write(name);
                packet.Send();
            }

            public static void StartServerRequest(int toClient, string guid, string name, int id, byte[] data)
            {
                using var packet = Main.packets.WriteToClient(nameof(ClientHandle.StartServerRequest), toClient);
                packet.Write(guid);
                packet.Write(name);
                packet.Write(id);
                packet.Write(data.Length);
                packet.Write(data);
                packet.Send();
            }

            public static void ResolveClientRequest(int fromClient, string guid, int id, byte[] data)
            {
                using var packet = Main.packets.WriteToClient(nameof(ClientHandle.ResolveClientRequest), fromClient);
                packet.Write(guid);
                packet.Write(id);
                packet.Write(data.Length);
                packet.Write(data);
                packet.Send();
            }

            public static void RejectClientRequest(int fromClient, string guid, int id, string message)
            {
                using var packet = Main.packets.WriteToClient(nameof(ClientHandle.RejectClientRequest), fromClient);
                packet.Write(guid);
                packet.Write(id);
                packet.Write((byte)RejectionReason.CustomMessage);
                packet.Write(message);
                packet.Send();
            }

            public static void RejectClientRequest(int fromClient, string guid, int id)
            {
                using var packet = Main.packets.WriteToClient(nameof(ClientHandle.RejectClientRequest), fromClient);
                packet.Write(guid);
                packet.Write(id);
                packet.Write((byte)RejectionReason.InternalException);
                packet.Send();
            }

            public static void RejectClientRequest(int fromClient, string guid, int id, bool knowsGUID)
            {
                using var packet = Main.packets.WriteToClient(nameof(ClientHandle.RejectClientRequest), fromClient);
                packet.Write(guid);
                packet.Write(id);
                packet.Write((byte)RejectionReason.UnknownPacket);
                packet.Write(knowsGUID);
                packet.Send();
            }
        }

        public static class ClientSend
        {
            public static void BadPacket(string guid, string name)
            {
                using var packet = Main.packets.WriteToServer(nameof(ServerHandle.BadPacket));
                packet.Write(guid);
                packet.Write(name != null);
                if (name != null) packet.Write(name);
                packet.Send();
            }

            public static void StartClientRequest(string guid, string name, int id, byte[] data)
            {
                using var packet = Main.packets.WriteToServer(nameof(ServerHandle.StartClientRequest));
                packet.Write(guid);
                packet.Write(name);
                packet.Write(id);
                packet.Write(data.Length);
                packet.Write(data);
                packet.Send();
            }

            public static void ResolveServerRequest(string guid, int id, byte[] data)
            {
                using var packet = Main.packets.WriteToServer(nameof(ServerHandle.ResolveServerRequest));
                packet.Write(guid);
                packet.Write(id);
                packet.Write(data.Length);
                packet.Write(data);
                packet.Send();
            }

            public static void RejectServerRequest(string guid, int id, string message)
            {
                using var packet = Main.packets.WriteToServer(nameof(ServerHandle.RejectServerRequest));
                packet.Write(guid);
                packet.Write(id);
                packet.Write((byte)RejectionReason.CustomMessage);
                packet.Write(message);
                packet.Send();
            }

            public static void RejectServerRequest(string guid, int id)
            {
                using var packet = Main.packets.WriteToServer(nameof(ServerHandle.RejectServerRequest));
                packet.Write(guid);
                packet.Write(id);
                packet.Write((byte)RejectionReason.InternalException);
                packet.Send();
            }

            public static void RejectServerRequest(string guid, int id, bool knowsGUID)
            {
                using var packet = Main.packets.WriteToServer(nameof(ServerHandle.RejectServerRequest));
                packet.Write(guid);
                packet.Write(id);
                packet.Write((byte)RejectionReason.UnknownPacket);
                packet.Write(knowsGUID);
                packet.Send();
            }
        }

        static class ServerHandle
        {
            [OffroadPacket]
            public static void BadPacket(int fromClient, BinaryReader reader)
            {
                var guid = reader.ReadString();
                var hasName = reader.ReadBoolean();
                var name = hasName ? reader.ReadString() : null;
                var username = Server.clients[fromClient].player.username;
                if (hasName)
                {
                    Main.Error($"{username} doesn't have packet {guid}/{name}.");
                }
                else
                {
                    Main.Error($"{username} doesn't have packet {guid}");
                }
            }

            [OffroadPacket]
            public static async void StartClientRequest(int fromClient, BinaryReader reader)
            {
                var guid = reader.ReadString();
                var name = reader.ReadString();
                var id = reader.ReadInt32();
                var length = reader.ReadInt32();
                var bytes = reader.ReadBytes(length);

                if (!OffroadPackets.Instances.TryGetValue(guid, out var packets))
                {
                    ServerSend.RejectClientRequest(fromClient, guid, id, false);
                    return;
                }

                if (!packets.ServerRequestHandlers.TryGetValue(name, out var handler))
                {
                    ServerSend.RejectClientRequest(fromClient, guid, id, true);
                    return;
                }

                try
                {
                    var response = await handler(bytes);
                    ServerSend.ResolveClientRequest(fromClient, guid, id, response);
                }
                catch (RejectedRequestException reject)
                {
                    ServerSend.RejectClientRequest(fromClient, guid, id, reject.Message);
                }
                catch (Exception ex)
                {
                    ServerSend.RejectClientRequest(fromClient, guid, id);
                    Debug.LogError(ex);
                }
            }

            [OffroadPacket]
            public static void RejectServerRequest(int fromClient, BinaryReader reader)
            {
                var guid = reader.ReadString();
                var id = reader.ReadInt32();
                var pending = OffroadPackets.Instances[guid].PendingServerRequests[(fromClient, id)];
                try
                {
                    var reason = (RejectionReason)reader.ReadByte();
                    switch (reason)
                    {
                        case RejectionReason.UnknownPacket:
                            var knowsGUID = reader.ReadBoolean();
                            if (knowsGUID)
                            {
                                Main.Error($"Server doesn't have packet {guid}/{pending.packet}.");
                            }
                            else
                            {
                                Main.Error($"Server doesn't have packet {guid}");
                            }
                            pending.completionSource.SetException(new RejectedRequestException("The server doesn't know this packet."));
                            break;
                        case RejectionReason.InternalException:
                            pending.completionSource.SetException(new RejectedRequestException("The server encountered an unhandled exception processing this request."));
                            break;
                        case RejectionReason.CustomMessage:
                            var message = reader.ReadString();
                            pending.completionSource.SetException(new RejectedRequestException(message));
                            break;
                    }
                }
                catch (Exception ex)
                {
                    pending.completionSource.SetException(ex);
                    throw;
                }
                finally
                {
                    OffroadPackets.Instances[guid].PendingClientRequests.Remove(id);
                }
            }

            [OffroadPacket]
            public static void ResolveServerRequest(int fromClient, BinaryReader reader)
            {
                var guid = reader.ReadString();
                var id = reader.ReadInt32();
                var length = reader.ReadInt32();
                var bytes = reader.ReadBytes(length);

                var pending = OffroadPackets.Instances[guid].PendingServerRequests[(fromClient, id)];
                try
                {
                    pending.completionSource.SetResult(bytes);
                }
                finally
                {
                    OffroadPackets.Instances[guid].PendingServerRequests.Remove((fromClient, id));
                }
            }
        }

        static class ClientHandle
        {
            [OffroadPacket]
            public static void BadPacket(BinaryReader reader)
            {
                var guid = reader.ReadString();
                var hasName = reader.ReadBoolean();
                var name = hasName ? reader.ReadString() : null;
                if (hasName)
                {
                    Main.Error($"Server doesn't have packet {guid}/{name}.");
                }
                else
                {
                    Main.Error($"Server doesn't have packet {guid}");
                }
            }

            [OffroadPacket]
            public static async void StartServerRequest(BinaryReader reader)
            {
                var guid = reader.ReadString();
                var name = reader.ReadString();
                var id = reader.ReadInt32();
                var length = reader.ReadInt32();
                var bytes = reader.ReadBytes(length);

                if (!OffroadPackets.Instances.TryGetValue(guid, out var packets))
                {
                    ClientSend.RejectServerRequest(guid, id, false);
                    return;
                }

                if (!packets.ServerRequestHandlers.TryGetValue(name, out var handler))
                {
                    ClientSend.RejectServerRequest(guid, id, true);
                    return;
                }

                try
                {
                    var response = await handler(bytes);
                    ClientSend.ResolveServerRequest(guid, id, response);
                }
                catch (RejectedRequestException reject)
                {
                    ClientSend.RejectServerRequest(guid, id, reject.Message);
                }
                catch (Exception ex)
                {
                    ClientSend.RejectServerRequest(guid, id);
                    Debug.LogError(ex);
                }
            }

            [OffroadPacket]
            public static void RejectClientRequest(BinaryReader reader)
            {
                var guid = reader.ReadString();
                var id = reader.ReadInt32();
                var pending = OffroadPackets.Instances[guid].PendingClientRequests[id];
                try
                {
                    var reason = (RejectionReason)reader.ReadByte();
                    switch (reason)
                    {
                        case RejectionReason.UnknownPacket:
                            var knowsGUID = reader.ReadBoolean();
                            if (knowsGUID)
                            {
                                Main.Error($"Server doesn't have packet {guid}/{pending.packet}.");
                            }
                            else
                            {
                                Main.Error($"Server doesn't have packet {guid}");
                            }
                            pending.completionSource.SetException(new RejectedRequestException("The server doesn't know this packet."));
                            break;
                        case RejectionReason.InternalException:
                            pending.completionSource.SetException(new RejectedRequestException("The server encountered an unhandled exception processing this request."));
                            break;
                        case RejectionReason.CustomMessage:
                            var message = reader.ReadString();
                            pending.completionSource.SetException(new RejectedRequestException(message));
                            break;
                    }
                }
                catch (Exception ex)
                {
                    pending.completionSource.SetException(ex);
                    throw;
                }
                finally
                {
                    OffroadPackets.Instances[guid].PendingClientRequests.Remove(id);
                }
            }

            [OffroadPacket]
            public static void ResolveClientRequest(BinaryReader reader)
            {
                var guid = reader.ReadString();
                var id = reader.ReadInt32();
                var length = reader.ReadInt32();
                var bytes = reader.ReadBytes(length);

                var pending = OffroadPackets.Instances[guid].PendingClientRequests[id];
                try
                {
                    pending.completionSource.SetResult(bytes);
                }
                finally
                {
                    OffroadPackets.Instances[guid].PendingClientRequests.Remove(id);
                }
            }
        }
    }
}