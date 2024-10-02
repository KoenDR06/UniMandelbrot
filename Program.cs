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
int maxIterations = 256;
Renderer renderer = new Renderer(800, maxIterations, Triangle.GenerateRandom(), Environment.ProcessorCount);

Form screen = new Form
{
    ClientSize = new Size(800 + 250, 800),
    Text = "Mandelbrot",
    Icon = appIcon,
    BackColor = Color.FromArgb(34, 76, 91),
    FormBorderStyle = FormBorderStyle.FixedSingle, // Prevents window resizing
    MaximizeBox = false // Prevents window maximising
};

// I have had a long fought battle with FlowLayout and GroupBox, but alas, I lost
// This is still semi-responsive as I can easily add elements between elements that are already there without
// having to change all the hardcoded values :)
Point locationPoint = new Point(12, 12);

Label title = new Label()
{
    Text = "MandelScope",
    Font = new Font("Bahnschrift", 18, FontStyle.Bold),
    AutoSize = true,
    ForeColor = Color.White,
    Location = locationPoint
};
locationPoint.Offset(0, 40);;

LabeledInput zoomLabel = new LabeledInput("Zoom:", renderer.Zoom.ToString(), locationPoint);
locationPoint.Offset(0, 30);
LabeledInput iterationLabel = new LabeledInput("Max iterations:", renderer.MaxIterations.ToString(),locationPoint);
locationPoint.Offset(0, 30);
LabeledInput horTransLabel = new LabeledInput("Horizontal translation:", renderer.XCenter.ToString(), locationPoint);
locationPoint.Offset(0, 30);
LabeledInput verTransLabel = new LabeledInput("Vertical translation:", renderer.YCenter.ToString(), locationPoint);
locationPoint.Offset(0, 30);

Label renderModeLabel = new Label()
{
    Text = "Render mode:",
    TextAlign = ContentAlignment.MiddleLeft,
    AutoSize = true,
    ForeColor = Color.White,
    Location = locationPoint
};
locationPoint.Offset(250 - renderModeLabel.Width - 24, 0);
ComboBox renderModeField = new ComboBox()
{
    Items = { "Grayscale", "Hue", "Lerp", "FlipFlop", "Triangle" },
    Text = renderer.RenderMode.ToString(),
    Location = locationPoint,
    Width = 100
};
locationPoint.Offset(-250 + renderModeLabel.Width + 24, 30);


Label presetsLabel = new Label()
{
    Text = "Or choose a preset:",
    TextAlign = ContentAlignment.MiddleLeft,
    AutoSize = true,
    ForeColor = Color.White,
    Location = locationPoint
};
locationPoint.Offset(250 - renderModeLabel.Width - 24, 0);

ComboBox importRenderField = new ComboBox()
{
    Text = "Choose preset",
    Location = locationPoint,
    Width = 100
};
locationPoint.Offset(-250 + renderModeLabel.Width + 24, 50);

try {
    foreach (string filename in Directory.GetFiles(Directory.GetCurrentDirectory() + "..\\..\\..\\..\\presets", "*.mandel")) {
        importRenderField.Items.Add(filename.Split("\\").Last().Split(".").First());
    }
} catch(DirectoryNotFoundException) { /* womp womp */}


Button randomiseRenderModeButton = new StyledButton("Random colours", locationPoint);
locationPoint.Offset(0, 30);
Button renderButton = new StyledButton("Render", locationPoint, true);
locationPoint.Offset(125 - 6, 0);
Button resetButton = new StyledButton("Reset", locationPoint, true);
locationPoint.Offset(-125 + 6, 4 * 10);
Button exportRenderButton = new StyledButton("Export as render", locationPoint);
locationPoint.Offset(0, 30);
Button exportImageButton = new StyledButton("Export as image", locationPoint);
locationPoint.Offset(0, 50);
screen.AcceptButton = renderButton; // Makes it that Enter "presses" the Render button

