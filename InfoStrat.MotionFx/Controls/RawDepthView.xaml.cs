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

namespace InfoStrat.MotionFx.Controls
{
    /// <summary>
    /// Interaction logic for RawDepthView.xaml
    /// </summary>
    public partial class RawDepthView : UserControl
    {
        #region Fields

        private MotionTrackingClient client;

        #endregion

        #region Constructors
        
        public RawDepthView()
        {
            InitializeComponent();

            HandPointGenerator.Default.FirstFrameReady += new EventHandler(HandPointGenerator_FirstFrameReady);
        }

        #endregion
        
        public void Init(MotionTrackingClient client)
        {
            this.client = client;
        }

        void HandPointGenerator_FirstFrameReady(object sender, EventArgs e)
        {
            if (this.client == null)
                return;
            Dispatcher.Invoke((Action)delegate
            {
                image1.Source = client.DepthVisualization;
            });
        }
    }
}
