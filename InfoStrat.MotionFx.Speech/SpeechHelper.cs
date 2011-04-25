using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Speech.Recognition;
using System.Speech.Synthesis;

namespace InfoStrat.MotionFx.Speech
{
    public class SpeechHelper
    {
        #region Class Members

        SpeechRecognitionEngine recog;
        SpeechSynthesizer synth;

        #endregion

        #region Events

        public event EventHandler<SpeechRecognitionRejectedEventArgs> SpeechRecognitionRejected;
        public event EventHandler<SpeechRecognizedEventArgs> SpeechRecognized;
        public event EventHandler<AudioLevelUpdatedEventArgs> AudioLevelUpdated;

        #endregion

        #region Properties

        public int AudioLevel
        {
            get
            {
                if (recog == null)
                    return 0;
                return recog.AudioLevel;
            }
        }

        #endregion

        #region Constructors

        public SpeechHelper()
        {
            recog = new SpeechRecognitionEngine();
            recog.SetInputToDefaultAudioDevice();
            recog.SpeechRecognitionRejected += new EventHandler<SpeechRecognitionRejectedEventArgs>(recog_SpeechRecognitionRejected);
            recog.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(recog_SpeechRecognized);
            recog.AudioLevelUpdated += new EventHandler<AudioLevelUpdatedEventArgs>(recog_AudioLevelUpdated);
            synth = new SpeechSynthesizer();
        }

        #endregion

        #region Public Methods

        public void Speak(string text)
        {
            synth.Speak(text);
        }

        public void StopRecognition()
        {
            recog.RecognizeAsyncStop();
            recog.UnloadAllGrammars();
        }

        public void Recognize(string phrase)
        {
            List<string> phrases = new List<string>();
            phrases.Add(phrase);
            Recognize(phrases);
        }

        public void Recognize(IEnumerable<string> phrases)
        {
            Recognize(phrases, false);
        }

        public void Recognize(IEnumerable<string> phrases, bool continuous)
        {
            Choices choices = new Choices();
            foreach (string s in phrases)
            {
                choices.Add(s);
            }
            Grammar grammar = new Grammar(choices.ToGrammarBuilder());

            Recognize(grammar, continuous);
        }

        public void Recognize(Grammar grammar)
        {
            Recognize(grammar, false);
        }

        public void Recognize(Grammar grammar, bool continuous)
        {
            StopRecognition();

            recog.LoadGrammar(grammar);

            RecognizeMode mode = RecognizeMode.Single;
            if (continuous)
                mode = RecognizeMode.Multiple;

            recog.RecognizeAsync(mode);
        }

        #endregion

        #region Recognition Event Handlers

        void recog_AudioLevelUpdated(object sender, AudioLevelUpdatedEventArgs e)
        {
            if (AudioLevelUpdated != null)
            {
                AudioLevelUpdated(this, e);
            }
        }

        void recog_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            if (SpeechRecognitionRejected != null)
            {
                SpeechRecognitionRejected(this, e);
            }
        }

        void recog_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (SpeechRecognized != null)
            {
                SpeechRecognized(this, e);
            }
        }

        #endregion
    }
}
