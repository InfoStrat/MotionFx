using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using DirectCanvas;
using InfoStrat.MotionFx.ImageProcessing.Effects;
using Microsoft.Surface.Presentation.Controls;
using DirectCanvas.Brushes;
using DirectCanvas.Misc;
using Blake.NUI.WPF.Utility;

namespace InfoStrat.MotionFx.Controls
{
    public class DepthFrameViewer : DrawingLayerViewer
    {
        #region Fields

        DrawingLayer effectLayer;
        UnpackDepthEffect unpackEffect;
        ColorMapDepthEffect colorMapEffect;
        Brush rectBrushForeground;
        Brush rectBrushBackground;

        bool isCropping = false;
        System.Windows.Point cropPoint1 = new System.Windows.Point();
        System.Windows.Point cropPoint2 = new System.Windows.Point();

        #endregion

        #region Properties

        #region LoadThresholdFromDepthFrame DP

        /// <summary>
        /// The <see cref="LoadThresholdFromDepthFrame" /> dependency property's name.
        /// </summary>
        public const string LoadThresholdFromDepthFramePropertyName = "LoadThresholdFromDepthFrame";

        /// <summary>
        /// Gets or sets the value of the <see cref="LoadThresholdFromDepthFrame" />
        /// property. This is a dependency property.
        /// </summary>
        public bool LoadThresholdFromDepthFrame
        {
            get
            {
                return (bool)GetValue(LoadThresholdFromDepthFrameProperty);
            }
            set
            {
                SetValue(LoadThresholdFromDepthFrameProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="LoadThresholdFromDepthFrame" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty LoadThresholdFromDepthFrameProperty = DependencyProperty.Register(
            LoadThresholdFromDepthFramePropertyName,
            typeof(bool),
            typeof(DepthFrameViewer),
            new UIPropertyMetadata(true));

        #endregion

        #region DepthFrame DP

        /// <summary>
        /// The <see cref="DepthFrame" /> dependency property's name.
        /// </summary>
        public const string DepthFramePropertyName = "DepthFrame";

        /// <summary>
        /// Gets or sets the value of the <see cref="DepthFrame" />
        /// property. This is a dependency property.
        /// </summary>
        public DepthFrame DepthFrame
        {
            get
            {
                return (DepthFrame)GetValue(DepthFrameProperty);
            }
            set
            {
                SetValue(DepthFrameProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="DepthFrame" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty DepthFrameProperty = DependencyProperty.Register(
            DepthFramePropertyName,
            typeof(DepthFrame),
            typeof(DepthFrameViewer),
            new UIPropertyMetadata(null, new PropertyChangedCallback(OnDepthFramePropertyChanged)));

        
        protected static void OnDepthFramePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            DepthFrameViewer viewer = obj as DepthFrameViewer;
            if (viewer == null)
                return;

            viewer.ProcessNewDepthFrame();
        }

        #endregion

        #region MinThreshold DP

        /// <summary>
        /// The <see cref="MinThreshold" /> dependency property's name.
        /// </summary>
        public const string MinThresholdPropertyName = "MinThreshold";

        /// <summary>
        /// Gets or sets the value of the <see cref="MinThreshold" />
        /// property. This is a dependency property.
        /// </summary>
        public double MinThreshold
        {
            get
            {
                return (double)GetValue(MinThresholdProperty);
            }
            set
            {
                SetValue(MinThresholdProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="MinThreshold" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinThresholdProperty = DependencyProperty.Register(
            MinThresholdPropertyName,
            typeof(double),
            typeof(DepthFrameViewer),
            new UIPropertyMetadata(100.0, new PropertyChangedCallback(UpdateImageOnPropertyChanged)));

        #endregion

        #region MaxThreshold DP

        /// <summary>
        /// The <see cref="MaxThreshold" /> dependency property's name.
        /// </summary>
        public const string MaxThresholdPropertyName = "MaxThreshold";

        /// <summary>
        /// Gets or sets the value of the <see cref="MaxThreshold" />
        /// property. This is a dependency property.
        /// </summary>
        public double MaxThreshold
        {
            get
            {
                return (double)GetValue(MaxThresholdProperty);
            }
            set
            {
                SetValue(MaxThresholdProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="MaxThreshold" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaxThresholdProperty = DependencyProperty.Register(
            MaxThresholdPropertyName,
            typeof(double),
            typeof(DepthFrameViewer),
            new UIPropertyMetadata(10000.0, new PropertyChangedCallback(UpdateImageOnPropertyChanged)));

        #endregion

        #region UIVisible

        /// <summary>
        /// The <see cref="ControlsVisibility" /> dependency property's name.
        /// </summary>
        public const string ControlsVisibilityPropertyName = "ControlsVisibility";

        /// <summary>
        /// Gets or sets the value of the <see cref="ControlsVisibility" />
        /// property. This is a dependency property.
        /// </summary>
        public Visibility ControlsVisibility
        {
            get
            {
                return (Visibility)GetValue(ControlsVisibilityProperty);
            }
            set
            {
                SetValue(ControlsVisibilityProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="ControlsVisibility" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty ControlsVisibilityProperty = DependencyProperty.Register(
            ControlsVisibilityPropertyName,
            typeof(Visibility),
            typeof(DepthFrameViewer),
            new UIPropertyMetadata(Visibility.Visible));

        #endregion
        
        #endregion

        #region Constructors

        static DepthFrameViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DepthFrameViewer), new FrameworkPropertyMetadata(typeof(DepthFrameViewer)));
        }

        public DepthFrameViewer()
        {
            
        }

        #endregion
        
        #region Overridden Methods

        public override void OnApplyTemplate()
        {
            this.Image = this.GetTemplateChild(Image_Name) as Image;
            if (this.Image != null)
            {
                this.Image.Width = 640;
                this.Image.Height = 480;
                this.Image.MouseDown += new System.Windows.Input.MouseButtonEventHandler(Image_MouseDown);
                this.Image.MouseMove += new System.Windows.Input.MouseEventHandler(Image_MouseMove);
                this.Image.MouseUp += new System.Windows.Input.MouseButtonEventHandler(Image_MouseUp);
            }
        }

        void Image_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (isCropping)
            {
                isCropping = false;
                this.Image.ReleaseMouseCapture();

                var p1 = e.GetPosition(this.Image);
                cropPoint2.X = MathUtility.MapValue(p1.X, 0, this.Image.ActualWidth, 0, this.DrawingLayerSize.Width);
                cropPoint2.Y = MathUtility.MapValue(p1.Y, 0, this.Image.ActualHeight, 0, this.DrawingLayerSize.Height);

                UpdateCrop();
            }   
        }

        void Image_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.RightButton == System.Windows.Input.MouseButtonState.Pressed && isCropping)
            {
                var p1 = e.GetPosition(this.Image);
                cropPoint2.X = MathUtility.MapValue(p1.X, 0, this.Image.ActualWidth, 0, this.DrawingLayerSize.Width);
                cropPoint2.Y = MathUtility.MapValue(p1.Y, 0, this.Image.ActualHeight, 0, this.DrawingLayerSize.Height);

                UpdateCrop();                
            }
        }

        void Image_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.RightButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                isCropping = true;
                this.Image.CaptureMouse();
                var p1 = e.GetPosition(this.Image);
                var p2 = e.GetPosition(this.Image);
                cropPoint1.X = MathUtility.MapValue(p1.X, 0, this.Image.ActualWidth, 0, this.DrawingLayerSize.Width);
                cropPoint1.Y = MathUtility.MapValue(p1.Y, 0, this.Image.ActualHeight, 0, this.DrawingLayerSize.Height);
                
                cropPoint2.X = MathUtility.MapValue(p2.X, 0, this.Image.ActualWidth, 0, this.DrawingLayerSize.Width);
                cropPoint2.Y = MathUtility.MapValue(p2.Y, 0, this.Image.ActualHeight, 0, this.DrawingLayerSize.Height);

                UpdateCrop();
            }
        }

        private void UpdateCrop()
        {
            if (DepthFrame == null)
            {
                return;
            }
            DepthFrame.Crop = new Rect(cropPoint1, cropPoint2);
            this.UpdateImage();
        }

        protected override bool VerifyInitOverride()
        {
            if (DepthFrame == null)
            {
                return false;
            }

            if (effectLayer == null)
            {
                effectLayer = Factory.CreateDrawingLayer(DepthFrame.Width, DepthFrame.Height);
            }
            if (unpackEffect == null)
            {
                unpackEffect = new UnpackDepthEffect(Factory);
            }
            if (colorMapEffect == null)
            {
                colorMapEffect = new ColorMapDepthEffect(Factory);
            }
            if (rectBrushForeground == null)
            {
                rectBrushForeground = Factory.CreateSolidColorBrush(new Color4(1, 1, 1, 1));
            }
            if (rectBrushBackground == null)
            {
                rectBrushBackground = Factory.CreateSolidColorBrush(new Color4(1, 0, 0, 0));
            }

            return base.VerifyInitOverride();
        }

        protected override void DrawLayer(DrawingLayer outputLayer)
        {
            base.DrawLayer(outputLayer);
            
            DepthFrame.MinThreshold = (ushort)this.MinThreshold;
            DepthFrame.MaxThreshold = (ushort)this.MaxThreshold;

            ushort minValue;
            ushort maxValue;
            var img = DepthFrame.ToDirectCanvasImage(Factory, out minValue, out maxValue);

            effectLayer.CopyFromImage(img);
            Rect crop = DepthFrame.Crop;
            Rectangle rect = new Rectangle((int)crop.X, (int)crop.Y, (int)crop.Width, (int)crop.Height);
            RectangleF rectf = new RectangleF((float)crop.X, (float)crop.Y, (float)crop.Width, (float)crop.Height);

            unpackEffect.MinThreshold = (float)MinThreshold;
            unpackEffect.MaxThreshold = (float)MaxThreshold;
            unpackEffect.MinValue = minValue;
            unpackEffect.MaxValue = maxValue;
            unpackEffect.TexSize = new DirectCanvas.Misc.Size(effectLayer.Width, effectLayer.Height);
            effectLayer.ApplyEffect(unpackEffect, outputLayer, true);   
         
            outputLayer.BeginDraw();
            outputLayer.DrawRectangle(rectBrushBackground, rectf, 3f);
            outputLayer.DrawRectangle(rectBrushForeground, rectf, 1f);
            outputLayer.EndDraw();
        }

        #endregion

        #region Private Methods
        
        private void ProcessNewDepthFrame()
        {
            if (DepthFrame != null)
            {
                DrawingLayerSize = new System.Windows.Size(DepthFrame.Width, DepthFrame.Height);
                Image.Width = DepthFrame.Width;
                Image.Height = DepthFrame.Height;
                if (LoadThresholdFromDepthFrame)
                {
                    MinThreshold = DepthFrame.MinThreshold;
                    MaxThreshold = DepthFrame.MaxThreshold;
                }
                else
                {
                    DepthFrame.MinThreshold = (ushort)MinThreshold;
                    DepthFrame.MaxDepth = (ushort)MaxThreshold;
                }
            }
            UpdateImage();
        }       

        #endregion
    }
}
