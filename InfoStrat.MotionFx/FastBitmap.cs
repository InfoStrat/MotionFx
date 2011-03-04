using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
//using System.Windows;

namespace InfoStrat.MotionFx
{
    public unsafe class FastBitmap
    {
        #region Properties

        private int _width;
        public int Width { get { return _width; } }

        private int _height;
        public int Height { get { return _height; } }

        public int NumPixels { get { return (int)(Width * Height); } }
        
        private ushort[] _bitmap;
        public ushort[] Bitmap { get { return _bitmap; } }

        #endregion

        #region Constructors

        public FastBitmap(int width, int height)
        {
            this._width = width;
            this._height = height;

            _bitmap = new ushort[this.NumPixels];            
        }

        #endregion
            
        #region Public Methods

        public unsafe ushort GetPixel(int x, int y)
        {
            int index = BitmapHelper.CoordinateToIndex(x, y, 1, Width);
            if (index < 0 || index > _bitmap.Length)
                throw new IndexOutOfRangeException();
            return _bitmap[index];
        }

        public unsafe ushort GetPixel(int index)
        {
            if (index < 0 || index > _bitmap.Length)
                throw new IndexOutOfRangeException();
            return _bitmap[index];
        }

        public unsafe void SetPixel(int x, int y, ushort value)
        {
            int index = BitmapHelper.CoordinateToIndex(x, y, 1, Width);
            if (index < 0 || index > _bitmap.Length)
                throw new IndexOutOfRangeException();
            _bitmap[index] = value;
        }

        public unsafe void SetPixel(int index, ushort value)
        {
            if (index < 0 || index > _bitmap.Length)
                throw new IndexOutOfRangeException();
            _bitmap[index] = value;
        }


        #endregion

    }

}
