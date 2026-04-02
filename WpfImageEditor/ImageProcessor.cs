using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfImageEditor
{
    public static class ImageProcessor
    {
        public static byte[] GetPixels(BitmapSource src, out int stride, out int width, out int height)
        {
            width = src.PixelWidth;
            height = src.PixelHeight;
            stride = width * 4;
            var pixels = new byte[stride * height];
            src.CopyPixels(pixels, stride, 0);
            return pixels;
        }

        public static WriteableBitmap FromPixels(byte[] pixels, int width, int height)
        {
            var wb = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            wb.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
            return wb;
        }

        private static BitmapSource EnsureBgra32(BitmapSource src)
        {
            if (src.Format == PixelFormats.Bgra32) return src;
            return new FormatConvertedBitmap(src, PixelFormats.Bgra32, null, 0);
        }

        private static byte Clamp(int v) => (byte)Math.Clamp(v, 0, 255);

        public static WriteableBitmap ToGrayscale(BitmapSource src)
        {
            src = EnsureBgra32(src);
            var p = GetPixels(src, out int stride, out int w, out int h);
            for (int i = 0; i < p.Length; i += 4)
            {
                byte gray = Clamp((int)(0.299 * p[i + 2] + 0.587 * p[i + 1] + 0.114 * p[i]));
                p[i] = p[i + 1] = p[i + 2] = gray;
            }
            return FromPixels(p, w, h);
        }

        public static WriteableBitmap AdjustBrightness(BitmapSource src, int delta)
        {
            src = EnsureBgra32(src);
            var p = GetPixels(src, out _, out int w, out int h);
            for (int i = 0; i < p.Length; i += 4)
            {
                p[i]     = Clamp(p[i]     + delta);
                p[i + 1] = Clamp(p[i + 1] + delta);
                p[i + 2] = Clamp(p[i + 2] + delta);
            }
            return FromPixels(p, w, h);
        }

        public static WriteableBitmap AdjustContrast(BitmapSource src, double factor)
        {
            src = EnsureBgra32(src);
            var p = GetPixels(src, out _, out int w, out int h);
            for (int i = 0; i < p.Length; i += 4)
            {
                p[i]     = Clamp((int)((p[i]     - 128) * factor + 128));
                p[i + 1] = Clamp((int)((p[i + 1] - 128) * factor + 128));
                p[i + 2] = Clamp((int)((p[i + 2] - 128) * factor + 128));
            }
            return FromPixels(p, w, h);
        }

        public static WriteableBitmap Negative(BitmapSource src)
        {
            src = EnsureBgra32(src);
            var p = GetPixels(src, out _, out int w, out int h);
            for (int i = 0; i < p.Length; i += 4)
            {
                p[i]     = (byte)(255 - p[i]);
                p[i + 1] = (byte)(255 - p[i + 1]);
                p[i + 2] = (byte)(255 - p[i + 2]);
            }
            return FromPixels(p, w, h);
        }

        public static WriteableBitmap Binarize(BitmapSource src, int threshold)
        {
            src = EnsureBgra32(src);
            var p = GetPixels(src, out _, out int w, out int h);
            for (int i = 0; i < p.Length; i += 4)
            {
                int gray = (int)(0.299 * p[i + 2] + 0.587 * p[i + 1] + 0.114 * p[i]);
                byte val = gray >= threshold ? (byte)255 : (byte)0;
                p[i] = p[i + 1] = p[i + 2] = val;
            }
            return FromPixels(p, w, h);
        }

        public static WriteableBitmap ApplyKernel(BitmapSource src, double[,] kernel)
        {
            src = EnsureBgra32(src);
            var p = GetPixels(src, out _, out int w, out int h);
            var result = new byte[p.Length];

            int kRows = kernel.GetLength(0);
            int kCols = kernel.GetLength(1);
            int kCY   = kRows / 2;
            int kCX   = kCols / 2;

            double kSum = 0;
            foreach (var v in kernel) kSum += v;
            if (Math.Abs(kSum) < 1e-9) kSum = 1;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    double r = 0, g = 0, b = 0;
                    for (int ky = 0; ky < kRows; ky++)
                    {
                        for (int kx = 0; kx < kCols; kx++)
                        {
                            int py = Math.Clamp(y + ky - kCY, 0, h - 1);
                            int px = Math.Clamp(x + kx - kCX, 0, w - 1);
                            int idx = (py * w + px) * 4;
                            double kv = kernel[ky, kx];
                            b += p[idx]     * kv;
                            g += p[idx + 1] * kv;
                            r += p[idx + 2] * kv;
                        }
                    }
                    int ridx = (y * w + x) * 4;
                    result[ridx]     = Clamp((int)(b / kSum));
                    result[ridx + 1] = Clamp((int)(g / kSum));
                    result[ridx + 2] = Clamp((int)(r / kSum));
                    result[ridx + 3] = p[ridx + 3];
                }
            }
            return FromPixels(result, w, h);
        }

        public static WriteableBitmap AverageFilter(BitmapSource src, int size = 3)
        {
            var k = new double[size, size];
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                    k[i, j] = 1.0;
            return ApplyKernel(src, k);
        }

        public static WriteableBitmap GaussianFilter(BitmapSource src, int size = 5, double sigma = 1.4)
        {
            var k = new double[size, size];
            int half = size / 2;
            double twoSigSq = 2 * sigma * sigma;
            for (int y = -half; y <= half; y++)
                for (int x = -half; x <= half; x++)
                    k[y + half, x + half] = Math.Exp(-(x * x + y * y) / twoSigSq);
            return ApplyKernel(src, k);
        }

        public static WriteableBitmap SharpenFilter(BitmapSource src, int size = 3)
        {
            double[,] k = size switch
            {
                5 => new double[,]
                {
                    {  0,  0, -1,  0,  0 },
                    {  0, -1, -2, -1,  0 },
                    { -1, -2, 17, -2, -1 },
                    {  0, -1, -2, -1,  0 },
                    {  0,  0, -1,  0,  0 }
                },
                7 => new double[,]
                {
                    { -1, -1, -1, -1, -1, -1, -1 },
                    { -1,  0,  0, -1,  0,  0, -1 },
                    { -1,  0, -1, -2, -1,  0, -1 },
                    { -1, -1, -2, 41, -2, -1, -1 },
                    { -1,  0, -1, -2, -1,  0, -1 },
                    { -1,  0,  0, -1,  0,  0, -1 },
                    { -1, -1, -1, -1, -1, -1, -1 }
                },
                _ => new double[,]
                {
                    {  0, -1,  0 },
                    { -1,  5, -1 },
                    {  0, -1,  0 }
                }
            };
            return ApplyKernelUnscaled(src, k);
        }

        public static WriteableBitmap ApplyKernelUnscaled(BitmapSource src, double[,] kernel, bool absValues = false)
        {
            src = EnsureBgra32(src);
            var p = GetPixels(src, out _, out int w, out int h);
            var result = new byte[p.Length];

            int kRows = kernel.GetLength(0);
            int kCols = kernel.GetLength(1);
            int kCY   = kRows / 2;
            int kCX   = kCols / 2;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    double r = 0, g = 0, b = 0;
                    for (int ky = 0; ky < kRows; ky++)
                    {
                        for (int kx = 0; kx < kCols; kx++)
                        {
                            int py = Math.Clamp(y + ky - kCY, 0, h - 1);
                            int px = Math.Clamp(x + kx - kCX, 0, w - 1);
                            int idx = (py * w + px) * 4;
                            double kv = kernel[ky, kx];
                            b += p[idx]     * kv;
                            g += p[idx + 1] * kv;
                            r += p[idx + 2] * kv;
                        }
                    }
                    int ridx = (y * w + x) * 4;
                    result[ridx]     = absValues ? Clamp((int)Math.Abs(b)) : Clamp((int)b);
                    result[ridx + 1] = absValues ? Clamp((int)Math.Abs(g)) : Clamp((int)g);
                    result[ridx + 2] = absValues ? Clamp((int)Math.Abs(r)) : Clamp((int)r);
                    result[ridx + 3] = p[ridx + 3];
                }
            }
            return FromPixels(result, w, h);
        }

        public static WriteableBitmap RobertsEdge(BitmapSource src)
        {
            src = EnsureBgra32(src);
            var p = GetPixels(src, out _, out int w, out int h);
            var result = new byte[p.Length];
            for (int i = 3; i < result.Length; i += 4) result[i] = 255;

            for (int y = 0; y < h - 1; y++)
            {
                for (int x = 0; x < w - 1; x++)
                {
                    int i00 = (y * w + x) * 4;
                    int i01 = (y * w + (x + 1)) * 4;
                    int i10 = ((y + 1) * w + x) * 4;
                    int i11 = ((y + 1) * w + (x + 1)) * 4;

                    for (int c = 0; c < 3; c++)
                    {
                        int gx = p[i00 + c] - p[i11 + c];
                        int gy = p[i01 + c] - p[i10 + c];
                        result[(y * w + x) * 4 + c] = Clamp((int)Math.Sqrt(gx * gx + gy * gy));
                    }
                    result[i00 + 3] = 255;
                }
            }
            return FromPixels(result, w, h);
        }

        public static WriteableBitmap SobelEdge(BitmapSource src)
        {
            src = EnsureBgra32(src);
            var p = GetPixels(src, out _, out int w, out int h);
            var result = new byte[p.Length];
            for (int i = 3; i < result.Length; i += 4) result[i] = 255;

            for (int y = 1; y < h - 1; y++)
            {
                for (int x = 1; x < w - 1; x++)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        int gx =
                            -1 * p[((y-1)*w+(x-1))*4+c] + 1 * p[((y-1)*w+(x+1))*4+c]
                          + -2 * p[(y    *w+(x-1))*4+c] + 2 * p[(y    *w+(x+1))*4+c]
                          + -1 * p[((y+1)*w+(x-1))*4+c] + 1 * p[((y+1)*w+(x+1))*4+c];
                        int gy =
                            -1 * p[((y-1)*w+(x-1))*4+c] + -2 * p[((y-1)*w+x)*4+c] + -1 * p[((y-1)*w+(x+1))*4+c]
                          +  1 * p[((y+1)*w+(x-1))*4+c] +  2 * p[((y+1)*w+x)*4+c] +  1 * p[((y+1)*w+(x+1))*4+c];
                        result[(y*w+x)*4+c] = Clamp((int)Math.Sqrt(gx * gx + gy * gy));
                    }
                    result[(y*w+x)*4+3] = 255;
                }
            }
            return FromPixels(result, w, h);
        }

        public static WriteableBitmap LaplacianEdge(BitmapSource src)
        {
            var k = new double[,]
            {
                {  0,  1,  0 },
                {  1, -4,  1 },
                {  0,  1,  0 }
            };
            return ApplyKernelUnscaled(src, k, absValues: true);
        }

        public static WriteableBitmap Erode(BitmapSource src, int size = 3)
        {
            src = EnsureBgra32(src);
            var p = GetPixels(src, out _, out int w, out int h);
            var result = new byte[p.Length];
            int half = size / 2;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int minB = 255, minG = 255, minR = 255;
                    for (int ky = -half; ky <= half; ky++)
                    {
                        for (int kx = -half; kx <= half; kx++)
                        {
                            int py  = Math.Clamp(y + ky, 0, h - 1);
                            int px  = Math.Clamp(x + kx, 0, w - 1);
                            int idx = (py * w + px) * 4;
                            if (p[idx]     < minB) minB = p[idx];
                            if (p[idx + 1] < minG) minG = p[idx + 1];
                            if (p[idx + 2] < minR) minR = p[idx + 2];
                        }
                    }
                    int ridx = (y * w + x) * 4;
                    result[ridx]     = (byte)minB;
                    result[ridx + 1] = (byte)minG;
                    result[ridx + 2] = (byte)minR;
                    result[ridx + 3] = p[ridx + 3];
                }
            }
            return FromPixels(result, w, h);
        }

        public static WriteableBitmap Dilate(BitmapSource src, int size = 3)
        {
            src = EnsureBgra32(src);
            var p = GetPixels(src, out _, out int w, out int h);
            var result = new byte[p.Length];
            int half = size / 2;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int maxB = 0, maxG = 0, maxR = 0;
                    for (int ky = -half; ky <= half; ky++)
                    {
                        for (int kx = -half; kx <= half; kx++)
                        {
                            int py  = Math.Clamp(y + ky, 0, h - 1);
                            int px  = Math.Clamp(x + kx, 0, w - 1);
                            int idx = (py * w + px) * 4;
                            if (p[idx]     > maxB) maxB = p[idx];
                            if (p[idx + 1] > maxG) maxG = p[idx + 1];
                            if (p[idx + 2] > maxR) maxR = p[idx + 2];
                        }
                    }
                    int ridx = (y * w + x) * 4;
                    result[ridx]     = (byte)maxB;
                    result[ridx + 1] = (byte)maxG;
                    result[ridx + 2] = (byte)maxR;
                    result[ridx + 3] = p[ridx + 3];
                }
            }
            return FromPixels(result, w, h);
        }

        public static (int[] r, int[] g, int[] b, int[] gray) ComputeHistogram(BitmapSource src)
        {
            src = EnsureBgra32(src);
            var p = GetPixels(src, out _, out int w, out int h);
            var rH = new int[256]; var gH = new int[256];
            var bH = new int[256]; var grH = new int[256];
            for (int i = 0; i < p.Length; i += 4)
            {
                bH[p[i]]++;
                gH[p[i + 1]]++;
                rH[p[i + 2]]++;
                int gr = (int)(0.299 * p[i + 2] + 0.587 * p[i + 1] + 0.114 * p[i]);
                grH[gr]++;
            }
            return (rH, gH, bH, grH);
        }

        public static double[] HorizontalProjection(BitmapSource src)
        {
            src = EnsureBgra32(src);
            var p = GetPixels(src, out _, out int w, out int h);
            var proj = new double[h];
            for (int y = 0; y < h; y++)
            {
                double sum = 0;
                for (int x = 0; x < w; x++)
                {
                    int i = (y * w + x) * 4;
                    sum += 0.299 * p[i + 2] + 0.587 * p[i + 1] + 0.114 * p[i];
                }
                proj[y] = sum / w;
            }
            return proj;
        }

        public static double[] VerticalProjection(BitmapSource src)
        {
            src = EnsureBgra32(src);
            var p = GetPixels(src, out _, out int w, out int h);
            var proj = new double[w];
            for (int x = 0; x < w; x++)
            {
                double sum = 0;
                for (int y = 0; y < h; y++)
                {
                    int i = (y * w + x) * 4;
                    sum += 0.299 * p[i + 2] + 0.587 * p[i + 1] + 0.114 * p[i];
                }
                proj[x] = sum / h;
            }
            return proj;
        }
    }
}
