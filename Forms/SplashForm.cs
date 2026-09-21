using System.Diagnostics;

namespace AuxCodex.Forms;

public sealed class SplashForm : Form
{
    private const double StableDurationMilliseconds = 1000;
    private const double FadeDurationMilliseconds = 1000;
    private readonly Stopwatch _clock = new();
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 33 };

    public SplashForm()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(31, 35, 43);
        ClientSize = new Size(420, 180);
        ControlBox = false;
        FormBorderStyle = FormBorderStyle.None;
        ForeColor = Color.FromArgb(245, 247, 250);
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "SplashForm";
        Opacity = 1.0;
        Padding = new Padding(28);
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "AuxCodex";
        TopMost = true;

        var accent = new Panel
        {
            BackColor = Color.FromArgb(91, 155, 213),
            Dock = DockStyle.Left,
            Width = 5
        };
        var title = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = ForeColor,
            Text = "AuxCodex",
            TextAlign = ContentAlignment.MiddleCenter
        };
        Controls.Add(title);
        Controls.Add(accent);

        _timer.Tick += OnTimerTick;
        Shown += OnShown;
        FormClosed += OnFormClosed;
    }

    private void OnShown(object? sender, EventArgs e)
    {
        _clock.Restart();
        _timer.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        var elapsed = _clock.Elapsed.TotalMilliseconds;
        if (elapsed < StableDurationMilliseconds)
        {
            Opacity = 1.0;
            return;
        }

        var progress = Math.Clamp((elapsed - StableDurationMilliseconds) / FadeDurationMilliseconds, 0.0, 1.0);
        Opacity = 1.0 - progress;
        if (progress >= 1.0) Close();
    }

    private void OnFormClosed(object? sender, FormClosedEventArgs e)
    {
        _timer.Stop();
        _timer.Dispose();
        _clock.Stop();
    }
}
