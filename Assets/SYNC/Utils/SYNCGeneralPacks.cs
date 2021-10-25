using System;
using LiteNetLib.Utils;
using UnityEngine;

namespace Sync.Utils {
	internal struct ObjectPack {
		public object Data { get; }

		internal ObjectPack(object data) {
			this.Data = data;
		}

		public static void Serialize(NetDataWriter writer, ObjectPack pack) {
			switch (pack.Data) {
				case char data:
					WriteType(writer, SYNCObjectType.Char);
					writer.Put(data);
					break;
				case string data:
					WriteType(writer, SYNCObjectType.String);
					writer.Put(data);
					break;
				case bool data:
					WriteType(writer, SYNCObjectType.Bool);
					writer.Put(data);
					break;
				case byte data:
					WriteType(writer, SYNCObjectType.Byte);
					writer.Put(data);
					break;
				case sbyte data:
					WriteType(writer, SYNCObjectType.SByte);
					writer.Put(data);
					break;
				case short data:
					WriteType(writer, SYNCObjectType.Short);
					writer.Put(data);
					break;
				case ushort data:
					WriteType(writer, SYNCObjectType.UShort);
					writer.Put(data);
					break;
				case int data:
					WriteType(writer, SYNCObjectType.Int);
					writer.Put(data);
					break;
				case uint data:
					WriteType(writer, SYNCObjectType.UInt);
					writer.Put(data);
					break;
				case long data:
					WriteType(writer, SYNCObjectType.Long);
					writer.Put(data);
					break;
				case ulong data:
					WriteType(writer, SYNCObjectType.ULong);
					writer.Put(data);
					break;
				case float data:
					WriteType(writer, SYNCObjectType.Float);
					writer.Put(data);
					break;
				case double data:
					WriteType(writer, SYNCObjectType.Double);
					writer.Put(data);
					break;
				case Vector3 data:
					WriteType(writer, SYNCObjectType.Vector3);
					Vector3Pack.Serialize(writer, data);
					break;
				case Vector2 data:
					WriteType(writer, SYNCObjectType.Vector2);
					Vector2Pack.Serialize(writer, data);
					break;
				default:
					throw new NotSupportedException($"The provided type {pack.Data.GetType().Name} is not supported");
			}
		}

		public static ObjectPack Deserialize(NetDataReader reader) {
			SYNCObjectType type = (SYNCObjectType)reader.GetByte();

			object data = type switch {
				SYNCObjectType.Char => reader.GetChar(),
				SYNCObjectType.String => reader.GetString(),
				SYNCObjectType.Bool => reader.GetBool(),
				SYNCObjectType.Byte => reader.GetByte(),
				SYNCObjectType.SByte => reader.GetSByte(),
				SYNCObjectType.Short => reader.GetShort(),
				SYNCObjectType.UShort => reader.GetUShort(),
				SYNCObjectType.Int => reader.GetInt(),
				SYNCObjectType.UInt => reader.GetUInt(),
				SYNCObjectType.Long => reader.GetLong(),
				SYNCObjectType.ULong => reader.GetULong(),
				SYNCObjectType.Float => reader.GetFloat(),
				SYNCObjectType.Double => reader.GetDouble(),
				SYNCObjectType.Vector3 => (Vector3)Vector3Pack.Deserialize(reader),
				SYNCObjectType.Vector2 => (Vector2)Vector2Pack.Deserialize(reader),
				_ => throw new ArgumentOutOfRangeException(),
			};

			return new ObjectPack(data);
		}

		private static void WriteType(NetDataWriter writer, SYNCObjectType type) => writer.Put((byte)type);
	}
}
