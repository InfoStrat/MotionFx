using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfoStrat.MotionFx
{
    public class MotionHelper
    {
        internal static System.Windows.Media.Media3D.Point3D XnPoint3DToPoint3D(OpenNI.Point3D point)
        {
            return new System.Windows.Media.Media3D.Point3D(point.X, point.Y, point.Z);
        }

        public static ushort BytesToShort(byte lowByte, byte highByte)
        {
            return (ushort)((highByte << 8) + lowByte);
        }

        public static void ShortToBytes(ushort number, out byte lowByte, out byte highByte)
        {
            highByte = (byte)(number >> 8); 
            lowByte = (byte)(number & 255);
        }
    }
}
