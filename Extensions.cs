using System.IO;
using UnityEngine;

namespace Terrain.Packets
{
    /// <summary>
    /// Contains extension methods on <see cref="BinaryWriter" /> for writing common Unity types.
    /// </summary>
    public static class BinaryWriterExtensions
    {
        /// <summary>
        /// Writes an 8-byte two-dimensional vector of floating point numbers and advances the stream position by 8 bytes.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter" /> to write to.</param>
        /// <param name="vector">The vector to write to <paramref name="writer" />.</param>
        public static void Write(this BinaryWriter writer, Vector2 vector)
        {
            writer.Write(vector.x);
            writer.Write(vector.y);
        }

        /// <summary>
        /// Writes an 8-byte two-dimensional vector of integers and advances the stream position by 8 bytes.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter" /> to write to.</param>
        /// <param name="vector">The vector to write to <paramref name="writer" />.</param>
        public static void Write(this BinaryWriter writer, Vector2Int vector)
        {
            writer.Write(vector.x);
            writer.Write(vector.y);
        }

        /// <summary>
        /// Writes a 12-byte three-dimensional vector of floating point numbers and advances the stream position by 12 bytes.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter" /> to write to.</param>
        /// <param name="vector">The vector to write to <paramref name="writer" />.</param>
        public static void Write(this BinaryWriter writer, Vector3 vector)
        {
            writer.Write(vector.x);
            writer.Write(vector.y);
            writer.Write(vector.z);
        }

        /// <summary>
        /// Writes a 12-byte three-dimensional vector of integers and advances the stream position by 12 bytes.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter" /> to write to.</param>
        /// <param name="vector">The vector to write to <paramref name="writer" />.</param>
        public static void Write(this BinaryWriter writer, Vector3Int vector)
        {
            writer.Write(vector.x);
            writer.Write(vector.y);
            writer.Write(vector.z);
        }

        /// <summary>
        /// Writes a 16-byte four-dimensional vector of floating point numbers and advances the stream position by 16 bytes.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter" /> to write to.</param>
        /// <param name="vector">The vector to write to <paramref name="writer" />.</param>
        public static void Write(this BinaryWriter writer, Vector4 vector)
        {
            writer.Write(vector.x);
            writer.Write(vector.y);
            writer.Write(vector.z);
            writer.Write(vector.w);
        }

        /// <summary>
        /// Writes a 16-byte quaternion and advances the stream position by 16 bytes.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter" /> to write to.</param>
        /// <param name="quaternion">The quaternion to write to <paramref name="writer" />.</param>
        public static void Write(this BinaryWriter writer, Quaternion quaternion)
        {
            writer.Write(quaternion.x);
            writer.Write(quaternion.y);
            writer.Write(quaternion.z);
            writer.Write(quaternion.w);
        }

        /// <summary>
        /// Writes a 64-byte 4x4 matrix and advances the stream position by 64 bytes.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter" /> to write to.</param>
        /// <param name="matrix">The 4x4 matrix to write to <paramref name="writer" />.</param>
        public static void Write(this BinaryWriter writer, Matrix4x4 matrix)
        {
            writer.Write(matrix.m00);
            writer.Write(matrix.m10);
            writer.Write(matrix.m20);
            writer.Write(matrix.m30);
            writer.Write(matrix.m01);
            writer.Write(matrix.m11);
            writer.Write(matrix.m21);
            writer.Write(matrix.m31);
            writer.Write(matrix.m02);
            writer.Write(matrix.m12);
            writer.Write(matrix.m22);
            writer.Write(matrix.m32);
            writer.Write(matrix.m03);
            writer.Write(matrix.m13);
            writer.Write(matrix.m23);
            writer.Write(matrix.m33);
        }

        /// <summary>
        /// Writes a 16-byte plane and advances the stream position by 16 bytes.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter" /> to write to.</param>
        /// <param name="plane">The plane to write to <paramref name="writer" />.</param>
        public static void Write(this BinaryWriter writer, Plane plane)
        {
            writer.Write(plane.normal);
            writer.Write(plane.distance);
        }

        /// <summary>
        /// Writes a 16-byte color represented by floating point values in the range 0..1 and advances the stream position by 16 bytes.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter" /> to write to.</param>
        /// <param name="color">The color to write to <paramref name="writer" />.</param>
        public static void Write(this BinaryWriter writer, Color color)
        {
            writer.Write(color.r);
            writer.Write(color.g);
            writer.Write(color.b);
            writer.Write(color.a);
        }

