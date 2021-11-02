using System;
using System.Linq;
using LiteNetLib.Utils;
using Sync.Utils.Extensions;
using UnityEngine;

namespace Sync.Utils {
	internal interface IPack {
		public int Size { get;  }
	}

	internal struct TransformPack : IPack {
		public int netID;
		public SYNCTransformOptions options;

		public Vector3 Position { get; }
		public Quaternion Rotation { get; }
		public Vector3 Scale { get; }

		public int Size => sizeof(int) // netID
		                   + sizeof(ushort) // options ushort
		                   + options.GetByteSize();

		internal TransformPack(int netID, SYNCTransformOptions options, Vector3 position, Quaternion rotation, Vector3 scale) {
			this.netID = netID;
			this.options = options;
			this.Position = position;
			this.Rotation = rotation;
			this.Scale = scale;
		}

		public static void Serialize(NetDataWriter writer, TransformPack pack) {
			writer.Put(pack.netID);
			writer.Put((ushort)pack.options);

			if ((pack.options & SYNCTransformOptions.PositionIgnore) == 0) {
				if ((pack.options & SYNCTransformOptions.PositionVector3Half) != 0)
					Vector3HalfPack.Serialize(writer, pack.Position);
				else if ((pack.options & SYNCTransformOptions.PositionVector3Float) != 0)
					Vector3Pack.Serialize(writer, pack.Position);
				else if ((pack.options & SYNCTransformOptions.PositionVector2Half) != 0)
					Vector2HalfPack.Serialize(writer, pack.Position);
				else if ((pack.options & SYNCTransformOptions.PositionVector2Float) != 0)
					Vector2Pack.Serialize(writer, pack.Position);
			}

			if ((pack.options & SYNCTransformOptions.RotationIgnore) == 0) {
				if ((pack.options & SYNCTransformOptions.Quaternion) != 0)
					QuaternionPack.Serialize(writer, pack.Rotation);
			}

			if ((pack.options & SYNCTransformOptions.ScaleIgnore) == 0) {
				if ((pack.options & SYNCTransformOptions.ScaleVector3Half) != 0)
					Vector3HalfPack.Serialize(writer, pack.Scale);
				else if ((pack.options & SYNCTransformOptions.ScaleVector3Float) != 0)
					Vector3Pack.Serialize(writer, pack.Scale);
				else if ((pack.options & SYNCTransformOptions.ScaleVector2Half) != 0)
					Vector2HalfPack.Serialize(writer, pack.Scale);
				else if ((pack.options & SYNCTransformOptions.ScaleVector2Float) != 0)
					Vector2Pack.Serialize(writer, pack.Scale);
				else if ((pack.options & SYNCTransformOptions.ScaleUniformHalf) != 0)
					writer.Put(Mathf.FloatToHalf(pack.Scale.x));
				else if ((pack.options & SYNCTransformOptions.ScaleUniformFloat) != 0)
					writer.Put(pack.Scale.x);
			}
		}

