using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using BepInEx;
using Debug = UnityEngine.Debug;

namespace Terrain.Packets
{
    /// <summary>
    /// An attribute that is applied to classes that contain packet or request handlers, as well as directly on packet or request handlers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class OffroadPacketAttribute : Attribute
    {
        private readonly string name;

        /// <summary>
        /// The name associated with this packet or request handler.
        /// </summary>
        /// <remarks>
        /// On a method, if unspecified, this defaults to the method name. <br />
        /// On a class, if unspecified, has a default value that depends on how it's used.
        /// </remarks>
        public string Name => name;

        /// <summary>
        /// Marks a method as a packet handler, or a class as containing packet handlers.
        /// </summary>
        /// <param name="name">The name of this packet or guid of the instance. This defaults to the method name, the full name of the type, or the plugin guid, depending on the context. </param>
        /// <remarks>
        /// On a class, this is only used when creating an instance of <see cref="OffroadPackets" /> (and even then isn't required). <br />
        /// On a method, this is only used when searching for packet handlers in a type.
        /// </remarks>
        public OffroadPacketAttribute(string name = null)
        {
            this.name = name;
        }
    }

    /// <summary>
    /// Contains packet and request handlers, and allows you to send packets and requests.
    /// </summary>
    public partial class OffroadPackets
    {
        /// <summary>
        /// A static lookup table of all instances that have been created, by their GUID. This is used to find
        /// </summary>
        public static Dictionary<string, OffroadPackets> Instances = new();

        private readonly string guid;

        /// <summary>
        /// The GUID that distinguishes packets from this instance between others.
        /// </summary>
        public string Guid => guid;

        /// <summary>
        /// Registers an instance of <see cref="OffroadPackets" />, searching through the current assembly for any types marked with <see cref="OffroadPacketAttribute" />. <br />
        /// This can be prone to error in some cases, as it uses <see cref="StackFrame" />. It's great for prototyping, but you should probably use <see cref="Register{Handler}" /> instead. <br />
        /// 
        /// The type is uses for packet handlers is the first in this list:
        /// <list type="number">
        ///     <item>The first type it finds in the assembly with a <see cref="OffroadPacketAttribute" />.</item>
        ///     <item>The type that called this method, which is assumed to have a <see cref="BepInPlugin" /> attribute.</item>
        /// </list>
        /// 
        /// The guid of the <see cref="OffroadPackets" /> instance will be the first in this list:
        /// <list type="number">
        ///     <item>The <see cref="OffroadPacketAttribute.Name" /> of the type it found.</item>
        ///     <item>The <see cref="Type.FullName" /> of the type it found.</item>
        ///     <item>The <see cref="BepInPlugin.GUID" /> of the type that called this method.</item>
        /// </list>
        /// </summary>
        /// <returns>The instance of <see cref="OffroadPackets" /> it created. Don't lose it, you'll need it to send packets.</returns>
        public static OffroadPackets Create()
        {
            var plugin = new StackFrame(1, false).GetMethod().DeclaringType;
            foreach (var type in plugin.Assembly.GetTypes())
            {
                var handler = type.GetCustomAttribute<OffroadPacketAttribute>();
                if (handler != null) return Create(handler.Name ?? type.FullName, type);
            }
            var guid = plugin.GetCustomAttribute<BepInPlugin>().GUID;
            return Create(guid, plugin);
        }

        /// <summary>
        /// Registers an instance of <see cref="OffroadPackets" />, searching through <typeparamref name="Handler" /> and all its subtypes for any methods marked with <see cref="OffroadPacketAttribute" />. <br />
        /// Generally, this is easier to understand than <see cref="Register" />, since it clearly defines which type it reads from. <br />
        /// 
        /// The guid of the <see cref="OffroadPackets" /> will be the first in this list:
        /// <list type="number">
        ///     <item>The <see cref="OffroadPacketAttribute.Name" /> of <typeparamref name="Handler" />.</item>
        ///     <item>The <see cref="Type.FullName" /> of <typeparamref name="Handler" />.</item>
        ///     <item>The <see cref="BepInPlugin.GUID" /> of the type that called this method.</item>
        /// </list>
        /// </summary>
        /// <typeparam name="Handler">The type where you defined all your packet handlers.</typeparam>
        /// <returns>The instance of <see cref="OffroadPackets" /> it created. Don't lose it, you'll need it to send packets.</returns>
        public static OffroadPackets Create<Handler>()
        {
            var type = typeof(Handler);
            var handler = type.GetCustomAttribute<OffroadPacketAttribute>();
            if (handler != null) return Create(handler.Name ?? type.FullName, type);

            var plugin = new StackFrame(1, false).GetMethod().DeclaringType;
            var guid = plugin.GetCustomAttribute<BepInPlugin>().GUID;
            return Create(guid, type);
        }

        private static OffroadPackets Create(string guid, Type type)
        {
            var packets = new OffroadPackets(guid);
            packets.Register(type);
            return packets;
        }

