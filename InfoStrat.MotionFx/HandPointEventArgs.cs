using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows;
using System.Windows.Media.Media3D;

namespace InfoStrat.MotionFx
{
    public enum HandPointStatus
    {
        InRange,
        HoverMove,
        OutOfRange,
        Down,
        Move,
        Up,
    }
    
    public class UserEventArgs : EventArgs
    {
        public int Id { get; private set; }

        public UserEventArgs(int id)
        {
            this.Id = id;
        }
    }

    public class HandPointEventArgs : EventArgs
    {
        public int Id { get; private set; }
        public HandPointStatus Status { get; private set; }
        public HandSession Session { get; private set; }
        public Point3D Position 
        { 
            get
            {
                if (Session == null)
                    return new Point3D();
                return Session.Position;
            }
        }
        public HandPointEventArgs(int id, HandPointStatus status, HandSession session)
        {
            this.Id = id;
            this.Status = status;
            this.Session = session;
        }
    }
}
