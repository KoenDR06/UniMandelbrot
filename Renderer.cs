﻿using System.Drawing.Imaging;

namespace Mandelbrot;

public class Renderer
{
    public int Resolution;
    public int MaxIterations;
    public RenderMode RenderMode;
    public int Cores = 1;
    public double XCenter = 0;
    public double YCenter = 0;
    public double Zoom = 0;
    public bool Julia = false;

    private Bitmap _image;
    private byte[] _pixelData;
    private BitmapData _imageData;
    
    public Renderer(int resolution, int maxIterations, RenderMode renderMode, int cores = 1, double xCenter = 0,
        double yCenter = 0, double zoom = 0, bool julia = false)
    {
        Resolution = resolution;
        MaxIterations = maxIterations;
        RenderMode = renderMode;
        Cores = cores;
        XCenter = xCenter;
        YCenter = yCenter;
        Zoom = zoom;
        Julia = julia;

        _image = new Bitmap(this.Resolution, this.Resolution, PixelFormat.Format24bppRgb);
    }

    private int IteratePoint(double zReal, double zImag, double cReal, double cImag)
    {
        var iterations = 0;
        
        // This condition is just Pythagoras without the square root for performance
        while (zReal * zReal + zImag * zImag <= 4) {
            var tempRe = zReal * zReal - zImag * zImag + cReal;
            zImag = 2 * zReal * zImag + cImag;
            zReal = tempRe;
            
            iterations++;
            if (iterations == MaxIterations) return -1;
        }

        return iterations;
    }

    private void Worker(double juliaX, double juliaY, int startX, int endX)
    {
        var zoomExp = Math.Exp(-1.0 * Zoom);
    
        for (var x = startX; x < endX; x++)
        {
            for (var y = 0; y < Resolution; y++)
            {
                // Pixel-space => mandelbrot-space
                var pointX = zoomExp * (4.0 * x / Resolution - 2.0) + XCenter;
                var pointY = zoomExp * (4.0 * y / Resolution - 2.0) + YCenter;
    
                var iterations = Julia
                    ? IteratePoint(pointX, pointY, juliaX, juliaY)
                    : IteratePoint(0.0, 0.0, pointX, pointY);
    
                var pointColour = iterations == -1
                    ? Color.Black
                    : RenderMode.CalculateColor(iterations, MaxIterations);
                
                // Code from: https://stackoverflow.com/questions/1563038/fast-work-with-bitmaps-in-c-sharp
                // Multiply by three because image has three bytes per pixel
                var pixelLocation = y * _imageData.Stride + 3 * x;
                _pixelData[pixelLocation] = pointColour.B;
                _pixelData[pixelLocation+1] = pointColour.R;
                _pixelData[pixelLocation+2] = pointColour.G;
            }
        }
    }

    public Task<Bitmap> RenderMandelbrot(double juliaX = 0, double juliaY = 0)
    {
        // Resolution is updated, required for high-res exports
        _image = new Bitmap(this.Resolution, this.Resolution, PixelFormat.Format24bppRgb);

        // Next section copied from: https://stackoverflow.com/questions/1563038/fast-work-with-bitmaps-in-c-sharp
        _imageData = _image.LockBits(new Rectangle(0, 0, _image.Width, _image.Height), ImageLockMode.ReadWrite, _image.PixelFormat);
        int size = _imageData.Stride * _imageData.Height;
        _pixelData = new byte[size];
        System.Runtime.InteropServices.Marshal.Copy(_imageData.Scan0, _pixelData, 0, size);

        var threads = new List<Thread>();

        for (var i = 0; i < Cores; i++)
        {
            // Rounding can cause threads to not reach the end, so the last thread cleans up
            var endX = (i + 1 == Cores) ? _image.Width : (i + 1) * (_image.Width / Cores);

            var thread = new Thread(() => Worker(juliaX, juliaY, i * (_image.Width / Cores), endX));
            thread.Start();
            
            threads.Add(thread);
            Thread.Sleep(1);
        }
        
        // Wait till all the threads are finished
        foreach (var thread in threads) thread.Join();

        // Code copied from: https://stackoverflow.com/questions/1563038/fast-work-with-bitmaps-in-c-sharp
        System.Runtime.InteropServices.Marshal.Copy(_pixelData, 0, _imageData.Scan0, _pixelData.Length);
        _image.UnlockBits(_imageData);

        return Task.FromResult(_image);
    }

    // BACKLOG: add these settings to the main control panel
    public async void SaveRenderedImage()
    {
        var filename = Directory.GetCurrentDirectory() + "..\\..\\..\\..\\mandelbrot.png";
        var oldMaxIterations = MaxIterations;
        Resolution *= 2;
        MaxIterations = 4096;
        
        // BACKLOG: For some reason the image only appears after closing the program
        var render = await RenderMandelbrot();
        render.Save(filename, ImageFormat.Png);
        
        Resolution /= 2;
        MaxIterations = oldMaxIterations;
    }
}

