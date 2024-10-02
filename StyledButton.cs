namespace Mandelbrot;

public class StyledButton : Button
{
    public StyledButton(string text, Point location, bool isHalf=false)
    {
        Text = text;
        BackColor = Color.White;
        ForeColor = Color.FromArgb(34, 76, 91);
        AutoSize = true;
        Location = location;
        Width = isHalf ? 125 - 18 : 250 - 12 * 2;
    }
}