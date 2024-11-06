using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pet.Ultilities;

namespace Pet.CNNVad
{
    public class Convolutor
    {
        Transform fft1;
        Transform fft2;
        Transform fftProduct;
        static void ZeroPad(float[] src, ref float[] dest, int padLength)
        {
            dest = new float[src.Length + padLength];
            Buffer.BlockCopy(src, 0, dest, 0, src.Length * sizeof(float));
        }
        static int Nearest2Pow(int num)
        {
            int pow2Size = 0x01;
            while (pow2Size < num)
                pow2Size = pow2Size << 1;
            return pow2Size;
        }
        int padLen1;
        int padLen2;
        int len1;
        int len2;
        public Convolutor(int firstLen, int secondLen)
        {
            int length = Math.Max(Nearest2Pow(firstLen), Nearest2Pow(secondLen));
            Console.WriteLine(length);
            fft1 = Transforms.newTransform(length, false);
            fft2 = Transforms.newTransform(length, false);
            fftProduct = Transforms.newTransform(length, false);
            len1 = firstLen;
            len2 = secondLen;
            padLen1 = length - firstLen;
            padLen2 = length - secondLen;
        }
        public float[] Convolve(float[] first, float[] second)
        {
            ZeroPad(first, ref first, padLen1);
            ZeroPad(second, ref second, padLen2);
            Transforms.ForwardFFT(ref fft1, first);
            Transforms.ForwardFFT(ref fft2, second); //TODO: Static initialization at start
            Transforms.Multiply(fft1, fft2, ref fftProduct);
            Transforms.InverseFFT(ref fftProduct);
            return fftProduct.real.SubArray(0, len1 + len2 - 1);
        }
    }
}
