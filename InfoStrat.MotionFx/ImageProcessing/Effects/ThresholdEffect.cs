
using System;
using System.IO;
using System.Reflection;
using DirectCanvas.Misc;
using DirectCanvas.Effects;
using DirectCanvas;


namespace InfoStrat.MotionFx.ImageProcessing.Effects
{
    public class ThresholdEffect : ShaderEffect
    {
        private const string SHADER_RESOURCE_NAME = "InfoStrat.MotionFx.ImageProcessing.Effects.Shaders.ThresholdEffect.ps";
        private const string SHADER_ENTRY = "ThresholdMain";

        private const int TINTCOLOR_REGISTER = 0;
        private const int MINTHRESHOLD_VALUE = 1;
        private const int MAXTHRESHOLD_VALUE = 2;

        private Color4 m_tint;

        private float m_minThreshold;
        private float m_maxThreshold;

        public ThresholdEffect(DirectCanvasFactory directCanvas)
            : base(directCanvas)
        {
            SetShaderSource(GetResourceString(SHADER_RESOURCE_NAME, Assembly.GetExecutingAssembly()), SHADER_ENTRY);

            RegisterProperty<Color4>(TINTCOLOR_REGISTER);
            RegisterProperty<float>(MINTHRESHOLD_VALUE);
            RegisterProperty<float>(MAXTHRESHOLD_VALUE);

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
    }
}

