using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace InfoStrat.MotionFx
{
    public static class BitmapHelper
    {
        #region Public Static Methods

        public static int CoordinateToIndex(int x, int y, int bytesPerPixel, int stride)
        {
            return x * bytesPerPixel + y * stride;
        }

        public static Point IndexToCoordinate(int index, int stride)
        {
            if (stride == 0)
            {
                return new Point();
            }

            return new Point()
            {
                X = index % stride,
                Y = index / stride
            };
        }

        #endregion

    }
}
