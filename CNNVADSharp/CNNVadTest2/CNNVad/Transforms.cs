using System;

namespace Pet.CNNVad
{
    public struct Transform : IDisposable
    {
        public int points;
        public int windowSize;
        public float[] real;
        public float[] imaginary;
        public float[] power;
        public float[] sine;
        public float[] cosine;
        public float[] window;
        //-unused-
        //public float totalPower;
        //public float dBSPL;
        //public float dbpower;
        //public float[] dbpowerBuffer;
        //public int framesPerSecond;

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }       
    }
    public static class Transforms
    {
        static float P_REF = -93.9794f;
        public static Transform newTransform(int window/*, int framesPerSecond*/ /*unused*/, bool hasWindow = true)
        {
            Transform newTransform = new Transform();

            newTransform.windowSize = window;
            //newTransform.framesPerSecond = framesPerSecond; //unused

            int pow2Size = 0x01;
            while (pow2Size < window)
                pow2Size = pow2Size << 1;
            newTransform.points = pow2Size;

            newTransform.real = new float[pow2Size];
            newTransform.imaginary = new float[pow2Size];
            newTransform.power = new float[pow2Size];
            newTransform.sine = new float[pow2Size / 2];
            newTransform.cosine = new float[pow2Size / 2];
            //-unused-
            //newTransform.dbpowerBuffer = new float[framesPerSecond];
            //newTransform.dbpower = 0;


            //precompute twiddle factors
            double arg;
            int i;
            for (i = 0; i < pow2Size / 2; i++)
            {
                arg = (-2.0 * Math.PI * i) / pow2Size;
                newTransform.cosine[i] = (float)Math.Cos(arg);
                newTransform.sine[i] = (float)Math.Sin(arg);
            }

            newTransform.window = new float[pow2Size];
            for (i = 0; i < window; i++)
            {
                if (hasWindow)
                    //Hanning
                    newTransform.window[i] = (float)((1.0 - Math.Cos(2.0 * Math.PI * (i + 1) / (window + 1))) * 0.5);
                else
                    newTransform.window[i] = 1f;
            }

            for (i = window; i < pow2Size; i++)
            {
                newTransform.window[i] = 0;
            }

            return newTransform;
        }
        public static void Multiply(Transform x, Transform y, ref Transform result)
        {
            if (x.real.Length != y.real.Length) throw new Exception("Must be of equal sizes");
            for (int i = 0; i < x.real.Length; i++)
            {
                result.real[i] = (x.real[i] * y.real[i]) - (x.imaginary[i] * y.imaginary[i]);
                result.imaginary[i] = (x.imaginary[i] * y.real[i]) + x.real[i] * y.imaginary[i];
            }
        }
        public static void ForwardFFT(ref Transform fft, float[] realInput)
        {
            int i, j, k, L, m, n, o, p, q;
            float tempReal, tempImaginary, cos, sin, xt, yt, temp;
            k = fft.points;
            //-unused-
            //fft.totalPower = 0;

            for (i = 0; i < k; i++)
            {
                fft.real[i] = 0;
                fft.imaginary[i] = 0;
            }

            for (i = 0; i < realInput.Length; i++)
            {
                //Windowing
                fft.real[i] = realInput[i] * fft.window[i];
            }

            j = 0;
            m = k / 2;

            //bit reversal
            for (i = 1; i < (k - 1); i++)
            {
                L = m;

                while (j >= L)
                {
                    j = j - L;
                    L = L / 2;
                }

                j = j + L;

                if (i < j)
                {
                    tempReal = fft.real[i];
                    tempImaginary = fft.imaginary[i];
                    fft.real[i] = fft.real[j];
                    fft.imaginary[i] = fft.imaginary[j];
                    fft.real[j] = tempReal;
                    fft.imaginary[j] = tempImaginary;
                }
            }

            L = 0;
            m = 1;
            n = k / 2;

            //computation
            for (i = k; i > 1; i = (i >> 1))
            {
                L = m;
                m = 2 * m;
                o = 0;

                for (j = 0; j < L; j++)
                {
                    cos = fft.cosine[o];
                    sin = fft.sine[o];
                    o = o + n;

                    for (p = j; p < k; p = p + m)
                    {
                        q = p + L;

                        xt = cos * fft.real[q] - sin * fft.imaginary[q];
                        yt = sin * fft.real[q] + cos * fft.imaginary[q];
                        fft.real[q] = (fft.real[p] - xt);
                        fft.real[p] = (fft.real[p] + xt);
                        fft.imaginary[q] = (fft.imaginary[p] - yt);
                        fft.imaginary[p] = (fft.imaginary[p] + yt);
                    }
                }
                n = n >> 1;
            }

            for (i = 0; i < k; i++)
            {
                fft.power[i] = (fft.real[i] * fft.real[i] + fft.imaginary[i] * fft.imaginary[i]);
                //-unused-
                //fft.totalPower += fft.power[i] / k;
            }
            //-unused-
            //fft.dBSPL = (float)(10 * Math.Log10(fft.totalPower + 1e-6) - P_REF);
            //temp = fft.dBSPL;
            //fft.dbpower = fft.dbpower + (temp - fft.dbpowerBuffer[0]) / fft.framesPerSecond;
            //memmove(fft.dbpowerBuffer, fft.dbpowerBuffer + 1, sizeof(*fft.dbpowerBuffer) * (fft.framesPerSecond - 1));
            //fft.dbpowerBuffer[fft.framesPerSecond - 1] = temp;
        }

        public static void InverseFFT(ref Transform fft)
        {
            int i, j, k, L, m, n, o, p, q;
            float tempReal, tempImaginary, cos, sin, xt, yt;
            k = fft.points;

            j = 0;
            m = k / 2;

            //bit reversal
            for (i = 1; i < (k - 1); i++)
            {
                L = m;

                while (j >= L)
                {
                    j = j - L;
                    L = L / 2;
                }

                j = j + L;

                if (i < j)
                {
                    tempReal = fft.real[i];
                    tempImaginary = fft.imaginary[i];
                    fft.real[i] = fft.real[j];
                    fft.imaginary[i] = fft.imaginary[j];
                    fft.real[j] = tempReal;
                    fft.imaginary[j] = tempImaginary;
                }
            }

            L = 0;
            m = 1;
            n = k / 2;

            //computation
            for (i = k; i > 1; i = (i >> 1))
            {
                L = m;
                m = 2 * m;
                o = 0;

                for (j = 0; j < L; j++)
                {
                    cos = fft.cosine[o];
                    sin = -fft.sine[o];
                    o = o + n;

                    for (p = j; p < k; p = p + m)
                    {
                        q = p + L;

                        xt = cos * fft.real[q] - sin * fft.imaginary[q];
                        yt = sin * fft.real[q] + cos * fft.imaginary[q];
                        fft.real[q] = (fft.real[p] - xt);
                        fft.real[p] = (fft.real[p] + xt);
                        fft.imaginary[q] = (fft.imaginary[p] - yt);
                        fft.imaginary[p] = (fft.imaginary[p] + yt);
                    }
                }
                n = n >> 1;
            }

            for (i = 0; i < k; i++)
            {
                fft.real[i] /= k;
            }
        }

        /// <summary>
        /// Convolve two arrays of number using FFT (result.length must be equal to fft1.Length + fft2.Length - 1)
        /// </summary>
        /// <param name="fft1">The first array's transform</param>
        /// <param name="fft2">The second array's transform</param>
        /// <param name="result">The convolved array</param>
        public static void FFTConvolution(ref Transform fft1, ref Transform fft2, ref float[] result)
        {
            ForwardFFT(ref fft1, fft1.real);
        }
    }
}
