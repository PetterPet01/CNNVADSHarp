using System;

namespace Pet.CNNVad
{
    using static Pet.Ultilities.Ultilities;
    public struct FIR : IDisposable
    {

        public int N;

        public float[] inputBuffer;

        public float[] filCoffs;

        public Convolutor convolutor;
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
    public static class FIRFilter
    {
        public static float checkRange(float input)
        {
            float output;
            if (input > 1.0)
            {
                output = 1.0f;
            }
            else if (input < -1.0)
            {
                output = -1.0f;
            }
            else
            {
                output = input;
            }

            return output;
        }
        
        public static FIR initFIR(int stepSize, float[] fillCoffs)
        {

            FIR fir = new FIR();

            fir.N = stepSize;

            fir.inputBuffer = new float[2 * stepSize];

            fir.filCoffs = fillCoffs;

            fir.convolutor = new Convolutor(stepSize + fillCoffs.Length, fillCoffs.Length);
            return fir;

        }
        public static float[] Convolve(float[] u, float[] v)
        {
            int m = u.Length;
            int n = v.Length;
            int k = m + n - 1;
            var w = new float[k];
            //for (int i = 0; i < k; i++)
            //    for (int j = 0; j < n; j++)
            //        if (i - j >= 0 && i - j < m)
            //            w[i] += u[i - j] * v[j];
            for (var i = 0; i < m; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    w[i + j] += u[i] * v[j];
                }
            }
            return w;
        }
        public static void process(ref FIR fir, float[] input, ref float[] output)
        {
            int bufferLength = fir.N;
            int coffsLength = fir.filCoffs.Length;

            int i;
            for (i = 0; i < bufferLength; i++)
            {
                fir.inputBuffer[i] = fir.inputBuffer[bufferLength + i];
                fir.inputBuffer[bufferLength + i] = input[i];
            }
            var convolved = Convolve(fir.inputBuffer.SubArray(bufferLength - (coffsLength), bufferLength), fir.filCoffs);
            for (i = coffsLength - 1; i < bufferLength + coffsLength - 1; i++)
                output[i - (coffsLength - 1)] = checkRange(convolved[i]);
        }
        public static void processFIRFilter(ref FIR fir, float[] input, ref float[] output)
        {
            int bufferLength = fir.N;
            int coffsLength = fir.filCoffs.Length;

            int i, j, idx;
            float temp;

            for (i = 0; i < bufferLength; i++)
            {
                fir.inputBuffer[i] = fir.inputBuffer[bufferLength + i];
                fir.inputBuffer[bufferLength + i] = input[i];
            }

            for (i = 0; i < bufferLength; i++)
            {
                temp = 0;

                for (j = 0; j < coffsLength; j++)
                {
                    idx = bufferLength + (i - j);
                    //if (idx >= bufferLength & idx < 2 * bufferLength - coffsLength - 1)
                        temp += (fir.inputBuffer[idx] * fir.filCoffs[j]);
                }
                output[i] = checkRange(temp);
            }
        }
    }
}
