using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

sealed class SolitairePatcherApp : Form
{
    readonly TextBox executablePath = new TextBox
    {
        ReadOnly = true,
        Anchor = AnchorStyles.Left | AnchorStyles.Right,
        AccessibleName = "Game executable"
    };
    readonly Label recognition = new Label { AutoSize = true };
    readonly Label patchStatus = new Label { AutoSize = true };
    readonly Label feedback = new Label { Dock = DockStyle.Fill, Margin = new Padding(3, 8, 3, 3) };
    readonly Button apply = CreateButton("Apply patch");
    readonly Button restore = CreateButton("Restore");

    [STAThread]
    static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new SolitairePatcherApp(args.Length == 1 ? args[0] : null));
    }

    SolitairePatcherApp(string requestedPath)
    {
        Text = PatchSolitaire.GameName + " Patcher";
        Font = SystemFonts.MessageBoxFont;
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(600, 190);
        MinimumSize = SizeFromClientSize(new Size(430, 190));
        StartPosition = FormStartPosition.CenterScreen;

        var browse = CreateButton("Browse...");
        browse.Click += delegate { Browse(); };
        apply.Click += delegate { UpdatePatch(true); };
        restore.Click += delegate { UpdatePatch(false); };

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            WrapContents = false
        };
        actions.Controls.Add(apply);
        actions.Controls.Add(restore);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 5,
            Padding = new Padding(12)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(executablePath, 0, 0);
        layout.SetColumnSpan(executablePath, 2);
        layout.Controls.Add(browse, 2, 0);
        layout.Controls.Add(new Label { Text = "File recognition", AutoSize = true }, 0, 1);
        layout.Controls.Add(recognition, 1, 1);
        layout.SetColumnSpan(recognition, 2);
        layout.Controls.Add(new Label { Text = "Patch status", AutoSize = true }, 0, 2);
        layout.Controls.Add(patchStatus, 1, 2);
        layout.SetColumnSpan(patchStatus, 2);
        layout.Controls.Add(feedback, 0, 3);
        layout.SetColumnSpan(feedback, 3);
        layout.Controls.Add(actions, 0, 4);
        layout.SetColumnSpan(actions, 3);
        Controls.Add(layout);

        if (string.IsNullOrWhiteSpace(requestedPath))
        {
            executablePath.Text = LoadExecutablePath() ?? FindLocalGame();
            RefreshStatus();
        }
        else
        {
            SelectExecutable(requestedPath);
        }
        ActiveControl = browse;
    }

    static Button CreateButton(string text)
    {
        return new Button
        {
            Text = text,
            AutoSize = true,
            MinimumSize = new Size(0, 32),
            Padding = new Padding(6, 4, 6, 4)
        };
    }

    static string FindLocalGame()
    {
        var folder = AppDomain.CurrentDomain.BaseDirectory;
        var game = Path.Combine(folder, "Zach", PatchSolitaire.GameFileName);
        if (File.Exists(game))
        {
            return game;
        }

        game = Path.Combine(folder, PatchSolitaire.GameFileName);
        return File.Exists(game) ? game : "";
    }

    void Browse()
    {
        using (var picker = new OpenFileDialog())
        {
            picker.Title = "Select " + PatchSolitaire.GameName + " executable";
            picker.Filter = "Executable files (*.exe)|*.exe";
            picker.FileName = PatchSolitaire.GameFileName;
            if (File.Exists(executablePath.Text))
            {
                picker.InitialDirectory = Path.GetDirectoryName(executablePath.Text);
            }
            if (picker.ShowDialog(this) == DialogResult.OK)
            {
                SelectExecutable(picker.FileName);
            }
        }
    }

    void SelectExecutable(string path)
    {
        executablePath.Text = path;
        RefreshStatus(SaveExecutablePath(path));
    }

    static string LoadExecutablePath()
    {
        try
        {
            var path = File.ReadAllText(ExecutableSettingsPath());
            return string.IsNullOrWhiteSpace(path) ? null : path;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    static string SaveExecutablePath(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var settingsPath = ExecutableSettingsPath();
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
            var temporary = settingsPath + "." + Guid.NewGuid().ToString("N") + ".tmp";

            try
            {
                File.WriteAllText(temporary, fullPath);
                if (File.Exists(settingsPath))
                {
                    File.Replace(temporary, settingsPath, null);
                }
                else
                {
                    File.Move(temporary, settingsPath);
                }
            }
            finally
            {
                File.Delete(temporary);
            }

            return null;
        }
        catch (Exception error)
        {
            return "Could not save the executable location. " + error.Message;
        }
    }

    static string ExecutableSettingsPath()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            PatchSolitaire.GameName + " Patcher", "last-executable.txt");
    }

    void UpdatePatch(bool enablePatch)
    {
        try
        {
            RefreshStatus(PatchSolitaire.SetPatched(executablePath.Text, enablePatch));
        }
        catch (UnauthorizedAccessException)
        {
            RefreshStatus("Windows denied write access. Run the patcher as administrator if the game is installed in Program Files.");
        }
        catch (InvalidDataException error)
        {
            RefreshStatus(error.Message);
        }
        catch (IOException error)
        {
            RefreshStatus(error.Message + " Close the game before trying again.");
        }
    }

    void RefreshStatus(string message = null)
    {
        apply.Enabled = false;
        restore.Enabled = false;
        patchStatus.Text = "Unavailable";
        feedback.Text = message ?? "";
        if (string.IsNullOrWhiteSpace(executablePath.Text))
        {
            recognition.Text = "No file selected";
            return;
        }

        try
        {
            var patched = PatchSolitaire.IsPatched(executablePath.Text);
            recognition.Text = "Recognized (supported GOG build)";
            patchStatus.Text = patched ? "Patched" : "Unpatched";
            apply.Enabled = !patched;
            restore.Enabled = patched;
        }
        catch (InvalidDataException error)
        {
            recognition.Text = "Unsupported executable";
            feedback.Text = message ?? error.Message;
        }
        catch (Exception error)
        {
            recognition.Text = "Could not read executable";
            feedback.Text = message ?? error.Message;
        }
    }
}
