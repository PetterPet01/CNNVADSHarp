using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pet.CNNVad;
using System.Numerics;

namespace Pet.Ultilities
{
    //Unused, too slow and inefficient
    public struct SComplex
    {
        public float Real;
        public float Imaginary;
        public float power;
        public SComplex(float real, float imaginary)
        {
            this.Real = real;
            this.Imaginary = imaginary;
            this.power = 0f;
        }
        public static SComplex Pow(SComplex val, int power)
        {
            if (power == 0)
                return new SComplex(1, 0);
            else if (power == 1)
                return val;
            SComplex part = Pow(val, power / 2);
            if (power % 2 == 0)
                return part * part;
            return val * (part * part);
        }
        public static SComplex operator *(SComplex x, SComplex y)
        {
            return new SComplex((x.Real * y.Real) - (x.Imaginary * y.Imaginary), (x.Imaginary * y.Real) + x.Real * y.Imaginary);
        }
        public static SComplex operator *(SComplex x, float y)
        {
            return new SComplex((x.Real * y) - (x.Imaginary * y), (x.Imaginary * y) + x.Real * y);
        }
        public static SComplex operator +(SComplex x, SComplex y)
        {
            return new SComplex((x.Real + y.Real), (x.Imaginary + y.Imaginary));

        }

        public static SComplex operator -(SComplex x, SComplex y)
        {
            return new SComplex((x.Real - y.Real), (x.Imaginary - y.Imaginary));
        }
        public static SComplex fromPolar(float magnitude, float phase)
        {
            //TODO: Could try a faster Cos / Sin algorithm?
            return new SComplex((magnitude * (float)Math.Cos(phase)), (magnitude * (float)Math.Sin(phase)));
        }
    }
    public static class FastFFT
    {
        static float M_PI = 3.1415926535897932384f;

        static int log2(int N)    /*function to calculate the log2(.) of int numbers*/
        {
            int k = N, i = 0;
            while (k != 0)
            {
                k >>= 1;
                i++;
            }
            return i - 1;
        }

        static bool check(int n)    //checking if the number of element is a power of 2
        {
            return n > 0 && (n & (n - 1)) == 0;
        }

        static int reverse(int N, int n)    //calculating revers number
        {
            int j, p = 0;
            for (j = 1; j <= log2(N); j++)
            {
                if ((n & (1 << (log2(N) - j))) != 0)
                    p |= 1 << (j - 1);
            }
            return p;
        }

        static void ordina(SComplex[] f1, int N) //using the reverse order in the array
        {
            SComplex[] f2 = new SComplex[N];
            for (int i = 0; i < N; i++)
                f2[i] = f1[reverse(N, i)];
            for (int j = 0; j < N; j++)
                f1[j] = f2[j];
        }

        static void transform(SComplex[] f, int N) //
        {
            ordina(f, N);    //first: reverse order
            SComplex[] W;
            W = new SComplex[N / 2];
            W[1] = SComplex.fromPolar(1.0f, -2.0f * M_PI / N);
            W[0] = new SComplex(1, 0);
            for (int i = 2; i < N / 2; i++)
                W[i] = SComplex.Pow(W[1], i);
            int n = 1;
            int a = N / 2;
            for (int j = 0; j < log2(N); j++)
            {
                for (int i = 0; i < N; i++)
                {
                    if ((i & n) == 0)
                    {
                        SComplex temp = f[i];
                        SComplex Temp = W[(i * a) % (n * a)] * f[i + n];
                        f[i] = temp + Temp;
                        f[i + n] = temp - Temp;
                    }
                }
                n *= 2;
                a = a / 2;
            }
        }

        public static void FFT(SComplex[] f, int N, float d)
        {
            transform(f, N);
            for (int i = 0; i < N; i++)
                f[i] *= d; //multiplying by step
        }
    }
}
