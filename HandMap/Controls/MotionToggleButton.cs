using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Surface.Presentation.Controls.Primitives;
using Microsoft.Surface.Presentation.Input;

namespace HandMap.Controls
{
    public class MotionToggleButton : SurfaceToggleButton
    {
        #region Fields

        private const double HoldThreshold = 500;
        private const double ScaleFactor = 0.8;
        private DispatcherTimer _dispatcherTimer;
        private InputDevice _lastTouchDevice;

        #endregion

        #region Constructors

        public MotionToggleButton()
        {
            this.TouchEnter += new EventHandler<System.Windows.Input.TouchEventArgs>(MotionButton_TouchEnter);
            this.TouchLeave += new EventHandler<TouchEventArgs>(MotionButton_TouchLeave);
        }

        #endregion

        #region Private Methods

        private void MotionButton_TouchEnter(object sender, TouchEventArgs e)
        {
            StartTimer();
            _lastTouchDevice = e.Device;

            ScaleDown();
        }

        private void MotionButton_TouchLeave(object sender, TouchEventArgs e)
        {
            ScaleUp();
        }

        private void StartTimer()
        {
            if (_dispatcherTimer == null)
            {
                _dispatcherTimer = new DispatcherTimer(
                    TimeSpan.FromMilliseconds(HoldThreshold),
                    DispatcherPriority.Input, OnTimerTick, Dispatcher);
            }

            _dispatcherTimer.Stop();
            _dispatcherTimer.Start();
        }

        private void StopTimer()
        {
            if (_dispatcherTimer != null)
            {
                _dispatcherTimer.Stop();
            }
        }

        private void OnTimerTick(object sender, EventArgs args)
        {
            StopTimer();
            if (_lastTouchDevice != null &&
                this.GetInputDevicesOver().Contains(_lastTouchDevice))
            {
                ProcessClick();
            }
        }

        private void ProcessClick()
        {
            if (this.IsChecked == true)
                this.IsChecked = false;
            else
                this.IsChecked = true;
        }


        private void ScaleDown()
        {
            Storyboard storyboard = new Storyboard();
            ScaleTransform scaleTranform = new ScaleTransform(1.0, 1.0);
            this.RenderTransformOrigin = new Point(0.5, 0.5);
            this.RenderTransform = scaleTranform;

            DoubleAnimation animationX = new DoubleAnimation();
            animationX.Duration = TimeSpan.FromMilliseconds(HoldThreshold);
            animationX.From = 1.0;
            animationX.To = ScaleFactor;
            storyboard.Children.Add(animationX);
            Storyboard.SetTargetProperty(animationX, new PropertyPath("RenderTransform.ScaleX"));
            Storyboard.SetTarget(animationX, this);

            DoubleAnimation animationY = new DoubleAnimation();
            animationY.Duration = TimeSpan.FromMilliseconds(HoldThreshold);
            animationY.From = 1.0;
            animationY.To = ScaleFactor;
            storyboard.Children.Add(animationY);
            Storyboard.SetTargetProperty(animationY, new PropertyPath("RenderTransform.ScaleY"));
            Storyboard.SetTarget(animationY, this);

            storyboard.Begin();
        }

        private void ScaleUp()
        {
            Storyboard storyboard = new Storyboard();
            ScaleTransform scaleTranform = new ScaleTransform(1.0, 1.0);
            this.RenderTransformOrigin = new Point(0.5, 0.5);
            this.RenderTransform = scaleTranform;

            DoubleAnimation animationX = new DoubleAnimation();
            animationX.Duration = TimeSpan.FromMilliseconds(HoldThreshold);
            animationX.From = ScaleFactor;
            animationX.To = 1.0;
            storyboard.Children.Add(animationX);
            Storyboard.SetTargetProperty(animationX, new PropertyPath("RenderTransform.ScaleX"));
            Storyboard.SetTarget(animationX, this);

            DoubleAnimation animationY = new DoubleAnimation();
            animationY.Duration = TimeSpan.FromMilliseconds(HoldThreshold);
            animationY.From = ScaleFactor;
            animationY.To = 1.0;
            storyboard.Children.Add(animationY);
            Storyboard.SetTargetProperty(animationY, new PropertyPath("RenderTransform.ScaleY"));
            Storyboard.SetTarget(animationY, this);

            storyboard.Begin();
        }

        #endregion
    }
}
