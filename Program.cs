using System.Diagnostics;
using System.Reflection;
using Mandelbrot;

// Import and use embedded resources
Assembly currentAssembly = Assembly.GetExecutingAssembly();
string[] resourceNames = currentAssembly.GetManifestResourceNames();

Stream stream = currentAssembly.GetManifestResourceStream("Mandelbrot.icon.ico");
Icon appIcon = new Icon(stream);


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
    Icon = appIcon,
    MaximizeBox = false
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

Button randomiseRenderModeButton = new Button()
{
    Text = "Random Kleur",
    BackColor = Color.White,
    ForeColor = Color.FromArgb(34, 76, 91),
    AutoSize = true
};

Button renderButton = new Button()
{
    Text = "Render",
    BackColor = Color.White,
    ForeColor = Color.FromArgb(34, 76, 91),
    AutoSize = true
};
screen.AcceptButton = renderButton;

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

ComboBox importRenderField = new ComboBox()
{
    Text = "Choose Render"
};

foreach (string filename in Directory.GetFiles(Directory.GetCurrentDirectory() + "..\\..\\..\\..\\presets", "*.mandel")) {
    importRenderField.Items.Add(filename.Split("\\").Last());
}

TrackBar coreSlider = new TrackBar()
{
    Minimum = 1,
    Maximum = Environment.ProcessorCount,
    Value = Environment.ProcessorCount / 2
};

ComboBox renderModeField = new ComboBox()
{
    Items = { "Grayscale", "Hue", "Lerp", "FlipFlop", "Triangle" },
    Text = renderer.RenderMode.ToString()
};

CheckBox juliaCheckBox = new CheckBox
{
    Checked = renderer.Julia
};

Control[] controls = new Control[]
{
    title, zoomLabel, iterationLabel, horTransLabel, verTransLabel, renderModeField, randomiseRenderModeButton, juliaCheckBox, renderButton, resetButton,
    coreSlider, exportImageButton, exportRenderButton, importRenderField
};

foreach (Control control in controls)
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
    
    exportImageButton.Enabled = false;
    exportRenderButton.Enabled = false;
    resetButton.Enabled = false;
    renderButton.Enabled = false;
    randomiseRenderModeButton.Enabled = false;
    renderButton.Text = "Rendering...";
    
    await Task.Run(async () =>
    {
        mandelbrotImage.Image = await renderer.RenderMandelbrot();
        screen.Refresh();
    });
    
    exportRenderButton.Enabled = true;
    exportImageButton.Enabled = true;
    resetButton.Enabled = true;
    renderButton.Enabled = true;
    randomiseRenderModeButton.Enabled = true;
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
        if (renderer.MaxIterations <= 0) renderer.MaxIterations = 1;
        renderer.XCenter = double.Parse(horTransLabel.InputField.Text);
        renderer.YCenter = double.Parse(verTransLabel.InputField.Text);
        renderer.Cores = coreSlider.Value;
        
        switch (renderModeField.Text) {
            case "Grayscale":
                renderer.RenderMode = new Grayscale();
                break;

            case "Hue":
                renderer.RenderMode = new Hue();
                break;

            case "FlipFlop":
                renderer.RenderMode = FlipFlop.Default();
                break;

            case "Lerp":
                renderer.RenderMode = Lerp.Default();
                break;

            case "Triangle":
                renderer.RenderMode = Triangle.Default();
                break;
            
            default:
                throw new ArgumentException("Not a renderMode");
        }
    }
    catch {
        zoomLabel.InputField.Text = renderer.Zoom.ToString();
        iterationLabel.InputField.Text = renderer.MaxIterations.ToString();
        horTransLabel.InputField.Text = renderer.XCenter.ToString();
        verTransLabel.InputField.Text = renderer.YCenter.ToString();
        coreSlider.Value = renderer.Cores;
        renderModeField.Text = renderer.RenderMode.ToString();
        
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
    else if (mea.Button == MouseButtons.Middle && !renderer.Julia)
    {
        renderer.SetJuliaCoords(mea);
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
    
    string filename = Directory.GetCurrentDirectory() + $"..\\..\\..\\..\\presets\\render_{year}-{month}-{day}-{hour}{minute}{second}.mandel";

    importRenderField.Items.Add(filename.Split("\\").Last());
    
    renderer.ExportMandelbrot(filename);
};

randomiseRenderModeButton.Click += (_, _) => 
{
    if (renderer.RenderMode is Lerp) {
        renderer.RenderMode = Lerp.GenerateRandom();
    } else if (renderer.RenderMode is Triangle) {
        renderer.RenderMode = Triangle.GenerateRandom();
    } else if (renderer.RenderMode is FlipFlop) {
        renderer.RenderMode = FlipFlop.GenerateRandom();
    } else if (renderer.RenderMode is Grayscale) {
        renderer.RenderMode = new Grayscale();
    } else if (renderer.RenderMode is Hue) {
        renderer.RenderMode = new Hue();
    }
    Render();
    UpdateUIFields();
};

renderModeField.TextChanged += (_, _) =>
{
    if (rendering) return;
    
    switch (renderModeField.Text) {
        case "Grayscale":
            renderer.RenderMode = new Grayscale();
            break;
        case "Hue":
            renderer.RenderMode = new Hue();
            break;
        case "FlipFlop":
            renderer.RenderMode = FlipFlop.Default();
            break;
        case "Lerp":
            renderer.RenderMode = Lerp.Default();
            break;
        case "Triangle":
            renderer.RenderMode = Triangle.Default();
            break;
    }
};

juliaCheckBox.Click += (_, _) =>
{
    renderer.Julia = juliaCheckBox.Checked;
    Render();
};

importRenderField.TextChanged += (_, _) =>
{
    renderer.ImportMandelbrot(Directory.GetCurrentDirectory() + "..\\..\\..\\..\\presets\\" + importRenderField.Text);
    UpdateUIFields();
    Render();
};

Render();
Application.Run(screen);