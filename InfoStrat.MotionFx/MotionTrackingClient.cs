using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Blake.NUI.WPF.Utility;
using InfoStrat.MotionFx.ImageProcessing;
using System.Windows.Media;
using System.ComponentModel;
using DirectCanvas.Imaging;
using DirectCanvas;
using System.Diagnostics;

namespace InfoStrat.MotionFx
{
    public class MotionTrackingClient : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(String info)
        {
            if (PropertyChanged == null)
                return;

            PropertyChanged(this, new PropertyChangedEventArgs(info));
        }

        #endregion

        #region Fields

        private Dictionary<int, MotionTrackingDevice> deviceDictionary = new Dictionary<int, MotionTrackingDevice>();

        ImageProcessor imageProcessor;

        #endregion

        #region Properties

        public Window Window { get; private set; }
        public MotionTrackingScreen Screen { get; private set; }
        public bool ProcessDepthImage { get; set; }

        public bool IsFirstFrameReady { get; private set; }

        #region HandVisualization

        /// <summary>
        /// The <see cref="HandVisualization" /> property's name.
        /// </summary>
        public const string HandVisualizationPropertyName = "HandVisualization";

        private ImageSource _handVisualization = null;

        /// <summary>
        /// Gets the HandVisualization property.
        /// </summary>
        public ImageSource HandVisualization
        {
            get
            {
                return _handVisualization;
            }

            set
            {
                if (_handVisualization == value)
                {
                    return;
                }

                var oldValue = _handVisualization;
                _handVisualization = value;

                // Update bindings, no broadcast
                RaisePropertyChanged(HandVisualizationPropertyName);
            }
        }



        #endregion

        #region DepthVisualization

        /// <summary>
        /// The <see cref="DepthVisualization" /> property's name.
        /// </summary>
        public const string DepthVisualizationPropertyName = "DepthVisualization";

        private ImageSource _depthVisualization = null;

        /// <summary>
        /// Gets the DepthVisualization property.
        /// </summary>
        public ImageSource DepthVisualization
        {
            get
            {
                return _depthVisualization;
            }

            set
            {
                if (_depthVisualization == value)
                {
                    return;
                }

                var oldValue = _depthVisualization;
                _depthVisualization = value;

                // Update bindings, no broadcast
                RaisePropertyChanged(DepthVisualizationPropertyName);
            }
        }

        #endregion

        #region Factory

        /// <summary>
        /// The <see cref="Factory" /> property's name.
        /// </summary>
        public const string FactoryPropertyName = "Factory";

        /// <summary>
        /// Gets the Factory property.
        /// </summary>
        public DirectCanvasFactory Factory
        {
            get
            {
                if (imageProcessor == null)
                    return null;
                return imageProcessor.Factory;
            }
        }

        #endregion

        #endregion

        #region Events

        #region FirstFrameReady

        public event EventHandler FirstFrameReady;

        private void OnFirstFrameReady()
        {
            IsFirstFrameReady = true;
            if (FirstFrameReady == null)
                return;
            FirstFrameReady(this, EventArgs.Empty);
        }

        #endregion

        #region FrameUpdated

        public event EventHandler<FrameUpdatedEventArgs> FrameUpdated;

        private void OnFrameUpdated(FrameUpdatedEventArgs e)
        {
            if (FrameUpdated == null)
                return;
            FrameUpdated(this, e);
        }

        #endregion

        #endregion

        #region Constructors

        public MotionTrackingClient(FrameworkElement root, MotionTrackingScreen screen = null)
        {
            RegisterWindow(root);

            if (Window.IsLoaded)
            {
                RegisterScreen(screen);
            }
            else
            {
                Window.Loaded += (s, e) =>
                    {
                        RegisterScreen(screen);
                    };
            }

            ProcessDepthImage = true;

            this.imageProcessor = ImageProcessor.Instance;
            this.DepthVisualization = imageProcessor.DepthPresenter.ImageSource;

            MotionTracking.RegisterEvents(this);
        }

        #endregion

        #region Public Methods

        private void RegisterWindow(FrameworkElement root)
        {
            Window window = VisualUtility.FindVisualParent<Window>(root);
            if (window == null)
            {
                throw new ArgumentException("Cannot register events without a window in the visual tree");
            }
            this.Window = window;

            window.Closing += (s, e) =>
            {
                Cleanup();
            };
        }

