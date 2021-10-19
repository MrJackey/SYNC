using System;

namespace SYNC.Utils {
	public enum SYNCFloatAccuracy : ushort {
		Half = 1,
		Float = 2,
	}

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
		PositionIgnore = SYNCPositionPrecision.Ignore,
		PositionVector3Half = SYNCPositionPrecision.Vector3Half,
		PositionVector3Float = SYNCPositionPrecision.Vector3Float,
		PositionVector2Half = SYNCPositionPrecision.Vector2Half,
		PositionVector2Float = SYNCPositionPrecision.Vector2Float,
		RotationIgnore = SYNCRotationPrecision.Ignore,
		Quaternion = SYNCRotationPrecision.Quaternion,
		ScaleIgnore = SYNCScalePrecision.Ignore,
		ScaleVector3Half = SYNCScalePrecision.Vector3Half,
		ScaleVector3Float = SYNCScalePrecision.Vector3Float,
		ScaleVector2Half = SYNCScalePrecision.Vector2Half,
		ScaleVector2Float = SYNCScalePrecision.Vector2Float,
		ScaleUniformHalf = SYNCScalePrecision.UniformHalf,
		ScaleUniformFloat = SYNCScalePrecision.UniformFloat,
	}

	public enum SYNCInstantiateMode : ushort {
		Standard = 4,
		PositionOnly = 8,
		RotationOnly = 16,
		PositionAndRotation = 32,
	}

	[Flags]
	public enum SYNCInstantiateOptions : ushort {
		Half = SYNCFloatAccuracy.Half,
		Float = SYNCFloatAccuracy.Float,
		Standard = SYNCInstantiateMode.Standard,
		PositionOnly = SYNCInstantiateMode.PositionOnly,
		RotationOnly = SYNCInstantiateMode.RotationOnly,
		PositionAndRotation = SYNCInstantiateMode.PositionAndRotation,
	}
}
