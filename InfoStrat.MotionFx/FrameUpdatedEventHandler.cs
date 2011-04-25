using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfoStrat.MotionFx
{
    public class FrameUpdatedEventArgs : EventArgs
    {
        public DepthFrame Frame { get; private set; }
        public IEnumerable<HandSession> HandSessions { get; private set; }

        public FrameUpdatedEventArgs(DepthFrame frame, List<HandSession> sessions)
        {
            this.Frame = frame;
            this.HandSessions = sessions;
        }

        public FrameUpdatedEventArgs(ushort[] depthPixels, int width, int height, IEnumerable<HandSession> sessions)
        {
            Frame = new DepthFrame(depthPixels, width, height);

            this.HandSessions = sessions;
        }
    }
}