        public void Cleanup()
        {
            if (this.Screen != null)
                imageProcessor.RemoveScreen(this.Screen);
            MotionTracking.UnregisterEvents(this);
        }

        public ImageSource DepthFrameToImageSource(DepthFrame frame)
        {
            return imageProcessor.ImageToImageSource(frame);
        }

        private void RegisterScreen(MotionTrackingScreen screen)
        {
            if (screen == null)
            {
                screen = new MotionTrackingScreen()
                {
                    ScreenWidth = Window.ActualWidth,
                    ScreenHeight = Window.ActualHeight
                };
            }

            this.Screen = screen;
            imageProcessor.AddScreen(this.Screen);

            this.HandVisualization = imageProcessor.GetImageSourceForScreen(this.Screen);
        }

        #endregion

        #region Private Methods
        
        internal void HandPointGenerator_PointCreated(object sender, HandPointEventArgs e)
        {
            if (Window == null)
                return;

            if (!Window.CheckAccess())
            {
                Window.Dispatcher.Invoke(new EventHandler<HandPointEventArgs>(HandPointGenerator_PointCreated), System.Windows.Threading.DispatcherPriority.Input, sender, e);
                return;
            }

            MotionTrackingDevice device = null;
            if (!deviceDictionary.Keys.Contains(e.Id))
            {
                device = new MotionTrackingDevice(Window, Screen);
                deviceDictionary.Add(e.Id, device);
            }

            if (device != null)
            {
                device.ReportMotionTrackingStarted(e);
            }
        }

        internal void HandPointGenerator_PointUpdated(object sender, HandPointEventArgs e)
        {
            if (Window == null)
                return;

            if (!Window.CheckAccess())
            {
                Window.Dispatcher.Invoke(new EventHandler<HandPointEventArgs>(HandPointGenerator_PointUpdated), System.Windows.Threading.DispatcherPriority.Input, sender, e);
                return;
            }

            int id = e.Id;
            if (!deviceDictionary.Keys.Contains(id))
            {
                HandPointGenerator_PointCreated(sender, e);
            }

            MotionTrackingDevice device = deviceDictionary[id];
            if (device != null)
            {
                device.ReportMotionTrackingUpdated(e);
            }
        }

        internal void HandPointGenerator_PointDestroyed(object sender, HandPointEventArgs e)
        {
            if (Window == null)
                return;

            if (!Window.CheckAccess())
            {
                Window.Dispatcher.Invoke(new EventHandler<HandPointEventArgs>(HandPointGenerator_PointDestroyed), System.Windows.Threading.DispatcherPriority.Input, sender, e);
                return;
            }

            int id = e.Id;
            if (!deviceDictionary.Keys.Contains(id))
            {
                return;
            }
            MotionTrackingDevice device = deviceDictionary[id];

            if (device != null)
            {
                device.ReportMotionTrackingLost(e);
            }

            deviceDictionary.Remove(id);

        }

        int frameCount = 0;
        Stopwatch fpsStopwatch = new Stopwatch();

        private void CountFrames()
        {
            if (!fpsStopwatch.IsRunning)
                fpsStopwatch.Start();
            frameCount++;
            if (fpsStopwatch.ElapsedMilliseconds >= 1000)
            {
                double fps = frameCount / fpsStopwatch.Elapsed.TotalSeconds;
                Trace.WriteLine("Fx FPS: " + fps);
                frameCount = 0;
                fpsStopwatch.Reset();
            }
        }

        internal void SourceFrameUpdated(FrameUpdatedEventArgs e)
        {
            Image sourceImage = null;

            try
            {
#if DEBUG
                CountFrames();
#endif
                if (ProcessDepthImage)
                {
                    var frame = e.Frame;
                    
                    sourceImage = e.Frame.ToDirectCanvasImage(imageProcessor.Factory);
                }

                ushort max = e.Frame.DepthPixels.AsParallel().Max();
                ushort min = e.Frame.DepthPixels.AsParallel()   .Where(v => v > 100).Min();
                imageProcessor.ProcessDepthSessions(sourceImage, deviceDictionary, max, min);

                OnFrameUpdated(e);
            }
            catch (AccessViolationException)
            {
                Trace.WriteLine("AccessViolationException - maybe during shutdown?");
                if (Application.Current == null)
                {
                    Trace.WriteLine("Application.Current == null");
                }
            }
        }

        internal void ReportFirstFrameReady()
        {
            OnFirstFrameReady();
        }

        #endregion
    }
}
