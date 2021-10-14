using LiteNetLib.Utils;
using UnityEngine;

namespace SYNC.Utils {
	internal struct TransformPack {
		public int netID;
		public Vector3Pack position, scale;

		public static void Serialize(NetDataWriter writer, TransformPack pack) {
			writer.Put(pack.netID);
			Vector3Pack.Serialize(writer, pack.position);
			Vector3Pack.Serialize(writer, pack.scale);
		}

		public static TransformPack Deserialize(NetDataReader reader) => new TransformPack {
			netID = reader.GetInt(),
			position = Vector3Pack.Deserialize(reader),
			scale = Vector3Pack.Deserialize(reader),
		};
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
	}
}
