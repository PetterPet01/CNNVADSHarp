using System;
using Lomont;

namespace Pet.CNNVad
{
    public class LomontTransform
    {
        LomontFFT fft = new LomontFFT();

        #region mixed-Radix Stuff
        double[] independentCmplx;
        int independentCmplxLen, pow2, iteration;
        double[] rowTemp;
        double[] columnTemp;
        double[][] U;
        double[,] cos;
        double[,] sin;
        #endregion

        #region Convolution Stuff
        double[] filterAgg;
        int filterLength;
        int maxLength; //Math.Max between filter and fft size
        #endregion

        bool enableMixedRadix = false;
        public LomontTransform(int pow2, int iteration, double[] filter, int filterLength)
        {
            int fftLength = pow2 * iteration;
            this.filterLength = filterLength;
            this.pow2 = pow2;
            this.iteration = iteration;
            independentCmplx = new double[pow2 - 2];
            rowTemp = new double[pow2];
            columnTemp = new double[iteration * 2];
            cos = new double[iteration, pow2];
            sin = new double[iteration, pow2];
            maxLength = Math.Max(fftLength, filterLength);
            fft.A = 1;
            fft.B = -1;
            if (iteration == 1)
            {
                this.pow2 = pow2;
            }
            else
            {
                enableMixedRadix = true;
                U = new double[iteration][];
                rowTemp = new double[pow2];
                columnTemp = new double[iteration * 2];

                for (int m = 0; m < iteration; m++)
                {
                    U[m] = new double[pow2 * 2];
                    for (int n = 0; n < pow2; n += 1)
                    {
                        double theta = 2 * Math.PI * m * n / fftLength;
                        cos[m, n] = Math.Cos(theta);
                        sin[m, n] = Math.Sin(theta);
                    }
                }
                this.pow2 = pow2;
                this.iteration = iteration;
                independentCmplxLen = pow2 - 2;
            }
            var filterCopy = new double[maxLength];
            Array.Copy(filter, 0, filterCopy, 0, filter.Length);
            if (iteration == 1)
                fft.RealFFT(filterCopy, true);
            else
                filterCopy = LomontRepFFT(filterCopy);
            double[] filterCopyAgg = new double[maxLength];
            for (int i = 0; i < maxLength / 2; i += 2)
            {
                filterCopyAgg[i] = filterCopy[i];
                filterCopyAgg[i + 1] = -filterCopy[i + 1];
            }
            filterAgg = new double[maxLength];
            MultiplyRealFFT(filterCopy, filterCopyAgg, ref filterAgg);
        }
        double[] LomontRepFFT(double[] input)
        {
            fft.A = 1;
            fft.B = -1;
            int m, n, P;
            P = pow2 * iteration;
            //double[][] U = new double[iteration][];
            //var temp = new double[pow2];
            //int independentCmplxLen = pow2 - 2;
            //double[] independentCmplx = new double[independentCmplxLen];
            for (m = 0; m < iteration; m++)
            {
                //U[m] = new double[pow2 * 2];
                for (n = 0; n < pow2; n++)
                    rowTemp[n] = input[iteration * n + m];
                fft.RealFFT(rowTemp, true);

                Array.Copy(rowTemp, 2, independentCmplx, 0, independentCmplxLen);
                Array.Copy(independentCmplx, 0, U[m], 2, independentCmplxLen);
                for (n = 2; n < independentCmplxLen + 2; n += 2) //offset 2 for the last complex in "temp"
                {
                    U[m][pow2 + n] = independentCmplx[independentCmplxLen - n]; //real
                    U[m][pow2 + n + 1] = -independentCmplx[independentCmplxLen - n + 1]; //conjugate imaginary
                }
                U[m][0] = rowTemp[0];
                U[m][pow2] = rowTemp[1];
                for (n = 0; n < pow2; n += 1)
                {
                    double cos = this.cos[m, n];
                    double sin = this.sin[m, n];
                    var or = U[m][n * 2];
                    var oi = U[m][n * 2 + 1];
                    U[m][n * 2] = or * cos + oi * sin;
                    U[m][n * 2 + 1] = -or * sin + oi * cos;
                }
            }
            double[] fullResult = new double[P * 2];
            //double[] temp2 = new double[iteration * 2];
            for (n = 0; n < pow2 * 2; n += 2)
            {
                for (m = 0; m < iteration; m++)
                {
                    columnTemp[m * 2] = U[m][n];
                    columnTemp[m * 2 + 1] = U[m][n + 1];
                }
                fft.FFT(columnTemp, true);
                for (m = 0; m < iteration; m++)
                {
                    fullResult[n + pow2 * 2 * m] = columnTemp[m * 2];
                    fullResult[n + pow2 * 2 * m + 1] = columnTemp[m * 2 + 1];
                }
            }
            double[] optimalResult = new double[P];
            optimalResult[0] = fullResult[0];
            optimalResult[1] = fullResult[P];
            Array.Copy(fullResult, 2, optimalResult, 2, P - 2);
            return optimalResult;
        }
        public double[] FFTConvolutionRawLomont(double[] input, double[] window = null)
        {
            //fft.A = 1;
            //fft.B = -1;
            double[] inputc = new double[maxLength];
            Array.Copy(input, 0, inputc, 0, input.Length);
            for (int i = 0; i < window.Length; i++)
                inputc[i] *= window[i];
            if (iteration == 1)
                fft.RealFFT(inputc, true);
            else
                inputc = LomontRepFFT(inputc);
            double[] product = new double[maxLength];
            MultiplyRealFFT(inputc, filterAgg, ref product);
            return product;
        }
        public static void MultiplyRealFFT(double[] x, double[] y, ref double[] result)
        {
            if (x.Length != y.Length) throw new Exception("Must be of equal sizes");
            result[0] = x[0] * y[0];
            //result[1] = 0; redundant
            for (int i = 2; i < x.Length; i += 2)
            {
                result[i] = (x[i] * y[i]) - (x[i + 1] * y[i + 1]);
                result[i + 1] = (x[i + 1] * y[i]) + x[i] * y[i + 1];
            }
        }
        public static double[] GetRealFFTPow(double[] realFFT)
        {
            int length = realFFT.Length;
            double[] result = new double[length / 2];
            result[0] = Math.Abs(realFFT[0]);
            for (int i = 2; i < length; i += 2)
            {
                result[i / 2] = realFFT[i] * realFFT[i] + realFFT[i + 1] * realFFT[i + 1];
            }
            return result;
        }
        public static double[] FreqTruncateLomont(int oriFs, int desiredFs, double[] fft)
        {
            int samplesLen = fft.Length;
            double freqPerBin = (double)oriFs / samplesLen;
            int len = (int)(desiredFs / 2 / freqPerBin);
            int inext = samplesLen - len;
            double[] result = new double[len * 2];
            Array.Copy(fft, 0, result, 0, len * 2);
            return result;
        }
    }
}
