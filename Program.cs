using System.Diagnostics;
using System.Drawing.Imaging;
using Mandelbrot;

int resolution = 4096;

Form screen = new Form();
screen.ClientSize = new Size(resolution, resolution);
Label label = new Label();
screen.Controls.Add(label);
label.Location = new Point(0, 0);
label.Size = new Size(resolution, resolution);

string filename = Directory.GetCurrentDirectory() + "..\\..\\..\\..\\presets\\infinite_spiral.mandel";
// Renderer renderer = new Renderer(resolution, 256, Triangle.RAINBOW_TRIANGLE());
Renderer renderer = Renderer.ImportMandelbrot(filename, resolution);

void Render() {
    Stopwatch stopWatch = new Stopwatch();
    stopWatch.Start();
    label.Image = renderer.RenderMandelbrot();
    stopWatch.Stop();
    Console.WriteLine(stopWatch.Elapsed);
}

void OnScroll(object? o, MouseEventArgs mea) {
    if (mea.Delta >= 1200) { renderer.MaxIters *= 2; }
    else if (mea.Delta <= -1200) { renderer.MaxIters /= 2; }
    else renderer.MaxIters += mea.Delta / 12;
    
    if (renderer.MaxIters <= 1) renderer.MaxIters = 1;

    Render();
}

void OnClick(object? o, MouseEventArgs mea) {
    renderer.XCenter = Math.Exp(-1.0 * renderer.Zoom) * (4.0 * mea.X / resolution - 2.0) + renderer.XCenter;
    renderer.YCenter = Math.Exp(-1.0 * renderer.Zoom) * (4.0 * mea.Y / resolution - 2.0) + renderer.YCenter;
    if (mea.Button == MouseButtons.Left) {
        renderer.Zoom += 1;
        renderer.MaxIters += 10;
    } else if (mea.Button == MouseButtons.Right) {
        renderer.Zoom -= 1;
        renderer.MaxIters -= 10;
    }
    Render();
}


label.MouseWheel += OnScroll;
label.MouseClick += OnClick;

Render();
Application.Run(screen);

filename = Directory.GetCurrentDirectory() + "..\\..\\..\\..\\data.mandel";
renderer.ExportMandelbrot(filename);

label.Image = renderer.RenderMandelbrot();


renderer.SaveRenderedImage(Directory.GetCurrentDirectory() + "..\\..\\..\\..\\render.png");