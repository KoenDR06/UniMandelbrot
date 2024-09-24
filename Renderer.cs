using System.Text;

namespace Mandelbrot;

public static class Renderer {
    static int IteratePoint(double zReal, double zImag, double cReal, double cImag, int maxIterations) {
        int iterations = 0;
        
        // This condition is just Pythagoras without the square root, because those are slow
        while (zReal * zReal + zImag * zImag <= 4) {
            double tempRe = zReal * zReal - zImag * zImag + cReal;
            zImag = 2 * zReal * zImag + cImag;
            zReal = tempRe;
            iterations++;
            if (iterations > maxIterations) {
                return -1;
            }
        }

        return iterations;
    }
    
    static List<(int x, int y, int iters)> Worker(double xCenter, double yCenter, double zoom, double juliaX, double juliaY, int maxIters, int resolution, bool julia, int startX, int endX) {
        Console.WriteLine(startX + "  " + endX);
        List<(int x, int y, int iters)> res = [];
        
        double zoomExp = Math.Exp(-1.0 * zoom);
        for (int x = startX; x < endX; x++) {
            for (int y = 0; y < resolution; y++) {
                double pointX = zoomExp * (4.0 * x / resolution - 2.0) + xCenter;
                double pointY = zoomExp * (4.0 * y / resolution - 2.0) + yCenter;
                if (!julia) {
                    int iters = IteratePoint(0.0, 0.0, pointX, pointY, maxIters);
                    res.Add((x, y, iters));
                } else {
                    int iters = IteratePoint(pointX, pointY, juliaX, juliaY, maxIters);
                    res.Add((x, y, iters));
                }
            }
        }

        return res;
    }
    
    public static Bitmap RenderMandelbrot(double xCenter, double yCenter, double zoom, int maxIters, RenderMode renderMode, Bitmap img, double juliaX = 0, double juliaY = 0) {
        var values = new List<List<(int x, int y, int iters)>>();
        List<Thread> threads = [];
        
        int height = img.Height;
        int width = img.Width;
        for (int i = 0; i < Environment.ProcessorCount; i++) {
            values.Add([]);
                
            int endX;
            
            if (i + 1 != Environment.ProcessorCount) {
                endX = (i + 1) * (width / Environment.ProcessorCount);
            } else {
                endX = img.Width;
            }
            
            Thread thread = new Thread(
                () =>
                {
                    values[i] = Worker(
                        xCenter,
                        yCenter,
                        zoom,
                        juliaX,
                        juliaY,
                        maxIters,
                        height,
                        renderMode.julia,
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

        foreach (var valueList in values) {
            foreach (var pixel in valueList) {
                Color pointColor;
                if (pixel.iters == -1) {
                    pointColor = Color.Black;
                } else {
                    pointColor = renderMode.CalculateColor(pixel.iters, maxIters);
                }

                // Console.WriteLine(pixel.x);
                img.SetPixel(pixel.x, pixel.y, pointColor);
            }
        }

        return img;
    }
    
    
    /**
     * Exports the current render as a .mandel file.
     */
    public static void ExportMandelbrot(string filename, double xCenter, double yCenter, double zoom, int maxIters, RenderMode renderMode) {
        using (FileStream stream = File.Open(filename, FileMode.Create)) {
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, false)) {
                
                // Magic bytes
                writer.Write("MANDEL");

                writer.Write(xCenter);
                writer.Write(yCenter);
                writer.Write(zoom);
                writer.Write(maxIters);

                writer.Write(new[] { renderMode.GetId() });
                if (renderMode is Lerp lerp) {
                    writer.Write(BitConverter.GetBytes((int)lerp.Start.R)[0]);
                    writer.Write(BitConverter.GetBytes((int)lerp.Start.G)[0]);
                    writer.Write(BitConverter.GetBytes((int)lerp.Start.B)[0]);
                    writer.Write(BitConverter.GetBytes((int)lerp.End.R)[0]);
                    writer.Write(BitConverter.GetBytes((int)lerp.End.G)[0]);
                    writer.Write(BitConverter.GetBytes((int)lerp.End.B)[0]);
                } else if (renderMode is FlipFlop flipFlop) {
                    writer.Write(BitConverter.GetBytes((int)flipFlop.A.R)[0]);
                    writer.Write(BitConverter.GetBytes((int)flipFlop.A.G)[0]);
                    writer.Write(BitConverter.GetBytes((int)flipFlop.A.B)[0]);
                    writer.Write(BitConverter.GetBytes((int)flipFlop.B.R)[0]);
                    writer.Write(BitConverter.GetBytes((int)flipFlop.B.G)[0]);
                    writer.Write(BitConverter.GetBytes((int)flipFlop.B.B)[0]);
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
    public static (double, double, double, int, RenderMode?) ImportMandelbrot(string filename, bool importRenderMode) {
        double xCenter, yCenter, zoom;
        int maxIters;
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

                if (importRenderMode) {
                    switch (reader.ReadByte()) {
                        case (byte)RenderModeEnum.GRAYSCALE:
                            renderMode = new Grayscale();

                            break;
                        case (byte)RenderModeEnum.HUE:
                            renderMode = new Hue();

                            break;
                        case (byte)RenderModeEnum.LERP:
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
                        case (byte)RenderModeEnum.FLIP_FLOP:
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
                        case (byte)RenderModeEnum.TRIANGLE:
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
                } else {
                    return (xCenter, yCenter, zoom, maxIters, null);
                }
            }
        }
        
        return (xCenter, yCenter, zoom, maxIters, renderMode);
    }
}