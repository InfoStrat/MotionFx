using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blake.NUI.WPF;
using System.Windows;
using System.Windows.Input;

namespace InfoStrat.MotionFx.Filters
{
    public class HoverInputFilter : InputFilter
    {
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
            if (motionDevice != null)
                motionDevice.ShouldPromoteToTouch = true;
            return true;
        }
    }
}
