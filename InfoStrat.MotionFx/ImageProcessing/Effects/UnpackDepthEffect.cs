
using System;
using System.IO;
using System.Reflection;
using DirectCanvas.Misc;
using DirectCanvas.Effects;
using DirectCanvas;


namespace InfoStrat.MotionFx.ImageProcessing.Effects
{
    public class UnpackDepthEffect : ShaderEffect
    {
        private const string SHADER_RESOURCE_NAME = "InfoStrat.MotionFx.ImageProcessing.Effects.Shaders.UnpackDepthEffect.ps";
        private const string SHADER_ENTRY = "UnpackDepthMain";

        private const int TINTCOLOR_REGISTER = 0;
        private const int MINTHRESHOLD_VALUE = 1;
        private const int MAXTHRESHOLD_VALUE = 2;
        private const int MIN_VALUE = 3;
        private const int MAX_VALUE = 4;
        private const int TEXSIZEX_VALUE = 5;
        private const int TEXSIZEY_VALUE = 6;

        private Color4 m_tint;

        private float m_minThreshold;
        private float m_maxThreshold;
        private float m_minValue;
        private float m_maxValue;
        private Size m_texSize;

        public UnpackDepthEffect(DirectCanvasFactory directCanvas)
            : base(directCanvas)
        {
            SetShaderSource(GetResourceString(SHADER_RESOURCE_NAME, Assembly.GetExecutingAssembly()), SHADER_ENTRY);

            RegisterProperty<Color4>(TINTCOLOR_REGISTER);
            RegisterProperty<float>(MINTHRESHOLD_VALUE);
            RegisterProperty<float>(MAXTHRESHOLD_VALUE);
            RegisterProperty<float>(MIN_VALUE);
            RegisterProperty<float>(MAX_VALUE);
            RegisterProperty<float>(TEXSIZEX_VALUE);
            RegisterProperty<float>(TEXSIZEY_VALUE);
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

        public float MinValue
        {
            get
            {
                return m_minValue;
            }
            set
            {
                m_minValue = value;
                SetValue<float>(MIN_VALUE, value);
            }
        }

        public float MaxValue
        {
            get
            {
                return m_maxValue;
            }
            set
            {
                m_maxValue = value;
                SetValue<float>(MAX_VALUE, value);
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

