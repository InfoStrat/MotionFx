using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using DirectCanvas;
using InfoStrat.MotionFx.ImageProcessing.Effects;

namespace InfoStrat.MotionFx.Controls
{
    public class DrawingLayerViewer : Control
    {
        #region Control Parts

        #region Image

        protected static string Image_Name = "PART_Image";

        private Image _image;
        protected Image Image
        {
            get
            {
                return _image;
            }
            set
            {
                _image = value;
            }
        }

        #endregion

        #endregion

        #region Fields
        
        WPFPresenter presenter;

        #endregion

        #region Properties

        #region Factory DP

        /// <summary>
        /// The <see cref="Factory" /> dependency property's name.
        /// </summary>
        public const string FactoryPropertyName = "Factory";

        /// <summary>
        /// Gets or sets the value of the <see cref="Factory" />
        /// property. This is a dependency property.
        /// </summary>
        public DirectCanvasFactory Factory
        {
            get
            {
                return (DirectCanvasFactory)GetValue(FactoryProperty);
            }
            set
            {
                SetValue(FactoryProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="Factory" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty FactoryProperty = DependencyProperty.Register(
            FactoryPropertyName,
            typeof(DirectCanvasFactory),
            typeof(DrawingLayerViewer),
            new UIPropertyMetadata(null, new PropertyChangedCallback(UpdateImageOnPropertyChanged)));

        #endregion

        #region DrawingLayerSize

        private Size _drawingLayerSize = new Size(640, 480);
        public Size DrawingLayerSize
        {
            get
            {
                return _drawingLayerSize;
            }
            protected set
            {
                if (_drawingLayerSize == value)
                    return;

                _drawingLayerSize = value;

                presenter = null;
                UpdateImage();
            }
        }

        #endregion

        #endregion

        #region Constructors

        static DrawingLayerViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DrawingLayerViewer), new FrameworkPropertyMetadata(typeof(DrawingLayerViewer)));
        }

        public DrawingLayerViewer()
        {
        }


        #endregion
        
        #region Overridden Methods

        public override void OnApplyTemplate()
        {
            this.Image = this.GetTemplateChild(Image_Name) as Image;
        }

        #endregion

        #region Private Methods

        protected static void UpdateImageOnPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            DrawingLayerViewer viewer = obj as DrawingLayerViewer;
            if (viewer == null)
                return;

            viewer.Dispatcher.BeginInvoke((Action)delegate
                {
                    viewer.UpdateImage();
                }, 
                System.Windows.Threading.DispatcherPriority.Render);
            
        }
        
        private bool VerifyInit()
        {
            if (Factory == null || this.Image == null)
            {
                return false;
            }
            if (_drawingLayerSize.Width == 0 || _drawingLayerSize.Height == 0)
                return false;

            if (presenter == null)
            {
                presenter = new WPFPresenter(Factory, (int)DrawingLayerSize.Width, (int)DrawingLayerSize.Height);
            }

            return VerifyInitOverride();
        }

        protected void UpdateImage()
        {
            if (!this.IsLoaded)
            {
                this.Loaded += (s, e) => { UpdateImage(); };
                return;
            }
            if (!VerifyInit())
            {
                if (this.Image != null)
                    Image.Source = null;
                
                return;
            }

            DrawLayer(presenter);

            presenter.Present();
            Image.Source = presenter.ImageSource;
        }


        #endregion

        #region Virtual Methods
        
        /// <summary>
        /// Override to add initialization code that is checked before each render
        /// </summary>
        /// <returns>True if rendering prerequisites are met, false otherwise</returns>
        protected virtual bool VerifyInitOverride()
        {
            return true;
        }

        /// <summary>
        /// Override to draw visuals to the drawing layer
        /// </summary>
        /// <param name="drawingLayer"></param>
        protected virtual void DrawLayer(DrawingLayer outputLayer)
        {

        }

        #endregion
    }
}
