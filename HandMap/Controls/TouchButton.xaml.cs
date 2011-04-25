using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Surface.Presentation.Input;
using System.Windows;

namespace HandMap.Controls
{
    /// <summary>
    /// Interaction logic for TouchButton.xaml
    /// </summary>
    public partial class TouchButton : UserControl
    {
        #region Fields

        private const double HoldThreshold = 500;
        private DispatcherTimer dispatcherTimer;
        private InputDevice lastTouchDevice;

        #endregion

        #region Properties

        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register("IsChecked", typeof(bool?), typeof(TouchButton), new PropertyMetadata(false));
        public bool? IsChecked
        {
            get { return SurfaceButton1.IsChecked; }
            set { SurfaceButton1.IsChecked = value; }
        }

        #endregion

        #region Constructors

        public TouchButton()
        {
            InitializeComponent();
        }

        #endregion

        #region Private Methods

        private void SurfaceButton1_TouchEnter(object sender, TouchEventArgs e)
        {
            StartTimer();
            lastTouchDevice = e.Device;
        }

        private void StartTimer()
        {
            if (dispatcherTimer == null)
            {
                dispatcherTimer = new DispatcherTimer(
                    TimeSpan.FromMilliseconds(HoldThreshold),
                    DispatcherPriority.Input, OnTimerTick, Dispatcher);
            }

            dispatcherTimer.Stop();
            dispatcherTimer.Start();
        }

        private void StopTimer()
        {
            if (dispatcherTimer != null)
            {
                dispatcherTimer.Stop();
            }
        }

        private void OnTimerTick(object sender, EventArgs args)
        {
            StopTimer();
            if (lastTouchDevice != null &&
                SurfaceButton1.GetInputDevicesOver().Contains(lastTouchDevice))
            {
                ProcessClick();
            }
        }

        private void ProcessClick()
        {
            if (SurfaceButton1.IsChecked == true)
                SurfaceButton1.IsChecked = false;
            else
                SurfaceButton1.IsChecked = true;

            SurfaceButton1.Content = SurfaceButton1.IsChecked.ToString();
        }

        #endregion
    }
}
