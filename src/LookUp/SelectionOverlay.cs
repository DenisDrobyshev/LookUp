using System.Drawing.Drawing2D;

namespace LookUp;

/// <summary>
/// Full-screen dimmed overlay that lets the user drag a rectangle over a frozen
/// screenshot. Drag = select, release = confirm, Esc / right-click = cancel.
/// Returns the picked region in screenshot-pixel coordinates.
/// </summary>
internal sealed class SelectionOverlay : Form
{
    private readonly Bitmap _screenshot;
    private readonly Brush _dimBrush = new SolidBrush(Color.FromArgb(130, 0, 0, 0));
    private readonly Pen _borderPen = new(Color.FromArgb(255, 64, 156, 255), 1.5f);

    private Point _anchor;
    private Rectangle _selection;
    private bool _dragging;
    private Rectangle? _result;

    public SelectionOverlay(Bitmap screenshot)
    {
        _screenshot = screenshot;

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = SystemInformation.VirtualScreen;
        ShowInTaskbar = false;
        TopMost = true;
        DoubleBuffered = true;
        KeyPreview = true;
        Cursor = Cursors.Cross;
        BackColor = Color.Black;
        Text = "LookUp — select area";
    }

    /// <summary>Shows the overlay modally and returns the chosen region, or null if cancelled.</summary>
    public Rectangle? PickRegion() =>
        ShowDialog() == DialogResult.OK ? _result : null;

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        // Make sure the overlay actually owns the keyboard/mouse.
        TopMost = true;
        Activate();
        BringToFront();
        Focus();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            Cancel();
            return;
        }

        if (e.Button == MouseButtons.Left)
        {
            _dragging = true;
            _anchor = e.Location;
            _selection = new Rectangle(e.Location, Size.Empty);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (!_dragging) return;
        _selection = Normalize(_anchor, e.Location);
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (!_dragging) return;
        _dragging = false;
        _selection = Normalize(_anchor, e.Location);
        _result = _selection;
        DialogResult = DialogResult.OK;
        Close();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
            Cancel();
        base.OnKeyDown(e);
    }

    private void Cancel()
    {
        _result = null;
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private static Rectangle Normalize(Point a, Point b) =>
        Rectangle.FromLTRB(
            Math.Min(a.X, b.X), Math.Min(a.Y, b.Y),
            Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;

        // Frozen screenshot, dimmed everywhere.
        g.DrawImageUnscaled(_screenshot, 0, 0);
        g.FillRectangle(_dimBrush, ClientRectangle);

        if (_selection.Width <= 0 || _selection.Height <= 0)
            return;

        // Punch the selected area back to full brightness.
        g.DrawImage(_screenshot, _selection, _selection, GraphicsUnit.Pixel);
        g.DrawRectangle(_borderPen, _selection);
        DrawSizeBadge(g, _selection);
    }

    private static void DrawSizeBadge(Graphics g, Rectangle sel)
    {
        string label = $"{sel.Width} × {sel.Height}";
        using var font = new Font("Segoe UI", 9f, FontStyle.Regular);
        var size = g.MeasureString(label, font);
        float pad = 6f;
        float boxW = size.Width + pad * 2;
        float boxH = size.Height + pad;

        // Prefer above the selection; drop inside if there's no room.
        float x = sel.Left;
        float y = sel.Top - boxH - 4;
        if (y < 0) y = sel.Top + 4;

        using var back = new SolidBrush(Color.FromArgb(220, 20, 22, 28));
        g.FillRectangle(back, x, y, boxW, boxH);
        g.DrawString(label, font, Brushes.White, x + pad, y + pad / 2);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _dimBrush.Dispose();
            _borderPen.Dispose();
        }
        base.Dispose(disposing);
    }
}
