#pragma warning disable CS1591 // document public members
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace Terrain.Packets.Plugin
{
    [BepInPlugin(Guid, Name, Version)]
    public class Main : BaseUnityPlugin
    {
        public const string
            Name = "OffroadPackets",
            Author = "Terrain",
            Guid = Author + "." + Name,
            Version = "2.0.0.0";

        internal static ManualLogSource log;
        internal readonly Harmony harmony;
        internal readonly Assembly assembly;
        internal readonly string modFolder;

        internal static OffroadPackets packets;

        Main()
        {
            log = Logger;
            harmony = new Harmony(Guid);
            assembly = Assembly.GetExecutingAssembly();
            modFolder = Path.GetDirectoryName(assembly.Location);

            packets = OffroadPackets.Create<ImportantPackets>();

            harmony.PatchAll(assembly);
        }

        internal static void SendChatMessage(string message) => ChatBox.Instance.AppendMessage(-1, message, "<color=#03DAC6>[OffroadPackets]");
        internal static void Error(string error)
        {
            log.LogError(error);
            SendChatMessage($"<color=#B00020>{error}");
        }
    }
}