using System.IO;
using System.Linq.Expressions;
using System.Reflection;

namespace Terrain.Packets
{
    internal static class CompileHandlers
    {
        static readonly ParameterExpression binaryreader = Expression.Parameter(typeof(BinaryReader), "reader");
        static readonly ParameterExpression binarywriter = Expression.Parameter(typeof(BinaryWriter), "writer");
        static readonly ParameterExpression int32 = Expression.Parameter(typeof(int), "fromClient");
        static readonly ParameterExpression bytearr = Expression.Parameter(typeof(byte[]), "data");

        static T Compile<T>(MethodInfo method, params ParameterExpression[] parameters)
        {
            var c = Expression.Call(method, parameters);
            var l = Expression.Lambda<T>(c, parameters);
            return l.Compile();
        }

        public static OffroadPackets.ServerPacketHandler CompileServerPacketHandler(MethodInfo method) => Compile<OffroadPackets.ServerPacketHandler>(method, binaryreader);
        public static OffroadPackets.ClientPacketHandler CompileClientPacketHandler(MethodInfo method) => Compile<OffroadPackets.ClientPacketHandler>(method, int32, binaryreader);

        public static OffroadPackets.ServerRawPacketHandler CompileServerRawPacketHandler(MethodInfo method) => Compile<OffroadPackets.ServerRawPacketHandler>(method, bytearr);
        public static OffroadPackets.ClientRawPacketHandler CompileClientRawPacketHandler(MethodInfo method) => Compile<OffroadPackets.ClientRawPacketHandler>(method, int32, bytearr);

        public static OffroadPackets.ServerRequestHandler CompileServerRequestHandler(MethodInfo method) => Compile<OffroadPackets.ServerRequestHandler>(method, binaryreader, binarywriter);
        public static OffroadPackets.ClientRequestHandler CompileClientRequestHandler(MethodInfo method) => Compile<OffroadPackets.ClientRequestHandler>(method, int32, binaryreader, binarywriter);

        public static OffroadPackets.ServerRawRequestHandler CompileServerRawRequestHandler(MethodInfo method) => Compile<OffroadPackets.ServerRawRequestHandler>(method, bytearr);
        public static OffroadPackets.ClientRawRequestHandler CompileClientRawRequestHandler(MethodInfo method) => Compile<OffroadPackets.ClientRawRequestHandler>(method, int32, bytearr);
    }
}