		public static TransformPack Deserialize(NetDataReader reader) {
			int netID = reader.GetInt();
			SYNCTransformOptions options = (SYNCTransformOptions)reader.GetUShort();
			Vector3 position = Vector3.zero;
			Vector3 scale = Vector3.zero;
			Quaternion rotation = Quaternion.identity;

			if ((options & SYNCTransformOptions.PositionIgnore) == 0) {
				if ((options & SYNCTransformOptions.PositionVector3Half) != 0)
					position = Vector3HalfPack.Deserialize(reader);
				else if ((options & SYNCTransformOptions.PositionVector3Float) != 0)
					position = Vector3Pack.Deserialize(reader);
				else if ((options & SYNCTransformOptions.PositionVector2Half) != 0)
					position = Vector2HalfPack.Deserialize(reader);
				else if ((options & SYNCTransformOptions.PositionVector2Float) != 0)
					position = Vector2Pack.Deserialize(reader);
			}

			if ((options & SYNCTransformOptions.RotationIgnore) == 0) {
				if ((options & SYNCTransformOptions.Quaternion) != 0)
					rotation = QuaternionPack.Deserialize(reader);
			}

			if ((options & SYNCTransformOptions.ScaleIgnore) == 0) {
				if ((options & SYNCTransformOptions.ScaleVector3Half) != 0)
					scale = Vector3HalfPack.Deserialize(reader);
				else if ((options & SYNCTransformOptions.ScaleVector3Float) != 0)
					scale = Vector3Pack.Deserialize(reader);
				else if ((options & SYNCTransformOptions.ScaleVector2Half) != 0)
					scale = Vector2HalfPack.Deserialize(reader);
				else if ((options & SYNCTransformOptions.ScaleVector2Float) != 0)
					scale = Vector2Pack.Deserialize(reader);
				else if ((options & SYNCTransformOptions.ScaleUniformHalf) != 0) {
					float value = Mathf.HalfToFloat(reader.GetUShort());
					scale.Set(value, value, value);
				}
				else if ((options & SYNCTransformOptions.ScaleUniformFloat) != 0) {
					float value = reader.GetFloat();
					scale.Set(value, value, value);
				}
			}

			return new TransformPack(netID, options, position, rotation, scale);
		}
	}

	internal struct AnimatorPack : IPack {
		public int NetID { get; }
		public AnimatorParameterPack[] ParameterPacks { get; }
		public byte ParameterCount => (byte)ParameterPacks.Length;

		public int Size => sizeof(int) + ParameterPacks.Sum(pack => pack.Size);

		internal AnimatorPack(int netID, AnimatorParameterPack[] parameterPacks) {
			this.NetID = netID;
			this.ParameterPacks = parameterPacks;
		}

		public static void Serialize(NetDataWriter writer, AnimatorPack pack) {
			writer.Put(pack.NetID);
			writer.Put(pack.ParameterCount);

			foreach (AnimatorParameterPack parameterPack in pack.ParameterPacks)
				AnimatorParameterPack.Serialize(writer, parameterPack);
		}

		public static AnimatorPack Deserialize(NetDataReader reader) {
			int netID = reader.GetInt();
			int parameterCount = reader.GetByte();

			AnimatorParameterPack[] parameterPacks = new AnimatorParameterPack[parameterCount];

			for (int i = 0; i < parameterCount; i++)
				parameterPacks[i] = AnimatorParameterPack.Deserialize(reader);

			return new AnimatorPack(netID, parameterPacks);
		}
	}

	internal struct AnimatorParameterPack : IPack {
		public int NameHash { get; }
		public AnimatorControllerParameterType Type { get; }

		public float FloatValue { get; }
		public bool BoolValue { get; }
		public int IntValue { get; }

		public int Size => sizeof(int)
		                   + sizeof(short)
		                   + Type switch {
			                   AnimatorControllerParameterType.Float => sizeof(float),
			                   AnimatorControllerParameterType.Int => sizeof(int),
			                   AnimatorControllerParameterType.Bool => sizeof(bool),
			                   _ => throw new ArgumentOutOfRangeException(),
		                   };

		private AnimatorParameterPack(int nameHash, AnimatorControllerParameterType type) {
			this.NameHash = nameHash;
			this.Type = type;
			this.FloatValue = 0f;
			this.BoolValue = false;
			this.IntValue = 0;
		}

		internal AnimatorParameterPack(int nameHash, AnimatorControllerParameterType type, float floatValue) : this(nameHash, type) {
			this.FloatValue = floatValue;
		}

		internal AnimatorParameterPack(int nameHash, AnimatorControllerParameterType type, int intValue) : this(nameHash, type) {
			this.IntValue = intValue;
		}

		internal AnimatorParameterPack(int nameHash, AnimatorControllerParameterType type, bool boolValue) : this(nameHash, type) {
			this.BoolValue = boolValue;
		}