// public class Renderer2(int resolution, int maxIters, RenderMode renderMode, int cores = 1, double xCenter = 0, double yCenter = 0, double zoom = 0, bool julia = false) {
//     public int MaxIters = maxIters;
//     public double XCenter = xCenter;
//     public double YCenter = yCenter;
//     public double Zoom = zoom;
//
//     Bitmap _img = new Bitmap(resolution, resolution, PixelFormat.Format24bppRgb);
//     byte[] _pixelData = [];
//     BitmapData _imgData;
//     
//     /**
//      * Calculates the mandelnumber of a point in mandelbrot space.
//      */
//     int IteratePoint(double zReal, double zImag, double cReal, double cImag) {
//         int iterations = 0;
//         
//         // This condition is just Pythagoras without the square root for performance
//         while (zReal * zReal + zImag * zImag <= 4) {
//             double tempRe = zReal * zReal - zImag * zImag + cReal;
//             zImag = 2 * zReal * zImag + cImag;
//             zReal = tempRe;
//             iterations++;
//             if (iterations > MaxIters) {
//                 return -1;
//             }
//         }
//
//         return iterations;
//     }
//     
//     /**
//      *  Thread function
//      */
//     void Worker(double juliaX, double juliaY, int startX, int endX) {
//         double zoomExp = Math.Exp(-1.0 * Zoom);
//         for (int x = startX; x < endX; x++) {
//             for (int y = 0; y < resolution; y++) {
//                 // Converts pixel-space to mandelbrot-space
//                 double pointX = zoomExp * (4.0 * x / resolution - 2.0) + XCenter;
//                 double pointY = zoomExp * (4.0 * y / resolution - 2.0) + YCenter;
//                 
//                 int iters;
//                 if (!julia) {
//                     iters = IteratePoint(0.0, 0.0, pointX, pointY);
//                 } else {
//                     iters = IteratePoint(pointX, pointY, juliaX, juliaY);
//                 }
//                 
//                 Color pointColor;
//                 if (iters == -1) {
//                     pointColor = Color.Black;
//                 } else {
//                     pointColor = renderMode.CalculateColor(iters, MaxIters);
//                 }
//
//                 // start code from: https://stackoverflow.com/questions/1563038/fast-work-with-bitmaps-in-c-sharp
//                 int bytesPerPixel = Image.GetPixelFormatSize(_img.PixelFormat) / 8;
//                 int pixelLocation = y * _imgData.Stride + bytesPerPixel * x;
//                 _pixelData[pixelLocation] = pointColor.B;
//                 _pixelData[pixelLocation+1] = pointColor.R;
//                 _pixelData[pixelLocation+2] = pointColor.G;
//                 // end code from: https://stackoverflow.com/questions/1563038/fast-work-with-bitmaps-in-c-sharp
//             }
//         }
//     }
//     
//     
//     /**
//      * The main rendering function
//      */
//     public Bitmap RenderMandelbrot(double juliaX = 0, double juliaY = 0) {
//         _img = new Bitmap(resolution, resolution, PixelFormat.Format24bppRgb);
//         
//         // start of copied code from: https://stackoverflow.com/questions/1563038/fast-work-with-bitmaps-in-c-sharp
//         _imgData = _img.LockBits(new Rectangle(0, 0, _img.Width, _img.Height), ImageLockMode.ReadWrite, _img.PixelFormat);
//         int size = _imgData.Stride * _imgData.Height;
//         _pixelData = new byte[size];
//         System.Runtime.InteropServices.Marshal.Copy(_imgData.Scan0, _pixelData, 0, size);
//         // end of copied code from: https://stackoverflow.com/questions/1563038/fast-work-with-bitmaps-in-c-sharp
//         
//         List<Thread> threads = [];
//         
//         int width = _img.Width;
//         for (int i = 0; i < cores; i++) {
//             int endX;
//             // Due to rounding sometimes the threads don't reach the end,
//             // so force the last one reach the end of the image
//             if (i + 1 != cores) {
//                 endX = (i + 1) * (width / cores);
//             } else {
//                 endX = _img.Width;
//             }
//             
//             Thread thread = new Thread(() => {
//                 Worker(
//                     juliaX,
//                     juliaY,
//                     i * (width / cores),
//                     endX
//                 );
//             });
//             threads.Add(thread);
//             thread.Start();
//             // We had some issues with timing stuff,
//             // so wait 1 ms between threads
//             Thread.Sleep(1);
//         }
//         
//         // Wait until every thread has completed
//         foreach (Thread thread in threads) {
//             thread.Join();
//         }
//         
//         // start of copied code from: https://stackoverflow.com/questions/1563038/fast-work-with-bitmaps-in-c-sharp
//         System.Runtime.InteropServices.Marshal.Copy(_pixelData, 0, _imgData.Scan0, _pixelData.Length);
//         _img.UnlockBits(_imgData);
//         // end of copied code from: https://stackoverflow.com/questions/1563038/fast-work-with-bitmaps-in-c-sharp
//         
//         return _img;
//     }
//     
//     
//     /**
//      * Exports the current render as a .mandel file.
//      */
//     public void ExportMandelbrot(string filename) {
//         using (FileStream stream = File.Open(filename, FileMode.Create)) {
//             using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, false)) {
//                 // Magic bytes
//                 writer.Write("MANDEL");
//
//                 writer.Write(XCenter);
//                 writer.Write(YCenter);
//                 writer.Write(Zoom);
//                 writer.Write(MaxIters);
//                 writer.Write(julia);
//                 
//                 writer.Write(renderMode.GetId());
//                 if (renderMode is Lerp lerp) {
//                     Utils.WriteColorPair(writer, lerp.Start, lerp.End);
//                 } else if (renderMode is FlipFlop flipFlop) {
//                     Utils.WriteColorPair(writer, flipFlop.A, flipFlop.B);
//                 } else if (renderMode is Triangle triangle) {
//                     writer.Write(BitConverter.GetBytes(triangle.Colors.Count));
//                     writer.Write(BitConverter.GetBytes(triangle.TriangleSize));
//                     writer.Write(BitConverter.GetBytes(triangle.Repeat));
//
//                     foreach ((Color a, Color b) in triangle.Colors) {
//                         Utils.WriteColorPair(writer, a, b);
//                     }
//                 }
//                 writer.Flush();
//             }
//         }
//     }
//
//     /**
//      * imports a .mandel file and loads as the current render.
//      */
//     public static Renderer ImportMandelbrot(string filename, int resolution = 512) {
//         double xCenter, yCenter, zoom;
//         int maxIters;
//         bool julia;
//         RenderMode renderMode;
//
//         using (FileStream stream = File.Open(filename, FileMode.Open)) {
//             using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, false)) {
//                 // For some reason the byte 0x06 is added to the beginning of the file, even though I never tell it
//                 // to write it, so I have to read it too.
//                 reader.ReadByte();
//                 
//                 string magicBytes = Utils.GetStringFromBytes(reader.ReadBytes(6));
//                 if (magicBytes != "MANDEL") {
//                     throw new Exception("Invalid file format.");
//                 }
//
//                 xCenter = reader.ReadDouble();
//                 yCenter = reader.ReadDouble();
//                 zoom = reader.ReadDouble();
//                 maxIters = reader.ReadInt32();
//                 julia = reader.ReadBoolean();
//                 
//                 switch (reader.ReadByte()) {
//                     case (byte)RenderModeEnum.Grayscale:
//                         renderMode = new Grayscale();
//                         break;
//                     
//                     case (byte)RenderModeEnum.Hue:
//                         renderMode = new Hue();
//                         break;
//                     
//                     case (byte)RenderModeEnum.Lerp:
//                         renderMode = new Lerp(
//                             Color.FromArgb(
//                                 reader.ReadByte(),
//                                 reader.ReadByte(),
//                                 reader.ReadByte()),
//                             Color.FromArgb(
//                                 reader.ReadByte(),
//                                 reader.ReadByte(),
//                                 reader.ReadByte())
//                         );
//                         break;
//                     
//                     case (byte)RenderModeEnum.FlipFlop:
//                         renderMode = new FlipFlop(
//                             Color.FromArgb(
//                                 reader.ReadByte(),
//                                 reader.ReadByte(),
//                                 reader.ReadByte()),
//                             Color.FromArgb(
//                                 reader.ReadByte(),
//                                 reader.ReadByte(),
//                                 reader.ReadByte())
//                         );
//                         break;
//                     
//                     case (byte)RenderModeEnum.Triangle:
//                         int colorLength = reader.ReadInt32();
//                         int triangleSize = reader.ReadInt32();
//                         int repeat = reader.ReadInt32();
//                         var colors = new List<(Color, Color)>();
//
//                         for (int i = 0; i < colorLength; i++) {
//                             colors.Add((
//                                 Color.FromArgb(
//                                     reader.ReadByte(),
//                                     reader.ReadByte(),
//                                     reader.ReadByte()),
//                                 Color.FromArgb(
//                                     reader.ReadByte(),
//                                     reader.ReadByte(),
//                                     reader.ReadByte())
//                                 )
//                             );
//                         }
//                         renderMode = new Triangle(colors, triangleSize, repeat);
//                         break;
//                     
//                     default:
//                         throw new Exception("Failed to read ID of input file, it might be corrupted.");
//                 }
//             }
//         }
//         
//         return new Renderer(resolution, maxIters, renderMode, cores: 1, xCenter, yCenter, zoom, julia);
//     }
//
//     
//     /**
//      * Saves the image currently displayed.
//      */
//     public void SaveRenderedImage(string filename, int renderResolution)
//     {
//         // resolution = renderResolution;
//         RenderMandelbrot().Save(filename, ImageFormat.Png);
//     }
// }