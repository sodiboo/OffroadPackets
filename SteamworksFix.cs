using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SteamworksFix
{
    public static class SteamworksUtils
    {
        public readonly static Type SteamId = AccessTools.TypeByName("Steamworks.SteamId");
        public readonly static Type P2PSend = AccessTools.TypeByName("Steamworks.P2PSend");

        public readonly static MethodInfo Uint64ToSteamId = AccessTools.Method(SteamId, "op_Implicit", new Type[] { typeof(ulong) });
        public static object ToSteamId(ulong value) => Uint64ToSteamId.Invoke(null, new object[] { value });
    }

    public static class SteamworksMethods
    {
        public static void SendPacket(ulong recipient, Packet data, P2PSend p2psend, SteamPacketManager.NetworkChannel channel) => Traverse.Create<SteamPacketManager>().Method("SendPacket", SteamworksUtils.ToSteamId(recipient), data, Enum.ToObject(SteamworksUtils.P2PSend, (int)p2psend), channel).GetValue();
    }

    public static class SteamworksExtensions
    {
        public static ulong SteamId(this Player player) => Traverse.Create(player).Field("steamId").Field("Value").GetValue<ulong>();
        public static ulong ServerHost(this LocalClient client) => Traverse.Create(client).Field("serverHost").Field("Value").GetValue<ulong>();
    }

    public enum P2PSend
    {
        Unreliable,
        UnreliableNoDelay,
        Reliable,
        ReliableWithBuffering,
    }
}