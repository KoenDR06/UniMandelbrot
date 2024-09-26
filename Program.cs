using System.Diagnostics;
using Mandelbrot;

// Settings
var resolution = 800;
var maxIterations = 256;


var screen = new Form
{
    ClientSize = new Size(resolution + 250, resolution),
    Text = "Mandelbrot"
    // BACKLOG: add icon?
};

// Control panel
var controlPanel = new FlowLayoutPanel
{
    Location = new Point(0, 0),
    FlowDirection = FlowDirection.TopDown,
    Dock = DockStyle.Left,
    Margin = new Padding(0),
    Padding = new Padding(0),
    BackColor = Color.Red
};


var mandelbrotImage = new Label
{
    Location = new Point(250, 0),
    Size = new Size(resolution, resolution)
};

screen.Controls.Add(mandelbrotImage);

var filename = Directory.GetCurrentDirectory() + "..\\..\\..\\..\\presets\\infinite_spiral.mandel";
var renderer = new Renderer(resolution, maxIterations, Triangle.RAINBOW_TRIANGLE());

void Render()
{
    var stopWatch = new Stopwatch();
    stopWatch.Start();
    mandelbrotImage.Image = renderer.RenderMandelbrot();
    stopWatch.Stop();
    Console.WriteLine(stopWatch.Elapsed);
}

void OnScroll(object? o, MouseEventArgs mea)
{
    if (mea.Delta >= 1200)
        renderer.MaxIters *= 2;
    else if (mea.Delta <= -1200)
        renderer.MaxIters /= 2;
    else renderer.MaxIters += mea.Delta / 12;

    if (renderer.MaxIters <= 1) renderer.MaxIters = 1;

    Render();
}

void OnClick(object? o, MouseEventArgs mea)
{
    renderer.XCenter = Math.Exp(-1.0 * renderer.Zoom) * (4.0 * mea.X / resolution - 2.0) + renderer.XCenter;
    renderer.YCenter = Math.Exp(-1.0 * renderer.Zoom) * (4.0 * mea.Y / resolution - 2.0) + renderer.YCenter;

    if (mea.Button == MouseButtons.Left)
    {
        renderer.Zoom += 1;
        renderer.MaxIters += 10;
    }
    else if (mea.Button == MouseButtons.Right)
    {
        renderer.Zoom -= 1;
        renderer.MaxIters -= 10;
    }

    Render();
}


mandelbrotImage.MouseWheel += OnScroll;
mandelbrotImage.MouseClick += OnClick;

Render();
Application.Run(screen);

// BACKLOG: wat doet dit hier?
//
// filename = Directory.GetCurrentDirectory() + "..\\..\\..\\..\\data.mandel";
// renderer.ExportMandelbrot(filename);
//
// // mandelbrotImage.Image = renderer.RenderMandelbrot();
//
//
// renderer.SaveRenderedImage(Directory.GetCurrentDirectory() + "..\\..\\..\\..\\render.png");