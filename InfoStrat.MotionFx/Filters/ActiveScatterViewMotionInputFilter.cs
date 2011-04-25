using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blake.NUI.WPF;
using System.Windows.Input;
using InfoStrat.MotionFx;
using System.Windows.Media.Media3D;
using System.Windows;
using Blake.NUI.WPF.Utility;
using Microsoft.Surface.Presentation.Controls;

namespace InfoStrat.MotionFx.Filters
{
    public class ActiveScatterViewMotionInputFilter : InputFilter
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
            new UIPropertyMetadata(400.0));

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

            var motionDevice = args.Device as MotionTrackingDevice;
            if (motionDevice == null)
                return true;
            if (motionDevice.Session == null)
                return false;

            var element = args.OriginalSource as UIElement;

            if (element == null)
                return false;

            var svi = VisualUtility.FindVisualParent<ScatterViewItem>(element);

            if (svi == null || !svi.IsContainerActive)
                return false;

            motionDevice.ShouldPromoteToTouch = true;
            
            return true;
        }
    }
}
