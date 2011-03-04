using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace InfoStrat.MotionFx
{
    
    public class MapUpdatedEventArgs : EventArgs
    {
        public int XRes { get; private set; }
        public int YRes { get; private set; }
        public int Stride { get; private set; }
        public PixelFormat Format { get; private set; }
        public byte[] Map { get; private set; }

        public void GetBitmapSource(Dispatcher dispatcher, Action<BitmapSource> BitmapSourceCallback)
        {
            if (dispatcher == null)
                throw new ArgumentNullException("dispatcher");

            if (BitmapSourceCallback == null)
                throw new ArgumentNullException("BitmapSourceCallback");

            dispatcher.Invoke((Action)delegate
            {
                BitmapSource bitmap = BitmapSource.Create(XRes, YRes, 96, 96,
                                                        Format, null,
                                                        Map, Stride);
                BitmapSourceCallback(bitmap);
            });
        }

        public MapUpdatedEventArgs(byte[] map, int xres, int yres, int stride, PixelFormat format)
        {
            this.Map = map;
            this.XRes = xres;
            this.YRes = yres;
            this.Stride = stride;
            this.Format = format;
        }
    }
}
