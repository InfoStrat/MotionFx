using System;
using System.Collections.Generic;
using System.Speech.Recognition;
using System.Windows;
using System.Windows.Documents;
using Blake.NUI.WPF.Touch;
using InfoStrat.MotionFx;
using InfoStrat.VE.Utilities;
using Microsoft.Surface.Presentation.Controls;
using InfoStrat.MotionFx.Speech;
using System.ComponentModel;

namespace HandMap
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

        SpeechHelper speech;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SurfaceWindow1()
        {
            InitializeComponent();

            InitSpeechRecognition();

            MotionTrackingClient = new MotionTrackingClient(this);
            
            NativeTouchDevice.RegisterEvents(this);

            this.DataContext = this;
        }

        private void InitSpeechRecognition()
        {
            speech = new SpeechHelper();
            speech.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(speech_SpeechRecognized);
            speech.SpeechRecognitionRejected += new EventHandler<SpeechRecognitionRejectedEventArgs>(speech_SpeechRecognitionRejected);

            Application.Current.Exit += new ExitEventHandler(Current_Exit);

            List<string> options = new List<string>();

            options.Add("computer road");
            options.Add("computer aerial");
            options.Add("computer hybrid");
            options.Add("computer ink on");
            options.Add("computer ink erase");
            options.Add("computer ink off");
            speech.Recognize(options, true);
        }

        void speech_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            //            MessageBox.Show("rejected: " + e.Result.Text + " " + e.Result.Confidence.ToString());
            ProcessSpeech(e.Result.Text, e.Result.Confidence);

            //            TextBlockMessage.Text = "speech rejected: " + e.Result.Text;
        }

        void speech_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            //MessageBox.Show(e.Result.Text + " " + e.Result.Confidence.ToString());
            ProcessSpeech(e.Result.Text, e.Result.Confidence);
        }

        void ProcessSpeech(string text, float confidence)
        {

            TextBlockMessage.Text = text + " (" + confidence.ToString() + ")";
            if (confidence < 0.25)
                return;

            switch (text)
            {
                case "computer road":
                    SetMapStyle(InfoStrat.VE.VEMapStyle.Road);
                    break;
                case "computer aerial":
                    SetMapStyle(InfoStrat.VE.VEMapStyle.Aerial);
                    break;
                case "computer hybrid":
                    SetMapStyle(InfoStrat.VE.VEMapStyle.Hybrid);
                    break;
                case "computer ink on":
                    ToggleButtonInk.IsChecked = true;
                    EnableInk();
                    break;
                case "computer ink erase":
                    EnableErase();
                    break;
                case "computer ink off":
                    ToggleButtonInk.IsChecked = false;
                    DisableInk();
                    break;
                default:
                    break;
            }
        }

        void Current_Exit(object sender, ExitEventArgs e)
        {
            if (speech != null)
                speech.StopRecognition();
        }

        private void ButtonAerial_Click(object sender, RoutedEventArgs e)
        {
            SetMapStyle(InfoStrat.VE.VEMapStyle.Aerial);
        }

        private void ButtonRoad_Click(object sender, RoutedEventArgs e)
        {
            SetMapStyle(InfoStrat.VE.VEMapStyle.Road);
        }

        private void ButtonHybrid_Click(object sender, RoutedEventArgs e)
        {
            SetMapStyle(InfoStrat.VE.VEMapStyle.Hybrid);
        }

        private void ButtonNewYork_Click(object sender, RoutedEventArgs e)
        {
            ThrowSVI(ScatterViewItem1, new Point(-600, -450), -30, 0.0, 1.0);

            double lat = 40.65;
            double lon = -74.0;

            Map1.FlyTo(new InfoStrat.VE.VELatLong(lat, lon), -30, Map1.Yaw, 3500);
            Map1.MapManipulationMode = InfoStrat.VE.MapManipulationMode.TiltSpinZoomPivot;
        }

        private void ButtonCanyon_Click(object sender, RoutedEventArgs e)
        {
            double lat = 36.076;
            double lon = -113.22;

            Map1.FlyTo(new InfoStrat.VE.VELatLong(lat, lon), -30, Map1.Yaw, 10000);
            Map1.MapManipulationMode = InfoStrat.VE.MapManipulationMode.TiltSpinZoomPivot;

            ThrowSVI(ScatterViewItem1, new Point(480, 280), 10.0, 0.0, 2.0);
        }

        private void ButtonUs_Click(object sender, RoutedEventArgs e)
        {
            ThrowSVI(ScatterViewItem1, new Point(-600, -450), -30, 0.0, 1.0);

            double lat = 37;
            double lon = -98.352;

            Map1.FlyTo(new InfoStrat.VE.VELatLong(lat, lon), -90, 0.0, 5507876);
            Map1.MapManipulationMode = InfoStrat.VE.MapManipulationMode.PanZoomPivot;
        }

        private void ToggleButtonInk_Checked(object sender, RoutedEventArgs e)
        {
            EnableInk();
        }

        private void ToggleButtonInk_Unchecked(object sender, RoutedEventArgs e)
        {
            DisableInk();
        }

        private void SetMapStyle(InfoStrat.VE.VEMapStyle veMapStyle)
        {
            Map1.MapStyle = veMapStyle;
        }

        private void EnableInk()
        {
            InkCanvas1.DefaultDrawingAttributes.Height = 2;
            InkCanvas1.DefaultDrawingAttributes.Width = 2;
            InkCanvas1.EditingMode = SurfaceInkEditingMode.Ink;
            InkCanvas1.IsHitTestVisible = true;
        }

        private void EnableErase()
        {
            InkCanvas1.EditingMode = SurfaceInkEditingMode.EraseByPoint;
            InkCanvas1.DefaultDrawingAttributes.Height = 50;
            InkCanvas1.DefaultDrawingAttributes.Width = 50;
            InkCanvas1.IsHitTestVisible = true;
        }

        private void DisableInk()
        {
            InkCanvas1.IsHitTestVisible = false;
            InkCanvas1.Strokes.Clear();
        }

        private void ThrowSVI(ScatterViewItem svi, Point targetPoint, double targetOrientation, double fromTime, double toTime)
        {

            AnimateUtility.AnimateElementPoint(svi, ScatterViewItem.CenterProperty,
                                                targetPoint, fromTime, toTime);
            AnimateUtility.AnimateElementDouble(svi, ScatterViewItem.OrientationProperty,
                                                targetOrientation, fromTime, toTime);
        }

        private void ButtonShutDown_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }



    }
}