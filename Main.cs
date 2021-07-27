using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using System.Reflection;
using Terrain.Packets.Compatibility;

namespace Terrain.Packets.Plugin
{
    [BepInPlugin(Guid, Name, Version)]
    public class Main : BaseUnityPlugin
    {
        public const string
            Name = "OffroadPackets",
            Author = "Terrain",
            Guid = Author + "." + Name,
            Version = "1.1.0.0";

        internal static ManualLogSource log;
        internal readonly Harmony harmony;
        internal readonly Assembly assembly;
        public readonly string modFolder;

        public static OffroadPackets packets;

        Main()
        {
            log = Logger;
            harmony = new Harmony(Guid);
            assembly = Assembly.GetExecutingAssembly();
            modFolder = Path.GetDirectoryName(assembly.Location);

            packets = OffroadPackets.Register<ImportantPackets>();

            harmony.PatchAll(assembly);
        }

        public static void SendChatMessage(string message) => ChatBox.Instance.AppendMessage(-1, message, "<color=#03DAC6>[OffroadPackets]");
        public static void Error(string error) {
            log.LogError(error);
            SendChatMessage($"<color=#B00020>{error}");
        }
    }
}