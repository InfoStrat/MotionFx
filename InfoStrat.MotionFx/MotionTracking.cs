using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Blake.NUI.WPF.Utility;

namespace InfoStrat.MotionFx
{
    public static class MotionTracking
    {
        #region RoutedEvents

        #region MotionTrackingStarted

        public static readonly RoutedEvent MotionTrackingStartedEvent = EventManager.RegisterRoutedEvent("MotionTrackingStarted", RoutingStrategy.Bubble, typeof(MotionTrackingEventHandler), typeof(MotionTracking));
        public static void AddMotionTrackingStartedHandler(DependencyObject d, MotionTrackingEventHandler handler)
        {
            AddMotionTrackingStartedHandlerHandledEventsToo(d, handler, false);
        }
        public static void AddMotionTrackingStartedHandlerHandledEventsToo(DependencyObject d, MotionTrackingEventHandler handler, bool handledEventsToo)
        {
            UIElement element = d as UIElement;
            if (element != null)
            {
                element.AddHandler(MotionTrackingStartedEvent, handler, handledEventsToo);
            }
        }
        public static void RemoveMotionTrackingStartedHandler(DependencyObject d, MotionTrackingEventHandler handler)
        {
            UIElement element = d as UIElement;
            if (element != null)
            {
                element.RemoveHandler(MotionTrackingStartedEvent, handler);
            }
        }

        #endregion

        #region MotionTrackingUpdated

        public static readonly RoutedEvent MotionTrackingUpdatedEvent = EventManager.RegisterRoutedEvent("MotionTrackingUpdated", RoutingStrategy.Bubble, typeof(MotionTrackingEventHandler), typeof(MotionTracking));

        public static void AddMotionTrackingUpdatedHandler(DependencyObject d, MotionTrackingEventHandler handler)
        {
            AddMotionTrackingUpdatedHandlerHandledEventsToo(d, handler, false);
        }
        public static void AddMotionTrackingUpdatedHandlerHandledEventsToo(DependencyObject d, MotionTrackingEventHandler handler, bool handledEventsToo)
        {
            UIElement element = d as UIElement;
            if (element != null)
            {
                element.AddHandler(MotionTrackingUpdatedEvent, handler, handledEventsToo);
            }
        }
        public static void RemoveMotionTrackingUpdatedHandler(DependencyObject d, MotionTrackingEventHandler handler)
        {
            UIElement element = d as UIElement;
            if (element != null)
            {
                element.RemoveHandler(MotionTrackingUpdatedEvent, handler);
            }
        }

        #endregion

        #region MotionTrackingLost
        
        public static readonly RoutedEvent MotionTrackingLostEvent = EventManager.RegisterRoutedEvent("MotionTrackingLost", RoutingStrategy.Bubble, typeof(MotionTrackingEventHandler), typeof(MotionTracking));

        public static void AddMotionTrackingLostHandler(DependencyObject d, MotionTrackingEventHandler handler)
        {
            AddMotionTrackingLostHandlerHandledEventsToo(d, handler, false);
        }
        public static void AddMotionTrackingLostHandlerHandledEventsToo(DependencyObject d, MotionTrackingEventHandler handler, bool handledEventsToo)
        {
            UIElement element = d as UIElement;
            if (element != null)
            {
                element.AddHandler(MotionTrackingLostEvent, handler, handledEventsToo);
            }
        }
        public static void RemoveMotionTrackingLostHandler(DependencyObject d, MotionTrackingEventHandler handler)
        {
            UIElement element = d as UIElement;
            if (element != null)
            {
                element.RemoveHandler(MotionTrackingLostEvent, handler);
            }
        }

        #endregion

        #endregion

        #region Constructors

        static MotionTracking()
        {            
        }

        #endregion
        
        #region Static Fields

        private static Dictionary<int, MotionTrackingDevice> deviceDictionary = new Dictionary<int, MotionTrackingDevice>();

        private static PresentationSource presentationSource = null;
        private static Window rootWindow = null;

        #endregion

        #region Public Static Methods

        public static void RegisterEvents(FrameworkElement root)
        {
            Window window = VisualUtility.FindVisualParent<Window>(root);

            if (window == null)
            {
                throw new ArgumentException("Cannot register events without a window in the visual tree");
            }

            if (window.IsLoaded)
            {
                SubscribeToEvents(window);
            }
            else
            {
                window.Loaded += (s, e) =>
                {
                    SubscribeToEvents(window);
                };
            }
        }

        public static void UnregisterEvents(FrameworkElement root)
        {
            Window window = VisualUtility.FindVisualParent<Window>(root);

            if (window == null)
            {
                throw new ArgumentException("Cannot register events without a window in the visual tree");
            }

            HandPointGenerator.Default.StopGenerating();
        }

        private static void SubscribeToEvents(Window window)
        {
            rootWindow = window;
            HandPointGenerator.Default.PointCreated += new EventHandler<HandPointEventArgs>(HandPointGenerator_PointCreated);
            HandPointGenerator.Default.PointUpdated += new EventHandler<HandPointEventArgs>(HandPointGenerator_PointUpdated);
            HandPointGenerator.Default.PointDestroyed += new EventHandler<HandPointEventArgs>(HandPointGenerator_PointDestroyed);

            HandPointGenerator.Default.StartGenerating(window.Dispatcher);
            window.Closing += (s, e) =>
            {
                HandPointGenerator.Default.StopGenerating();
            };
        }

        #endregion

        #region Private Static Methods

        static void HandPointGenerator_PointCreated(object sender, HandPointEventArgs e)
        {
            if (!rootWindow.CheckAccess())
            {
                rootWindow.Dispatcher.Invoke(new EventHandler<HandPointEventArgs>(HandPointGenerator_PointCreated), sender, e);
                return;
            }

            MotionTrackingDevice device = null;
            if (!deviceDictionary.Keys.Contains(e.Id))
            {
                device = new MotionTrackingDevice(rootWindow);
                deviceDictionary.Add(e.Id, device);
            }

            if (device != null)
            {
                device.ReportMotionTrackingStarted(e);
            }
        }

        static void HandPointGenerator_PointUpdated(object sender, HandPointEventArgs e)
        {
            if (!rootWindow.CheckAccess())
            {
                rootWindow.Dispatcher.Invoke(new EventHandler<HandPointEventArgs>(HandPointGenerator_PointUpdated), sender, e);
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

        static void HandPointGenerator_PointDestroyed(object sender, HandPointEventArgs e)
        {
            if (!rootWindow.CheckAccess())
            {
                rootWindow.Dispatcher.Invoke(new EventHandler<HandPointEventArgs>(HandPointGenerator_PointDestroyed), sender, e);
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

        #endregion
    }
}