        /// <summary>
        /// Registers all packet and request handlers in the given type and its subtypes. That is, all static methods with an <see cref="OffroadPacketAttribute" />.
        /// </summary>
        /// <typeparam name="T">The type that contains packet and request handlers.</typeparam>
        public void Register<T>() => Register(typeof(T));

        /// <summary>
        /// Registers all packet and request handlers in the given type and its subtypes. That is, all static methods with an <see cref="OffroadPacketAttribute" />.
        /// </summary>
        /// <param name="type">The type that contains packet and request handlers.</param>
        public void Register(Type type)
        {
            var packetHandlers = GetPacketHandlers(type);
            foreach (var (name, handler) in packetHandlers)
            {
                switch (handler)
                {
                    case ServerPacketHandler sph: Handle(name, sph); break;
                    case ServerRawPacketHandler srph: Handle(name, srph); break;
                    case ClientPacketHandler cph: Handle(name, cph); break;
                    case ClientRawPacketHandler crph: Handle(name, crph); break;
                    case ServerRequestHandler srh: Handle(name, srh); break;
                    case ServerRawRequestHandler srrh: Handle(name, srrh); break;
                    case ClientRequestHandler crh: Handle(name, crh); break;
                    case ClientRawRequestHandler crrh: Handle(name, crrh); break;
                }
            }
        }

        private static IEnumerable<(string name, MulticastDelegate method)> GetPacketHandlers(Type type)
        {
            foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var attr = method.GetCustomAttribute<OffroadPacketAttribute>();
                if (attr == null) continue;
                var name = attr.Name ?? method.Name;
                if (method.IsGenericMethod) goto invalidsig;
                var parameters = method.GetParameters();
                if (parameters.Length == 0 || parameters.Length > 3) goto invalidsig;
                if (method.ReturnType == typeof(void))
                {
                    switch (parameters.Length)
                    {
                        case 1:
                            if (parameters[0].ParameterType == typeof(BinaryReader))
                            {
                                yield return (name, CompileHandlers.CompileServerPacketHandler(method));
                                continue;
                            }
                            if (parameters[0].ParameterType == typeof(byte[]))
                            {
                                yield return (name, CompileHandlers.CompileServerRawPacketHandler(method));
                                continue;
                            }
                            goto invalidsig;
                        case 2:
                            if (parameters[0].ParameterType != typeof(int)) goto invalidsig;
                            if (parameters[1].ParameterType == typeof(BinaryReader))
                            {
                                yield return (name, CompileHandlers.CompileClientPacketHandler(method));
                                continue;
                            }
                            if (parameters[1].ParameterType == typeof(byte[]))
                            {
                                yield return (name, CompileHandlers.CompileClientRawPacketHandler(method));
                                continue;
                            }
                            goto invalidsig;
                        default:
                            goto invalidsig;
                    }
                }
                if (method.ReturnType == typeof(Task))
                {
                    switch (parameters.Length)
                    {
                        case 2:
                            if (parameters[0].ParameterType != typeof(BinaryReader)) goto invalidsig;
                            if (parameters[1].ParameterType != typeof(BinaryWriter)) goto invalidsig;
                            yield return (name, CompileHandlers.CompileServerRequestHandler(method));
                            continue;
                        case 3:
                            if (parameters[0].ParameterType != typeof(int)) goto invalidsig;
                            if (parameters[1].ParameterType != typeof(BinaryReader)) goto invalidsig;
                            if (parameters[2].ParameterType != typeof(BinaryWriter)) goto invalidsig;
                            yield return (name, CompileHandlers.CompileClientRequestHandler(method));
                            continue;
                        default:
                            goto invalidsig;
                    }
                }
                if (method.ReturnType == typeof(Task<byte[]>))
                {
                    switch (parameters.Length)
                    {
                        case 1:
                            if (parameters[0].ParameterType != typeof(byte[])) goto invalidsig;
                            yield return (name, CompileHandlers.CompileServerRawRequestHandler(method));
                            continue;
                        case 2:
                            if (parameters[0].ParameterType != typeof(int)) goto invalidsig;
                            if (parameters[1].ParameterType != typeof(byte[])) goto invalidsig;
                            yield return (name, CompileHandlers.CompileClientRawRequestHandler(method));
                            continue;
                        default:
                            goto invalidsig;
                    }
                }
            invalidsig:
                Debug.LogWarning($"Method {method.DeclaringType.FullName}::{method.Name} has OffroadPacketAttribute but invalid signature, ignoring...");
            }

            foreach (var child in type.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public))
            {
                foreach (var e in GetPacketHandlers(child)) yield return e;
            }
        }

        /// <summary>
        /// Creates an empty packet controller manually.
        /// </summary>
        /// <param name="guid">The GUID of this instance. This must be unique, just like your <see cref="BepInPlugin.GUID" />. Honestly, you should probably just use that one, it's unique anyways.</param>
        public OffroadPackets(string guid)
        {
            this.guid = guid;
            Instances[guid] = this;
        }
    }
}