using System.IO;
using UnityEngine;

namespace Terrain.Packets
{
    public static class BinaryWriterExtensions
    {
        static void ReadVector2(this BinaryWriter writer, Vector2 vector)
        {
            writer.Write(vector.x);
            writer.Write(vector.y);
        }

        static void Write(this BinaryWriter writer, Vector2Int vector)
        {
            writer.Write(vector.x);
            writer.Write(vector.y);
        }

        static void Write(this BinaryWriter writer, Vector3 vector)
        {
            writer.Write(vector.x);
            writer.Write(vector.y);
            writer.Write(vector.z);
        }

        static void Write(this BinaryWriter writer, Vector3Int vector)
        {
            writer.Write(vector.x);
            writer.Write(vector.y);
            writer.Write(vector.z);
        }

        static void Write(this BinaryWriter writer, Vector4 vector)
        {
            writer.Write(vector.x);
            writer.Write(vector.y);
            writer.Write(vector.z);
            writer.Write(vector.w);
        }

        static void Write(this BinaryWriter writer, Quaternion quaternion)
        {
            writer.Write(quaternion.x);
            writer.Write(quaternion.y);
            writer.Write(quaternion.z);
            writer.Write(quaternion.w);
        }

        static void Write(this BinaryWriter writer, Matrix4x4 matrix)
        {
            for (var i = 0; i < 16; i++) writer.Write(matrix[i]);
        }

        static void Write(this BinaryWriter writer, Plane plane)
        {
            writer.Write(plane.normal);
            writer.Write(plane.distance);
        }

        static void Write(this BinaryWriter writer, Color color)
        {
            writer.Write(color.r);
            writer.Write(color.g);
            writer.Write(color.b);
            writer.Write(color.a);
        }

        static void Write(this BinaryWriter writer, Color32 color)
        {
            writer.Write(color.r);
            writer.Write(color.g);
            writer.Write(color.b);
            writer.Write(color.a);
        }

        static void Write(this BinaryWriter writer, Rect rect)
        {
            writer.Write(rect.x);
            writer.Write(rect.y);
            writer.Write(rect.width);
            writer.Write(rect.height);
        }

        static void Write(this BinaryWriter writer, Pose pose)
        {
            writer.Write(pose.position);
            writer.Write(pose.rotation);
        }

        static void Write(this BinaryWriter writer, Bounds bounds)
        {
            writer.Write(bounds.center);
            writer.Write(bounds.extents);
        }

        static void Write(this BinaryWriter writer, BoundingSphere sphere)
        {
            writer.Write(sphere.position);
            writer.Write(sphere.radius);
        }
    }

    public static class BinaryReaderExtensions
    {
        static Vector2 ReadVector2(this BinaryReader reader)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            return new Vector2(x, y);
        }

        static Vector2Int ReadVector2Int(this BinaryReader reader)
        {
            var x = reader.ReadInt32();
            var y = reader.ReadInt32();
            return new Vector2Int(x, y);
        }

        static Vector3 ReadVector3(this BinaryReader reader)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var z = reader.ReadSingle();
            return new Vector3(x, y, z);
        }

        static Vector3Int ReadVector3Int(this BinaryReader reader)
        {
            var x = reader.ReadInt32();
            var y = reader.ReadInt32();
            var z = reader.ReadInt32();
            return new Vector3Int(x, y, z);
        }

        static Vector4 ReadVector4(this BinaryReader reader)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var z = reader.ReadSingle();
            var w = reader.ReadSingle();
            return new Vector4(x, y, z, w);
        }

        static Quaternion ReadQuaternion(this BinaryReader reader)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var z = reader.ReadSingle();
            var w = reader.ReadSingle();
            return new Quaternion(x, y, z, w);
        }

        static Matrix4x4 ReadMatrix4x4(this BinaryReader reader)
        {
            var col1 = reader.ReadVector4();
            var col2 = reader.ReadVector4();
            var col3 = reader.ReadVector4();
            var col4 = reader.ReadVector4();
            return new Matrix4x4(col1, col2, col3, col4);
        }

        static Plane ReadPlane(this BinaryReader reader)
        {
            var normal = reader.ReadVector3();
            var distance = reader.ReadSingle();
            return new Plane(normal, distance);
        }

        static Color ReadColor(this BinaryReader reader)
        {
            var r = reader.ReadSingle();
            var g = reader.ReadSingle();
            var b = reader.ReadSingle();
            var a = reader.ReadSingle();
            return new Color(r, g, b, a);
        }

        static Color32 ReadColor32(this BinaryReader reader)
        {
            var r = reader.ReadInt32();
            var g = reader.ReadInt32();
            var b = reader.ReadInt32();
            var a = reader.ReadInt32();
            return new Color(r, g, b, a);
        }

        static Rect ReadRect(this BinaryReader reader)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var width = reader.ReadSingle();
            var height = reader.ReadSingle();
            return new Rect(x, y, width, height);
        }

        static Pose ReadPose(this BinaryReader reader)
        {
            var position = reader.ReadVector3();
            var rotation = reader.ReadQuaternion();
            return new Pose(position, rotation);
        }

        static Bounds ReadBounds(this BinaryReader reader)
        {
            var center = reader.ReadVector3();
            var extents = reader.ReadVector3();
            return new Bounds(center, extents * 2);
        }

        static BoundingSphere ReadBoundingSphere(this BinaryReader reader)
        {
            var position = reader.ReadVector3();
            var radius = reader.ReadSingle();
            return new BoundingSphere(position, radius);
        }
    }
}