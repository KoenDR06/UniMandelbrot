using System.Drawing.Imaging;
using System.Text;

namespace Mandelbrot;

public class Renderer(int resolution, int maxIters, RenderMode renderMode, double xCenter = 0, double yCenter = 0, double zoom = 0, bool julia = false) {
    public int MaxIters = maxIters;
    public double XCenter = xCenter;
    public double YCenter = yCenter;
    public double Zoom = zoom;

    Bitmap _img = new Bitmap(resolution, resolution, PixelFormat.Format24bppRgb);
    byte[] data = [];
    BitmapData bData;
    
    int IteratePoint(double zReal, double zImag, double cReal, double cImag) {
        int iterations = 0;
        
        // This condition is just Pythagoras without the square root, because those are slow
        while (zReal * zReal + zImag * zImag <= 4) {
            double tempRe = zReal * zReal - zImag * zImag + cReal;
            zImag = 2 * zReal * zImag + cImag;
            zReal = tempRe;
            iterations++;
            if (iterations > MaxIters) {
                return -1;
            }
        }

        return iterations;
    }
    
    void Worker(double juliaX, double juliaY, int startX, int endX) {
        double zoomExp = Math.Exp(-1.0 * Zoom);
        for (int x = startX; x < endX; x++) {
            for (int y = 0; y < resolution; y++) {
                double pointX = zoomExp * (4.0 * x / resolution - 2.0) + XCenter;
                double pointY = zoomExp * (4.0 * y / resolution - 2.0) + YCenter;
                int iters;
                if (!julia) {
                    iters = IteratePoint(0.0, 0.0, pointX, pointY);
                } else {
                    iters = IteratePoint(pointX, pointY, juliaX, juliaY);
                }
                
                Color pointColor;
                if (iters == -1) {
                    pointColor = Color.Black;
                } else {
                    pointColor = renderMode.CalculateColor(iters, MaxIters);
                }
                
                int index = y * bData.Stride + 3*x;
                data[index] = pointColor.B;
                data[index+1] = pointColor.R;
                data[index+2] = pointColor.G;
                
                // _img.SetPixel(x, y, pointColor);
            }
        }
    }
    
