using LiteNetLib.Utils;
using SYNC.Utils.Extensions;
using UnityEngine;

namespace SYNC.Utils {
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

	internal struct InstantiatePack {
		public SYNCInstantiateOptions options;

		public Vector3 Position { get; }
		public Quaternion Rotation { get; }

		internal InstantiatePack(Vector3 position, Quaternion rotation, SYNCInstantiateOptions options) {
			this.Position = position;
			this.Rotation = rotation;
			this.options = options;
		}

		public static void Serialize(NetDataWriter writer, InstantiatePack pack) {
			writer.Put((ushort)pack.options);
			if ((pack.options & SYNCInstantiateOptions.Standard) != 0)
				return;

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
