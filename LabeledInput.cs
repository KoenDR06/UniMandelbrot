namespace Mandelbrot;

public class LabeledInput : FlowLayoutPanel
{
    public Label TextLabel;
    public TextBox InputField;
    
    public LabeledInput(string label)
    {
        AutoSize = true;
        FlowDirection = FlowDirection.LeftToRight;
        Width = 250;

        TextLabel = new Label()
        {
            Text = label,
            Anchor = AnchorStyles.Left,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true,
            Margin = new Padding(0),
            Font = new Font("Consolas", 12, FontStyle.Bold),
            ForeColor = Color.White
        };

        Controls.Add(TextLabel);

        InputField = new TextBox()
        {
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0),
            BackColor = Color.Azure
        };
        Controls.Add(InputField);
    }

    public sealed override bool AutoSize
    {
        get { return base.AutoSize; }
        set { base.AutoSize = value; }
    }
}