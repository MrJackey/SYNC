using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
		public byte BehaviourID { get; }
		public (string fieldName, object fieldValue)[] Vars { get; }
		public byte VarsCount => (byte)Vars.Length;

		public int ByteSize => sizeof(int)
		                       + sizeof(byte);

		internal BehaviourVarsPack(byte behaviourID, (string fieldName, object fieldValue)[] vars) {
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
			byte behaviourID = reader.GetByte();
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

		public int ByteSize {
			get {
				bool isArray = Data.GetType().IsArray;
				int size = sizeof(ushort); // ObjectType

				if (isArray) {
					size += sizeof(ushort); // ArrayLength
					size += ((object[])Data).Sum(GetObjectByteSize); // ArrayContent
				}
				else {
					size += GetObjectByteSize(Data);
				}

				return size;
			}
		}

		public static void Serialize(NetDataWriter writer, ObjectPack pack) {
			SYNCObjectType typeFlags = 0;

			if (pack.Data.GetType().IsArray) {
				typeFlags |= SYNCObjectType.IsArray;

				Array packData = pack.Data as Array;

				WriteType(writer, packData.GetValue(0), typeFlags);
				writer.Put((ushort)packData.Length);
				foreach (object obj in packData)
					WriteValue(writer, obj);
			}
			else {
				WriteType(writer, pack.Data, typeFlags);
				WriteValue(writer, pack.Data);
			}
		}

		public static ObjectPack Deserialize(NetDataReader reader) {
			SYNCObjectType type = (SYNCObjectType)reader.GetUShort();

			if ((type & SYNCObjectType.IsArray) != 0) {
				ushort arrayLength = reader.GetUShort();

				Array data = Array.CreateInstance(ObjectTypeToType(type), arrayLength);

				for (int i = 0; i < data.Length; i++)
					data.SetValue(ReadValue(reader, type), i);

				return new ObjectPack(data);
			}

			return new ObjectPack(ReadValue(reader, type));
		}

		private static Type ObjectTypeToType(SYNCObjectType type) {
			if ((type & SYNCObjectType.Char) != 0)
				return typeof(char);
			if ((type & SYNCObjectType.String) != 0)
				return typeof(string);
			if ((type & SYNCObjectType.Bool) != 0)
				return typeof(bool);
			if ((type & SYNCObjectType.Byte) != 0)
				return typeof(byte);
			if ((type & SYNCObjectType.SByte) != 0)
				return typeof(sbyte);
			if ((type & SYNCObjectType.Short) != 0)
				return typeof(short);
			if ((type & SYNCObjectType.UShort) != 0)
				return typeof(ushort);
			if ((type & SYNCObjectType.Int) != 0)
				return typeof(int);
			if ((type & SYNCObjectType.UInt) != 0)
				return typeof(uint);
			if ((type & SYNCObjectType.Long) != 0)
				return typeof(long);
			if ((type & SYNCObjectType.ULong) != 0)
				return typeof(ulong);
			if ((type & SYNCObjectType.Float) != 0)
				return typeof(float);
			if ((type & SYNCObjectType.Double) != 0)
				return typeof(double);
			if ((type & SYNCObjectType.Vector3) != 0)
				return typeof(Vector3);
			if ((type & SYNCObjectType.Vector2) != 0)
				return typeof(Vector2);

			throw new ArgumentOutOfRangeException();
		}

		private static object ReadValue(NetDataReader reader, SYNCObjectType type) {
			if ((type & SYNCObjectType.Char) != 0)
				return reader.GetChar();
			if ((type & SYNCObjectType.String) != 0)
				return reader.GetString();
			if ((type & SYNCObjectType.Bool) != 0)
				return reader.GetBool();
			if ((type & SYNCObjectType.Byte) != 0)
				return reader.GetByte();
			if ((type & SYNCObjectType.SByte) != 0)
				return reader.GetSByte();
			if ((type & SYNCObjectType.Short) != 0)
				return reader.GetShort();
			if ((type & SYNCObjectType.UShort) != 0)
				return reader.GetUShort();
			if ((type & SYNCObjectType.Int) != 0)
				return reader.GetInt();
			if ((type & SYNCObjectType.UInt) != 0)
				return reader.GetUInt();
			if ((type & SYNCObjectType.Long) != 0)
				return reader.GetLong();
			if ((type & SYNCObjectType.ULong) != 0)
				return reader.GetULong();
			if ((type & SYNCObjectType.Float) != 0)
				return reader.GetFloat();
			if ((type & SYNCObjectType.Double) != 0)
				return reader.GetDouble();
			if ((type & SYNCObjectType.Vector3) != 0)
				return (Vector3)Vector3Pack.Deserialize(reader);
			if ((type & SYNCObjectType.Vector2) != 0)
				return (Vector2)Vector2Pack.Deserialize(reader);

			throw new ArgumentOutOfRangeException();
		}

		private static int GetObjectByteSize(object obj) {
			return obj switch {
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

		private static void WriteType(NetDataWriter writer, object obj, SYNCObjectType typeFlags) {
			switch (obj) {
				case char _:
					WriteType(writer, typeFlags | SYNCObjectType.Char);
					break;
				case string _:
					WriteType(writer, typeFlags | SYNCObjectType.String);
					break;
				case bool _:
					WriteType(writer, typeFlags | SYNCObjectType.Bool);
					break;
				case byte _:
					WriteType(writer, typeFlags | SYNCObjectType.Byte);
					break;
				case sbyte _:
					WriteType(writer, typeFlags | SYNCObjectType.SByte);
					break;
				case short _:
					WriteType(writer, typeFlags | SYNCObjectType.Short);
					break;
				case ushort _:
					WriteType(writer, typeFlags | SYNCObjectType.UShort);
					break;
				case int _:
					WriteType(writer, typeFlags | SYNCObjectType.Int);
					break;
				case uint _:
					WriteType(writer, typeFlags | SYNCObjectType.UInt);
					break;
				case long _:
					WriteType(writer, typeFlags | SYNCObjectType.Long);
					break;
				case ulong _:
					WriteType(writer, typeFlags | SYNCObjectType.ULong);
					break;
				case float _:
					WriteType(writer, typeFlags | SYNCObjectType.Float);
					break;
				case double _:
					WriteType(writer, typeFlags | SYNCObjectType.Double);
					break;
				case Vector3 _:
					WriteType(writer, typeFlags & SYNCObjectType.Vector3);
					break;
				case Vector2 _:
					WriteType(writer, typeFlags & SYNCObjectType.Vector2);
					break;
				default:
					throw new NotSupportedException($"The provided type {obj.GetType().Name} is not supported");
			}
		}

		private static void WriteType(NetDataWriter writer, SYNCObjectType type) => writer.Put((ushort)type);

		private static void WriteValue(NetDataWriter writer, object obj) {
			switch (obj) {
				case char data:
					writer.Put(data);
					break;
				case string data:
					writer.Put(data);
					break;
				case bool data:
					writer.Put(data);
					break;
				case byte data:
					writer.Put(data);
					break;
				case sbyte data:
					writer.Put(data);
					break;
				case short data:
					writer.Put(data);
					break;
				case ushort data:
					writer.Put(data);
					break;
				case int data:
					writer.Put(data);
					break;
				case uint data:
					writer.Put(data);
					break;
				case long data:
					writer.Put(data);
					break;
				case ulong data:
					writer.Put(data);
					break;
				case float data:
					writer.Put(data);
					break;
				case double data:
					writer.Put(data);
					break;
				case Vector3 data:
					Vector3Pack.Serialize(writer, data);
					break;
				case Vector2 data:
					Vector2Pack.Serialize(writer, data);
					break;
				default:
					throw new NotSupportedException($"The provided type {obj.GetType().Name} is not supported");
			}
		}
	}
}