        /// <summary>
        /// Writes a 4-byte color represented by integer values in the range 0..255 and advances the stream position by 4 bytes.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter" /> to write to.</param>
        /// <param name="color">The color to write to <paramref name="writer" />.</param>
        public static void Write(this BinaryWriter writer, Color32 color)
        {
            writer.Write(color.r);
            writer.Write(color.g);
            writer.Write(color.b);
            writer.Write(color.a);
        }

        /// <summary>
        /// Writes a 16-byte two-dimensional rectangle and advances the stream position by 16 bytes.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter" /> to write to.</param>
        /// <param name="rect">The rectangle to write to <paramref name="writer" />.</param>
        public static void Write(this BinaryWriter writer, Rect rect)
        {
            writer.Write(rect.x);
            writer.Write(rect.y);
            writer.Write(rect.width);
            writer.Write(rect.height);
        }

        /// <summary>
        /// Writes a 28-byte three-dimensional pose and advances the stream position by 28 bytes.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter" /> to write to.</param>
        /// <param name="pose">The pose to write to <paramref name="writer" />.</param>
        public static void Write(this BinaryWriter writer, Pose pose)
        {
            writer.Write(pose.position);
            writer.Write(pose.rotation);
        }

        /// <summary>
        /// Writes a 24-byte three-dimensional bounding box and advances the stream position by 24 bytes.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter" /> to write to.</param>
        /// <param name="bounds">The bounding box to write to <paramref name="writer" />.</param>
        public static void Write(this BinaryWriter writer, Bounds bounds)
        {
            writer.Write(bounds.center);
            writer.Write(bounds.extents);
        }

        /// <summary>
        /// Writes a 16-byte three-dimensional bounding sphere and advances the stream position by 16 bytes.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter" /> to write to.</param>
        /// <param name="sphere">The bounding sphere to write to <paramref name="writer" />.</param>
        public static void Write(this BinaryWriter writer, BoundingSphere sphere)
        {
            writer.Write(sphere.position);
            writer.Write(sphere.radius);
        }
    }

    /// <summary>
    /// Contains extension methods on <see cref="BinaryReader" /> for reading common Unity types.
    /// </summary>
    public static class BinaryReaderExtensions
    {
        /// <summary>
        /// Reads an 8-byte two-dimensional vector of floating point numbers and advances the current position of the stream by 8 bytes.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader" /> to read from.</param>
        /// <returns>An 8-byte two-dimensional vector of floating point numbers that was read from <paramref name="reader" />.</returns>
        public static Vector2 ReadVector2(this BinaryReader reader)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            return new Vector2(x, y);
        }

        /// <summary>
        /// Reads an 8-byte two-dimensional vector of integers and advances the current position of the stream by 8 bytes.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader" /> to read from.</param>
        /// <returns>An 8-byte two-dimensional vector of integers that was read from <paramref name="reader" />.</returns>
        public static Vector2Int ReadVector2Int(this BinaryReader reader)
        {
            var x = reader.ReadInt32();
            var y = reader.ReadInt32();
            return new Vector2Int(x, y);
        }

        /// <summary>
        /// Reads a 12-byte three-dimensional vector of floating point numbers and advances the current position of the stream by 12 bytes.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader" /> to read from.</param>
        /// <returns>A 12-byte three-dimensional vector of floating point numbers that was read from <paramref name="reader" />.</returns>
        public static Vector3 ReadVector3(this BinaryReader reader)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var z = reader.ReadSingle();
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Reads a 12-byte three-dimensional vector of integers and advances the current position of the stream by 12 bytes.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader" /> to read from.</param>
        /// <returns>A 12-byte three-dimensional vector of integers that was read from <paramref name="reader" />.</returns>
        public static Vector3Int ReadVector3Int(this BinaryReader reader)
        {
            var x = reader.ReadInt32();
            var y = reader.ReadInt32();
            var z = reader.ReadInt32();
            return new Vector3Int(x, y, z);
        }

