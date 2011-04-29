
using System;
using System.IO;
using System.Reflection;
using DirectCanvas.Misc;
using DirectCanvas.Effects;
using DirectCanvas;


namespace InfoStrat.MotionFx.ImageProcessing.Effects
{
    public class ColorMapDepthEffect : ShaderEffect
    {
        private const string SHADER_RESOURCE_NAME = "InfoStrat.MotionFx.ImageProcessing.Effects.Shaders.ColorMapDepthEffect.ps";
        private const string SHADER_ENTRY = "ColorMapDepthMain";

        private const int TINTCOLOR_REGISTER = 0;
        private const int MINTHRESHOLD_VALUE = 1;
        private const int MAXTHRESHOLD_VALUE = 2;
        private const int MIN_VALUE = 3;
        private const int MAX_VALUE = 4;

        private Color4 m_tint;

        private float m_minThreshold;
        private float m_maxThreshold;
        private float m_minValue = 0;
        private float m_maxValue = 10000f;

        public ColorMapDepthEffect(DirectCanvasFactory directCanvas)
            : base(directCanvas)
        {
            SetShaderSource(GetResourceString(SHADER_RESOURCE_NAME, Assembly.GetExecutingAssembly()), SHADER_ENTRY);

            RegisterProperty<Color4>(TINTCOLOR_REGISTER);
            RegisterProperty<float>(MINTHRESHOLD_VALUE);
            RegisterProperty<float>(MAXTHRESHOLD_VALUE);
            RegisterProperty<float>(MIN_VALUE);
            RegisterProperty<float>(MAX_VALUE);

            Filter = ShaderEffectFilter.Point;
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

