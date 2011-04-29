using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.IO;
using System.Diagnostics;
using DirectCanvas.Imaging;
using DirectCanvas;
using System.Windows.Media.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;

namespace InfoStrat.MotionFx
{
    public class DepthFrame
    {
        #region Properties

        public ushort[] DepthPixels { get; private set; }

        #region Width
        private int _width;
        public int Width
        {
            get { return _width; }
            private set { _width = value; }
        } 
        #endregion

        #region Height
        private int _height;
        public int Height
        {
            get { return _height; }
            private set { _height = value; }
        } 
        #endregion

        public ushort MaxDepth = 10000;

        #region IsLoaded
        private bool _isLoaded = false;
        public bool IsLoaded
        {
            get { return _isLoaded; }
            private set { _isLoaded = value; }
        } 
        #endregion

        #region MinThreshold
        private ushort _minThreshold = 100;
        public ushort MinThreshold { 
            get { return _minThreshold; }
            set { _minThreshold = value; }
        }
        #endregion

        #region MaxThreshold
        private ushort _maxThreshold = 10000;
        public ushort MaxThreshold
        {
            get { return _maxThreshold; }
            set { _maxThreshold = value; }
        } 
        #endregion

        #region Crop

        private Rect _crop = new Rect(0, 0, 640, 480);
        public Rect Crop
        {
            get { return _crop; }
            set { _crop = value; }
        }

        #endregion

        #endregion

        #region Constructors

        public DepthFrame(ushort[] depthPixels, int width, int height)
        {
            this.DepthPixels = depthPixels;
            this.Width = width;
            this.Height = height;
        }

        public DepthFrame(string filename)
        {
            IsLoaded = Load(filename);
        }

        #endregion

        public ushort Sample(Vector texCoord)
        {
            int x = (int)(texCoord.X * Width);
            int y = (int)(texCoord.Y * Height);
            
            return Sample(x, y);
        }

        public ushort Sample(int x, int y)
        {
            if (x < _crop.Left || x >= _crop.Right||
                y < _crop.Top || y >= _crop.Bottom)
                return MaxDepth;
            int index = x + y * Width;
            if (index < 0 || index >= DepthPixels.Length)
                return MaxDepth;
            var sample = DepthPixels[x + y * Width];
            if (sample >= MaxThreshold ||
                sample <= MinThreshold)
                sample = MaxDepth;
            return sample;
        }

        public void Clear()
        {
            int size = _width * _height;
            this.DepthPixels = new ushort[size];
            for (int i = 0; i < size; i++)
            {
                this.DepthPixels[i] = MaxDepth;
            }
        }

        public bool Load(string filepath)
        {
            try
            {
                using (var stream = File.OpenRead(filepath))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        this.Width = reader.ReadInt32();
                        this.Height = reader.ReadInt32();
                        int len = reader.ReadInt32();
                        DepthPixels = new ushort[len];
                        for (int i = 0; i < len; i++)
                        {
                            DepthPixels[i] = reader.ReadUInt16();
                        }
                        this.MinThreshold = reader.ReadUInt16();
                        this.MaxThreshold = reader.ReadUInt16();
                        if (stream.Position < stream.Length)
                        {
                            double x = reader.ReadDouble();
                            double y = reader.ReadDouble();
                            double width = reader.ReadDouble();
                            double height = reader.ReadDouble();
                            this.Crop = new Rect(x, y, width, height);
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {
                Trace.WriteLine("File not found: " + filepath);
                return false;
            }
            catch (EndOfStreamException)
            {
                Trace.WriteLine("File too short: " + filepath);
                return true;
            }
            return true;
        }

        public void Save(string filepath)
        {
            using (var stream = new FileStream(filepath, FileMode.Create))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(this.Width);
                    writer.Write(this.Height);
                    writer.Write(DepthPixels.Length);
                    for (int i = 0; i < DepthPixels.Length; i++)
                    {
                        writer.Write(DepthPixels[i]);
                    }
                    writer.Write(this.MinThreshold);
                    writer.Write(this.MaxThreshold);
                    writer.Write(this.Crop.Left);
                    writer.Write(this.Crop.Top);
                    writer.Write(this.Crop.Width);
                    writer.Write(this.Crop.Height);
                }
            }
        }

        unsafe public Image ToDirectCanvasImage(DirectCanvasFactory factory)
        {
            //ushort min, max;
            //return ToDirectCanvasImage(factory, out min, out max);
            Image sourceImage = factory.CreateImage(_width, _height);
            var imageData = sourceImage.Lock(DirectCanvas.Imaging.ImageLock.ReadWrite);
            int numPixels = _width * _height;

            //Image is BGRA format, 4 bytes per pixel
            //byte* imagePtr = (byte*)imageData.Scan0.ToPointer();

            int len = _width * _height * sizeof(ushort);

            //imagePtr[i * 4 + 0] = (byte)(value & 255);  //low byte of pixel 1 in B
            //imagePtr[i * 4 + 1] = (byte)(value >> 8);   //high byte of pixel 1 in G
            //imagePtr[i * 4 + 2] = (byte)(value2 & 255); //low byte of pixel 2 in R
            //imagePtr[i * 4 + 3] = (byte)(value2 >> 8);  //high byte of pixel 2 in A

            fixed (ushort* sourceDataPtr = this.DepthPixels)
            {
                NativeInterop.MoveMemory(imageData.Scan0, (IntPtr)sourceDataPtr, len);
            }
            sourceImage.Unlock(imageData);

            return sourceImage;
        }

        unsafe public Image ToDirectCanvasImage(DirectCanvasFactory factory, out ushort minValue, out ushort maxValue)
        {
            Image sourceImage = factory.CreateImage(_width, _height);
            var imageData = sourceImage.Lock(DirectCanvas.Imaging.ImageLock.ReadWrite);
            int numPixels = _width * _height;

            //Image is BGRA format, 4 bytes per pixel
            byte* imagePtr = (byte*)imageData.Scan0.ToPointer();

            minValue = ushort.MaxValue;
            maxValue = ushort.MinValue;
            
            fixed (ushort* sourceDataPtr = this.DepthPixels)
            {
                ushort* sourcePtr = sourceDataPtr;
                ushort* targetPtr = (ushort*)imagePtr;
                //for (int i = 0; i < numPixels; i++)
                for (int x = 0; x < _width; x++)
                {
                    for (int y = 0; y < _height; y++)
                    {
                        //int i = x + y * _width;
                        //ushort value = sourceData[i];
                        ushort value = *sourcePtr;

                        if (value > maxValue)
                            maxValue = value;
                        if (value > 100 && value < minValue)
                            minValue = value;

                        //pack ushort into first two bytes of pixel
                        //((ushort*)imagePtr)[i * 2] = value;
                        *targetPtr = value;
                        //imagePtr[i * 4] = (byte)(value & 255); //low byte in B
                        //imagePtr[i * 4 + 1] = (byte)(value >> 8); //high byte in G

                        //store 255 in Alpha channel of pixel
                        //imagePtr[i * 4 + 3] = 255;
                        //if (_crop.Contains(x, y))
                        //{
                        //    *(imagePtr + 3) = 255;
                        //}
                        //else
                        //{
                        //    *(imagePtr + 3) = 128;
                        //}

                        sourcePtr++;
                        imagePtr += 4;
                        targetPtr += 1;
                    }
                }
            }

            sourceImage.Unlock(imageData);

            return sourceImage;
        }
    }
}
