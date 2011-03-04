using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blake.NUI.WPF;
using System.Windows.Input;
using InfoStrat.MotionFx;
using System.Windows.Media.Media3D;
using System.Windows;

namespace InfoStrat.MotionFx.Filters
{
    public class PushMotionInputFilter : InputFilter
    {
        #region MinimumDistance

        /// <summary>
        /// The <see cref="MinimumDistance" /> dependency property's name.
        /// </summary>
        public const string MinimumDistancePropertyName = "MinimumDistance";

        /// <summary>
        /// Gets or sets the value of the <see cref="MinimumDistance" />
        /// property. This is a dependency property. If the distance 
        /// between the 3-D hand position to the shoulder position, 
        /// measured in millimeters, is greater than MinimumDistance, then the MotionTouchDevice
        /// will be valid.
        /// If the TouchDevice is not a MotionTouchDevice, the filter will be valid.
        /// </summary>
        public double MinimumDistance
        {
            get
            {
                return (double)GetValue(MinimumDistanceProperty);
            }
            set
            {
                SetValue(MinimumDistanceProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="MinimumDistance" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumDistanceProperty = DependencyProperty.Register(
            MinimumDistancePropertyName,
            typeof(double),
            typeof(PushMotionInputFilter),
            new UIPropertyMetadata(450.0));

        #endregion
            
        protected override void RegisterEvents(UIElement element)
        {
            MotionTracking.AddMotionTrackingStartedHandler(element, ProcessEvent);
            MotionTracking.AddMotionTrackingUpdatedHandler(element, ProcessEvent);
            MotionTracking.AddMotionTrackingLostHandler(element, ProcessEvent);
        }

        protected override void UnregisterEvents(UIElement element)
        {
            MotionTracking.RemoveMotionTrackingStartedHandler(element, ProcessEvent);
            MotionTracking.RemoveMotionTrackingUpdatedHandler(element, ProcessEvent);
            MotionTracking.RemoveMotionTrackingLostHandler(element, ProcessEventDeactivation);
        }
        
        protected override bool IsDeviceValid(bool? wasValid, InputDevice device)
        {
            var motionDevice = device as MotionTrackingDevice;
            if (motionDevice == null)
                return NotifyTransition(wasValid, motionDevice, true);
            if (motionDevice.Session == null)
                return NotifyTransition(wasValid, motionDevice, false);

            Vector3D vector = motionDevice.Session.Position - motionDevice.Session.ShoulderPosition;

            if (Math.Abs(vector.Z) > MinimumDistance)
                return NotifyTransition(wasValid, motionDevice, true);

            return NotifyTransition(wasValid, motionDevice, false);
        }

        private bool NotifyTransition(bool? wasValid, MotionTrackingDevice device, bool isValid)
        {
            if (isValid)
                device.ShouldPromoteToTouch = true;

            if (wasValid.HasValue &&
                wasValid != isValid)
            {
                //this.Dispatcher.BeginInvoke((Action)delegate
                //    {
                //        HandPointGenerator.ResetHandSession(device.Session);
                //    },
                    //System.Windows.Threading.DispatcherPriority.Input);
                //HandPointGenerator.ResetHandSession(device.Session);
                
            }

            return isValid;
        }
    }
}
