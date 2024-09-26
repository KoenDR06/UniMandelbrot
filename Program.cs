using System.Diagnostics;
using Mandelbrot;

// Settings
var resolution = 800;
var maxIterations = 256;
var renderer = new Renderer(resolution, maxIterations, Triangle.RAINBOW_TRIANGLE());


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
    BackColor = Color.FromArgb(34, 76, 91),
    Width = 250
};

var title = new Label()
{
    Text = "Mandelbrot",
    Font = new Font("OCR-A Extended", 16, FontStyle.Bold),
    AutoSize = true,
    ForeColor = Color.White
};

var zoomLabel = new LabeledInput("Zoom:");
zoomLabel.InputField.Text = renderer.Zoom.ToString();

var iterationLabel = new LabeledInput("Max iterations:");
iterationLabel.InputField.Text = renderer.MaxIters.ToString();

var horTransLabel = new LabeledInput("Horizontal translation:");
horTransLabel.InputField.Text = renderer.XCenter.ToString();

var verTransLabel = new LabeledInput("Vertical translation:");
verTransLabel.InputField.Text = renderer.YCenter.ToString();

var renderButton = new Button()
{
    Text = "Render",
    BackColor = Color.White,
    ForeColor = Color.FromArgb(34, 76, 91)
};

var resetButton = new Button()
{
    Text = "Reset",
    BackColor = Color.White,
    ForeColor = Color.FromArgb(34, 76, 91)
};

controlPanel.Controls.Add(title);
controlPanel.Controls.Add(zoomLabel);
controlPanel.Controls.Add(iterationLabel);
controlPanel.Controls.Add(horTransLabel);
controlPanel.Controls.Add(verTransLabel);
controlPanel.Controls.Add(renderButton);
controlPanel.Controls.Add(resetButton);
//


var mandelbrotImage = new Label
{
    Location = new Point(250, 0),
    Size = new Size(resolution, resolution)
};

screen.Controls.Add(controlPanel);
screen.Controls.Add(mandelbrotImage);

// BACKLOG: de stopwatch ff weghalen? was vgm op zich alleen debugging?
void Render()
{
    var stopWatch = new Stopwatch();
    stopWatch.Start();

    renderer.Zoom = int.Parse(zoomLabel.InputField.Text);
    renderer.MaxIters = int.Parse(iterationLabel.InputField.Text);
    renderer.XCenter = double.Parse(horTransLabel.InputField.Text);
    renderer.YCenter = double.Parse(verTransLabel.InputField.Text);
    
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

    iterationLabel.InputField.Text = renderer.MaxIters.ToString();
    
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

    zoomLabel.InputField.Text = renderer.Zoom.ToString();
    iterationLabel.InputField.Text = renderer.MaxIters.ToString();
    horTransLabel.InputField.Text = renderer.XCenter.ToString();
    verTransLabel.InputField.Text = renderer.YCenter.ToString();

    Render();
}

void Reset(object? o, EventArgs mea)
{
    renderer.XCenter = 0;
    horTransLabel.InputField.Text = "0";
    
    renderer.YCenter = 0;
    verTransLabel.InputField.Text = "0";
    
    renderer.Zoom = 0;
    zoomLabel.InputField.Text = "0";

    renderer.MaxIters = 256;
    
    Render();
}

mandelbrotImage.MouseWheel += OnScroll;
mandelbrotImage.MouseClick += OnClick;
renderButton.Click += (object? o, EventArgs mea) => { Render(); };
resetButton.Click += Reset;

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