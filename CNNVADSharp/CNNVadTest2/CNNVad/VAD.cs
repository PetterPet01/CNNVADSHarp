using System;

namespace Pet.CNNVad
{
    using Ultilities;
    public class VAD
    {
        public AudioRecorder audioRec = new AudioRecorder();
        SimpleTimer timer;
        public bool isSpeaking;
        int predictCount = 10;
        int framesPerCheck = 5;
        int framesChecked;
        int selfCount;
        string modelPath;
        int sampleFreq;
        public VAD(string modelPath, int sampleFreq)
        {
            this.modelPath = modelPath;
            this.sampleFreq = sampleFreq;
        }
        public void start()
        {
            audioRec.start(modelPath, sampleFreq);
            timer = new SimpleTimer(() =>
            {
                //framesChecked += audioRec.processAudio() ? 1 : 0;
                //if (framesChecked == 5)
                //{
                //    audioRec.predict();
                //    selfCount++;
                //    framesChecked = 0;
                //    if (audioRec.predictBuffer.count > 10)
                //    {
                //        isSpeaking = audioRec.predictBuffer.movingAverage > 0.5;
                //        selfCount = 0;
                //    }
                //}
                Console.WriteLine(audioRec.predictBuffer.count);                
            }, 10);
            timer.StartAction();
        }
    }
}
