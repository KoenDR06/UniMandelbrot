﻿using System.Drawing.Imaging;
using System.Text;

namespace Mandelbrot;

public class Renderer
{
    public int Resolution;
    public int MaxIterations;
    public RenderMode RenderMode;
    public int Cores;
    public double XCenter;
    public double YCenter;
    public double Zoom;
    public bool Julia;

    Bitmap _image;
    byte[] _pixelData;
    BitmapData _imageData;
    
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

    int IteratePoint(double zReal, double zImag, double cReal, double cImag)
    {
        int iterations = 0;
        
        // This condition is just Pythagoras without the square root for performance
        while (zReal * zReal + zImag * zImag <= 4) {
            double tempRe = zReal * zReal - zImag * zImag + cReal;
            zImag = 2 * zReal * zImag + cImag;
            zReal = tempRe;
            
            iterations++;
            if (iterations == MaxIterations) return -1;
        }

        return iterations;
    }

    private void Worker(double juliaX, double juliaY, int startX, int endX)
    {
        double zoomExp = Math.Exp(-1.0 * Zoom);
    
        for (int x = startX; x < endX; x++)
        {
            for (int y = 0; y < Resolution; y++)
            {
                // Pixel-space => mandelbrot-space
                double pointX = zoomExp * (4.0 * x / Resolution - 2.0) + XCenter;
                double pointY = zoomExp * (4.0 * y / Resolution - 2.0) + YCenter;
    
                int iterations = Julia
                    ? IteratePoint(pointX, pointY, juliaX, juliaY)
                    : IteratePoint(0.0, 0.0, pointX, pointY);
                
                Color pointColour = iterations == -1
                    ? Color.Black
                    : RenderMode.CalculateColor(iterations, MaxIterations);
                
                // Code from: https://stackoverflow.com/questions/1563038/fast-work-with-bitmaps-in-c-sharp
                // Multiply by three because image has three bytes per pixel
                int pixelLocation = y * _imageData.Stride + 3 * x;
                _pixelData[pixelLocation] = pointColour.B;
                _pixelData[pixelLocation+1] = pointColour.G;
                _pixelData[pixelLocation+2] = pointColour.R;
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

        for (int i = 0; i < Cores; i++)
        {
            // Rounding can cause threads to not reach the end, so the last thread cleans up
            int endX = (i + 1 == Cores) ? _image.Width : (i + 1) * (_image.Width / Cores);

            Thread thread = new Thread(() => Worker(juliaX, juliaY, i * (_image.Width / Cores), endX));
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
    
    public void ExportMandelbrot(string filename) {
        using (FileStream stream = File.Open(filename, FileMode.Create)) {
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, false)) {
                // Magic bytes
                writer.Write("MANDEL");

                writer.Write(XCenter);
                writer.Write(YCenter);
                writer.Write(Zoom);
                writer.Write(MaxIterations);
                writer.Write(Julia);
                
                writer.Write(RenderMode.GetId());
                if (RenderMode is Lerp lerp) {
                    Utils.WriteColorPair(writer, lerp.Start, lerp.End);
                } else if (RenderMode is FlipFlop flipFlop) {
                    Utils.WriteColorPair(writer, flipFlop.A, flipFlop.B);
                } else if (RenderMode is Triangle triangle) {
                    writer.Write(BitConverter.GetBytes(triangle.Colors.Count));
                    writer.Write(BitConverter.GetBytes(triangle.TriangleSize));
                    writer.Write(BitConverter.GetBytes(triangle.Repeat));

                    foreach ((Color a, Color b) in triangle.Colors) {
                        Utils.WriteColorPair(writer, a, b);
                    }
                }
                writer.Flush();
            }
        }
    }
    
    public void ImportMandelbrot(string filename) {
        using (FileStream stream = File.Open(filename, FileMode.Open)) {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, false)) {
                // For some reason the byte 0x06 is added to the beginning of the file, even though I never tell it
                // to write it, so I have to read it too.
                reader.ReadByte();
                
                string magicBytes = Utils.GetStringFromBytes(reader.ReadBytes(6));
                if (magicBytes != "MANDEL") {
                    throw new Exception("Invalid file format.");
                }

                XCenter = reader.ReadDouble();
                YCenter = reader.ReadDouble();
                Zoom = reader.ReadDouble();
                MaxIterations = reader.ReadInt32();
                Julia = reader.ReadBoolean();
                
                switch (reader.ReadByte()) {
                    case (byte)RenderModeEnum.Grayscale:
                        RenderMode = new Grayscale();
                        break;
                    
                    case (byte)RenderModeEnum.Hue:
                        RenderMode = new Hue();
                        break;
                    
                    case (byte)RenderModeEnum.Lerp:
                        RenderMode = new Lerp(
                            Color.FromArgb(
                                reader.ReadByte(),
                                reader.ReadByte(),
                                reader.ReadByte()),
                            Color.FromArgb(
                                reader.ReadByte(),
                                reader.ReadByte(),
                                reader.ReadByte())
                        );
                        break;
                    
                    case (byte)RenderModeEnum.FlipFlop:
                        RenderMode = new FlipFlop(
                            Color.FromArgb(
                                reader.ReadByte(),
                                reader.ReadByte(),
                                reader.ReadByte()),
                            Color.FromArgb(
                                reader.ReadByte(),
                                reader.ReadByte(),
                                reader.ReadByte())
                        );
                        break;
                    
                    case (byte)RenderModeEnum.Triangle:
                        int colorLength = reader.ReadInt32();
                        int triangleSize = reader.ReadInt32();
                        int repeat = reader.ReadInt32();
                        var colors = new List<(Color, Color)>();

                        for (int i = 0; i < colorLength; i++) {
                            colors.Add((
                                Color.FromArgb(
                                    reader.ReadByte(),
                                    reader.ReadByte(),
                                    reader.ReadByte()),
                                Color.FromArgb(
                                    reader.ReadByte(),
                                    reader.ReadByte(),
                                    reader.ReadByte())
                                )
                            );
                        }
                        RenderMode = new Triangle(colors, triangleSize, repeat);
                        break;
                    
                    default:
                        throw new Exception("Failed to read ID of input file, it might be corrupted.");
                }
            }
        }
    }

    // BACKLOG: add these settings to the main control panel
    public async void SaveRenderedImage(string filename)
    {
        int oldMaxIterations = MaxIterations;
        Resolution *= 2;
        MaxIterations = 4096;
        
        // BACKLOG: For some reason the image only appears after closing the program
        Bitmap render = await RenderMandelbrot();
        render.Save(filename, ImageFormat.Png);
        
        Resolution /= 2;
        MaxIterations = oldMaxIterations;
    }
}