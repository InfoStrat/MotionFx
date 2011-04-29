using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DirectCanvas;
using DirectCanvas.Imaging;
using InfoStrat.MotionFx.ImageProcessing.Effects;
using DirectCanvas.Misc;
using System.Windows.Threading;
using DirectCanvas.Brushes;
using Blake.NUI.WPF.Utility;

namespace InfoStrat.MotionFx.ImageProcessing
{
    public class ImageProcessor
    {
        #region Static Properties

        private static ImageProcessor _instance = null;

        public static ImageProcessor Instance
        {
            get
            {
                if (_instance == null)
                {
                    DirectCanvas.Misc.Size imageSize = new DirectCanvas.Misc.Size(640, 480);
                    _instance = new ImageProcessor(imageSize);
                }
                return _instance;
            }
        }

        #endregion

        public DirectCanvasFactory Factory { get; set; }
        public WPFPresenter DepthPresenter { get; private set; }
        DrawingLayer intermediateLayer;
        DrawingLayer rawDepthLayer;
        ThresholdEffect thresholdEffect;
        EdgeDetectEffect edgeEffect;
        UnpackDepthEffect unpackEffect;
        ColorMapDepthEffect colorMapEffect;
        SolidColorBrush hoverBrush;
        SolidColorBrush contactBrush;
        Brush DepthBrush;

        Dictionary<MotionTrackingScreen, WPFPresenter> ScreenVisualizations = new Dictionary<MotionTrackingScreen, WPFPresenter>();

        public ImageProcessor(Size ImageSize)
        {
            try
            {
                Factory = new DirectCanvasFactory();
                thresholdEffect = new ThresholdEffect(Factory);
                edgeEffect = new EdgeDetectEffect(Factory);
                unpackEffect = new UnpackDepthEffect(Factory);
                colorMapEffect = new ColorMapDepthEffect(Factory);
                InitLayers(ImageSize);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Exception initializing ImageProcessor: " + ex.ToString());
            }
        }

        public void InitLayers(Size ImageSize)
        {
            hoverBrush = Factory.CreateSolidColorBrush(new Color4(.4f, 0, 0, 0));
            contactBrush = Factory.CreateSolidColorBrush(new Color4(.8f, 0, 0, 0));

            DepthPresenter = new WPFPresenter(Factory, ImageSize.Width, ImageSize.Height);
            intermediateLayer = Factory.CreateDrawingLayer(ImageSize.Width, ImageSize.Height);
            rawDepthLayer = Factory.CreateDrawingLayer(ImageSize.Width, ImageSize.Height);

            DepthBrush = Factory.CreateDrawingLayerBrush(DepthPresenter);
            DepthBrush.Alignment = DirectCanvas.Brushes.BrushAlignment.DrawingLayerAbsolute;
        }

        public void AddScreen(MotionTrackingScreen screen)
        {
            WPFPresenter presenter = new WPFPresenter(Factory, DepthPresenter.Width, DepthPresenter.Height);
            ScreenVisualizations.Add(screen, presenter);
        }

        public System.Windows.Media.ImageSource GetImageSourceForScreen(MotionTrackingScreen screen)
        {
            if (ScreenVisualizations.ContainsKey(screen))
            {
                return ScreenVisualizations[screen].ImageSource;
            }
            return null;
        }

        public void RemoveScreen(MotionTrackingScreen screen)
        {
            if (ScreenVisualizations.ContainsKey(screen))
            {
                ScreenVisualizations.Remove(screen);
            }
        }

