using System;

namespace SYNC.Utils {
	public enum SYNCPositionPrecision : ushort {
		Ignore = 1,
		Vector3Half = 2,
		Vector3Float = 4,
		Vector2Half = 8,
		Vector2Float = 16,
	}

	public enum SYNCRotationPrecision : ushort {
		Ignore = 32,
		Quaternion = 64,
	}

	public enum SYNCScalePrecision : ushort {
		Ignore = 128,
		Vector3Half = 256,
		Vector3Float = 512,
		Vector2Half = 1024,
		Vector2Float = 2048,
		UniformHalf = 4096,
		UniformFloat = 8192,
	}

	[Flags]
	public enum SYNCTransformOptions : ushort {
		PositionIgnore = 1,
		PositionVector3Half = 2,
		PositionVector3Float = 4,
		PositionVector2Half = 8,
		PositionVector2Float = 16,
		RotationIgnore = 32,
		Quaternion = 64,
		ScaleIgnore = 128,
		ScaleVector3Half = 256,
		ScaleVector3Float = 512,
		ScaleVector2Half = 1024,
		ScaleVector2Float = 2048,
		ScaleUniformHalf = 4096,
		ScaleUniformFloat = 8192,
	}
}
