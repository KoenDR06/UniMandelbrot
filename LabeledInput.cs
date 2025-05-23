﻿namespace Mandelbrot;

public class LabeledInput : Control
{
    public Label TextLabel;
    public TextBox InputField;

    public LabeledInput(string label, string placeholderValue, Point location)
    {
        TextLabel = new Label()
        {
            Text = label,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true,
            ForeColor = globals.textForeColor,
            Location = location
        };
        
        location.Offset(250 - TextLabel.Width - location.X - 12, 0);
        InputField = new TextBox()
        {
            BackColor = globals.textForeColor,
            Text = placeholderValue,
            AutoSize = true,
            Location = location,
            BorderStyle = BorderStyle.FixedSingle
        };
    }
}