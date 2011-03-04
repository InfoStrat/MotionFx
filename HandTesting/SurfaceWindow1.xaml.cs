using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using Blake.NUI.WPF.Touch;
using InfoStrat.MotionFx;
using Microsoft.Surface.Presentation.Controls;
using Blake.NUI.WPF.Utility;
using Blake.NUI.WPF.SurfaceToolkit.Utility;

namespace HandTesting
{
    /// <summary>
    /// Interaction logic for SurfaceWindow1.xaml
    /// </summary>
    public partial class SurfaceWindow1 : SurfaceWindow
    {
        public SurfaceWindow1()
        {
            InitializeComponent();
            
            MotionTracking.RegisterEvents(this);
            NativeTouchDevice.RegisterEvents(this);
        }

        private void ButtonShutDown_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MotionToggleButtonDesert_Checked(object sender, RoutedEventArgs e)
        {
            SurfaceAnimateUtility.ThrowSVI(sviDesert, new Point(500, 500), 0, 0.0, 1.0);
        }

        private void MotionToggleButtonDesert_Unchecked(object sender, RoutedEventArgs e)
        {
            SurfaceAnimateUtility.ThrowSVI(sviDesert, new Point(-600, -450), -30, 0.0, 1.0);
        }


        private void MotionToggleButtonJellyfish_Checked(object sender, RoutedEventArgs e)
        {
            SurfaceAnimateUtility.ThrowSVI(sviJellyfish, new Point(500, 500), 0, 0.0, 1.0);
        }

        private void MotionToggleButtonJellyfish_Unchecked(object sender, RoutedEventArgs e)
        {
            SurfaceAnimateUtility.ThrowSVI(sviJellyfish, new Point(-600, -450), -30, 0.0, 1.0);
        }


        private void MotionToggleButtonPenguins_Checked(object sender, RoutedEventArgs e)
        {
            SurfaceAnimateUtility.ThrowSVI(sviPenguins, new Point(500, 500), 0, 0.0, 1.0);
        }

        private void MotionToggleButtonPenguins_Unchecked(object sender, RoutedEventArgs e)
        {
            SurfaceAnimateUtility.ThrowSVI(sviPenguins, new Point(-600, -450), -30, 0.0, 1.0);
        }       
    }
}