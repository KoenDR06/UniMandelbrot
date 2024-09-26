using System.Diagnostics;
using Accessibility;
using Mandelbrot;

// Settings
var rendering = false;
var resolution = 800;
var maxIterations = 256;
var renderer = new Renderer(resolution, maxIterations, Triangle.RAINBOW_TRIANGLE(), cores:16);

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
iterationLabel.InputField.Text = renderer.MaxIterations.ToString();

var horTransLabel = new LabeledInput("Horizontal translation:");
horTransLabel.InputField.Text = renderer.XCenter.ToString();

var verTransLabel = new LabeledInput("Vertical translation:");
verTransLabel.InputField.Text = renderer.YCenter.ToString();

var renderButton = new Button()
{
    Text = "Render",
    BackColor = Color.White,
    ForeColor = Color.FromArgb(34, 76, 91),
    AutoSize = true
};

var resetButton = new Button()
{
    Text = "Reset",
    BackColor = Color.White,
    ForeColor = Color.FromArgb(34, 76, 91),
    AutoSize = true
};

var exportButton = new Button()
{
    Text = "Export",
    BackColor = Color.White,
    ForeColor = Color.FromArgb(34, 76, 91),
    AutoSize = true
};

controlPanel.Controls.Add(title);
controlPanel.Controls.Add(zoomLabel);
controlPanel.Controls.Add(iterationLabel);
controlPanel.Controls.Add(horTransLabel);
controlPanel.Controls.Add(verTransLabel);
controlPanel.Controls.Add(renderButton);
controlPanel.Controls.Add(resetButton);
controlPanel.Controls.Add(exportButton);
//


var mandelbrotImage = new Label
{
    Location = new Point(250, 0),
    Size = new Size(resolution, resolution)
};

screen.Controls.Add(controlPanel);
screen.Controls.Add(mandelbrotImage);

// BACKLOG: de stopwatch ff weghalen? was vgm op zich alleen debugging?
async void Render()
{
    rendering = true;

    var stopWatch = new Stopwatch();
    stopWatch.Start();

    try
    {
        renderer.Zoom = int.Parse(zoomLabel.InputField.Text);
        renderer.MaxIterations = int.Parse(iterationLabel.InputField.Text);
        renderer.XCenter = double.Parse(horTransLabel.InputField.Text);
        renderer.YCenter = double.Parse(verTransLabel.InputField.Text);
    }
    catch
    {
        MessageBox.Show("Please make sure all the inputs are valid.");
    }


    exportButton.Enabled = false;
    resetButton.Enabled = false;
    renderButton.Enabled = false;
    renderButton.Text = "Rendering...";
    
    await Task.Run(async () =>
    {
        mandelbrotImage.Image = await renderer.RenderMandelbrot();
        screen.Refresh();
    });
    
    exportButton.Enabled = true;
    resetButton.Enabled = true;
    renderButton.Enabled = true;
    renderButton.Text = "Render";

    stopWatch.Stop();
    Console.WriteLine(stopWatch.Elapsed);
    
    rendering = false;
}

void OnScroll(object? o, MouseEventArgs mea)
{
    if (rendering) return;
    
    if (mea.Delta >= 1200)
        renderer.MaxIterations *= 2;
    else if (mea.Delta <= -1200)
        renderer.MaxIterations /= 2;
    else renderer.MaxIterations += mea.Delta / 12;

    if (renderer.MaxIterations <= 1) renderer.MaxIterations = 1;

    iterationLabel.InputField.Text = renderer.MaxIterations.ToString();
    
    Render();
}

void OnClick(object? o, MouseEventArgs mea)
{
    if (rendering) return;

    renderer.XCenter = Math.Exp(-1.0 * renderer.Zoom) * (4.0 * mea.X / resolution - 2.0) + renderer.XCenter;
    renderer.YCenter = Math.Exp(-1.0 * renderer.Zoom) * (4.0 * mea.Y / resolution - 2.0) + renderer.YCenter;

    if (mea.Button == MouseButtons.Left)
    {
        renderer.Zoom += 1;
        renderer.MaxIterations += 10;
    }
    else if (mea.Button == MouseButtons.Right)
    {
        renderer.Zoom -= 1;
        renderer.MaxIterations -= 10;
    }

    zoomLabel.InputField.Text = renderer.Zoom.ToString();
    iterationLabel.InputField.Text = renderer.MaxIterations.ToString();
    horTransLabel.InputField.Text = renderer.XCenter.ToString();
    verTransLabel.InputField.Text = renderer.YCenter.ToString();

    Render();
}

void Reset(object? o, EventArgs mea)
{
    if (rendering) return;

    renderer.XCenter = 0;
    horTransLabel.InputField.Text = "0";
    
    renderer.YCenter = 0;
    verTransLabel.InputField.Text = "0";
    
    renderer.Zoom = 0;
    zoomLabel.InputField.Text = "0";

    renderer.MaxIterations = 256;
    
    Render();
}

mandelbrotImage.MouseWheel += OnScroll;
mandelbrotImage.MouseClick += OnClick;
renderButton.Click += (_, _) => { Render(); };
resetButton.Click += Reset;
exportButton.Click += (_, _) =>
{
    renderer.SaveRenderedImage();
};

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