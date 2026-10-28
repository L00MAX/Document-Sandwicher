using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace DocumentSandwich;
public sealed class Settings
{
    public bool UseFixedFolder { get; set; }
    public string SaveFolder { get; set; } = "";
    static string SettingsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Document Sandwich", "settings.json");
    public static Settings Load()
    {
        try
        {
            var path = File.Exists(SettingsPath) ? SettingsPath : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PDF Sandwich", "settings.json");
            return JsonSerializer.Deserialize<Settings>(File.ReadAllText(path)) ?? new();
        }
        catch { return new(); }
    }
    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        var staging = SettingsPath + ".tmp";
        File.WriteAllText(staging, JsonSerializer.Serialize(this));
        File.Move(staging, SettingsPath, true);
    }
}
public sealed class SettingsWindow : Window
{
    public SettingsWindow(Settings settings)
    {
        Title = "Document Sandwich — Settings"; Width = 590; Height = 310; ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new Thickness(24) }; Content = panel;
        panel.Children.Add(new TextBlock { Text = "Save location", FontSize = 22, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0,0,0,18) });
        var enabled = new CheckBox { Content = "Use a fixed save folder (skip Save As)", IsChecked = settings.UseFixedFolder, Margin = new Thickness(0,0,0,14) };
        panel.Children.Add(enabled);
        var row = new DockPanel();
        var browse = new Button { Content = "Browse…", Margin = new Thickness(10,0,0,0) }; DockPanel.SetDock(browse, Dock.Right); row.Children.Add(browse);
        var folder = new TextBox { Text = settings.SaveFolder, ToolTip = "Folder for combined PDFs" }; row.Children.Add(folder); panel.Children.Add(row);
        browse.Click += (_, _) => { var dialog = new OpenFolderDialog { Title = "Choose where Document Sandwich saves your PDFs" }; if (dialog.ShowDialog(this) == true) folder.Text = dialog.FolderName; };
        panel.Children.Add(new TextBlock { Text = "Files use the name on the main screen. Existing PDFs require confirmation before replacement.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,12,0,14), Foreground = System.Windows.Media.Brushes.SlateGray });
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = new Button { Content = "Cancel", IsCancel = true, Margin = new Thickness(0,0,8,0) }; buttons.Children.Add(cancel);
        var save = new Button { Content = "Save settings", IsDefault = true }; buttons.Children.Add(save); panel.Children.Add(buttons);
        save.Click += (_, _) =>
        {
            try
            {
                var path = folder.Text.Trim();
                if (enabled.IsChecked == true && (!Path.IsPathFullyQualified(path) || !Directory.Exists(path))) throw new InvalidOperationException("Choose an existing save folder first.");
                var next = new Settings { UseFixedFolder = enabled.IsChecked == true, SaveFolder = path }; next.Save();
                settings.UseFixedFolder = next.UseFixedFolder; settings.SaveFolder = next.SaveFolder; DialogResult = true;
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not save settings", MessageBoxButton.OK, MessageBoxImage.Warning); }
        };
    }
}
