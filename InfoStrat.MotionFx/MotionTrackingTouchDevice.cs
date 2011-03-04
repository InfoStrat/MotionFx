using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows;
using Blake.NUI.WPF.Utility;
using System.Windows.Media.Media3D;
using System.Windows.Media;

namespace InfoStrat.MotionFx
{
    public class MotionTrackingTouchDevice : TouchDevice
    {   
        #region EventType enum

        private enum EventType
        {
            None,
            TouchDown,
            TouchMove,
            TouchMoveIntermediate,
            TouchUp
        }

        #endregion

        #region Class Fields

        private HandPointEventArgs lastEventArgs;
        private List<HandPointEventArgs> intermediateEvents = new List<HandPointEventArgs>();

        private Point lastEventPosition;
        private DateTime lastEventTime;
        private EventType lastEventType = EventType.None;

        private double movementThreshold = 1;
        private double timeThreshold = 5;

        private IInputElement directlyOver;

        private Window rootWindow = null;

        private InputManager inputManager = InputManager.Current;

        #endregion

        #region Properties

        public HandSession Session { get; private set; }

        #endregion

        #region Constructors

        public MotionTrackingTouchDevice(HandPointEventArgs e, Window window) :
            base(e.Id)
        {
            lastEventArgs = e;

            if (window == null)
                throw new ArgumentNullException("window");

            this.rootWindow = window;
        }

        #endregion

        #region Overridden methods

        public override TouchPointCollection GetIntermediateTouchPoints(IInputElement relativeTo)
        {
            TouchPointCollection collection = new TouchPointCollection();
            UIElement element = relativeTo as UIElement;

            if (element == null)
                return collection;

            foreach (HandPointEventArgs e in intermediateEvents)
            {
                Point point = MapPositionToScreen(e.Session);
                if (relativeTo != null)
                {
                    point = this.ActiveSource.RootVisual.TransformToDescendant((Visual)relativeTo).Transform(point);
                }

                //Rect rect = e.BoundingRect;
                Rect rect = new Rect(MapPositionToScreen(e.Session),
                                 new Size(1, 1));

                TouchAction action = TouchAction.Move;
                if (lastEventArgs.Status == HandPointStatus.Down)
                {
                    action = TouchAction.Down;
                }
                else if (lastEventArgs.Status == HandPointStatus.Up)
                {
                    action = TouchAction.Up;
                }
                collection.Add(new TouchPoint(this, point, rect, action));
            }
            return collection;
        }

        public override TouchPoint GetTouchPoint(IInputElement relativeTo)
        {
            Point point = new Point();
            Rect rect = new Rect(point, new Size(1, 1));

            if (lastEventArgs.Session != null)
            {
                point = MapPositionToScreen(lastEventArgs.Session);
                if (relativeTo != null)
                {
                    point = this.ActiveSource.RootVisual.TransformToDescendant((Visual)relativeTo).Transform(point);
                }

                rect = new Rect(MapPositionToScreen(lastEventArgs.Session),
                                     new Size(1, 1));
            }
            TouchAction action = TouchAction.Move;
            if (lastEventArgs.Status == HandPointStatus.Down)
            {
                action = TouchAction.Down;
            }
            else if (lastEventArgs.Status == HandPointStatus.Up)
            {
                action = TouchAction.Up;
            }
            return new TouchPoint(this, point, rect, action);
        }

        #endregion

        #region Private Methods
        
        private Point MapPositionToScreen(HandSession session)
        {
            Point3D position = session.PositionProjective;

            double x = MathUtility.MapValue(position.X, 0, 640, 0, this.rootWindow.ActualWidth);
            double y = MathUtility.MapValue(position.Y, 0, 480, 0, this.rootWindow.ActualHeight);
            return new Point(x, y);
        }

        internal void TouchDown(HandPointEventArgs e)
        {
            if (lastEventType != EventType.TouchMoveIntermediate)
            {
                intermediateEvents.Clear();
            }

            this.lastEventArgs = e;
            lastEventPosition = MapPositionToScreen(e.Session);
            lastEventTime = DateTime.Now;
            lastEventType = EventType.TouchDown;
            this.Session = e.Session;
            var source = PresentationSource.FromVisual(this.rootWindow);
            this.SetActiveSource(source);
            this.Activate();
            this.ReportDown();
        }

        internal void TouchMove(HandPointEventArgs e)
        {
            if (!this.IsActive)
            {
                return;
            }
            this.Session = e.Session;
            CoalesceEvents(e);
        }

        private void CoalesceEvents(HandPointEventArgs e)
        {
            TimeSpan span = DateTime.Now - lastEventTime;
            Vector delta = MapPositionToScreen(e.Session) - lastEventPosition;

            if (lastEventType != EventType.TouchMoveIntermediate)
            {
                intermediateEvents.Clear();
            }

            if (span.TotalMilliseconds < timeThreshold ||
                Math.Ceiling(delta.Length) < movementThreshold)
            {
                intermediateEvents.Add(e);
                lastEventType = EventType.TouchMoveIntermediate;
                //Debug.WriteLine("Event NO: " + touch.TouchDevice.Id + " " + point.Position.ToString() + " " + touch.Timestamp.ToString());
            }
            else
            {
                //Debug.WriteLine("Event go: " + touch.TouchDevice.Id + " " + point.Position.ToString() + " " + touch.Timestamp.ToString());

                lastEventPosition = MapPositionToScreen(e.Session);
                lastEventTime = DateTime.Now;
                lastEventType = EventType.TouchMove;

                this.lastEventArgs = e;
                this.ReportMove();
                    
            }
        }

        internal void TouchUp(HandPointEventArgs e)
        {
            if (lastEventType != EventType.TouchMoveIntermediate)
            {
                intermediateEvents.Clear();
            }

            if (!this.IsActive)
            {
                return;
            }

            this.lastEventArgs = e;
            this.Session = null;
            this.ReportUp();
            this.Deactivate();
            lastEventType = EventType.TouchUp;
        }

        #endregion
    }
}
