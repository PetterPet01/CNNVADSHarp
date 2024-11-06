using System;

namespace Pet.CNNVad
{
    public struct MelSpectrogram
    {
        public int nFilt;
        public int nFFT;
        public float freqLow;
        public float freqHigh;
        public int frameSize;
        public int Fs;
        public float[,] filtBank;
        public float[] melPower;
        public float[,] melSpectrogramImage;
    }
    public static class MelSpectr
    {
        public static MelSpectrogram initMelSpectrogram(int nFilt, float freqLow, float freqHigh, int frameSize, int Fs, int nFFT)
        {
            MelSpectrogram melSpectrogram = new MelSpectrogram();

            melSpectrogram.nFilt = nFilt;
            melSpectrogram.nFFT = nFFT;
            melSpectrogram.freqLow = freqLow;
            melSpectrogram.freqHigh = freqHigh;
            melSpectrogram.frameSize = frameSize;
            melSpectrogram.Fs = Fs;

            melSpectrogram.filtBank = buildFilterbank(freqLow, freqHigh, nFilt, nFFT, Fs);
            melSpectrogram.melPower = new float[nFilt];
            melSpectrogram.melSpectrogramImage = new float[nFilt, nFilt];
            return melSpectrogram;
        }

        public static float[,] buildFilterbank(float l, float h, int nFilt, int nFFT, int Fs)
        {
            float lowerMel = (float)(1125 * Math.Log(1 + l / 700));
            float higherMel = (float)(1125 * Math.Log(1 + h / 700));
            //float lowerMel = (float)(2595 * Math.Log10(1 + l / 700));
            //float higherMel = (float)(2595 * Math.Log10(1 + h / 700));
            float diff, melBand, freqBand;
            int[] f = new int[nFilt + 2];
            int rows, columns;

            rows = (int)Math.Floor(nFFT * 0.5d);
            columns = nFilt;
            float[,] filterbank = new float[rows, columns];

            diff = (higherMel - lowerMel) / (nFilt + 1);
            melBand = lowerMel;
            freqBand = (float)(700 * (Math.Exp(melBand / 1125) - 1));
            //freqBand = (float)(700 * (Math.Pow(10, melBand * 0.5595) - 1));
            f[0] = (int)Math.Floor((nFFT + 1) * freqBand / Fs);
            for (int i = 1; i < nFilt + 2; i++)
            {
                melBand = melBand + diff;
                freqBand = (float)(700 * (Math.Exp(melBand / 1125) - 1));
                //freqBand = (float)(700 * (Math.Pow(10, melBand * 0.5595) - 1));
                f[i] = (int)Math.Floor((nFFT + 1) * freqBand / Fs);
            }

            for (int k = 1; k <= rows; k++)
            {
                for (int m = 1; m < columns + 1; m++)
                {

                    if ((k > f[m - 1]) && (k <= f[m]))
                    {
                        filterbank[k - 1, m - 1] = (float)(k - f[m - 1]) / (float)(f[m] - f[m - 1]);
                    }
                    else if ((k > f[m]) && (k <= f[m + 1]))
                    {
                        filterbank[k - 1, m - 1] = (float)(f[m + 1] - k) / (float)(f[m + 1] - f[m]);
                    }
                }
            }
            return filterbank;
        }

        public static void melCalculate(double[] fft, int nFFT, float[,] filterbank, int nFilt, ref float[] melP)
        {
            //  float melMin = -20,//log(FLT_MIN),
            //         melMax = 20;//log(FLT_MAX);
            double sum = 0;
            int nyquistLim = (int)(nFFT * .5f);
            int i, j;
            for (i = 0; i < nFilt; i++)
            {
                for (j = 0; j < nyquistLim; j++)
                {
                    sum += filterbank[j, i] * fft[j]; //Multiplication of Filter Bank Coeffs and Power Spectrum
                }
                melP[i] = (float)Math.Log(sum + 1e-8); //Mean Normalization? I don't recognize this formula
                //var x = sum + 1e-8f;
                //melP[i] = (float)(6 * (x - 1) / (x + 1 + 4 * (Math.Sqrt(x))));
                sum = 0;
            }
        }

        public static void melImageCreate(ref float[,] melSpectrogramImage, float[] melPower, int nFilt)
        {
            int i, j;
            // Shift the 2-d image up
            for (i = 0; i < nFilt - 1; i++)
            {
                for (j = 0; j < nFilt; j++)
                    melSpectrogramImage[i, j] = melSpectrogramImage[i + 1, j];
            }

            for (j = 0; j < nFilt; j++)
            {
                melSpectrogramImage[nFilt - 1, j] = melPower[j];
            }

            //  for (size_t j = 0; j < nFilt - 1; j++) {
            //    for (size_t i = 0; i < nFilt; i++) {
            //      melSpectrogramImage[i][j] = melSpectrogramImage[i][j+1];
            //    }
            //  }
            //  // Add the current mel-spectrogram power to the right most column
            //  for (size_t i = 0; i < nFilt; i++) {
            //    melSpectrogramImage[i][nFilt - 1] = melPower[i];
            //  }
        }

        public static void updateImage(ref MelSpectrogram melSpectrogram, double[] fft)
        {

            melCalculate(fft, melSpectrogram.nFFT, melSpectrogram.filtBank, melSpectrogram.nFilt, ref melSpectrogram.melPower);
            melImageCreate(ref melSpectrogram.melSpectrogramImage, melSpectrogram.melPower, melSpectrogram.nFilt);
        }
    }
}
