using System.Diagnostics;
using Mandelbrot;

// Settings
bool rendering = false;
int resolution = 800;
int maxIterations = 256;
Renderer renderer = new Renderer(resolution, maxIterations, Triangle.GenerateRandom(), Environment.ProcessorCount);

// BACKLOG: Hardcoding the path to the icon is probably a bad idea
Form screen = new Form
{
    ClientSize = new Size(resolution + 250, resolution),
    Text = "Mandelbrot",
    FormBorderStyle = FormBorderStyle.FixedSingle,
    Icon = new Icon("../../../icon.ico")
};

// Control panel
FlowLayoutPanel controlPanel = new FlowLayoutPanel
{
    Location = new Point(0, 0),
    FlowDirection = FlowDirection.TopDown,
    Dock = DockStyle.Left,
    Margin = new Padding(0),
    Padding = new Padding(0),
    BackColor = Color.FromArgb(34, 76, 91),
    Width = 250
};

Label title = new Label()
{
    Text = "Mandelbrot",
    Font = new Font("OCR-A Extended", 16, FontStyle.Bold),
    AutoSize = true,
    ForeColor = Color.White
};

LabeledInput zoomLabel = new LabeledInput("Zoom:");
zoomLabel.InputField.Text = renderer.Zoom.ToString();

LabeledInput iterationLabel = new LabeledInput("Max iterations:");
iterationLabel.InputField.Text = renderer.MaxIterations.ToString();

LabeledInput horTransLabel = new LabeledInput("Horizontal translation:");
horTransLabel.InputField.Text = renderer.XCenter.ToString();

LabeledInput verTransLabel = new LabeledInput("Vertical translation:");
verTransLabel.InputField.Text = renderer.YCenter.ToString();

Button renderButton = new Button()
{
    Text = "Render",
    BackColor = Color.White,
    ForeColor = Color.FromArgb(34, 76, 91),
    AutoSize = true
};

Button resetButton = new Button()
{
    Text = "Reset",
    BackColor = Color.White,
    ForeColor = Color.FromArgb(34, 76, 91),
    AutoSize = true
};

Button exportImageButton = new Button()
{
    Text = "Export Image",
    BackColor = Color.White,
    ForeColor = Color.FromArgb(34, 76, 91),
    AutoSize = true
};

Button exportRenderButton = new Button()
{
    Text = "Export Render",
    BackColor = Color.White,
    ForeColor = Color.FromArgb(34, 76, 91),
    AutoSize = true
};

Button importRenderButton = new Button()
{
    Text = "Import Render",
    BackColor = Color.White,
    ForeColor = Color.FromArgb(34, 76, 91),
    AutoSize = true
};
var coreSlider = new TrackBar()
{
    Minimum = 1,
    Maximum = Environment.ProcessorCount
};

ComboBox renderModeField = new ComboBox()
{
    Items = { "Grayscale", "Hue", "Lerp", "FlipFlop", "Triangle" },
    Text = renderer.RenderMode.ToString()
};

Control[] controls = new Control[]
{
    title, zoomLabel, iterationLabel, horTransLabel, verTransLabel, renderModeField, renderButton, resetButton,
    coreSlider, exportImageButton, exportRenderButton, importRenderButton
};

foreach (var control in controls)
    controlPanel.Controls.Add(control);

Label mandelbrotImage = new Label
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

    Stopwatch stopWatch = new Stopwatch();
    stopWatch.Start();

    importRenderButton.Enabled = false;
    exportImageButton.Enabled = false;
    exportRenderButton.Enabled = false;
    resetButton.Enabled = false;
    renderButton.Enabled = false;
    renderButton.Text = "Rendering...";
    
    await Task.Run(async () =>
    {
        mandelbrotImage.Image = await renderer.RenderMandelbrot();
        screen.Refresh();
    });
    
    importRenderButton.Enabled = true;
    exportRenderButton.Enabled = true;
    exportImageButton.Enabled = true;
    resetButton.Enabled = true;
    renderButton.Enabled = true;
    renderButton.Text = "Render";

    stopWatch.Stop();
    Console.WriteLine(stopWatch.Elapsed);
    
    rendering = false;
}

