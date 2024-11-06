using System;
using System.Linq;
using Pet.Ultilities;
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics;

namespace Pet.CNNVad
{
    using static MelSpectr;
    using static Ultilities.Ultilities;
    public struct Variables
    {
        //public Resample resampler;
        //public FIR downsampleFilter;
        public LomontTransform fft;
        public MelSpectrogram melSpectrogram;

        public double[] inputBuffer;
        public double[] window;
        //public float[] downsampled;
        //public float[] decimated;
        //public float[] frame;

        public int samplingFrequency;
        public int stepSize;
        public int decimatedStepSize;
        public int[] decimationIndexies;

    }
    public static class audioProcessing
    {
        public static float SHORT2FLOAT = 1 / 32768.0f;
        public static float FLOAT2SHORT = 32768.0f;
        static int NFILT = 40;
        static int FREQLOW = 300;
        static int FREQHIGH = 8000;
        static int TARGET_FREQ = 16000;
        static int TARGET_STEPSIZE = 200;
        public static float EPS = 1.0e-7f;
        public static float S2F = 3.051757812500000e-05f;
        public static float F2S = 32768;

        static int NCOEFFS = 81;
        static int cutOffFreq = 8000;
        
        public static Variables initialize(int frequency, int pow2, int iteration = 1) //pow2 * iteration = full frame size (stepSize * 2)
        {
            int stepsize = pow2 * iteration / 2;
            int fftsize = pow2 * iteration;

            int samplesLen = fftsize;
            double freqPerBin = (double)frequency / samplesLen;
            int len = (int)(TARGET_FREQ / 2 / freqPerBin);
            Variables inParam = new Variables();

            inParam.stepSize = stepsize;
            inParam.decimatedStepSize = len * 2;
            inParam.samplingFrequency = TARGET_FREQ;

            int i;
            float j;
            float decimationFactor = frequency / (float)TARGET_FREQ;
            inParam.decimationIndexies = new int[inParam.decimatedStepSize];
            for (i = 0, j = 0; i < inParam.decimatedStepSize; i++, j += decimationFactor)
            {
                inParam.decimationIndexies[i] = (int)Math.Floor(j);
                Console.WriteLine(Math.Floor(j));
            }
            Console.WriteLine(decimationFactor);

            inParam.inputBuffer = new double[fftsize];
            //inParam.downsampled = new float[stepsize];
            //inParam.decimated = new float[2 * inParam.decimatedStepSize]; //Full 400ms audio container (included overlap)

            //inParam.fft = newTransform(2 * inParam.decimatedStepSize/*, (int)(frequency / stepsize)*/ /*unused*/);
            var normFilCoffs = FIRFilterBuilder.initLowFIRCoff(NCOEFFS, frequency, cutOffFreq);
            inParam.fft = new LomontTransform(pow2, iteration, normFilCoffs, NCOEFFS);
            inParam.window = FIRFilterBuilder.ComputeWindowD(fftsize, WindowType.Hann);
            inParam.melSpectrogram = initMelSpectrogram(NFILT, FREQLOW, FREQHIGH, 2 * inParam.decimatedStepSize, inParam.samplingFrequency, fftsize);

            //var window = FIRFilterBuilder.ComputeWindowF(NCOEFFS, WindowType.Hann);
            //float[] windowedFilCoffs = FIRFilterBuilder.ComputeWindowedResponses(NCOEFFS, normFilCoffs, window);

            //inParam.downsampleFilter = initFIR(stepsize, normFilCoffs);
            //inParam.resampler = new Resample(inParam.downsampled.Length, inParam.decimated.Length, dest_offset: inParam.decimatedStepSize);

            return inParam;
        }
        static float[] ForwardFFT(float[] input, float[] window)
        {
            int inputLength = Nearest2Pow(Nearest2Pow(input.Length) + 1);
            Complex[] cmplx = new Complex[inputLength];
            for (int i = 0; i < input.Length; i++)
                cmplx[i] = new Complex(input[i] * window[i], 0);
            Fourier.Forward(cmplx, FourierOptions.NoScaling);
            float[] power = new float[inputLength];
            for (int i = 0; i < inputLength; i++)
                power[i] = (float)cmplx[i].MagnitudeSquared();
            return power;
        }
        public static void compute(ref Variables memoryPointer, float[] input)
        {
            //var watch = new System.Diagnostics.Stopwatch();
            //watch.Start();
            int i, j;

            //    for (i = 0; i < inParam.stepSize; i++) {
            //        inParam.inputBuffer[i] = input[i];
            //    }
            // Downsample the audio
            //processFIRFilter(ref memoryPointer.downsampleFilter, input, ref memoryPointer.downsampled);
            //memoryPointer.downsampled = memoryPointer.downsampleFilter.process(input);
            //memoryPointer.downsampled = new float[680];
            //process(ref memoryPointer.downsampleFilter, input, ref memoryPointer.downsampled);
            //for (int n = 0; n < memoryPointer.downsampled.Length; n++)
            //    memoryPointer.downsampled[n] = checkRange(memoryPointer.downsampled[n]);

            // Decimate the audio
            for (i = 0; i < memoryPointer.stepSize; i++)
            {
                //j = memoryPointer.decimationIndexies[i];
                memoryPointer.inputBuffer[i] = memoryPointer.inputBuffer[i + memoryPointer.stepSize];
                //memoryPointer.decimated[i + memoryPointer.decimatedStepSize] = memoryPointer.downsampled[j];
            }
            Array.Copy(input, 0, memoryPointer.inputBuffer, memoryPointer.stepSize, memoryPointer.stepSize);
            //Resample.ResampleArray(memoryPointer.downsampled, memoryPointer.downsampled.Length, ref memoryPointer.decimated, memoryPointer.decimated.Length, dest_offset: memoryPointer.decimatedStepSize);
            //memoryPointer.resampler.Compute(memoryPointer.downsampled, ref memoryPointer.decimated);

            //Console.WriteLine("Min: " + errorMargin.Min());
            //FastFFT.FFT(memoryPointer.fft, memoryPointer.decimated.Length, memoryPointer.decimated);
            //Transforms.ForwardFFT(ref memoryPointer.fft, memoryPointer.decimated);
            var fftpower = LomontTransform.GetRealFFTPow(memoryPointer.fft.FFTConvolutionRawLomont(memoryPointer.inputBuffer, memoryPointer.window));
            //memoryPointer.fft.power = ForwardFFT(memoryPointer.decimated, memoryPointer.fft.window);

            updateImage(ref memoryPointer.melSpectrogram, fftpower);
            //watch.Stop();
            //System.Console.WriteLine(watch.ElapsedMilliseconds);
        }

        public static void getMelImage(Variables memoryPointer, ref float[,] melImage)
        {
            for (int i = 0; i < NFILT; i++)
            {
                for (int j = 0; j < NFILT; j++)
                {
                    melImage[i, j] = memoryPointer.melSpectrogram.melSpectrogramImage[i, j];
                }
            }
        }
    }
}
