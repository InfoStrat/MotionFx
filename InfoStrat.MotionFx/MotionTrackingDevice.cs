using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows;
using System.Windows.Media;
using Blake.NUI.WPF.Utility;
using System.Windows.Media.Media3D;

namespace InfoStrat.MotionFx
{
    public class MotionTrackingDevice : InputDevice
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

        private IInputElement directlyOver = null;

        private Window rootWindow = null;

        private InputManager inputManager = InputManager.Current;
        private Point currentPosition;
        private bool currentPositionInBounds = false;
        private bool lastEventInBounds = false;
        private HandPointEventArgs lastHandPointEventArgs = null;
        private bool lastEventPromoted = false;
        private MotionTrackingTouchDevice promotionTouchDevice = null;

        private MotionTrackingScreen screen = null;

        #endregion

        #region Properties

        public int Id { get; private set; }
        public HandSession Session { get; private set; }
        public bool IsActive { get; private set; }

        public bool ShouldPromoteToTouch { get; set; }

        #endregion

        #region Constructors

        public MotionTrackingDevice(Window window, MotionTrackingScreen screen) :
            base()
        {
            if (window == null)
                throw new ArgumentNullException("window");
            if (screen == null)
                throw new ArgumentNullException("screen");

            this.screen = screen;
            this.rootWindow = window;
            _activeSource = PresentationSource.FromVisual(window);

            Activate();
        }

        #endregion

        #region Overridden methods

        private PresentationSource _activeSource = null;
        public override PresentationSource ActiveSource
        {
            get
            {
                return _activeSource;
            }
        }

        public override IInputElement Target
        {
            get
            {
                return directlyOver;
            }
        }

        #endregion

        #region Public Methods
        public Point GetPosition(IInputElement relativeTo)
        {
            if (relativeTo != null &&
                this.ActiveSource != null &&
                this.ActiveSource.RootVisual != null)
            {
                return this.ActiveSource.RootVisual.TransformToDescendant((Visual)relativeTo).Transform(currentPosition);
            }
            return new Point();
        }

        #endregion

        #region Internal Methods

        internal void Activate()
        {
            if (this.IsActive)
            {
                throw new InvalidOperationException("MotionTrackingDevice already activated");
            }

            this.IsActive = true;

            this.inputManager.PostProcessInput += new ProcessInputEventHandler(PostProcessInput);
            this.inputManager.HitTestInvalidatedAsync += new EventHandler(HitTestInvalidatedAsync);
        }

        internal void ReportMotionTrackingStarted(HandPointEventArgs e)
        {
            UpdateProperties(e);
            if (!currentPositionInBounds)
            {
                return;
            }
            ProcessMotionTrackingStarted();
        }

        internal void ReportMotionTrackingUpdated(HandPointEventArgs e)
        {
            UpdateProperties(e);
            if (!currentPositionInBounds)
            {
                if (lastEventInBounds)
                {
                    ProcessMotionTrackingLost();
                }
                return;
            }
            if (!lastEventInBounds)
            {
                ProcessMotionTrackingStarted();
            }
            ProcessMotionTrackingUpdated();
        }
        
        internal void ReportMotionTrackingLost(HandPointEventArgs e)
        {
            UpdateProperties(e);
            if (!lastEventInBounds)
            {
                return;
            }
            ProcessMotionTrackingLost();
        }

        #endregion

        #region Private Methods

        private void ProcessMotionTrackingStarted()
        {
            var args = CreateEventArgs(MotionTracking.MotionTrackingStartedEvent);
            inputManager.ProcessInput(args);
        }

        private void ProcessMotionTrackingUpdated()
        {
            var args = CreateEventArgs(MotionTracking.MotionTrackingUpdatedEvent);
            inputManager.ProcessInput(args);
        }

        private void ProcessMotionTrackingLost()
        {
            var args = CreateEventArgs(MotionTracking.MotionTrackingLostEvent);
            inputManager.ProcessInput(args);
        }
        
        void HitTestInvalidatedAsync(object sender, EventArgs e)
        {
            UpdateDirectlyOver();
        }

        void PostProcessInput(object sender, ProcessInputEventArgs e)
        {
            MotionTrackingEventArgs input = e.StagingItem.Input as MotionTrackingEventArgs;
            if (input == null || input.Device != this)
                return;

            if (!this.ShouldPromoteToTouch)
            {
                if (lastEventPromoted)
                {
                    PromoteToTouchUp();
                }
                lastEventPromoted = false;
            }
            else
            {
                if (input.RoutedEvent == MotionTracking.MotionTrackingLostEvent)
                {
                    PromoteToTouchUp();
                }
                else
                {
                    PromoteToTouchMove();
                }
                lastEventPromoted = true;
            }
        }

        private void PromoteToTouchDown()
        {
            if (promotionTouchDevice == null)
            {
                promotionTouchDevice = new MotionTrackingTouchDevice(lastHandPointEventArgs, rootWindow, screen);
            }
            promotionTouchDevice.TouchDown(lastHandPointEventArgs);
        }

        private void PromoteToTouchMove()
        {
            if (promotionTouchDevice == null)
            {
                PromoteToTouchDown();
                return;
            }

            promotionTouchDevice.TouchMove(lastHandPointEventArgs);
        }

        private void PromoteToTouchUp()
        {
            if (promotionTouchDevice == null)
            {
                PromoteToTouchDown();
            }
            promotionTouchDevice.TouchUp(lastHandPointEventArgs);
            promotionTouchDevice = null;
        }

        private void UpdateProperties(HandPointEventArgs e)
        {
            ShouldPromoteToTouch = false;
            this.lastHandPointEventArgs = e;
            this.Session = e.Session;
            this.Id = e.Id;
            lastEventInBounds = currentPositionInBounds;
            currentPositionInBounds = screen.IsSessionInBounds(e.Session);
            if (currentPositionInBounds)
            {
                currentPosition = screen.MapPositionToScreen(e.Session);
                UpdateDirectlyOver();
            }
        }

        private MotionTrackingEventArgs CreateEventArgs(RoutedEvent routedEvent)
        {
            MotionTrackingEventArgs args = new MotionTrackingEventArgs(this, Environment.TickCount);
            args.RoutedEvent = routedEvent;
            args.Source = this.directlyOver;
            return args;
        }

        private void UpdateDirectlyOver()
        {
            if (currentPositionInBounds)
            {
                CriticalHitTest(currentPosition);
            }
        }

        private void CriticalHitTest(Point position)
        {
            VisualTreeHelper.HitTest(this.rootWindow, null, MyHitTestResult, new PointHitTestParameters(position));
        }

        public HitTestResultBehavior MyHitTestResult(HitTestResult result)
        {
            var element = result.VisualHit as UIElement;
            if (element == null || element.IsHitTestVisible == false)
                return HitTestResultBehavior.Continue;

            this.directlyOver = element;
            return HitTestResultBehavior.Stop;
        }

        #endregion
    }
}
