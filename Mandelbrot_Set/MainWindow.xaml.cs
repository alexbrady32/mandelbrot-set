using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;

namespace Mandelbrot_Set
{
    
    public partial class MainWindow : Window
    {

        // Mandelbrot Set Generation code taken from 
        // http://rosettacode.org/wiki/Mandelbrot_set -> C# section
        public class MandelbrotSetCreation
        {
            const double bound = 2.0;

            static double CalculatePixelColor(ComplexNumber c)
            {
                const int MaxIterations = 500;
                const double MaxNorm = bound * bound;

                int iteration = 0;
                ComplexNumber z = new ComplexNumber();
                do
                {
                    z = z * z + c;
                    iteration++;
                } while (z.Norm() < MaxNorm && iteration < MaxIterations);
                if (iteration < MaxIterations)
                {
                    return (double)iteration / MaxIterations;
                }
                else
                {
                    return 0;
                }
            }

            static public void GenerateBitMap(byte[] buffer, int startX, int startY, int endx, int endy, int width, int height, int depth)
            {
                double scale = 1.25 * bound / Math.Min(width, height);
                
                int bufSize = (endx - startX) * (endy - startY) * depth;
                int threadHeight = (endy - startY);
                int threadWidth = (endx - startX);
                int widthInBytes = threadWidth * 4;
                int startingPosition = startY * widthInBytes + startX * 4;
                for (int i = startingPosition; i < startingPosition + bufSize ; i += 4)
                {

                    double y = (height / 2  - Math.Floor((double) i / widthInBytes)) * scale;
                    double x = (((i / 4) % threadWidth) - threadWidth / 2) * scale;
                    double colorValue = CalculatePixelColor(new ComplexNumber(x, y));
                    System.Drawing.Color color = GetColor(colorValue);
                    buffer[i + 0] = color.B;
                    buffer[i + 1] = color.G;
                    buffer[i + 2] = color.R;
                    buffer[i + 3] = 255;
                    
                }


            }


            static System.Drawing.Color GetColor(double value)
            {
                const double MaxColor = 256;
                const double ContrastValue = 0.2;
                return System.Drawing.Color.FromArgb((int)(MaxColor * Math.Pow(value, ContrastValue)), 0, 0);

            }
        }



        struct ComplexNumber
        {
            public double real;
            public double imaginary;

            public ComplexNumber(double r, double i)
            {
                real = r;
                imaginary = i;

            }

            public static ComplexNumber operator +(ComplexNumber x, ComplexNumber y)
            {
                return new ComplexNumber(x.real + y.real, x.imaginary + y.imaginary);
            }

            public static ComplexNumber operator *(ComplexNumber x, ComplexNumber y)
            {
                return new ComplexNumber(x.real * y.real - x.imaginary * y.imaginary,
                    x.real * y.imaginary + x.imaginary * y.real);
            }

            public double Norm()
            {
                return real * real + imaginary * imaginary;
            }
        }

        // Taken from http://stackoverflow.com/questions/6484357/converting-bitmapimage-to-bitmap-and-vice-versa
        private BitmapSource Bitmap2BitmapImage(System.Drawing.Bitmap bitmap)
        {
            IntPtr hBitmap = bitmap.GetHbitmap();
            BitmapSource retval;

            retval = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());


            return retval;
        }

        public MainWindow()
        {
            InitializeComponent();

            this.Height = SystemParameters.PrimaryScreenHeight;
            this.Width = SystemParameters.PrimaryScreenWidth;
            image.Height = this.Height;
            image.Width = this.Width;

            
        }

        private void generateButton_Click(object sender, RoutedEventArgs e)
        {

            Stopwatch stopwatch = new Stopwatch();

            // Some code and ideas taken from http://stackoverflow.com/questions/21497537/allow-an-image-to-be-accessed-by-several-threads

            System.Drawing.Bitmap btm = new System.Drawing.Bitmap((int)image.Width, (int)image.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            System.Drawing.Rectangle r1 = new System.Drawing.Rectangle(0, 0, btm.Width, btm.Height);
            var data = btm.LockBits(r1, System.Drawing.Imaging.ImageLockMode.ReadWrite, btm.PixelFormat);
            var depth = System.Drawing.Bitmap.GetPixelFormatSize(data.PixelFormat) / 8;

            var buffer = new byte[data.Width * data.Height * depth];

            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
            

            int numThreads = int.Parse(numberOfThreads.Text);
            stopwatch.Start();
            // Parallel for taken from http://tipsandtricks.runicsoft.com/CSharp/ParallelClass.html
            Parallel.For(0, numThreads, i => MandelbrotSetCreation.GenerateBitMap(buffer, 0, (data.Height / numThreads) * i, data.Width, (data.Height / numThreads) * (i + 1), data.Width, data.Height, depth));
            stopwatch.Stop();
            //Copy the buffer back to image
            System.Runtime.InteropServices.Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);

            btm.UnlockBits(data);


            image.Source = Bitmap2BitmapImage(btm);
            timeElapsed.Content = stopwatch.Elapsed.TotalMilliseconds.ToString() + " ms";
        }
    }

    
}