		public static void Serialize(NetDataWriter writer, AnimatorParameterPack pack) {
			writer.Put(pack.NameHash);
			writer.Put((short)pack.Type);

			switch (pack.Type) {
				case AnimatorControllerParameterType.Float:
					writer.Put(pack.FloatValue);
					break;
				case AnimatorControllerParameterType.Int:
					writer.Put(pack.IntValue);
					break;
				case AnimatorControllerParameterType.Bool:
					writer.Put(pack.BoolValue);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static AnimatorParameterPack Deserialize(NetDataReader reader) {
			int nameHash = reader.GetInt();
			AnimatorControllerParameterType type = (AnimatorControllerParameterType)reader.GetShort();

			return type switch {
				AnimatorControllerParameterType.Float => new AnimatorParameterPack(nameHash, type, reader.GetFloat()),
				AnimatorControllerParameterType.Int => new AnimatorParameterPack(nameHash, type, reader.GetInt()),
				AnimatorControllerParameterType.Bool => new AnimatorParameterPack(nameHash, type, reader.GetBool()),
				_ => throw new ArgumentOutOfRangeException(),
			};
		}

	}

	internal struct InstantiatePack {
		public SYNCInstantiateOptions options;

		public Vector3 Position { get; }
		public Quaternion Rotation { get; }
		public int Parent { get; }

		internal InstantiatePack(Vector3 position, Quaternion rotation, SYNCInstantiateOptions options) {
			this.Position = position;
			this.Rotation = rotation;
			this.Parent = -1;
			this.options = options;
		}

		internal InstantiatePack(int parentNetID, SYNCInstantiateOptions options) {
			this.Position = Vector3.zero;
			this.Rotation = Quaternion.identity;
			this.Parent = parentNetID;
			this.options = options;
		}

		public static void Serialize(NetDataWriter writer, InstantiatePack pack) {
			writer.Put((ushort)pack.options);

			if ((pack.options & SYNCInstantiateOptions.Standard) != 0)
				return;

			if ((pack.options & SYNCInstantiateOptions.Parent) != 0 || (pack.options & SYNCInstantiateOptions.ParentWorldSpace) != 0) {
				writer.Put(pack.Parent);
				return;
			}

			if ((pack.options & (SYNCInstantiateOptions.PositionOnly | SYNCInstantiateOptions.PositionAndRotation)) != 0) {
				if ((pack.options & SYNCInstantiateOptions.Half) != 0)
					Vector3HalfPack.Serialize(writer, pack.Position);
				else if ((pack.options & SYNCInstantiateOptions.Float) != 0)
					Vector3Pack.Serialize(writer, pack.Position);
			}

			if ((pack.options & (SYNCInstantiateOptions.RotationOnly | SYNCInstantiateOptions.PositionAndRotation)) != 0) {
				QuaternionPack.Serialize(writer, pack.Rotation);
			}
		}

		public static InstantiatePack Deserialize(NetDataReader reader) {
			SYNCInstantiateOptions options = (SYNCInstantiateOptions)reader.GetUShort();

			if ((options & SYNCInstantiateOptions.Parent) != 0 || (options & SYNCInstantiateOptions.ParentWorldSpace) != 0) {
				int parentNetID = reader.GetInt();
				return new InstantiatePack(parentNetID, options);
			}

			Vector3 position = Vector3.zero;
			Quaternion rotation = Quaternion.identity;

			if ((options & (SYNCInstantiateOptions.PositionOnly | SYNCInstantiateOptions.PositionAndRotation)) != 0) {
				if ((options & SYNCInstantiateOptions.Half) != 0)
					position = Vector3HalfPack.Deserialize(reader);
				else if ((options & SYNCInstantiateOptions.Float) != 0)
					position = Vector3Pack.Deserialize(reader);
			}

			if ((options & (SYNCInstantiateOptions.RotationOnly | SYNCInstantiateOptions.PositionAndRotation)) != 0) {
				rotation = QuaternionPack.Deserialize(reader);
			}

			return new InstantiatePack(position, rotation, options);
		}
	}

	internal struct Vector3Pack {
		public float x, y, z;

		public static void Serialize(NetDataWriter writer, Vector3Pack pack) {
			writer.Put(pack.x);
			writer.Put(pack.y);
			writer.Put(pack.z);
		}

		public static Vector3Pack Deserialize(NetDataReader reader) => new Vector3Pack {
			x = reader.GetFloat(),
			y = reader.GetFloat(),
			z = reader.GetFloat(),
		};

		public static implicit operator Vector3(Vector3Pack vec) => new Vector3(vec.x, vec.y, vec.z);
		public static implicit operator Vector3Pack(Vector3 vec) => new Vector3Pack {x = vec.x, y = vec.y, z = vec.z};
	}

	internal struct Vector3HalfPack {
		public float x, y, z;

		public static void Serialize(NetDataWriter writer, Vector3HalfPack pack) {
			writer.Put(Mathf.FloatToHalf(pack.x));
			writer.Put(Mathf.FloatToHalf(pack.y));
			writer.Put(Mathf.FloatToHalf(pack.z));
		}

		public static Vector3HalfPack Deserialize(NetDataReader reader) => new Vector3HalfPack {
			x = Mathf.HalfToFloat(reader.GetUShort()),
			y = Mathf.HalfToFloat(reader.GetUShort()),
			z = Mathf.HalfToFloat(reader.GetUShort()),
		};

		public static implicit operator Vector3(Vector3HalfPack vec) => new Vector3(vec.x, vec.y, vec.z);
		public static implicit operator Vector3HalfPack(Vector3 vec) => new Vector3HalfPack {x = vec.x, y = vec.y, z = vec.z};
	}

	internal struct Vector2Pack {
		public float x, y;

		public static void Serialize(NetDataWriter writer, Vector2Pack pack) {
			writer.Put(pack.x);
			writer.Put(pack.y);
		}

		public static Vector2Pack Deserialize(NetDataReader reader) => new Vector2Pack {
			x = reader.GetFloat(),
			y = reader.GetFloat(),
		};

		public static implicit operator Vector2(Vector2Pack vec) => new Vector2(vec.x, vec.y);
		public static implicit operator Vector2Pack(Vector2 vec) => new Vector2Pack() {x = vec.x, y = vec.y};
		public static implicit operator Vector3(Vector2Pack vec) => new Vector3(vec.x, vec.y, 0);
		public static implicit operator Vector2Pack(Vector3 vec) => new Vector2Pack() {x = vec.x, y = vec.y};
	}

	internal struct Vector2HalfPack {
		public float x, y;

		public static void Serialize(NetDataWriter writer, Vector2HalfPack pack) {
			writer.Put(Mathf.FloatToHalf(pack.x));
			writer.Put(Mathf.FloatToHalf(pack.y));
		}

		public static Vector2HalfPack Deserialize(NetDataReader reader) => new Vector2HalfPack {
			x = Mathf.HalfToFloat(reader.GetUShort()),
			y = Mathf.HalfToFloat(reader.GetUShort()),
		};

		public static implicit operator Vector2(Vector2HalfPack vec) => new Vector2(vec.x, vec.y);
		public static implicit operator Vector2HalfPack(Vector2 vec) => new Vector2HalfPack() {x = vec.x, y = vec.y};
		public static implicit operator Vector3(Vector2HalfPack vec) => new Vector3(vec.x, vec.y, 0);
		public static implicit operator Vector2HalfPack(Vector3 vec) => new Vector2HalfPack() {x = vec.x, y = vec.y};
	}

	internal struct QuaternionPack {
		public float x, y, z, w;

		public static void Serialize(NetDataWriter writer, QuaternionPack pack) {
			writer.Put(pack.x);
			writer.Put(pack.y);
			writer.Put(pack.z);
			writer.Put(pack.w);
		}

		public static QuaternionPack Deserialize(NetDataReader reader) => new QuaternionPack() {
			x = reader.GetFloat(),
			y = reader.GetFloat(),
			z = reader.GetFloat(),
			w = reader.GetFloat(),
		};

		public static implicit operator Quaternion(QuaternionPack quat) => new Quaternion(quat.x, quat.y, quat.z, quat.w);
		public static implicit operator QuaternionPack(Quaternion quat) => new QuaternionPack() {x = quat.x, y = quat.y, z = quat.z, w = quat.w};
	}
}
