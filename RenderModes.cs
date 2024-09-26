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

    public bool Julia { get; set; }
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

    public bool Julia { get; set; }
}

/**
 * Maps iterations in a rainbow.
 */
public class Hue : RenderMode
{
    public Color CalculateColor(int iterations, int maxIterations)
    {
        var hue = iterations / (maxIterations * 1.0);
        if (hue is >= 0 and < 1.0 / 6) // RED
            return Color.FromArgb(255, (int)(hue * 1530.0), 0);
        if (hue is >= 1.0 / 6 and < 2.0 / 6) // YELLOW
            return Color.FromArgb(255 - (int)((hue - 1.0 / 6) * 1530.0), 255, 0);
        if (hue is >= 2.0 / 6 and < 3.0 / 6) // GREEN
            return Color.FromArgb(0, 255, (int)((hue - 2.0 / 6) * 1530.0));
        if (hue is >= 3.0 / 6 and < 4.0 / 6) // CYAN
            return Color.FromArgb(0, 255 - (int)((hue - 3.0 / 6) * 1530.0), 255);
        if (hue is >= 4.0 / 6 and < 5.0 / 6) // BLUE
            return Color.FromArgb((int)((hue - 4.0 / 6) * 1530.0), 0, 255);
        if (hue is >= 5.0 / 6 and <= 1) // MAGENTA
            return Color.FromArgb(255, 0, 255 - (int)((hue - 5.0 / 6) * 1530.0));
        throw new Exception("Unreachable code reached.");
    }

    public byte GetId()
    {
        return (byte)RenderModeEnum.Hue;
    }

    public bool Julia { get; set; }
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
        var t = iterations / (maxIterations * 1.0);

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

    public bool Julia { get; set; }

    public static Lerp GenerateRandom()
    {
        var rnd = new Random();
        return new Lerp(
            Color.FromArgb(255, rnd.Next(0, 256), rnd.Next(0, 256), rnd.Next(0, 256)),
            Color.FromArgb(255, rnd.Next(0, 256), rnd.Next(0, 256), rnd.Next(0, 256))
        );
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

    public bool Julia { get; set; }

    public static FlipFlop GenerateRandom()
    {
        var rnd = new Random();
        return new FlipFlop(
            Color.FromArgb(255, rnd.Next(0, 256), rnd.Next(0, 256), rnd.Next(0, 256)),
            Color.FromArgb(255, rnd.Next(0, 256), rnd.Next(0, 256), rnd.Next(0, 256))
        );
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
        var index = iterations / (Repeat * TriangleSize) % Colors.Count;

        int startR = Colors[index].start.R;
        int startG = Colors[index].start.G;
        int startB = Colors[index].start.B;
        int endR = Colors[index].end.R;
        int endG = Colors[index].end.G;
        int endB = Colors[index].end.B;

        var level = iterations % TriangleSize;
        var t = level / (TriangleSize - 1.0);

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

    public bool Julia { get; set; }

    public static Triangle GenerateRandom()
    {
        var rnd = new Random();
        var colorLength = rnd.Next(2, 5);
        List<(Color, Color)> colors =  []
        ;

        for (var i = 0; i < colorLength; i++)
            colors.Add(
                (Color.FromArgb(255, rnd.Next(0, 256), rnd.Next(0, 256), rnd.Next(0, 256)),
                    Color.FromArgb(255, rnd.Next(0, 256), rnd.Next(0, 256), rnd.Next(0, 256)))
            );

        return new Triangle(
            colors,
            rnd.Next(3, 32),
            rnd.Next(1, 5)
        );
    }

    public static Triangle RAINBOW_TRIANGLE()
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
}