    public Bitmap RenderMandelbrot(double juliaX = 0, double juliaY = 0) {
        _img.Dispose();
        _img = new Bitmap(resolution, resolution, PixelFormat.Format24bppRgb);
        // start of copied code from: https://stackoverflow.com/questions/1563038/fast-work-with-bitmaps-in-c-sharp
        bData = _img.LockBits(new Rectangle(0, 0, _img.Width, _img.Height), ImageLockMode.ReadWrite, _img.PixelFormat);
        int bitsPerPixel = Image.GetPixelFormatSize(_img.PixelFormat);
        int size = bData.Stride * bData.Height;
        data = new byte[size];
        System.Runtime.InteropServices.Marshal.Copy(bData.Scan0, data, 0, size);
        // end of copied code from: https://stackoverflow.com/questions/1563038/fast-work-with-bitmaps-in-c-sharp
        
        List<Thread> threads = [];
        
        int width = _img.Width;
        for (int i = 0; i < Environment.ProcessorCount; i++) {
            int endX;
            if (i + 1 != Environment.ProcessorCount) {
                endX = (i + 1) * (width / Environment.ProcessorCount);
            } else {
                endX = _img.Width;
            }
            
            Thread thread = new Thread(
                () =>
                {
                    Worker(
                        juliaX,
                        juliaY,
                        i * (width / Environment.ProcessorCount),
                        endX
                    );
                });
            threads.Add(thread);
            thread.Start(); 
            Thread.Sleep(1);
        }
        
        foreach (Thread thread in threads) {
            thread.Join();
        }
        
        // start of copied code from: https://stackoverflow.com/questions/1563038/fast-work-with-bitmaps-in-c-sharp
        System.Runtime.InteropServices.Marshal.Copy(data, 0, bData.Scan0, data.Length);
        _img.UnlockBits(bData);
        // end of copied code from: https://stackoverflow.com/questions/1563038/fast-work-with-bitmaps-in-c-sharp
        
        return _img;
    }
    
    
    /**
     * Exports the current render as a .mandel file.
     */
    public void ExportMandelbrot(string filename) {
        using (FileStream stream = File.Open(filename, FileMode.Create)) {
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, false)) {
                
                // Magic bytes
                writer.Write("MANDEL");

                writer.Write(XCenter);
                writer.Write(YCenter);
                writer.Write(Zoom);
                writer.Write(MaxIters);
                writer.Write(julia);

                writer.Write(new[] { renderMode.GetId() });
                if (renderMode is Lerp lerp) {
                    foreach (var colour in new [] { lerp.Start, lerp.End })
                    {
                        writer.Write(BitConverter.GetBytes((int)colour.R)[0]);
                        writer.Write(BitConverter.GetBytes((int)colour.G)[0]);
                        writer.Write(BitConverter.GetBytes((int)colour.B)[0]);
                    }
                } else if (renderMode is FlipFlop flipFlop) {
                    foreach (var colour in new [] {flipFlop.A, flipFlop.B})
                    {
                        writer.Write(BitConverter.GetBytes((int)colour.R)[0]);
                        writer.Write(BitConverter.GetBytes((int)colour.G)[0]);
                        writer.Write(BitConverter.GetBytes((int)colour.B)[0]);
                    }
                } else if (renderMode is Triangle triangle) {
                    writer.Write(BitConverter.GetBytes(triangle.Colors.Count));
                    writer.Write(BitConverter.GetBytes(triangle.TriangleSize));
                    writer.Write(BitConverter.GetBytes(triangle.Repeat));

                    foreach ((Color a, Color b) in triangle.Colors) {
                        writer.Write(BitConverter.GetBytes((int)a.R)[0]);
                        writer.Write(BitConverter.GetBytes((int)a.G)[0]);
                        writer.Write(BitConverter.GetBytes((int)a.B)[0]);
                        writer.Write(BitConverter.GetBytes((int)b.R)[0]);
                        writer.Write(BitConverter.GetBytes((int)b.G)[0]);
                        writer.Write(BitConverter.GetBytes((int)b.B)[0]);
                    }
                }

                writer.Flush();
            }

        }

    }

    /**
     * imports a given .mandel file and loads as the current render.
     */
    public static Renderer ImportMandelbrot(string filename, int resolution = 512) {
        double xCenter, yCenter, zoom;
        int maxIters;
        bool julia;
        RenderMode renderMode;

        using (FileStream stream = File.Open(filename, FileMode.Open)) {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, false)) {
                // For some reason the byte 0x06 is added to the beginning of the file, even though I never tell it
                // to write it, so I have to read it too.
                reader.ReadByte();
                
                string magicBytes = Utils.GetStringFromBytes(reader.ReadBytes(6));
                if (magicBytes != "MANDEL") {
                    throw new Exception("Invalid file format.");
                }

                xCenter = reader.ReadDouble();
                yCenter = reader.ReadDouble();
                zoom = reader.ReadDouble();
                maxIters = reader.ReadInt32();
                julia = reader.ReadBoolean();
                
                switch (reader.ReadByte()) {
                    case (byte)RenderModeEnum.Grayscale:
                        renderMode = new Grayscale();

                        break;
                    case (byte)RenderModeEnum.Hue:
                        renderMode = new Hue();

                        break;
                    case (byte)RenderModeEnum.Lerp:
                        renderMode = new Lerp(
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
                        renderMode = new FlipFlop(
                            Color.FromArgb(
                                reader.ReadByte(),
                                reader.ReadByte(),
                                reader.ReadByte()),
                            Color.FromArgb(
                                reader.ReadByte(),
                                reader.ReadByte(),
                                reader.ReadByte()));

                        break;
                    case (byte)RenderModeEnum.Triangle:
                        int colorLength = reader.ReadInt32();
                        int triangleSize = reader.ReadInt32();
                        int repeat = reader.ReadInt32();
                        var colors = new List<(Color, Color)>();

                        for (int i = 0; i < colorLength; i++) {
                            colors.Add(
                                (Color.FromArgb(
                                        reader.ReadByte(),
                                        reader.ReadByte(),
                                        reader.ReadByte()),
                                    Color.FromArgb(
                                        reader.ReadByte(),
                                        reader.ReadByte(),
                                        reader.ReadByte()))
                            );
                        }

                        renderMode = new Triangle(colors, triangleSize, repeat);

                        break;
                    default:
                        throw new Exception("Invalid input file. It might be corrupted.");
                }
            }
        }
        
        return new Renderer(resolution, maxIters, renderMode, xCenter, yCenter, zoom, julia);
    }

    public void SaveRenderedImage(string filename) {
        _img.Save(filename, ImageFormat.Png);
    }
}