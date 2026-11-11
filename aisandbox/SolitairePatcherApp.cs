using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

internal sealed class SolitairePatcherApp : Form
{
    private readonly TextBox executablePath = new TextBox { ReadOnly = true, Dock = DockStyle.Fill };
    private readonly Label status = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Button apply = new Button { Text = "Apply patch", AutoSize = true };
    private readonly Button restore = new Button { Text = "Restore", AutoSize = true };

    [STAThread]
    private static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new SolitairePatcherApp(args.Length == 1 ? args[0] : FindLocalGame()));
    }

    private SolitairePatcherApp(string path)
    {
        Text = "Zachtronics Solitaire Patcher";
        Font = SystemFonts.MessageBoxFont;
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(540, 119);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var browse = new Button { Text = "Browse...", AutoSize = true, Dock = DockStyle.Fill };
        executablePath.AccessibleName = "Game executable";
        status.AccessibleName = "Patch status";
        browse.Click += delegate { Browse(); };
        apply.Click += delegate { UpdatePatch(true); };
        restore.Click += delegate { UpdatePatch(false); };

        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Margin = Padding.Empty };
        actions.Controls.Add(apply);
        actions.Controls.Add(restore);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(9)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(executablePath, 0, 0);
        layout.Controls.Add(browse, 1, 0);
        layout.Controls.Add(status, 0, 1);
        layout.SetColumnSpan(status, 2);
        layout.Controls.Add(actions, 0, 2);
        layout.SetColumnSpan(actions, 2);
        Controls.Add(layout);

        executablePath.Text = path;
        RefreshStatus();
        ActiveControl = browse;
    }

    private static string FindLocalGame()
    {
        string folder = AppDomain.CurrentDomain.BaseDirectory;
        string game = Path.Combine(folder, "Zach", PatchSolitaire.GameFileName);
        if (File.Exists(game))
        {
            return game;
        }

        game = Path.Combine(folder, PatchSolitaire.GameFileName);
        return File.Exists(game) ? game : "";
    }

    private void Browse()
    {
        using (var picker = new OpenFileDialog())
        {
            picker.Title = "Select the GOG game executable";
            picker.Filter = "Executable files (*.exe)|*.exe";
            picker.FileName = PatchSolitaire.GameFileName;
            if (File.Exists(executablePath.Text))
            {
                picker.InitialDirectory = Path.GetDirectoryName(executablePath.Text);
            }
            if (picker.ShowDialog(this) == DialogResult.OK)
            {
                executablePath.Text = picker.FileName;
                RefreshStatus();
            }
        }
    }

    private void UpdatePatch(bool enablePatch)
    {
        try
        {
            RefreshStatus(PatchSolitaire.SetPatched(executablePath.Text, enablePatch));
        }
        catch (UnauthorizedAccessException)
        {
            RefreshStatus("Windows denied write access. Run the patcher as administrator if the game is installed in Program Files.");
        }
        catch (IOException error)
        {
            RefreshStatus(error.Message + " Close the game before trying again.");
        }
    }

    private void RefreshStatus(string message = null)
    {
        apply.Enabled = false;
        restore.Enabled = false;
        if (string.IsNullOrWhiteSpace(executablePath.Text))
        {
            status.Text = "Choose the GOG game executable.";
            return;
        }

        try
        {
            bool patched = PatchSolitaire.IsPatched(executablePath.Text);
            apply.Enabled = !patched;
            restore.Enabled = patched;
            status.Text = message ?? (patched ? "Minimized rendering is disabled." : "Ready to apply the minimized rendering patch.");
        }
        catch (Exception error)
        {
            status.Text = message ?? error.Message;
        }
    }
}
