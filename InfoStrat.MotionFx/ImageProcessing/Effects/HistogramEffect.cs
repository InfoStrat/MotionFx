
using System;
using System.IO;
using System.Reflection;
using DirectCanvas.Misc;
using DirectCanvas.Effects;
using DirectCanvas;


namespace InfoStrat.MotionFx.ImageProcessing.Effects
{
    public class HistogramEffect : ShaderEffect
    {
        private const string SHADER_RESOURCE_NAME = "InfoStrat.MotionFx.ImageProcessing.Effects.Shaders.HistogramEffect.ps";
        private const string SHADER_ENTRY = "HistogramMain";

        private const int TINTCOLOR_REGISTER = 0;
        private const int MINTHRESHOLD_VALUE = 1;
        private const int MAXTHRESHOLD_VALUE = 2;
        private const int SOURCEHEIGHT_VALUE = 3;

        private Color4 m_tint;

        private float m_minThreshold;
        private float m_maxThreshold;
        private float m_sourceHeight;

        public HistogramEffect(DirectCanvasFactory directCanvas)
            : base(directCanvas)
        {
            SetShaderSource(GetResourceString(SHADER_RESOURCE_NAME, Assembly.GetExecutingAssembly()), SHADER_ENTRY);

            RegisterProperty<Color4>(TINTCOLOR_REGISTER);
            RegisterProperty<float>(MINTHRESHOLD_VALUE);
            RegisterProperty<float>(MAXTHRESHOLD_VALUE);
            RegisterProperty<float>(SOURCEHEIGHT_VALUE);
        }

        private static string GetResourceString(string embeddedResourceName, Assembly assembly)
        {
            using (var stream = assembly.GetManifestResourceStream(embeddedResourceName))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public float MinThreshold
        {
            get
            {
                return m_minThreshold;
            }
            set
            {
                m_minThreshold = value;
                SetValue<float>(MINTHRESHOLD_VALUE, (float)m_minThreshold);
            }
        }

        public float MaxThreshold
        {
            get
            {
                return m_maxThreshold;
            }
            set
            {
                m_maxThreshold = value;
                SetValue<float>(MAXTHRESHOLD_VALUE, (float)m_maxThreshold);
            }
        }

        public Color4 Tint
        {
            get { return m_tint; }
            set
            {
                m_tint = value;
                SetValue(TINTCOLOR_REGISTER, m_tint);
            }
        }

        public float SourceHeight
        {
            get { return m_sourceHeight; }
            set
            {
                m_sourceHeight = value;
                SetValue(SOURCEHEIGHT_VALUE, m_sourceHeight);
            }
        }
        
        public DrawingLayer CalculateHistogram(DrawingLayer originalLayer)
        {
            DrawingLayer ping = originalLayer.Factory.CreateDrawingLayer(originalLayer.Width, originalLayer.Height);
            DrawingLayer pong = originalLayer.Factory.CreateDrawingLayer(originalLayer.Width, originalLayer.Height);

            float kernelHeight = 2;
            Rectangle rect = new Rectangle(0, 0, originalLayer.Width, (int)Math.Ceiling(originalLayer.Height / kernelHeight));

            DrawingLayer source = ping;
            DrawingLayer target = pong;
            SourceHeight = rect.Height;
            originalLayer.ApplyEffect(this, source, rect, false);
            
            rect.Height = (int)Math.Ceiling(rect.Height / kernelHeight);

            while (rect.Height > 1)
            {
                SourceHeight = rect.Height;
                source.ApplyEffect(this, target, rect, false);

                //Swap layers for next round
                DrawingLayer temp = source;
                source = target;
                target = temp;

                rect.Height = (int)Math.Ceiling(rect.Height / kernelHeight);                
            }
            
            //Target was switched with source, so source points to last result
            return source;
        }
    }
}

