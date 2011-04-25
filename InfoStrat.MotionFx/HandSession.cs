    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Blake.NUI.WPF.Utility;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace InfoStrat.MotionFx
{
    public enum HandPose
    {
        Pointing,
        Fist,
        Palm,
        Unknown
    }

    public struct Point2D
    {
        public int X;
        public int Y;
        public Point2D(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!this.GetType().IsAssignableFrom(obj.GetType()))
            {
                throw new InvalidCastException();
            }
            Point2D other = (Point2D)obj;
            return this.X == other.X && this.Y == other.Y;
        }

        public static bool operator ==(Point2D left, Point2D right)
        {
            return left.X == right.X && left.Y == right.Y;
        }

        public static bool operator !=(Point2D left, Point2D right)
        {
            return !(left == right);
        }
    }

    public class PolarCoord
    {
        public int Angle { get; set; }

        public bool IsRadiusOverride { get; set; }
        public double Radius { get; set; }
        public Point2D Position { get; set; }
        public bool IsGap { get; set; }
        public bool IsClassified { get; set; }

        public PolarCoord()
        {
            IsGap = true;
            IsClassified = false;
            IsRadiusOverride = false;
        }

        public PolarCoord(Point2D position) : this()
        {
            this.Position = position;
        }

        public void CalculateCenter(Point2D center, Vector up)
        {
            Vector v = new Vector(Position.X - center.X, Position.Y - center.Y);

            this.Radius = v.Length;
            int angle = (int)MathUtility.Get2DAngle(up, v);
            this.Angle = (int)MathUtility.NormalizeAngle(angle);
        }

        public void UpdatePosition(Point2D center, Vector up)
        {
            int x = (int)(center.X + Math.Cos(this.Angle * Math.PI / 180.0) * this.Radius);
            int y = (int)(center.Y + Math.Sin(this.Angle * Math.PI / 180.0) * this.Radius);
            this.Position = new Point2D(x, y);
        }
    }
    
    public class PolarCoordCollection
    {
        #region Properties

        #region Points

        private List<PolarCoord> _pointsPrivate;
        protected IList<PolarCoord> PointsPrivate
        {
            get
            {
                if (_pointsPrivate == null)
                {
                    _pointsPrivate = new List<PolarCoord>();
                }
                return _pointsPrivate;
            }
        }

        private ReadOnlyCollection<PolarCoord> _pointsReadOnly;
        public ReadOnlyCollection<PolarCoord> Points
        {
            get
            {
                if (_pointsReadOnly == null)
                {
                    _pointsReadOnly = new ReadOnlyCollection<PolarCoord>(PointsPrivate);
                }
                return _pointsReadOnly;
            }
        }

        #endregion

        #region Center
        private Point2D _center;
        public Point2D Center
        {
            get
            {
                return _center;
            }
            private set
            {
                if (_center == value)
                    return;
                _center = value;
            }
        }

        #endregion

        #region Up

        private Vector _up = new Vector(0, -1);
        public Vector Up
        {
            get
            {
                return _up;
            }
            private set
            {
                if (_up == value)
                    return;
                _up = value;
            }
        }

        #endregion

        #region MinRadius

        public double MinRadius { get; set; }

        #endregion

        #region AngleHistogram

        private Dictionary<int, List<PolarCoord>> AngleHistogram = new Dictionary<int, List<PolarCoord>>();

        #endregion

        #endregion

        #region Constructors

        public PolarCoordCollection()
        {
            InitHistogram();
        }

        #endregion

        #region Private Methods
        
        private void InitHistogram()
        {
            AngleHistogram.Clear();
            for (int i = 0; i < 360; i++)
            {
                AngleHistogram.Add(i, new List<PolarCoord>());
            }
            foreach (var coord in this.Points)
            {
                AngleHistogram[coord.Angle].Add(coord);
            }
        }

        #endregion

        #region Public Methods
        public void AddPoint(PolarCoord coord)
        {
            coord.CalculateCenter(this.Center, this.Up);
            this.PointsPrivate.Add(coord);
            AngleHistogram[coord.Angle].Add(coord);

            if (coord.Radius < MinRadius)
                MinRadius = coord.Radius;
        }
        public void AddPoint(Point2D position)
        {
            var coord = new PolarCoord(position);
            AddPoint(coord);
        }

        public void RemovePoint(PolarCoord coord)
        {
            if (AngleHistogram[coord.Angle].Contains(coord))
            {
                AngleHistogram[coord.Angle].Remove(coord);
            }
            if (this.PointsPrivate.Contains(coord))
            {
                this.PointsPrivate.Remove(coord);
            }
        }

        public void Clear()
        {
            this.PointsPrivate.Clear();
        }

        public bool IsPointPresent(int x, int y)
        {
            foreach (var coord in Points)
            {
                if (coord.Position.X == x &&
                    coord.Position.Y == y)
                {
                    return true;
                }
            }
            return false;
        }

        public void UpdateCenter(Point2D center)
        {
            UpdateCenter(center, this.Up);
        }

        public void UpdateCenter(Point2D center, Vector up)
        {
            this.Center = center;
            this.Up = up;
            MinRadius = double.MaxValue;
            foreach (var coord in Points)
            {
                coord.CalculateCenter(this.Center, this.Up);

                if (coord.Radius < MinRadius)
                    MinRadius = coord.Radius;
            }
            InitHistogram();
        }

        public IEnumerable<PolarCoord> GetCoordsByAngle(int angle)
        {
            return AngleHistogram[angle];
        }

        #endregion
    }

    public class HandSession
    {
        #region Properties

        public int Id { get; set; }

        internal OpenNI.Point3D xnPosition { get; set; }

        public Point3D Position { get; set; }
        public Point3D ShoulderPosition { get; set; }
        public Point3D PositionProjective { get; set; }

        #region Pose
        
        private HandPose _pose = HandPose.Unknown;
        public HandPose Pose
        {
            get
            {
                return _pose;
            }
            set
            {
                if (_pose == value)
                    return;
                _pose = value;
                OnPoseChanged();
            }
        }
        
        #endregion
        
        #endregion

        #region Events

        public event EventHandler PoseChanged;

        protected void OnPoseChanged()
        {
            if (PoseChanged == null)
                return;
            PoseChanged(this, EventArgs.Empty);
        }

        #endregion

        #region Constructors

        public HandSession()
        {
            Pose = HandPose.Unknown;
        }

        #endregion

        #region Public Methods

        #endregion

        #region Private Methods

        #endregion
    }
}
