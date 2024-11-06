using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pet.Ultilities
{
    public class Resample
    {
        #region  Static Part       
        static float PI = (float)Math.PI;
        static float Lanczos(float x, int Radius)
        {
            if (x == 0.0) return 1.0f;
            if (x <= -Radius || x >= Radius) return 0.0f;
            float pi_x = (x * PI);
            return (float)(Radius * Math.Sin(pi_x) * Math.Sin(pi_x / Radius) / (pi_x * pi_x));
        }

        const int FilterRadius = 3;

        public static void ResampleArray(float[] source, int src_len, ref float[] result, int dest_len, int src_offset = 0, int dest_offset = 0)
        {
            src_len -= src_offset;
            dest_len -= dest_offset;
            float blur = 1.0f;
            float factor = dest_len / (float)src_len;
    
            float scale = Math.Min(factor, 1.0f) / blur;
            float support = FilterRadius / scale;

            float[] contribution = new float[Math.Min(src_len, 5+(int)(2*support))];
            /* 5 = room for rounding up in calculations of start, stop and support */

            if (support <= 0.5f) { support = 0.5f + 1E-12f; scale = 1.0f; }

            for (int x=0; x<dest_len; ++x)
            {
                float center = (x + 0.5f) / factor;
                int start = (int)Math.Max(center - support + 0.5f, (float)0);
                int stop = (int)Math.Min(center + support + 0.5f, (float)src_len);
                float density = 0.0f;
                int nmax = stop - start;
                float s = start - center + 0.5f;
                result[x + dest_offset] = 0;
                for (int n=0; n<nmax; ++n, ++s)
                {
                contribution[n] = Lanczos(s* scale, FilterRadius);
                density += contribution[n];
                result[x + dest_offset] += source[start + n + src_offset]* contribution[n];
                }
                if (density != 0.0 && density != 1.0)
                    /* Normalize. */
                    result[x + dest_offset] /= density;
            }
        }
        #endregion

        float[,] contribution;
        int[] nmax, start;
        float[] density;
        int src_offset;
        int dest_offset;
        int src_len;
        int dest_len;

        public Resample(int src_len, int dest_len, int src_offset = 0, int dest_offset = 0)
        {
            src_len -= src_offset;
            dest_len -= dest_offset;
            float blur = 1.0f;
            float factor = dest_len / (float)src_len;

            float scale = Math.Min(factor, 1.0f) / blur;
            float support = FilterRadius / scale;

            contribution = new float[dest_len, Math.Min(src_len, 5 + (int)(2 * support))];
            nmax = new int[dest_len];
            density = new float[dest_len];
            start = new int[dest_len];
            this.src_len = src_len;
            this.dest_len = dest_len;
            this.src_offset = src_offset;
            this.dest_offset = dest_offset;
            /* 5 = room for rounding up in calculations of start, stop and support */

            if (support <= 0.5f) { support = 0.5f + 1E-12f; scale = 1.0f; }

            for (int x = 0; x < dest_len; ++x)
            {
                float center = (x + 0.5f) / factor;
                start[x] = (int)Math.Max(center - support + 0.5f, (float)0);
                int stop = (int)Math.Min(center + support + 0.5f, (float)src_len);
                nmax[x] = stop - start[x];
                float s = start[x] - center + 0.5f;
                //result[x + dest_offset] = 0;
                for (int n = 0; n < nmax[x]; ++n, ++s)
                {
                    contribution[x, n] = Lanczos(s * scale, FilterRadius);
                    density[x] += contribution[x, n];
                    //result[x + dest_offset] += source[start + n + src_offset] * contribution[n];
                }

                //TODO: Reverse division to multiplication
                //if (density != 0.0 && density != 1.0)
                    /* Normalize. */
                    //result[x + dest_offset] /= density;
            }
        }
        public void Compute2(float[] source, ref float[] result)
        {
            src_len -= src_offset;
            dest_len -= dest_offset;
            float blur = 1.0f;
            float factor = dest_len / (float)src_len;

            float scale = Math.Min(factor, 1.0f) / blur;
            float support = FilterRadius / scale;

            float[] contribution = new float[Math.Min(src_len, 5 + (int)(2 * support))];
            /* 5 = room for rounding up in calculations of start, stop and support */

            if (support <= 0.5f) { support = 0.5f + 1E-12f; scale = 1.0f; }

            for (int x = 0; x < dest_len; ++x)
            {
                float center = (x + 0.5f) / factor;
                int start = (int)Math.Max(center - support + 0.5f, (float)0);
                int stop = (int)Math.Min(center + support + 0.5f, (float)src_len);
                float density = 0.0f;
                int nmax = stop - start;
                float s = start - center + 0.5f;
                result[x + dest_offset] = 0;
                for (int n = 0; n < nmax; ++n, ++s)
                {
                    contribution[n] = Lanczos(s * scale, FilterRadius);
                    density += contribution[n];
                    result[x + dest_offset] += source[start + n + src_offset] * contribution[n];
                }
                if (density != 0.0 && density != 1.0)
                    /* Normalize. */
                    result[x + dest_offset] /= density;
            }
        }
        public void Compute(float[] source, ref float[] result)
        {
            for (int x = 0; x < dest_len; ++x)
            {
                for (int n = 0; n < nmax[x]; ++n)
                    result[x + dest_offset] += source[start[x] + n + src_offset] * contribution[x, n];
                if (density[x] != 0.0 && density[x] != 1.0)
                    /* Normalize. */
                    result[x + dest_offset] /= density[x];
            }
        }
    }
}
