namespace SYNC.Utils.Extensions {
	internal static class SYNCTransformOptionsExtensions {
		internal static int GetByteSize(this SYNCTransformOptions options) {
			int size = 0;

			if ((options & SYNCTransformOptions.PositionIgnore) == 0) {
				if ((options & SYNCTransformOptions.PositionVector3Half) != 0)
					size += sizeof(ushort) * 3;
				else if ((options & SYNCTransformOptions.PositionVector3Float) != 0)
					size += sizeof(float) * 3;
				else if ((options & SYNCTransformOptions.PositionVector2Half) != 0)
					size += sizeof(ushort) * 2;
				else if ((options & SYNCTransformOptions.PositionVector2Float) != 0)
					size += sizeof(float) * 2;
			}

			if ((options & SYNCTransformOptions.RotationIgnore) == 0) {
				if ((options & SYNCTransformOptions.Quaternion) != 0)
					size += sizeof(float) * 4;
			}

			if ((options & SYNCTransformOptions.ScaleIgnore) == 0) {
				if ((options & SYNCTransformOptions.ScaleVector3Half) != 0)
					size += sizeof(ushort) * 3;
				else if ((options & SYNCTransformOptions.ScaleVector3Float) != 0)
					size += sizeof(float) * 3;
				else if ((options & SYNCTransformOptions.ScaleVector2Half) != 0)
					size += sizeof(ushort) * 2;
				else if ((options & SYNCTransformOptions.ScaleVector2Float) != 0)
					size += sizeof(float) * 2;
				else if ((options & SYNCTransformOptions.ScaleUniformHalf) != 0)
					size += sizeof(ushort);
				else if ((options & SYNCTransformOptions.ScaleUniformFloat) != 0)
					size += sizeof(float);
			}

			return size;
		}
	}
}
