using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;
using Blake.NUI.WPF.Utility;

namespace InfoStrat.MotionFx
{
    public class MotionTrackingScreen
    {
        public double ScreenWidth { get; set; }
        public double ScreenHeight { get; set; }
        
        public virtual bool IsSessionInBounds(HandSession session)
        {
            return true;
        }

        public Point MapPositionToScreen(HandSession session)
        {
            return MapPositionToScreen(session, this.ScreenWidth, this.ScreenHeight);
        }
        
        /// <summary>
        /// Maps the 3D hand session position to the current screen.
        /// </summary>
        /// <param name="session">The current hand session</param>
        /// <param name="targetWidth">The target mapping width</param>
        /// <param name="targetHeight">The target mapping height</param>
        /// <returns>2D Point representing the hand position mapped to the screen</returns>
        public virtual Point MapPositionToScreen(HandSession session, double targetWidth, double targetHeight)
        {
            Point3D position = session.PositionProjective;

            double x = MathUtility.MapValue(position.X, 10, 630, 0, targetWidth);
            double y = MathUtility.MapValue(position.Y, 30, 450, 0, targetHeight);
            
            return new Point(x, y);
        }
    }
}
