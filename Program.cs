using System.Drawing.Imaging;
using Mandelbrot;

int resolution = 1024;

Form screen = new Form();
screen.ClientSize = new Size(resolution, resolution);
Label label = new Label();
screen.Controls.Add(label);
label.Location = new Point(0, 0);
label.Size = new Size(resolution, resolution);

Bitmap img = new Bitmap(resolution, resolution);

string filename = "C:\\Users\\koend\\RiderProjects\\Mandelbrot\\presets\\default.mandel";

(double xCenter, double yCenter, double zoom, int maxIters, RenderMode? renderMode) = Renderer.ImportMandelbrot(filename, true);

renderMode.julia = false;

renderMode = Triangle.RAINBOW_TRIANGLE();

void Render() {
    img.Dispose();
    img = new Bitmap(resolution, resolution);
    label.Image = Renderer.RenderMandelbrot(  
        xCenter,
        yCenter,
        zoom,
        maxIters,
        renderMode,
        img,
        juliaX: -0.5423295792003203,
        juliaY: 0.6149806437039895
    );
}

void OnScroll(object? o, MouseEventArgs mea) {
    if (mea.Delta >= 1200) { maxIters *= 2; }
    else if (mea.Delta <= -1200) { maxIters /= 2; }
    else maxIters += mea.Delta / 12;
    
    if (maxIters <= 1) maxIters = 1;

    Render();
}

void OnClick(object? o, MouseEventArgs mea) {
    xCenter = Math.Exp(-1.0 * zoom) * (4.0 * mea.X / resolution - 2.0) + xCenter;
    yCenter = Math.Exp(-1.0 * zoom) * (4.0 * mea.Y / resolution - 2.0) + yCenter;
    if (mea.Button == MouseButtons.Left) {
        zoom += 1;
        maxIters += 10;
    } else if (mea.Button == MouseButtons.Right) {
        zoom -= 1;
        maxIters -= 10;
    }
    Render();
}


label.MouseWheel += OnScroll;
label.MouseClick += OnClick;

Render();
Application.Run(screen);

filename = "C:\\Users\\koend\\RiderProjects\\Mandelbrot\\data.mandel";
Renderer.ExportMandelbrot(filename, xCenter, yCenter, zoom, maxIters, renderMode);

Bitmap renderImage = new Bitmap(512, 512);
label.Image = Renderer.RenderMandelbrot(  
    xCenter,
    yCenter,
    zoom,
    maxIters,
    renderMode,
    renderImage
);

renderImage.Save("C:\\Users\\koend\\RiderProjects\\Mandelbrot\\render.png", ImageFormat.Png);