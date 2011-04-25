
using System;
using System.IO;
using System.Reflection;
using DirectCanvas.Misc;
using DirectCanvas.Effects;
using DirectCanvas;


namespace InfoStrat.MotionFx.ImageProcessing.Effects
{
    public class EdgeDetectEffect : ShaderEffect
    {
        private const string SHADER_RESOURCE_NAME = "InfoStrat.MotionFx.ImageProcessing.Effects.Shaders.EdgeDetectEffect.ps";
        private const string SHADER_ENTRY = "EdgeDetectMain";

        private const int TINTCOLOR_REGISTER = 0;
        private const int MINTHRESHOLD_VALUE = 1;
        private const int MAXTHRESHOLD_VALUE = 2;
        private const int TEXSIZEX_VALUE = 3;
        private const int TEXSIZEY_VALUE = 4;
        private const int EDGETHRESHOLDSQ_VALUE = 5;

        private Color4 m_tint;

        private float m_minThreshold;
        private float m_maxThreshold;
        private Size m_texSize;
        private float m_edgeThreshold;

        public EdgeDetectEffect(DirectCanvasFactory directCanvas)
            : base(directCanvas)
        {
            SetShaderSource(GetResourceString(SHADER_RESOURCE_NAME, Assembly.GetExecutingAssembly()), SHADER_ENTRY);

            RegisterProperty<Color4>(TINTCOLOR_REGISTER);
            RegisterProperty<float>(MINTHRESHOLD_VALUE);
            RegisterProperty<float>(MAXTHRESHOLD_VALUE);
            RegisterProperty<float>(TEXSIZEX_VALUE);
            RegisterProperty<float>(TEXSIZEY_VALUE);
            RegisterProperty<float>(EDGETHRESHOLDSQ_VALUE);

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

        public Size TexSize
        {
            get
            {
                return m_texSize;
            }
            set
            {
                m_texSize = value;
                SetValue(TEXSIZEX_VALUE, (float)value.Width);
                SetValue(TEXSIZEY_VALUE, (float)value.Height);
            }
        }

        public float EdgeThreshold
        {
            get
            {
                return m_edgeThreshold;
            }
            set
            {
                m_edgeThreshold = value;
                //Value should be squared
                SetValue(EDGETHRESHOLDSQ_VALUE, m_edgeThreshold * m_edgeThreshold);
            }
        }
    }
}

