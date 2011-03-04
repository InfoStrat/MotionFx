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
        
        public DepthView()
        {
            InitializeComponent();

            Init();
        }

        private void Init()
        {
            HandPointGenerator.Default.FirstFrameReady += new EventHandler(HandPointGenerator_FirstFrameReady);

            HandPointGenerator.Default.UserFound += new EventHandler<UserEventArgs>(HandPointGenerator_UserFound);
            HandPointGenerator.Default.UserLost += new EventHandler<UserEventArgs>(HandPointGenerator_UserLost);
            HandPointGenerator.Default.PoseRecognized += new EventHandler<UserEventArgs>(HandPointGenerator_PoseRecognized);
            HandPointGenerator.Default.SkeletonReady += new EventHandler<UserEventArgs>(HandPointGenerator_SkeletonReady);
            HandPointGenerator.Default.FrameUpdated += new EventHandler(HandPointGenerator_FrameUpdated);
        }

        void HandPointGenerator_FirstFrameReady(object sender, EventArgs e)
        {
            Dispatcher.Invoke((Action)delegate
                {
                    this.txtStatus.Text = "Ready";
                    image1.Source = HandPointGenerator.Default.DepthMap;
                });
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

        void HandPointGenerator_FrameUpdated(object sender, EventArgs e)
        {

            if (Application.Current == null)
            {
                return;
            }

        }

        void HandPointGenerator_DataMapUpdated(object sender, MapUpdatedEventArgs e)
        {
            if (Application.Current == null)
            {
                return;
            }

            e.GetBitmapSource(this.Dispatcher, (b) =>
            {
                image1.Source = b;
            });
        }
    }
}
