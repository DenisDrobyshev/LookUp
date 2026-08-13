using System.Runtime.InteropServices;

namespace LookUp;

/// <summary>
/// A hidden message-only window that owns the global capture hotkey and raises
/// <see cref="HotKeyPressed"/> whenever it fires.
/// </summary>
internal sealed class HotKeyWindow : NativeWindow, IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int HotKeyId = 0xB001;
    private const uint MOD_NOREPEAT = 0x4000;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public event EventHandler? HotKeyPressed;

    private bool _registered;

    public HotKeyWindow()
    {
        CreateHandle(new CreateParams
        {
            Caption = "LookUpHotKeyWindow",
            // Message-only window: parented to HWND_MESSAGE (-3).
            Parent = new IntPtr(-3),
        });
    }

    public bool Register(HotkeySpec spec)
    {
        Unregister();
        _registered = RegisterHotKey(Handle, HotKeyId, spec.Modifiers | MOD_NOREPEAT, (uint)spec.Key);
        return _registered;
    }

    public void Unregister()
    {
        if (_registered)
        {
            UnregisterHotKey(Handle, HotKeyId);
            _registered = false;
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HotKeyId)
            HotKeyPressed?.Invoke(this, EventArgs.Empty);

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        Unregister();
        if (Handle != IntPtr.Zero)
            DestroyHandle();
    }
}
