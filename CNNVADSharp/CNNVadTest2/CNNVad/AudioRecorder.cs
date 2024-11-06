using System;
using System.Linq;
using NAudio.Wave;
using Pet.Ultilities;
using Cyotek.Collections.Generic;
using TensorFlow;
using System.Threading.Tasks;
using System.Timers;
namespace Pet.CNNVad
{
    using System.IO;
    using static audioProcessing;
    using static Ultilities.Ultilities;
    public class AudioRecorder
    {
        #region Tensorflow
        TFSession model;

        TFGraph graph = new TFGraph();

        string inputOpName = "inputs/x-input";
        string outputOpName = "model/Softmax";
        #endregion

        #region Audio
        WaveIn waveIn;
        public Variables memoryPointer;

        static float capSecs = 0.0125f; //600 and 48000 is the default frameSize and sampleRate, which i'm calculating the seconds audio captures rate 
        static float minCapSecs = 0.0115f; //11.5ms minimum, to get efficient fft
        static float maxCapSecs = 0.0135f; //13.5ms maximum, boundary for fft size
        static int maxIteration = 7; //Just found it's the best maximum iteration for optimal power of 2's
        int FRAMESIZE;
        int SAMPLINGFREQUENCY;
        CircularBuffer<float> buffer = new CircularBuffer<float>(2048 * 16);
        public MovingAverageBuffer predictBuffer;
        float[] prevBufferFloat;
        #endregion

        #region Processing
        bool isSessionOccupied;
        
        Timer inferenceTimer = new Timer();
        int CNNTime = 62;
        #endregion

        public bool isSpeaking;
        
        void loadGraphFromPath(string modelPath)
        {
            graph.Import(File.ReadAllBytes(modelPath));
        }
        void createSession()
        {
            model = new TFSession(graph);
        }
        // This is called periodically by the media server.
        void processAudio(object sender, WaveInEventArgs e)
        {
            int bufferLength = e.BytesRecorded;
            float[] inputBufferFloat;

            float[] audio = new float[bufferLength / 2];
            for (int i = 0; i < bufferLength; i += 2)
            {
                audio[i / 2] = (short)((e.Buffer[i + 1] << 8) | e.Buffer[i + 0]) * SHORT2FLOAT;  //Instead of diving by 32767
            }
            buffer.Put(audio);
            if (buffer.Size >= FRAMESIZE)
                inputBufferFloat = buffer.Get(FRAMESIZE);
            else
                return;
            //if (buffer.Size >= FRAMESIZE)
            //    if (prevBufferFloat != null)
            //        inputBufferFloat = buffer.Get(FRAMESIZE);
            //    else
            //    {
            //        prevBufferFloat = buffer.Get(FRAMESIZE);
            //        return;
            //    }
            //else
            //    return;
            //inputBufferFloat = prevBufferFloat.Concat(inputBufferFloat);
            //prevBufferFloat = inputBufferFloat.SubArray(FRAMESIZE, FRAMESIZE);
            compute(ref memoryPointer, inputBufferFloat);
        }
        void predict(object sender, ElapsedEventArgs e)
        {

            //float[,] input = new float[1, 1600];
            //for (int i = 0; i < 40; i++)
            //    for (int j = 0; j < 40; j++)
            //        input[0, 40 * i + j] = memoryPointer.melSpectrogram.melSpectrogramImage[i, j];

            //System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            //watch.Start();
            if (isSessionOccupied) return;
            isSessionOccupied = true;
            var tensor = new TFTensor(memoryPointer.melSpectrogram.melSpectrogramImage);
            TFSession.Runner runner = model.GetRunner();

            runner.AddInput(inputOpName, tensor);
            runner.Fetch(outputOpName);
            var output = runner.Run();
            //watch.Stop();

            TFTensor result = output[0];
            var realRes = (float[,])result.GetValue();
            predictBuffer.addDatum(realRes[0, 1]);
            isSpeaking = predictBuffer.movingAverage > 0.5;
            //Console.WriteLine(predictBuffer.movingAverage);
            isSessionOccupied = false;

            //Console.WriteLine(watch.ElapsedMilliseconds);
        }

        public void start(string modelPath, int sampleFreq)
        {
            //Tensorflow
            loadGraphFromPath(modelPath);
            createSession();

            //Variable Init
            SAMPLINGFREQUENCY = sampleFreq;
            //FRAMESIZE = (int)(SAMPLINGFREQUENCY * capSecs);

            int maxFrameSize = (int)(maxCapSecs * SAMPLINGFREQUENCY);
            int minFrameSize = (int)(minCapSecs * SAMPLINGFREQUENCY);
            int desiredFrameSize = (int)(capSecs * SAMPLINGFREQUENCY);
            int nearestPow2 = Nearest2Pow(minFrameSize);
            if (nearestPow2 <= maxFrameSize)
            {
                FRAMESIZE = nearestPow2;
                memoryPointer = initialize(SAMPLINGFREQUENCY, FRAMESIZE);
            }
            else
            {
                int[] answer = optimalPower2Iteration(desiredFrameSize, 7);
                FRAMESIZE = answer[0] * answer[1];
                memoryPointer = initialize(SAMPLINGFREQUENCY, answer[0], answer[1]);
            }
            predictBuffer = new MovingAverageBuffer(5);

            inferenceTimer = new Timer(CNNTime);
            inferenceTimer.Elapsed += predict;
            inferenceTimer.Start();

            //Recording Settings
            waveIn = new WaveIn();
            waveIn.DeviceNumber = 0;
            waveIn.DataAvailable += processAudio;
            waveIn.WaveFormat = new NAudio.Wave.WaveFormat(sampleFreq, 1);
            waveIn.BufferMilliseconds = (int)Math.Floor(capSecs * 1000);
            waveIn.StartRecording();
        }

        /// <summary>
        /// Stop all operations and release all variables without actually disposing
        /// </summary>
        public void stop()
        {
            model.CloseSession();
            waveIn.Dispose();
            inferenceTimer.Stop();
        }
        /// <summary>
        /// Stop receiving new audio data
        /// </summary>
        public void pause()
        {
            waveIn.StopRecording();
        }
        /// <summary>
        /// Continue receiving new audio data
        /// </summary>
        public void resume()
        {
            waveIn.StartRecording();
        }
    }
}
