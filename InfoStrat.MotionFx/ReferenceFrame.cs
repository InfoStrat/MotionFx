using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Media.Media3D;
using Blake.NUI.WPF.Utility;

namespace InfoStrat.MotionFx
{
    public class ReferenceFrame : INotifyPropertyChanged
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

        #region Fields
        
        #endregion

        #region Properties

        #region Matrix

        public const string MatrixPropertyName = "Matrix";

        private Matrix3D _matrix = Matrix3D.Identity;
        public Matrix3D Matrix
        {
            get
            {
                return _matrix;
            }
            private set
            {
                if (_matrix == value)
                    return;
                _matrix = value;
                RaisePropertyChanged(MatrixPropertyName);
            }
        }

        #endregion

        #region Offset

        /// <summary>
        /// The <see cref="Offset" /> property's name.
        /// </summary>
        public const string OffsetPropertyName = "Offset";

        private Vector3D _offset = new Vector3D();

        /// <summary>
        /// Gets the Offset property.
        /// </summary>
        public Vector3D Offset
        {
            get
            {
                return _offset;
            }

            set
            {
                if (_offset == value)
                {
                    return;
                }

                var oldValue = _offset;
                _offset = value;

                UpdateMatrix();
                // Update bindings, no broadcast
                RaisePropertyChanged(OffsetPropertyName);
            }
        }

        #endregion

        #region Rotation

        /// <summary>
        /// The <see cref="Rotation" /> property's name.
        /// </summary>
        public const string RotationPropertyName = "Rotation";

        private Vector3D _rotation = new Vector3D();

        /// <summary>
        /// Gets the Rotation property.
        /// </summary>
        public Vector3D Rotation
        {
            get
            {
                return _rotation;
            }

            set
            {
                if (_rotation == value)
                {
                    return;
                }
                
                var oldValue = _rotation;
                _rotation = value;

                _rotation.X = MathUtility.NormalizeAngle(_rotation.X);
                _rotation.Y = MathUtility.NormalizeAngle(_rotation.Y);
                _rotation.Z = MathUtility.NormalizeAngle(_rotation.Z);

                UpdateMatrix();
                // Update bindings, no broadcast
                RaisePropertyChanged(RotationPropertyName);
            }
        }

        #endregion

        #region RotationOrigin

        /// <summary>
        /// The <see cref="RotationOrigin" /> property's name.
        /// </summary>
        public const string RotationOriginPropertyName = "RotationOrigin";

        private Vector3D _rotationOrigin = new Vector3D();

        /// <summary>
        /// Gets the RotationOrigin property. RotationOrigin the center of rotation.
        /// </summary>
        public Vector3D RotationOrigin
        {
            get
            {
                return _rotationOrigin;
            }

            set
            {
                if (_rotationOrigin == value)
                {
                    return;
                }

                var oldValue = _rotationOrigin;
                _rotationOrigin = value;

                UpdateMatrix();

                // Update bindings, no broadcast
                RaisePropertyChanged(RotationOriginPropertyName);
            }
        }

        #endregion

        #endregion

        #region Private Methods

        private void UpdateMatrix()
        {
            var m = Matrix3D.Identity;
            var p = new Point3D(RotationOrigin.X, RotationOrigin.Y, RotationOrigin.Z);
            
            m.RotateAt(new Quaternion(new Vector3D(1, 0, 0), Rotation.X), p);
            m.RotateAt(new Quaternion(new Vector3D(0, 1, 0), Rotation.Y), p);
            m.RotateAt(new Quaternion(new Vector3D(0, 0, 1), Rotation.Z), p);
            
            m.Translate(Offset);
            Matrix = m;
        }

        #endregion

        #region Public Methods

        public ReferenceFrame Add(ReferenceFrame other)
        {
            ReferenceFrame frame = new ReferenceFrame();
            frame.Offset = this.Offset + other.Offset;
            frame.Rotation = this.Rotation + other.Rotation;

            return frame;
        }

        #endregion
    }
}
