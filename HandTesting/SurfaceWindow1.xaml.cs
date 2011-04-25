using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using Blake.NUI.WPF.Touch;
using InfoStrat.MotionFx;
using Microsoft.Surface.Presentation.Controls;
using Blake.NUI.WPF.Utility;
using Blake.NUI.WPF.SurfaceToolkit.Utility;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace HandTesting
{
    /// <summary>
    /// Interaction logic for SurfaceWindow1.xaml
    /// </summary>
    public partial class SurfaceWindow1 : SurfaceWindow, INotifyPropertyChanged
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

        private ObservableCollection<ImageModel> _images = new ObservableCollection<ImageModel>();
        public ObservableCollection<ImageModel> Images
        {
            get
            {
                return _images;
            }
        }

        #region MotionTrackingClient

        /// <summary>
        /// The <see cref="MotionTrackingClient" /> property's name.
        /// </summary>
        public const string MotionTrackingClientPropertyName = "MotionTrackingClient";

        private MotionTrackingClient _motionTrackingClient = null;

        public MotionTrackingClient MotionTrackingClient
        {
            get
            {
                return _motionTrackingClient;
            }

            set
            {
                if (_motionTrackingClient == value)
                {
                    return;
                }

                var oldValue = _motionTrackingClient;
                _motionTrackingClient = value;

                // Update bindings, no broadcast
                RaisePropertyChanged(MotionTrackingClientPropertyName);
            }
        }

        #endregion

        #endregion

        public SurfaceWindow1()
            : this(false)
        { }

        Point centerPoint;

        ImageModel desertImage;
        ImageModel jellyImage;
        ImageModel penguinImage;

        public SurfaceWindow1(bool isHorizontal)
        {
            InitializeComponent();

            //this.WindowState = System.Windows.WindowState.Normal;
            //this.Width = 800;
            //this.Height = 400;
            //this.Left = 400;
            //this.Top = 0;
            //this.IsHorizontal = isHorizontal;
            this.Loaded += new RoutedEventHandler(SurfaceWindow1_Loaded);

            MotionTrackingClient = new MotionTrackingClient(this);
            
            NativeTouchDevice.RegisterEvents(this);

            InitData();
            this.DataContext = this;
        }

        private void InitData()
        {
            desertImage = new ImageModel(new Uri(@"Images\Desert.jpg", UriKind.Relative));
            jellyImage = new ImageModel(new Uri(@"Images\Jellyfish.jpg", UriKind.Relative));
            penguinImage = new ImageModel(new Uri(@"Images\Penguins.jpg", UriKind.Relative));
            Images.Add(desertImage);
            Images.Add(jellyImage);
            Images.Add(penguinImage);
        }

        void SurfaceWindow1_Loaded(object sender, RoutedEventArgs e)
        {
            centerPoint = new Point(this.ActualWidth / 2, this.ActualHeight / 2);
        }

        private void ButtonShutDown_Click(object sender, RoutedEventArgs e)
        {
            foreach (Window w in Application.Current.Windows)
            {
                w.Close();
            }
        }

        private void MotionToggleButtonDesert_Checked(object sender, RoutedEventArgs e)
        {
            var svi = scatterview.ItemContainerGenerator.ContainerFromItem(desertImage) as ScatterViewItem;
            SurfaceAnimateUtility.ThrowSVI(svi, centerPoint, 0, 0.0, 1.0);
        }

        private void MotionToggleButtonDesert_Unchecked(object sender, RoutedEventArgs e)
        {
            var svi = scatterview.ItemContainerGenerator.ContainerFromItem(desertImage) as ScatterViewItem;
            SurfaceAnimateUtility.ThrowSVI(svi, new Point(-600, -450), -30, 0.0, 1.0);
        }


        private void MotionToggleButtonJellyfish_Checked(object sender, RoutedEventArgs e)
        {
            var svi = scatterview.ItemContainerGenerator.ContainerFromItem(jellyImage) as ScatterViewItem;

            SurfaceAnimateUtility.ThrowSVI(svi, centerPoint, 0, 0.0, 1.0);
        }

        private void MotionToggleButtonJellyfish_Unchecked(object sender, RoutedEventArgs e)
        {
            var svi = scatterview.ItemContainerGenerator.ContainerFromItem(jellyImage) as ScatterViewItem;

            SurfaceAnimateUtility.ThrowSVI(svi, new Point(-600, -450), -30, 0.0, 1.0);
        }


        private void MotionToggleButtonPenguins_Checked(object sender, RoutedEventArgs e)
        {
            var svi = scatterview.ItemContainerGenerator.ContainerFromItem(penguinImage) as ScatterViewItem;

            SurfaceAnimateUtility.ThrowSVI(svi, centerPoint, 0, 0.0, 1.0);
        }

        private void MotionToggleButtonPenguins_Unchecked(object sender, RoutedEventArgs e)
        {
            var svi = scatterview.ItemContainerGenerator.ContainerFromItem(penguinImage) as ScatterViewItem;

            SurfaceAnimateUtility.ThrowSVI(svi, new Point(-600, -450), -30, 0.0, 1.0);
        }
    }
}