        /// <summary>
        /// Reads a 16-byte four-dimensional vector of floating point numbers and advances the current position of the stream by 16 bytes.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader" /> to read from.</param>
        /// <returns>A 16-byte four-dimensional vector of floating point numbers that was read from <paramref name="reader" />.</returns>
        public static Vector4 ReadVector4(this BinaryReader reader)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var z = reader.ReadSingle();
            var w = reader.ReadSingle();
            return new Vector4(x, y, z, w);
        }

        /// <summary>
        /// Reads a 16-byte quaternion and advances the current position of the stream by 16 bytes.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader" /> to read from.</param>
        /// <returns>A 16-byte quaternion that was read from <paramref name="reader" />.</returns>
        public static Quaternion ReadQuaternion(this BinaryReader reader)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var z = reader.ReadSingle();
            var w = reader.ReadSingle();
            return new Quaternion(x, y, z, w);
        }

        /// <summary>
        /// Reads a 64-byte 4x4 matrix and advances the current position of the stream by 64 bytes.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader" /> to read from.</param>
        /// <returns>A 64-byte 4x4 matrix that was read from <paramref name="reader" />.</returns>
        public static Matrix4x4 ReadMatrix4x4(this BinaryReader reader)
        {
            var col1 = reader.ReadVector4();
            var col2 = reader.ReadVector4();
            var col3 = reader.ReadVector4();
            var col4 = reader.ReadVector4();
            return new Matrix4x4(col1, col2, col3, col4);
        }

        /// <summary>
        /// Reads a 16-byte plane and advances the current position of the stream by 16 bytes.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader" /> to read from.</param>
        /// <returns>A 16-byte plane that was read from <paramref name="reader" />.</returns>
        public static Plane ReadPlane(this BinaryReader reader)
        {
            var normal = reader.ReadVector3();
            var distance = reader.ReadSingle();
            return new Plane(normal, distance);
        }

        /// <summary>
        /// Reads a 16-byte color represented by floating point values in the range 0..1 and advances the current position of the stream by 16 bytes.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader" /> to read from.</param>
        /// <returns>A 16-byte color that was read from <paramref name="reader" />.</returns>
        public static Color ReadColor(this BinaryReader reader)
        {
            var r = reader.ReadSingle();
            var g = reader.ReadSingle();
            var b = reader.ReadSingle();
            var a = reader.ReadSingle();
            return new Color(r, g, b, a);
        }

        /// <summary>
        /// Reads a 4-byte color represented by integer values in the range 0..255 and advances the current position of the stream by 16 bytes.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader" /> to read from.</param>
        /// <returns>A 4-byte color that was read from <paramref name="reader" />.</returns>
        public static Color32 ReadColor32(this BinaryReader reader)
        {
            var r = reader.ReadByte();
            var g = reader.ReadByte();
            var b = reader.ReadByte();
            var a = reader.ReadByte();
            return new Color32(r, g, b, a);
        }

        /// <summary>
        /// Reads a 16-byte two-dimensional rectangle and advances the current position of the stream by 16 bytes.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader" /> to read from.</param>
        /// <returns>A 16-byte two-dimensional rectangle that was read from <paramref name="reader" />.</returns>
        public static Rect ReadRect(this BinaryReader reader)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var width = reader.ReadSingle();
            var height = reader.ReadSingle();
            return new Rect(x, y, width, height);
        }

        /// <summary>
        /// Reads a 28-byte three-dimensional pose and advances the current position of the stream by 28 bytes.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader" /> to read from.</param>
        /// <returns>A 28-byte three-dimensional pose that was read from <paramref name="reader" />.</returns>
        public static Pose ReadPose(this BinaryReader reader)
        {
            var position = reader.ReadVector3();
            var rotation = reader.ReadQuaternion();
            return new Pose(position, rotation);
        }

        /// <summary>
        /// Reads a 24-byte three-dimensional bounding box and advances the current position of the stream by 24 bytes.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader" /> to read from.</param>
        /// <returns>A 24-byte three-dimensional bounding box that was read from <paramref name="reader" />.</returns>
        public static Bounds ReadBounds(this BinaryReader reader)
        {
            var center = reader.ReadVector3();
            var extents = reader.ReadVector3();
            return new Bounds(center, extents * 2);
        }

        /// <summary>
        /// Reads a 16-byte three-dimensional bounding sphere and advances the current position of the stream by 16 bytes.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader" /> to read from.</param>
        /// <returns>A 16-byte three-dimensional bounding sphere that was read from <paramref name="reader" />.</returns>
        public static BoundingSphere ReadBoundingSphere(this BinaryReader reader)
        {
            var position = reader.ReadVector3();
            var radius = reader.ReadSingle();
            return new BoundingSphere(position, radius);
        }
    }
}