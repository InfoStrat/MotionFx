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
            if (motionDevice != null)
                motionDevice.ShouldPromoteToTouch = true;
            return true;
        }
    }
}
