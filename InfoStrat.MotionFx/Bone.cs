using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace InfoStrat.MotionFx
{
    public class Bone : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(String info)
        {
            if (PropertyChanged == null)
                return;

            PropertyChanged(this, new PropertyChangedEventArgs(info));
        }

        #endregion

        #region Properties

        #region Scale

        /// <summary>
        /// The <see cref="Scale" /> property's name.
        /// </summary>
        public const string ScalePropertyName = "Scale";

        private Vector3D _scale = new Vector3D(1, 1, 1);

        /// <summary>
        /// Gets the Scale property.
        /// </summary>
        public Vector3D Scale
        {
            get
            {
                return _scale;
            }

            set
            {
                if (_scale == value)
                {
                    return;
                }

                var oldValue = _scale;
                _scale = value;
                UpdateChildren();
                // Update bindings, no broadcast
                RaisePropertyChanged(ScalePropertyName);
            }
        }

        #endregion

        #region Origin

        /// <summary>
        /// The <see cref="Origin" /> property's name.
        /// </summary>
        public const string OriginPropertyName = "Origin";

        private Vector3D _origin = new Vector3D();

        /// <summary>
        /// Gets the Origin property.
        /// </summary>
        public Vector3D Origin
        {
            get
            {
                return _origin;
            }

            set
            {
                if (_origin == value)
                {
                    return;
                }

                var oldValue = _origin;
                _origin = value;

                MotionFrame.RotationOrigin = _origin;
                RestFrame.RotationOrigin = _origin;
                RootFrame.RotationOrigin = _origin;

                UpdateChildren();

                RaisePropertyChanged(OriginPropertyName);
            }
        }

        #endregion

        #region EffectiveFrame

        public const string EffectiveFramePropertyName = "EffectiveFrame";

        public ReferenceFrame EffectiveFrame
        {
            get
            {
                var f = MotionFrame.Add(RestFrame).Add(RootFrame);
                f.RotationOrigin = Origin;
                f.Offset -= Origin;
                return f;
                //var m = MotionFrame.Matrix * RootFrame.Matrix * RestFrame.Matrix;
                //m.Translate(-Origin);
                //return m;
            }
        }

        #endregion

        #region RootFrame

        /// <summary>
        /// The <see cref="RootFrame" /> property's name.
        /// </summary>
        public const string RootFramePropertyName = "RootFrame";

        private ReferenceFrame _rootFrame = new ReferenceFrame();

        /// <summary>
        /// Gets the RootFrame property. RootFrame represents the frame of reference determined by the 
        /// parent's joint this bone is connected to.
        /// </summary>
        public ReferenceFrame RootFrame
        {
            get
            {
                return _rootFrame;
            }

            protected set
            {
                if (_rootFrame == value)
                {
                    return;
                }

                var oldValue = _rootFrame;
                _rootFrame = value;

                UpdateChildren();
                // Update bindings, no broadcast
                RaisePropertyChanged(RootFramePropertyName);
                RaisePropertyChanged(EffectiveFramePropertyName);
            }
        }

        #endregion

        #region RestFrame

        /// <summary>
        /// The <see cref="RestFrame" /> property's name.
        /// </summary>
        public const string RestFramePropertyName = "RestFrame";

        private ReferenceFrame _restFrame = new ReferenceFrame();

        /// <summary>
        /// Gets the RestFrame property. RestFrame is the frame of reference of the start of this bone
        /// at rest as compared to the parent bone's frame of reference
        /// </summary>
        public ReferenceFrame RestFrame
        {
            get
            {
                return _restFrame;
            }

            set
            {
                if (_restFrame == value)
                {
                    return;
                }

                var oldValue = _restFrame;
                _restFrame = value;
                UpdateChildren();
                // Update bindings, no broadcast
                RaisePropertyChanged(RestFramePropertyName);
            }
        }

        #endregion

        #region MotionFrame

        /// <summary>
        /// The <see cref="MotionFrame" /> property's name.
        /// </summary>
        public const string MotionFramePropertyName = "MotionFrame";

        private ReferenceFrame _motionFrame = new ReferenceFrame();

        /// <summary>
        /// Gets the MotionFrame property. MotionFrame is the frame of reference that includes 
        /// the movement of the root joint as compared to the RestFrame.
        /// </summary>
        public ReferenceFrame MotionFrame
        {
            get
            {
                return _motionFrame;
            }

            set
            {
                if (_motionFrame == value)
                {
                    return;
                }

                var oldValue = _motionFrame;
                _motionFrame = value;
                UpdateChildren();
                // Update bindings, no broadcast
                RaisePropertyChanged(MotionFramePropertyName);
            }
        }

        #endregion

        #region ChildJoints

        /// <summary>
        /// The <see cref="ChildJoints" /> property's name.
        /// </summary>
        public const string ChildJointsPropertyName = "ChildJoints";

        private Dictionary<int, ReferenceFrame> _childJoints = new Dictionary<int, ReferenceFrame>();

        /// <summary>
        /// Gets the ChildJoints property.
        /// </summary>
        public IDictionary<int, ReferenceFrame> ChildJoints
        {
            get
            {
                return _childJoints;
            }
        }

        #endregion

        #region ParentJointId

        /// <summary>
        /// The <see cref="ParentJointId" /> property's name.
        /// </summary>
        public const string ParentJointIdPropertyName = "ParentJointId";

        private int _parentJointId = -1;

        /// <summary>
        /// Gets the ParentJointId property. The Id of the parent bone's joint that this bone is 
        /// attached to. -1 if no parent.
        /// </summary>
        public int ParentJointId
        {
            get
            {
                return _parentJointId;
            }

            set
            {
                if (_parentJointId == value)
                {
                    return;
                }

                var oldValue = _parentJointId;
                _parentJointId = value;

                // Update bindings, no broadcast
                RaisePropertyChanged(ParentJointIdPropertyName);
            }
        }

        #endregion

        #region Children

        /// <summary>
        /// The <see cref="Children" /> property's name.
        /// </summary>
        public const string ChildrenPropertyName = "Children";

        private ObservableCollection<Bone> _children;

        /// <summary>
        /// Gets the Children property.
        /// </summary>
        public IList<Bone> Children
        {
            get
            {
                if (_children == null)
                {
                    _children = new ObservableCollection<Bone>();
                    _children.CollectionChanged += Children_CollectionChanged;
                }
                return _children;
            }
        }

        #endregion

        #endregion

        #region Private Methods

        void Children_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateChildren();
        }

        private void UpdateChildren()
        {
            foreach (var bone in Children)
            {
                int id = bone.ParentJointId;
                if (!ChildJoints.ContainsKey(id))
                {
                    continue;
                }

                var jointFrame = ChildJoints[id];
                bone.RootFrame = this.EffectiveFrame.Add(jointFrame);
            }
        }

        #endregion

        #region Public Methods

        public void Update()
        {
            UpdateChildren();
            RaisePropertyChanged(EffectiveFramePropertyName);
        }

        public double? GetDepthValue(double x, double y)
        {
            var matrixInvert = this.EffectiveFrame.Matrix;
            if (!matrixInvert.HasInverse)
                throw new InvalidOperationException("Matrix has no inverse.");

            matrixInvert.Invert();
            for (double depth = 3; depth > -3; depth -= 0.05)
            {
                Point3D position = new Point3D(x, y, depth);
                var localPosition = matrixInvert.Transform(position);

                if (localPosition.X > 0 &&
                    localPosition.Y > 0 &&
                    localPosition.X < Scale.X &&
                    localPosition.Y < Scale.Y &&
                    localPosition.Z > 0 &&
                    localPosition.Z < Scale.Z)
                {
                    return depth;
                }
            }
            return null;            
        }

        public IEnumerable<Point3D> GetDepthMap(double resolutionX, double resolutionY)
        {
            List<Point3D> ret = new List<Point3D>();

            //int numX = 10;
            //int numY = 10;

            double xInterval = Scale.X / 20.0;
            double yInterval = Scale.Y / 20.0;
            var matrix = this.EffectiveFrame.Matrix;
            for (double i = 0; i < Scale.X; i += xInterval)
            {
                for (double j = 0; j < Scale.Y; j += yInterval)
                {
                    Point3D v = new Point3D(i, j, this.Origin.Z);
                    var v2 = matrix.Transform(v);
                    ret.Add(v2);
                }
            }

            return ret;
        }

        #endregion

    }
}
