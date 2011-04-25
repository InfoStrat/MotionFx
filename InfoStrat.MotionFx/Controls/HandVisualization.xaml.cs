using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Blake.NUI.WPF.Utility;

namespace InfoStrat.MotionFx.Controls
{
    /// <summary>
    /// Interaction logic for HandVisualization.xaml
    /// </summary>
    public partial class HandVisualization : UserControl
    {
        Window window = null;

        #region Properties

        #region MotionTrackingClient

        /// <summary>
        /// The <see cref="MotionTrackingClient" /> dependency property's name.
        /// </summary>
        public const string MotionTrackingClientPropertyName = "MotionTrackingClient";

        /// <summary>
        /// Gets or sets the value of the <see cref="MotionTrackingClient" />
        /// property. This is a dependency property.
        /// </summary>
        public MotionTrackingClient MotionTrackingClient
        {
            get
            {
                return (MotionTrackingClient)GetValue(MotionTrackingClientProperty);
            }
            set
            {
                SetValue(MotionTrackingClientProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="MotionTrackingClient" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty MotionTrackingClientProperty = DependencyProperty.Register(
            MotionTrackingClientPropertyName,
            typeof(MotionTrackingClient),
            typeof(HandVisualization),
            new UIPropertyMetadata(null, new PropertyChangedCallback(OnMotionTrackingClientPropertyChanged)));

        private static void OnMotionTrackingClientPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var control = obj as HandVisualization;
            if (control == null)
                return;

            control.SetImageSource();
        }

        #endregion
        
        #endregion

        #region Constructors

        public HandVisualization()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(HandVisualization_Loaded);
        }

        #endregion

        #region Private Methods

        void HandVisualization_Loaded(object sender, RoutedEventArgs e)
        {
            window = VisualUtility.FindVisualParent<Window>(this);

            if (window == null)
            {
                throw new InvalidOperationException("HandVisualization must be within the visual tree of a Window-derived control");
            }

            VerifyNoTransform(window);

            image1.Width = window.ActualWidth;
            image1.Height = window.ActualHeight;
        }
        
        private void SetImageSource()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke((Action)delegate
                    {
                        SetImageSource();
                    });
                return;
            }

            if (MotionTrackingClient == null)
            {
                image1.Source = null;
            }
            else
            {
                if (MotionTrackingClient.IsFirstFrameReady)
                {
                    image1.Source = MotionTrackingClient.HandVisualization;
                }
                else
                {
                    MotionTrackingClient.FirstFrameReady += (s, e) =>
                        {
                            SetImageSource();
                        };
                }
            }
        }

        private void VerifyNoTransform(Window window)
        {
            //TODO: this doesn't work
            var transform = this.TransformToVisual(window) as MatrixTransform;
            if (transform != null && !transform.Matrix.IsIdentity)
            {
                throw new InvalidOperationException("HandVisualization must fill the entire window within transformation or margins");
            }
        }


        #endregion
    }
}
