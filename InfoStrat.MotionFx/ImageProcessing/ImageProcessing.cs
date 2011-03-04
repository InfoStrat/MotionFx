using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DirectCanvas;
using DirectCanvas.Imaging;
using InfoStrat.MotionFx.ImageProcessing.Effects;
using DirectCanvas.Misc;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Threading;

namespace InfoStrat.MotionFx.ImageProcessing
{
    public class ImageProcessor
    {
        public DirectCanvasFactory Factory { get; set; }
        public WPFPresenter DepthPresenter { get; private set; }
        DrawingLayer intermediateLayer;
        DrawingLayer effectLayer;
        public WPFPresenter HandPresenter { get; private set; }
        ThresholdEffect effect;

        public ImageProcessor(Size ImageSize, Dispatcher dispatcher)
        {
            Factory = new DirectCanvasFactory();
            effect = new ThresholdEffect(Factory);
            InitLayers(ImageSize, dispatcher);
        }

        public void InitLayers(Size ImageSize, Dispatcher dispatcher)
        {
            DepthPresenter = new WPFPresenter(Factory, ImageSize.Width, ImageSize.Height);
            intermediateLayer = Factory.CreateDrawingLayer(ImageSize.Width, ImageSize.Height);
            effectLayer = Factory.CreateDrawingLayer(ImageSize.Width, ImageSize.Height);
            HandPresenter = new WPFPresenter(Factory, ImageSize.Width, ImageSize.Height);
        }

        public static void ConvertImageToBitmapSource(Dispatcher dispatcher, Action<BitmapSource> BitmapSourceCallback, Image image)
        {
            if (dispatcher == null)
                throw new ArgumentNullException("dispatcher");

            if (BitmapSourceCallback == null)
                throw new ArgumentNullException("BitmapSourceCallback");

            dispatcher.Invoke((Action)delegate
                {
                    var imageData = image.Lock(ImageLock.Read);
                    BitmapSource bitmap = BitmapSource.Create(image.Width, image.Height, 96, 96,
                                                              PixelFormats.Bgra32, null,
                                                                imageData.Scan0, imageData.BufferSize, imageData.Stride);

                    image.Unlock(imageData);
                    BitmapSourceCallback(bitmap);
                });
        }

        private unsafe void LoadData(Image image, byte[] sourceData)
        {
            var imageData = image.Lock(ImageLock.ReadWrite);

            byte* data = (byte*)imageData.Scan0.ToPointer();
            int index = 0;
            for (int i = 0; i < imageData.BufferSize; i += 4, index++)
            {
                //byte value = (byte)((double)i * 255 / (double)imageData.BufferSize);
                for (int k = 0; k < 3; k++)
                {
                    if (i + k < imageData.BufferSize)
                        data[i + k] = sourceData[index];
                }
                data[i + 3] = 255;
            }

            image.Unlock(imageData);
        }

        public unsafe void SaveImageToBytes(Image image, ref byte[] targetData)
        {
            var imageData = image.Lock(ImageLock.ReadWrite);

            byte* data = (byte*)imageData.Scan0.ToPointer();
            int index = 0;
            int size = imageData.BufferSize;
            for (int i = 0; i < size; i += 4, index++)
            {
                for (int k = 0; k < 4; k++)
                {
                    //if (i + k < imageData.BufferSize)
                    targetData[i + k] = data[i + k];
                }
            }

            image.Unlock(imageData);
        }

        public void ProcessDepthSessions(Image depthImage, Dictionary<int, HandSession> sessions)
        {
            DepthPresenter.CopyFromImage(depthImage);

            var depthBrush = Factory.CreateDrawingLayerBrush(DepthPresenter);
            depthBrush.Alignment = DirectCanvas.Brushes.BrushAlignment.DrawingLayerAbsolute;


            HandPresenter.Clear();

            effectLayer.Clear();

            foreach (var kvp in sessions)
            {
                var session = kvp.Value;
                var rectf = new RectangleF((float)(session.PositionProjective.X - 100), (float)(session.PositionProjective.Y - 100), 200, 200);
                intermediateLayer.Clear();
                intermediateLayer.BeginDraw();
                
                intermediateLayer.FillRectangle(depthBrush, rectf);

                intermediateLayer.EndDraw();

                if (session.IsPromotedToTouch)
                    effect.Tint = new Color4(.8f, 0, 0, 0);
                else
                    effect.Tint = new Color4(.4f, 0, 0, 0);
                effect.MinThreshold = (float)(session.PositionProjective.Z - 100);
                effect.MaxThreshold = (float)(session.PositionProjective.Z + 100);
                intermediateLayer.ApplyEffect(effect, effectLayer, false);

                HandPresenter.BeginCompose();
                HandPresenter.ComposeLayer(effectLayer, rectf, rectf, new RotationParameters(), new Color4(1, 1, 1, 1));
                HandPresenter.EndCompose();
            }
            DepthPresenter.Present();
            HandPresenter.Present();
        }
    }
}
