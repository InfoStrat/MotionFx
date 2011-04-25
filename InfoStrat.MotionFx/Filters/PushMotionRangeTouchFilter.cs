using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using InfoStrat.MotionFx;
using System.Windows.Media.Media3D;
using System.Windows;
using Blake.NUI.WPF;

namespace InfoStrat.MotionFx.Filters
{
    public class PushMotionRangeTouchFilter : InputFilter
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
            typeof(PushMotionRangeTouchFilter),
            new UIPropertyMetadata(350.0));

        #endregion

        #region MaximumDistance

        /// <summary>
        /// The <see cref="MaximumDistance" /> dependency property's name.
        /// </summary>
        public const string MaximumDistancePropertyName = "MaximumDistance";

        /// <summary>
        /// Gets or sets the value of the <see cref="MaximumDistance" />
        /// property. This is a dependency property. If the distance 
        /// between the 3-D hand position to the shoulder position, 
        /// measured in millimeters, is less than MaximumDistance, then the MotionTouchDevice
        /// will be valid.
        /// If the TouchDevice is not a MotionTouchDevice, the filter will be valid.
        /// </summary>
        public double MaximumDistance
        {
            get
            {
                return (double)GetValue(MaximumDistanceProperty);
            }
            set
            {
                SetValue(MaximumDistanceProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="MinimumDistance" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaximumDistanceProperty = DependencyProperty.Register(
            MaximumDistancePropertyName,
            typeof(double),
            typeof(PushMotionRangeTouchFilter),
            new UIPropertyMetadata(650.0));

        #endregion

        #region CurrentDistance

        /// <summary>
        /// The <see cref="CurrentDistance" /> dependency property's name.
        /// </summary>
        public const string CurrentDistancePropertyName = "CurrentDistance";


        public double CurrentDistance
        {
            get
            {
                return (double)GetValue(CurrentDistanceProperty);
            }
            set
            {
                SetValue(CurrentDistanceProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="MinimumDistance" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty CurrentDistanceProperty = DependencyProperty.Register(
            CurrentDistancePropertyName,
            typeof(double),
            typeof(PushMotionRangeTouchFilter),
            new UIPropertyMetadata(0.0));

        #endregion

        #region ValidTouchDevice

        /// <summary>
        /// The <see cref="ValidTouchDevice" /> dependency property's name.
        /// </summary>
        public const string ValidTouchDevicePropertyName = "ValidTouchDevice";


        public bool ValidTouchDevice
        {
            get
            {
                return (bool)GetValue(ValidTouchDeviceProperty);
            }
            set
            {
                SetValue(ValidTouchDeviceProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="MinimumDistance" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValidTouchDeviceProperty = DependencyProperty.Register(
            ValidTouchDevicePropertyName,
            typeof(bool),
            typeof(PushMotionRangeTouchFilter),
            new UIPropertyMetadata(false));

        #endregion

        protected override void RegisterEvents(UIElement element)
        {
            MotionTracking.AddMotionTrackingStartedHandler(element, ProcessEvent);
            MotionTracking.AddMotionTrackingUpdatedHandler(element, ProcessEvent);
            MotionTracking.AddMotionTrackingLostHandler(element, ProcessEventDeactivation);
        }

        protected override void UnregisterEvents(UIElement element)
        {
            MotionTracking.RemoveMotionTrackingStartedHandler(element, ProcessEvent);
            MotionTracking.RemoveMotionTrackingUpdatedHandler(element, ProcessEvent);
            MotionTracking.RemoveMotionTrackingLostHandler(element, ProcessEventDeactivation);
        }

        protected override bool IsDeviceValid(bool? wasValid, InputEventArgs args)
        {
            var motion = args.Device as MotionTrackingDevice;
            if (motion == null)
                return NotifyTransition(wasValid, motion, true);
            if (motion.Session == null)
                return NotifyTransition(wasValid, motion, false);

            Vector3D vector = motion.Session.Position - motion.Session.ShoulderPosition;

            CurrentDistance = vector.Length;

            if ((vector.Length > MinimumDistance) && (vector.Length < MaximumDistance))
            {
                ValidTouchDevice = true;
                return NotifyTransition(wasValid, motion, true);
            }

            ValidTouchDevice = false;

            return NotifyTransition(wasValid, motion, false);
        }

        private bool NotifyTransition(bool? wasValid, MotionTrackingDevice device, bool isValid)
        {
            if (wasValid.HasValue &&
                wasValid != isValid)
            {
            }

            return isValid;
        }
    }
}

