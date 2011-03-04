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

        #region Constructors

        public HandVisualization()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(HandVisualization_Loaded);
        }

        void HandVisualization_Loaded(object sender, RoutedEventArgs e)
        {

            window = VisualUtility.FindVisualParent<Window>(this);

            if (window == null)
            {
                throw new InvalidOperationException("HandVisualization must be within the visual tree of a Window-derived control");
            }

            if (!window.IsLoaded)
            {
                window.Loaded += (s, ee) =>
                {
                    Init();
                };
            }
            else
            {
                Init();
            }
        }

        #endregion

        #region Private Methods


        private void Init()
        {
            VerifyNoTransform(window);
            image1.Width = window.ActualWidth;
            image1.Height = window.ActualHeight;
            HandPointGenerator.Default.FirstFrameReady += new EventHandler(Default_FirstFrameReady);
        }

        void Default_FirstFrameReady(object sender, EventArgs e)
        {
            Dispatcher.Invoke((Action)delegate
            {
                image1.Source = HandPointGenerator.Default.HandMap;
            });
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