Label coreLabel = new Label()
{
    Text = "Cores to use:",
    TextAlign = ContentAlignment.MiddleLeft,
    AutoSize = true,
    ForeColor = Color.White,
    Location = locationPoint
};
locationPoint.Offset(250 - renderModeLabel.Width - 24, 0);
TrackBar coreSlider = new TrackBar()
{
    Minimum = 1,
    Maximum = Environment.ProcessorCount, 
    Value = Environment.ProcessorCount / 2,
    Location = locationPoint,
    TickStyle = TickStyle.None,
    Width = 100
};
locationPoint.Offset(-250 + renderModeLabel.Width + 24, 50);

Label timeDisplay = new Label
{
    Size = new Size(250, 20),
    Location = new Point(0, 800 - 20),
    BackColor = Color.FromArgb(34, 76, 91),
    ForeColor = Color.White,
};

Label mandelbrotImage = new Label
{
    Location = new Point(250, 0),
    Size = new Size(800, 800)
};

// Actually show the things on the screen
LabeledInput[] labels = { zoomLabel, iterationLabel, horTransLabel, verTransLabel };
foreach (var lab in labels)
{
    screen.Controls.Add(lab.TextLabel);
    screen.Controls.Add(lab.InputField);
}

Control[] controlList =
{
    title, renderModeField, randomiseRenderModeButton, renderButton, resetButton, coreSlider,
    exportImageButton, exportRenderButton, importRenderField, timeDisplay, mandelbrotImage, renderModeLabel,
    presetsLabel, coreLabel
};
foreach (var control in controlList) screen.Controls.Add(control);


async void Render()
{
    // Tell the GUI to not accept any input
    rendering = true;
    exportImageButton.Enabled = false;
    exportRenderButton.Enabled = false;
    resetButton.Enabled = false;
    renderButton.Enabled = false;
    randomiseRenderModeButton.Enabled = false;
    renderButton.Text = "Rendering...";
    
    Stopwatch stopWatch = new Stopwatch();
    stopWatch.Start();
    await Task.Run(async () =>
    {
        mandelbrotImage.Image = await renderer.RenderMandelbrot();
        screen.Refresh();
    });
    stopWatch.Stop();
    timeDisplay.Text = $"Rendering took: {stopWatch.Elapsed.Milliseconds} ms";
    
    // Tell the GUI to accept input again
    exportRenderButton.Enabled = true;
    exportImageButton.Enabled = true;
    resetButton.Enabled = true;
    renderButton.Enabled = true;
    randomiseRenderModeButton.Enabled = true;
    renderButton.Text = "Render";
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

        if (renderer.RenderMode.ToString() != renderModeField.Text) {
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
    zoomLabel.InputField.Text = renderer.Zoom.ToString();
    iterationLabel.InputField.Text = renderer.MaxIterations.ToString();
    horTransLabel.InputField.Text = renderer.XCenter.ToString();
    verTransLabel.InputField.Text = renderer.YCenter.ToString();
    renderModeField.Text = renderer.RenderMode.ToString();
    coreSlider.Text = renderer.Cores.ToString();
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

mandelbrotImage.MouseClick += (_, mea) =>
{
    if (rendering) return;

    renderer.XCenter = Math.Exp(-1.0 * renderer.Zoom) * (4.0 * mea.X / 800 - 2.0) + renderer.XCenter;
    renderer.YCenter = Math.Exp(-1.0 * renderer.Zoom) * (4.0 * mea.Y / 800 - 2.0) + renderer.YCenter;

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
};

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
    
    Directory.CreateDirectory(Directory.GetCurrentDirectory() + "..\\..\\..\\..\\presets\\");
    
    string filename = Directory.GetCurrentDirectory() +
                      $"..\\..\\..\\..\\presets\\render_{year}-{month}-{day}-{hour}{minute}{second}.mandel";
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

importRenderField.TextChanged += (_, _) =>
{
    renderer.ImportMandelbrot(Directory.GetCurrentDirectory() + "..\\..\\..\\..\\presets\\" + importRenderField.Text + ".mandel");
    UpdateUIFields();
    Render();
};

Render();
Application.Run(screen);