void UpdateRenderParams() {
    try
    {
        renderer.Zoom = double.Parse(zoomLabel.InputField.Text);
        renderer.MaxIterations = int.Parse(iterationLabel.InputField.Text);
        renderer.XCenter = double.Parse(horTransLabel.InputField.Text);
        renderer.YCenter = double.Parse(verTransLabel.InputField.Text);
        renderer.Cores = coreSlider.Value;

        if (renderer.RenderMode.ToString() != renderModeField.Text) {
            switch (renderModeField.Text) {
                case "Grayscale":
                    renderer.RenderMode = new Grayscale();
                    break;

                case "Hue":
                    renderer.RenderMode = new Hue();
                    break;

                case "Flipflop":
                    renderer.RenderMode = FlipFlop.GenerateRandom();
                    break;

                case "Lerp":
                    renderer.RenderMode = Lerp.GenerateRandom();
                    break;

                case "Triangle":
                    renderer.RenderMode = Triangle.RAINBOW_TRIANGLE();
                    break;

                default:
                    throw new Exception("Unreachable code reached.");
            }
        }
    }
    catch
    {
        MessageBox.Show("Please make sure all the inputs are valid.");
    }
}

void UpdateUIFields() {
    try {
        zoomLabel.InputField.Text = renderer.Zoom.ToString();
        iterationLabel.InputField.Text = renderer.MaxIterations.ToString();
        horTransLabel.InputField.Text = renderer.XCenter.ToString();
        verTransLabel.InputField.Text = renderer.YCenter.ToString();
        renderModeField.Text = renderer.RenderMode.ToString();
        coreSlider.Text = renderer.Cores.ToString();
    }
    catch
    {
        MessageBox.Show("Please make sure all the inputs are valid.");
    }
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
renderButton.Click += (_, _) =>
{
    UpdateRenderParams();
    Render(); 
};
resetButton.Click += Reset;

exportImageButton.Click += (_, _) => 
{
    DateTime date = DateTime.Now;

    string year = date.Year.ToString().PadLeft(4, '0');
    string month = date.Month.ToString().PadLeft(2, '0');
    string day = date.Day.ToString().PadLeft(2, '0');
    string hour = date.Hour.ToString().PadLeft(2, '0');
    string minute = date.Minute.ToString().PadLeft(2, '0');
    string second = date.Second.ToString().PadLeft(2, '0');
    
    string filename = Directory.GetCurrentDirectory() + $"..\\..\\..\\..\\render_{year}-{month}-{day}-{hour}{minute}{second}.png";
    renderer.SaveRenderedImage(filename);
};

exportRenderButton.Click += (_, _) =>
{
    DateTime date = DateTime.Now;

    string year = date.Year.ToString().PadLeft(4, '0');
    string month = date.Month.ToString().PadLeft(2, '0');
    string day = date.Day.ToString().PadLeft(2, '0');
    string hour = date.Hour.ToString().PadLeft(2, '0');
    string minute = date.Minute.ToString().PadLeft(2, '0');
    string second = date.Second.ToString().PadLeft(2, '0');
    
    string filename = Directory.GetCurrentDirectory() + $"..\\..\\..\\..\\render_{year}-{month}-{day}-{hour}{minute}{second}.mandel";
    renderer.ExportMandelbrot(filename);
};

importRenderButton.Click += (_, _) => 
{
    string filename = Directory.GetCurrentDirectory() + "..\\..\\..\\..\\presets\\infinite_spiral.mandel";
    renderer.ImportMandelbrot(filename);
    Render();
    UpdateUIFields();
};

Render();
Application.Run(screen);