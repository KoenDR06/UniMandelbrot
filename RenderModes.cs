namespace Mandelbrot;

// Assigns a unique ID to every RenderMode
public enum RenderModeEnum
{
    Grayscale = 0,
    Hue = 1,
    Lerp = 2,
    FlipFlop = 3,
    Triangle = 4
}

public interface RenderMode
{
    public Color CalculateColor(int iterations, int maxIterations);
    public byte GetId();
}

/**
 * Maps iterations from black to white.
 */
public class Grayscale : RenderMode
{
    public Color CalculateColor(int iterations, int maxIterations)
    {
        return Color.FromArgb(
            (int)(iterations / (maxIterations / 255.0)),
            (int)(iterations / (maxIterations / 255.0)),
            (int)(iterations / (maxIterations / 255.0)));
    }

    public byte GetId()
    {
        return (byte)RenderModeEnum.Grayscale;
    }

    public override string ToString() {
        return "Grayscale";
    }
}

/**
 * Maps iterations in a rainbow.
 */
public class Hue : RenderMode
{
    public Color CalculateColor(int iterations, int maxIterations)
    {
        int hue = 8*iterations % 1530;
        if (hue is >= 0 and < 256) // RED
            return Color.FromArgb(255, hue, 0);
        if (hue is >= 256 and < 512) // YELLOW
            return Color.FromArgb(255 - (hue-256), 255, 0);
        if (hue is >= 512 and < 768) // GREEN
            return Color.FromArgb(0, 255, hue-512);
        if (hue is >= 768 and < 1024) // CYAN
            return Color.FromArgb(0, 255 - (hue-768), 255);
        if (hue is >= 1024 and < 1280) // BLUE
            return Color.FromArgb(hue-1024, 0, 255);
        if (hue is >= 1280 and < 1530) // MAGENTA
            return Color.FromArgb(255, 0, 255 - (hue-1280));
        throw new Exception("Unreachable code reached.");
    }

    public byte GetId()
    {
        return (byte)RenderModeEnum.Hue;
    }

    public override string ToString() {
        return "Hue";
    }
}

/**
 * Maps iterations from startColor to endColor.
 */
public class Lerp(Color startColor, Color endColor) : RenderMode
{
    public readonly Color Start = startColor;
    public readonly Color End = endColor;

    public Color CalculateColor(int iterations, int maxIterations)
    {
        double t = iterations / (maxIterations * 1.0);

        return Color.FromArgb(
            (int)((1 - t) * Start.R + t * End.R),
            (int)((1 - t) * Start.G + t * End.G),
            (int)((1 - t) * Start.B + t * End.B)
        );
    }

    public byte GetId()
    {
        return (byte)RenderModeEnum.Lerp;
    }

    public static Lerp GenerateRandom()
    {
        Random rnd = new Random();
        return new Lerp(
            Color.FromArgb(255, rnd.Next(0, 256), rnd.Next(0, 256), rnd.Next(0, 256)),
            Color.FromArgb(255, rnd.Next(0, 256), rnd.Next(0, 256), rnd.Next(0, 256))
        );
    }
    
    public static Lerp Default()
    {
        return new Lerp(
            Color.FromArgb(255, 012, 042, 232),
            Color.FromArgb(255, 255, 000, 230)
        );
    }
    
    public override string ToString() {
        return "Lerp";
    }
}

/**
 * Maps iterations as either colorA or colorB.
 */
public class FlipFlop(Color colorA, Color colorB) : RenderMode
{
    public readonly Color A = colorA;
    public readonly Color B = colorB;

    public Color CalculateColor(int iterations, int maxIterations)
    {
        if (iterations % 2 == 0)
            return A;
        return B;
    }

    public byte GetId()
    {
        return (byte)RenderModeEnum.FlipFlop;
    }

    public static FlipFlop GenerateRandom()
    {
        Random rnd = new Random();
        return new FlipFlop(
            Color.FromArgb(255, rnd.Next(0, 256), rnd.Next(0, 256), rnd.Next(0, 256)),
            Color.FromArgb(255, rnd.Next(0, 256), rnd.Next(0, 256), rnd.Next(0, 256))
        );
    }
    
    public static FlipFlop Default()
    {
        return new FlipFlop(
            Color.FromArgb(255, 255, 255, 255),
            Color.FromArgb(255, 000, 000, 000)
        );
    }
    
    public override string ToString() {
        return "FlipFlop";
    }
}

/**
 * Maps iterations from colors[i].start to colors[i].end in size steps n times, when it will switch to i+1.
 */
public class Triangle(List<(Color start, Color end)> colorList, int size, int n) : RenderMode
{
    public readonly List<(Color start, Color end)> Colors = colorList;
    public readonly int TriangleSize = size;
    public readonly int Repeat = n;

    public Color CalculateColor(int iterations, int maxIterations)
    {
        int index = iterations / (Repeat * TriangleSize) % Colors.Count;

        int startR = Colors[index].start.R;
        int startG = Colors[index].start.G;
        int startB = Colors[index].start.B;
        int endR = Colors[index].end.R;
        int endG = Colors[index].end.G;
        int endB = Colors[index].end.B;

        int level = iterations % TriangleSize;
        double t = level / (TriangleSize - 1.0);

        return Color.FromArgb(
            (int)((1 - t) * startR + t * endR),
            (int)((1 - t) * startG + t * endG),
            (int)((1 - t) * startB + t * endB)
        );
    }

    public byte GetId()
    {
        return (byte)RenderModeEnum.Triangle;
    }

    public static Triangle GenerateRandom()
    {
        Random rnd = new Random();
        int colorLength = rnd.Next(2, 5);
        List<(Color, Color)> colors = new List<(Color, Color)>();

        for (var i = 0; i < colorLength; i++)
            colors.Add((
                Color.FromArgb(255, rnd.Next(0, 256), rnd.Next(0, 256), rnd.Next(0, 256)),
                Color.FromArgb(255, rnd.Next(0, 256), rnd.Next(0, 256), rnd.Next(0, 256)))
            );

        return new Triangle(
            colors,
            rnd.Next(3, 32),
            rnd.Next(1, 5)
        );
    }

    public static Triangle Default()
    {
        return new Triangle(
            new List<(Color, Color)>
            {
                (Color.Black, Color.FromArgb(255, 255, 000, 000)),
                (Color.Black, Color.FromArgb(255, 255, 128, 000)),
                (Color.Black, Color.FromArgb(255, 255, 255, 000)),
                (Color.Black, Color.FromArgb(255, 128, 255, 000)),
                (Color.Black, Color.FromArgb(255, 000, 255, 000)),
                (Color.Black, Color.FromArgb(255, 000, 255, 128)),
                (Color.Black, Color.FromArgb(255, 000, 255, 255)),
                (Color.Black, Color.FromArgb(255, 000, 128, 255)),
                (Color.Black, Color.FromArgb(255, 000, 000, 255)),
                (Color.Black, Color.FromArgb(255, 128, 000, 255)),
                (Color.Black, Color.FromArgb(255, 255, 000, 255)),
                (Color.Black, Color.FromArgb(255, 255, 000, 128))
            },
            10,
            1);
    }
    
    public override string ToString() {
        return "Triangle";
    }
}