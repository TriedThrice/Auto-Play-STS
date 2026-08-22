using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace STSAutoPlay;

public interface IKeyboardInput
{
    void Press(string keyName);
}

public sealed class WindowsKeyboardInput : IKeyboardInput
{
    private const uint InputKeyboard = 1;
    private const uint KeyEventKeyUp = 0x0002;

    private static readonly IReadOnlyDictionary<string, ushort> VirtualKeys =
        new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase)
        {
            ["Enter"] = 0x0D,
            ["Escape"] = 0x1B,
            ["Space"] = 0x20,
            ["0"] = 0x30,
            ["1"] = 0x31,
            ["2"] = 0x32,
            ["3"] = 0x33,
            ["4"] = 0x34,
            ["5"] = 0x35,
            ["6"] = 0x36,
            ["7"] = 0x37,
            ["8"] = 0x38,
            ["9"] = 0x39
        };

    private readonly IntPtr windowHandle;

    public WindowsKeyboardInput(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            throw new ArgumentException("A valid target window handle is required.", nameof(windowHandle));
        }

        this.windowHandle = windowHandle;
    }

    public static WindowsKeyboardInput ForProcess(string processName)
    {
        Process? process = Process.GetProcessesByName(processName).FirstOrDefault(
            candidate => candidate.MainWindowHandle != IntPtr.Zero);

        if (process is null)
        {
            throw new InvalidOperationException($"No visible window found for process '{processName}'.");
        }

        return new WindowsKeyboardInput(process.MainWindowHandle);
    }

    public void Press(string keyName)
    {
        if (!VirtualKeys.TryGetValue(keyName.Trim(), out ushort virtualKey))
        {
            throw new ArgumentException($"Unsupported key '{keyName}'.", nameof(keyName));
        }

        if (!SetForegroundWindow(windowHandle))
        {
            throw new InvalidOperationException($"Windows could not activate the target window. Error code: {Marshal.GetLastWin32Error()}.");
        }

        Input[] inputs =
        [
            new Input { Type = InputKeyboard, Data = new InputData { Keyboard = new KeyboardInput { VirtualKey = virtualKey } } },
            new Input { Type = InputKeyboard, Data = new InputData { Keyboard = new KeyboardInput { VirtualKey = virtualKey, Flags = KeyEventKeyUp } } }
        ];

        if (SendInput((uint)inputs.Length, inputs, NativeInputSize) != inputs.Length)
        {
            throw new InvalidOperationException($"Windows could not send the keyboard input. Error code: {Marshal.GetLastWin32Error()}.");
        }
    }

    private static int NativeInputSize => IntPtr.Size == 8 ? 40 : 28;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint inputCount, Input[] inputs, int inputSize);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr windowHandle);

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputData Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputData
    {
        [FieldOffset(0)]
        public KeyboardInput Keyboard;

        [FieldOffset(0)]
        public MouseInput Mouse;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public IntPtr X;
        public IntPtr Y;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }
}

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        try
        {
            ApplicationConfiguration.Initialize();
            TargetApplication? target = ApplicationPicker.Show();
            if (target is null)
            {
                return;
            }

            IKeyboardInput keyboard = new WindowsKeyboardInput(target.WindowHandle);
            Console.WriteLine($"Selected: {target.DisplayName}");
            Console.WriteLine("Type a key name (Enter, Escape, Space, 0-9). Type 'quit' to exit.");

            while (true)
            {
                string keyName = Console.ReadLine()?.Trim() ?? string.Empty;
                if (keyName.Equals("quit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                keyboard.Press(keyName);
            }
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            Console.Error.WriteLine(exception.Message);
        }
    }
}

internal sealed record TargetApplication(string DisplayName, IntPtr WindowHandle);

internal static class ApplicationPicker
{
    public static TargetApplication? Show()
    {
        List<TargetApplication> applications = Process.GetProcesses()
            .Where(process => process.MainWindowHandle != IntPtr.Zero)
            .Select(process => CreateTargetApplication(process))
            .Where(application => application is not null)
            .Cast<TargetApplication>()
            .OrderBy(application => application.DisplayName)
            .ToList();

        using PickerForm form = new(applications);
        return form.ShowDialog() == DialogResult.OK ? form.SelectedApplication : null;
    }

    private static TargetApplication? CreateTargetApplication(Process process)
    {
        try
        {
            string title = process.MainWindowTitle.Trim();
            string name = process.ProcessName;
            return new TargetApplication(
                string.IsNullOrWhiteSpace(title) ? name : $"{title} ({name})",
                process.MainWindowHandle);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return null;
        }
        finally
        {
            process.Dispose();
        }
    }
}

internal sealed class PickerForm : Form
{
    private readonly ComboBox applicationList = new();

    public PickerForm(IReadOnlyList<TargetApplication> applications)
    {
        Text = "Select target application";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(520, 150);
        Size = new Size(620, 180);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        Label prompt = new()
        {
            AutoSize = true,
            Text = "Choose an open application:",
            Location = new Point(16, 16)
        };

        applicationList.DropDownStyle = ComboBoxStyle.DropDownList;
        applicationList.DataSource = applications.ToList();
        applicationList.DisplayMember = nameof(TargetApplication.DisplayName);
        applicationList.Location = new Point(16, 42);
        applicationList.Width = 570;

        Button selectButton = new()
        {
            DialogResult = DialogResult.OK,
            Text = "Select",
            Location = new Point(416, 82),
            Width = 82
        };
        selectButton.Click += (_, _) => ValidateSelection();

        Button cancelButton = new()
        {
            DialogResult = DialogResult.Cancel,
            Text = "Cancel",
            Location = new Point(504, 82),
            Width = 82
        };

        Controls.AddRange([prompt, applicationList, selectButton, cancelButton]);
        AcceptButton = selectButton;
        CancelButton = cancelButton;
    }

    public TargetApplication? SelectedApplication => applicationList.SelectedItem as TargetApplication;

    private void ValidateSelection()
    {
        if (SelectedApplication is null)
        {
            DialogResult = DialogResult.None;
            MessageBox.Show(this, "Select an application first.", "No application selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
