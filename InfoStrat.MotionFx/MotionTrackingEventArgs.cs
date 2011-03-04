using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace InfoStrat.MotionFx
{
    public class MotionTrackingEventArgs : InputEventArgs
    {
        #region Properties

        public MotionTrackingDevice MotionTrackingDevice
        {
            get
            {
                return this.Device as MotionTrackingDevice;
            }
        }

        #endregion
        #region Constructors

        public MotionTrackingEventArgs(MotionTrackingDevice device, int timestamp) : base(device, timestamp)
        {
        }

        #endregion

        #region Overridden Methods

        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            MotionTrackingEventHandler handler = (MotionTrackingEventHandler)genericHandler;
            handler(genericTarget, this);
        }

        #endregion
    }
}