        public void ProcessDepthSessions(Image depthImage, Dictionary<int, MotionTrackingDevice> devices, ushort max, ushort min)
        {
            if (depthImage != null)
            {
                intermediateLayer.CopyFromImage(depthImage);

                unpackEffect.TexSize = new DirectCanvas.Misc.Size(rawDepthLayer.Width, rawDepthLayer.Height);
                intermediateLayer.ApplyEffect(unpackEffect, rawDepthLayer, true);

                colorMapEffect.MinThreshold = 100f;
                colorMapEffect.MaxThreshold = 10000f;
                colorMapEffect.MinValue = min;
                colorMapEffect.MaxValue = max;
                rawDepthLayer.ApplyEffect(colorMapEffect, DepthPresenter, true);

                //edgeEffect.Tint = new Color4(1, 1, 0, 0);
                //edgeEffect.MinThreshold = 100f;
                //edgeEffect.MaxThreshold = 3000f;
                //edgeEffect.EdgeThreshold = 200f;
                //edgeEffect.TexSize = new Size(rawDepthLayer.Width, rawDepthLayer.Height);
                //rawDepthLayer.ApplyEffect(edgeEffect, DepthPresenter, true);
            }
            foreach (var kvp in ScreenVisualizations)
            {
                var screen = kvp.Key;
                var handPresenter = kvp.Value;
                UpdateScreenVisualization(devices, screen, handPresenter);
            }
            if (depthImage != null)
            {
                DepthPresenter.Present();
            }
        }

        private void UpdateScreenVisualization(Dictionary<int, MotionTrackingDevice> devices, MotionTrackingScreen screen, WPFPresenter handPresenter)
        {
            handPresenter.Clear();
            
            edgeEffect.Tint = new Color4(1, 1, 0, 0);
            edgeEffect.MinThreshold = 300f;
            edgeEffect.MaxThreshold = 10000f;
            edgeEffect.EdgeThreshold = 100f;
            edgeEffect.TexSize = new Size(intermediateLayer.Width, intermediateLayer.Height);

            foreach (var innerkvp in devices)
            {
                var session = innerkvp.Value.Session;
                var rectf = new RectangleF((float)(session.PositionProjective.X - 100), (float)(session.PositionProjective.Y - 100), 200, 200);
                if (!screen.IsSessionInBounds(session))
                {
                    continue;
                }

                var p = screen.MapPositionToScreen(session, handPresenter.Width, handPresenter.Height);

                var targetRectf = new RectangleF((float)(p.X - 100), (float)(p.Y - 100), 200, 200);

                if (innerkvp.Value.ShouldPromoteToTouch)
                    thresholdEffect.Tint = new Color4(.8f, 0, 0, 0);
                else
                    thresholdEffect.Tint = new Color4(.4f, 0, 0, 0);
                thresholdEffect.MinThreshold = (float)(session.PositionProjective.Z - 100);
                thresholdEffect.MaxThreshold = (float)(session.PositionProjective.Z + 100);
                rawDepthLayer.ApplyEffect(thresholdEffect, intermediateLayer, true);

                //intermediateLayer.ApplyEffect(unpackEffect, effectLayer, true);

                //handPresenter.BeginDraw();
                //if (session.IsPromotedToTouch)
                //handPresenter.FillEllipse(contactBrush, new DirectCanvas.Shapes.Ellipse(new PointF((float)p.X, (float)p.Y), 20, 20));
                //else
                //  handPresenter.FillEllipse(hoverBrush, new DirectCanvas.Shapes.Ellipse(new PointF((float)p.X, (float)p.Y), 20, 20));
                //handPresenter.EndDraw();
                var tint = new Color4(1, 1, 1, 1);
                //if (innerkvp.Value.ShouldPromoteToTouch)
                //{
                //    tint.Alpha = 0.8f;
                //}
                handPresenter.BeginCompose();
                handPresenter.ComposeLayer(intermediateLayer, rectf, targetRectf, new RotationParameters(), tint);
                handPresenter.EndCompose();
            }

            handPresenter.Present();
        }

        public System.Windows.Media.ImageSource ImageToImageSource(DepthFrame frame)
        {
            WPFPresenter presenter = new WPFPresenter(Factory, frame.Width, frame.Height);
            ushort minValue;
            ushort maxValue;
            var image = frame.ToDirectCanvasImage(Factory, out minValue, out maxValue);
            
            rawDepthLayer.CopyFromImage(image);

            unpackEffect.MinThreshold = 100f;
            unpackEffect.MaxThreshold = 1000f;
            unpackEffect.MinValue = minValue;
            unpackEffect.MaxValue = maxValue;
            unpackEffect.TexSize = new DirectCanvas.Misc.Size(rawDepthLayer.Width, rawDepthLayer.Height);
            rawDepthLayer.ApplyEffect(unpackEffect, presenter, true);
            presenter.Present();
            return presenter.ImageSource;
        }
    }
}
