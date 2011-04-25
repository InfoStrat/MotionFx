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
using InfoStrat.MotionFx;
using Blake.NUI.WPF.Utility;
using System.ComponentModel;

namespace InfoStrat.MotionFx.Controls
{
    /// <summary>
    /// Interaction logic for DepthView.xaml
    /// </summary>
    public partial class DepthView : UserControl, INotifyPropertyChanged
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
            typeof(DepthView),
            new UIPropertyMetadata(null, new PropertyChangedCallback(OnMotionTrackingClientPropertyChanged)));

        private static void OnMotionTrackingClientPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var control = obj as DepthView;
            if (control == null)
                return;

            control.SetImageSource();
        }

        #endregion

        #endregion

        public DepthView()
        {
            InitializeComponent();

            HandPointGenerator.Default.UserFound += new EventHandler<UserEventArgs>(HandPointGenerator_UserFound);
            HandPointGenerator.Default.UserLost += new EventHandler<UserEventArgs>(HandPointGenerator_UserLost);
            HandPointGenerator.Default.PoseRecognized += new EventHandler<UserEventArgs>(HandPointGenerator_PoseRecognized);
            HandPointGenerator.Default.SkeletonReady += new EventHandler<UserEventArgs>(HandPointGenerator_SkeletonReady);
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
                this.txtStatus.Text = "";
                image1.Source = null;
            }
            else
            {
                image1.Source = MotionTrackingClient.DepthVisualization;
            }
        }

        void HandPointGenerator_UserFound(object sender, UserEventArgs e)
        {
            Dispatcher.Invoke((Action)delegate
            {
                this.txtStatus.Text = "User found: " + e.Id;
                borderUser.Background = Brushes.Red;
                borderUser.Opacity = 1.0;
            });
        }

        void HandPointGenerator_UserLost(object sender, UserEventArgs e)
        {
            Dispatcher.Invoke((Action)delegate
            {
                this.txtStatus.Text = "User lost: " + e.Id;
            });
        }

        void HandPointGenerator_PoseRecognized(object sender, UserEventArgs e)
        {
            Dispatcher.Invoke((Action)delegate
            {
                this.txtStatus.Text = "Pose: " + e.Id;
                borderUser.Background = Brushes.Yellow;
            });
        }

        void HandPointGenerator_SkeletonReady(object sender, UserEventArgs e)
        {
            Dispatcher.Invoke((Action)delegate
            {
                this.txtStatus.Text = "Skeleton: " + e.Id;
                borderUser.Background = Brushes.Green;
                AnimateUtility.AnimateElementDouble(borderUser, OpacityProperty, 0.5, 0.0, 2.0);
            });
        }
    }
}
