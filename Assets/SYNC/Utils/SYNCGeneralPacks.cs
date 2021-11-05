using System;
using System.Linq;
using System.Text;
using LiteNetLib.Utils;
using Sync.Utils;
using UnityEngine;

namespace Sync.Packs {
	internal struct IdentityVarsPack : IPack {
		public int NetID { get; }
		public BehaviourVarsPack[] Packs { get; }
		public byte PacksCount => (byte)Packs.Length;

		public int ByteSize => sizeof(int)
		                       + sizeof(byte)
		                       + Packs.Sum(pack => pack.ByteSize);

		internal IdentityVarsPack(int netID, BehaviourVarsPack[] packs) {
			this.NetID = netID;
			this.Packs = packs;
		}

		public static void Serialize(NetDataWriter writer, IdentityVarsPack pack) {
			writer.Put(pack.NetID);
			writer.Put(pack.PacksCount);

			foreach (BehaviourVarsPack behaviourVarsPack in pack.Packs)
				BehaviourVarsPack.Serialize(writer, behaviourVarsPack);
		}

		public static IdentityVarsPack Deserialize(NetDataReader reader) {
			int netID = reader.GetInt();
			byte packsCount = reader.GetByte();

			BehaviourVarsPack[] packs = new BehaviourVarsPack[packsCount];
			for (int i = 0; i < packs.Length; i++)
				packs[i] = BehaviourVarsPack.Deserialize(reader);

			return new IdentityVarsPack(netID, packs);
		}

	}

	internal struct BehaviourVarsPack : IPack {
		public int BehaviourID { get; }
		public (string fieldName, object fieldValue)[] Vars { get; }
		public byte VarsCount => (byte)Vars.Length;

		public int ByteSize => sizeof(int)
		                       + sizeof(byte);

		internal BehaviourVarsPack(int behaviourID, (string fieldName, object fieldValue)[] vars) {
			this.BehaviourID = behaviourID;
			this.Vars = vars;
		}

		public static void Serialize(NetDataWriter writer, BehaviourVarsPack pack) {
			writer.Put(pack.BehaviourID);

			writer.Put(pack.VarsCount);
			foreach ((string name, object value) in pack.Vars) {
				writer.Put(name);
				ObjectPack.Serialize(writer, new ObjectPack(value));
			}
		}

		public static BehaviourVarsPack Deserialize(NetDataReader reader) {
			int behaviourID = reader.GetInt();
			byte varsCount = reader.GetByte();

			(string nameof, object value)[] vars = new (string nameof, object value)[varsCount];
			for (int i = 0; i < vars.Length; i++)
				vars[i] = (reader.GetString(), ObjectPack.Deserialize(reader).Data);

			return new BehaviourVarsPack(
				behaviourID,
				vars
			);
		}
	}

	internal struct ObjectPack : IPack {
		public object Data { get; }

		internal ObjectPack(object data) {
			this.Data = data;
		}

		public int ByteSize => sizeof(byte)
		                       + GetObjectByteSize();

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

		private int GetObjectByteSize() {
			return Data switch {
				char _ => sizeof(char),
				string s => Encoding.UTF8.GetByteCount(s),
				bool _ => sizeof(bool),
				byte _ => sizeof(byte),
				sbyte _ => sizeof(sbyte),
				short _ => sizeof(short),
				ushort _ => sizeof(ushort),
				int _ => sizeof(int),
				uint _ => sizeof(uint),
				long _ => sizeof(long),
				ulong _ => sizeof(ulong),
				float _ => sizeof(float),
				double _ => sizeof(double),
				Vector3 _ => sizeof(float) * 3,
				Vector2 _ => sizeof(float) * 2,
				_ => throw new ArgumentOutOfRangeException(),
			};
		}

		private static void WriteType(NetDataWriter writer, SYNCObjectType type) => writer.Put((byte)type);
	}
}
