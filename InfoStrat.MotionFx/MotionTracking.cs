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
            HandPointGenerator.Default.PointCreated += new EventHandler<HandPointEventArgs>(HandPointGenerator_PointCreated);
            HandPointGenerator.Default.PointUpdated += new EventHandler<HandPointEventArgs>(HandPointGenerator_PointUpdated);
            HandPointGenerator.Default.PointDestroyed += new EventHandler<HandPointEventArgs>(HandPointGenerator_PointDestroyed);
            HandPointGenerator.Default.FrameUpdated += new EventHandler<FrameUpdatedEventArgs>(HandPointGenerator_FrameUpdated);
            HandPointGenerator.Default.FirstFrameReady += new EventHandler(HandPointGenerator_FirstFrameReady);
        }

        #endregion

        #region Static Fields

        private static List<MotionTrackingClient> clients = new List<MotionTrackingClient>();

        #endregion

        #region Internal Static Methods

        internal static void RegisterEvents(MotionTrackingClient client)
        {
            if (client == null)
                throw new ArgumentNullException("client");

            bool isFirstClient = false;
            lock (clients)
            {
                if (clients.Count == 0)
                    isFirstClient = true;
                clients.Add(client);
            }

            if (isFirstClient)
            {
                HandPointGenerator.Default.StartGenerating();
            }

        }

        internal static void UnregisterEvents(MotionTrackingClient client)
        {
            bool isClientsEmpty = false;
            lock (clients)
            {
                if (clients.Contains(client))
                {
                    clients.Remove(client);
                }
                isClientsEmpty = clients.Count == 0;
            }
            if (isClientsEmpty)
            {
                HandPointGenerator.Default.StopGenerating();
            }
        }

        #endregion

        #region Private Static Methods

        private static void HandPointGenerator_FirstFrameReady(object sender, EventArgs e)
        {
            List<MotionTrackingClient> clientsCopy = new List<MotionTrackingClient>();
            lock (clients)
            {
                clientsCopy.AddRange(clients);
            }
            foreach (var client in clientsCopy)
            {
                client.ReportFirstFrameReady();
            }
        }

        private static void HandPointGenerator_FrameUpdated(object sender, FrameUpdatedEventArgs e)
        {
            List<MotionTrackingClient> clientsCopy = new List<MotionTrackingClient>();
            lock (clients)
            {
                clientsCopy.AddRange(clients);
            }
            foreach (var client in clientsCopy)
            {
                client.SourceFrameUpdated(e);
            }
        }

        private static void HandPointGenerator_PointCreated(object sender, HandPointEventArgs e)
        {
            List<MotionTrackingClient> clientsCopy = new List<MotionTrackingClient>();
            lock (clients)
            {
                clientsCopy.AddRange(clients);
            }
            foreach (var client in clientsCopy)
            {
                client.HandPointGenerator_PointCreated(sender, e);
            }
        }

        private static void HandPointGenerator_PointUpdated(object sender, HandPointEventArgs e)
        {
            List<MotionTrackingClient> clientsCopy = new List<MotionTrackingClient>();
            lock (clients)
            {
                clientsCopy.AddRange(clients);
            }
            foreach (var client in clientsCopy)
            {
                client.HandPointGenerator_PointUpdated(sender, e);
            }

        }

        private static void HandPointGenerator_PointDestroyed(object sender, HandPointEventArgs e)
        {
            List<MotionTrackingClient> clientsCopy = new List<MotionTrackingClient>();
            lock (clients)
            {
                clientsCopy.AddRange(clients);
            }
            foreach (var client in clientsCopy)
            {
                client.HandPointGenerator_PointDestroyed(sender, e);
            }
        }


        #endregion
    }
}
