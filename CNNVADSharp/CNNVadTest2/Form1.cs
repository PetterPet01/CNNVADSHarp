using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Pet.CNNVad;
using Pet.Ultilities;
using TensorFlow;
using NAudio.Dsp;

namespace CNNVadTest2
{
    using static Pet.Ultilities.Ultilities;
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        AudioRecorder vad = new AudioRecorder();
        TFSession model;

        TFGraph graph = new TFGraph();

        string inputOpName = "inputs/x-input";
        string outputOpName = "model/Softmax";
        static int length = (int)Math.Pow(2, 12);
        static Random rand = new Random();
        float[] fakeDat = new float[length];
        Transform transform = Transforms.newTransform(length);
        private void Form1_Load(object sender, EventArgs e)
        {
            graph.Import(File.ReadAllBytes("E:\\frozen_without_dropout.pb"));
            model = new TFSession(graph);         
            
            for(int i = 0; i < length; i++)
            {
                fakeDat[i] = (float)rand.NextDouble();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            vad.start("E:\\frozen_without_dropout.pb", 48000);
            //timer1.Start();
        }
        public static Bitmap float2Image(float[,] data, float scaleFactor = 1)
        {
            int rowLength = data.GetLength(0);
            int colLength = data.GetLength(1);
            // create a bitmap we will work with
            Bitmap bitmap = new Bitmap(colLength, rowLength, PixelFormat.Format8bppIndexed);

            // modify the indexed palette to make it grayscale
            ColorPalette pal = bitmap.Palette;
            for (int i = 0; i < 256; i++)
                pal.Entries[i] = Color.FromArgb(255, i, i, i);
            bitmap.Palette = pal;

            // prepare to access data via the bitmapdata object
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                                    ImageLockMode.ReadOnly, bitmap.PixelFormat);

            // create a byte array to reflect each pixel in the image
            byte[] pixels = new byte[bitmapData.Stride * bitmap.Height];

            // fill pixel array with data
            for (int col = 0; col < colLength; col++)
            {

                for (int row = 0; row < rowLength; row++)
                {
                    int bytePosition = row * bitmapData.Stride + col;
                    double pixelVal = data[col, row] * scaleFactor;
                    pixelVal = Math.Max(0, pixelVal);
                    pixelVal = Math.Min(255, pixelVal);
                    pixels[bytePosition] = (byte)(pixelVal);
                }
            }

            // turn the byte array back into a bitmap
            Marshal.Copy(pixels, 0, bitmapData.Scan0, pixels.Length);
            bitmap.UnlockBits(bitmapData);

            return bitmap;
        }
        Bitmap nearestNeighborScaling(Bitmap bmp, float scaleFactor = 1f)
        {
            Size sz = bmp.Size;
            Bitmap zoomed;

            zoomed = new Bitmap((int)(sz.Width * scaleFactor), (int)(sz.Height * scaleFactor));

            using (Graphics g = Graphics.FromImage(zoomed))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.DrawImage(bmp, new Rectangle(Point.Empty, zoomed.Size));
            }
            return zoomed;
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            //vad.processAudio();
            var oribmp = float2Image(vad.memoryPointer.melSpectrogram.melSpectrogramImage, 75);
            pictureBox1.Image = nearestNeighborScaling(oribmp, pictureBox1.Height / 40);
            //pictureBox1.Image = oribmp;
            button1.Text = vad.isSpeaking.ToString();
        }
        private void timer2_Tick(object sender, EventArgs e)
        {

            //Pet.Ultilities.FastFFT.FFT(cmplx, length, 1);
            Pet.CNNVad.Transforms.ForwardFFT(ref transform, fakeDat);
            this.Text = "yep";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //vad.waveWriter.Dispose();
            //vad.waveWriter = null;
        }
        
        private void timer3_Tick(object sender, EventArgs e)
        {
            float[,] input = new float[40, 40];

            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            var tensor = new TFTensor(input);
            TFSession.Runner runner = model.GetRunner();

            runner.AddInput(inputOpName, tensor);
            runner.Fetch(outputOpName);
            var output = runner.Run();
            this.Text = watch.ElapsedMilliseconds.ToString();
            watch.Stop();
        }
        
        private void button3_Click(object sender, EventArgs e)
        {
            timer3.Start();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var coffs = FIRFilterBuilder.initLowFIRCoff(81, 48000, 8000);
            foreach (float f in coffs)
                Console.WriteLine(f);
        }
    }
}
