using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace DocumentSandwich;
public sealed record InputFile(string FullPath)
{
    public override string ToString() => Path.GetFileName(FullPath);
}
public partial class MainWindow : Window
{
    readonly List<ObservableCollection<InputFile>> files = [];
    readonly Settings settings = Settings.Load();
    bool busy;
    string? finished;
    public MainWindow()
    {
        InitializeComponent();
        OutputName.PreviewKeyDown += (_, e) =>
        {
            if (e.Key != System.Windows.Input.Key.Enter || System.Windows.Input.Keyboard.Modifiers != System.Windows.Input.ModifierKeys.None) return;
            e.Handled = true;
            if (!e.IsRepeat && !busy && CombineButton.IsEnabled)
                CombineButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        };
        for (var i = 0; i < 3; i++) AddLayer();
        UpdateHint();
        Closing += (_, e) => { if (busy) { e.Cancel = true; MessageBox.Show(this, "Please wait until your PDF has finished combining.", "Combining files"); } };
    }
    void AddBox(int index)
    {
        var layer = files[index];
        var border = new Border { Background = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(213,223,234)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(11), Margin = new Thickness(0,0,0,10), Padding = new Thickness(16), AllowDrop = true };
        border.Height = 155;
        Boxes.Children.Add(border);
        var grid = new Grid(); grid.RowDefinitions.Add(new() { Height = GridLength.Auto }); grid.RowDefinitions.Add(new()); border.Child = grid;
        var header = new DockPanel { Margin = new Thickness(0,0,0,9) };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right }; DockPanel.SetDock(buttons, Dock.Right); header.Children.Add(buttons);
        var list = new ListBox { ItemsSource = layer, BorderThickness = new Thickness(0), Background = Brushes.Transparent, FontSize = 14, HorizontalContentAlignment = HorizontalAlignment.Stretch };
        Button Button(string text, string hint, Action action)
        {
            var b = new Button { Content = text, ToolTip = hint, Padding = new Thickness(10,5,10,5), Margin = new Thickness(5,0,0,0) }; b.Click += (_, _) => { if (!busy) action(); }; buttons.Children.Add(b); return b;
        }
        var up = Button("↑", "Move this entire layer up", () => MoveLayer(layer, border, -1)); up.Tag = "LayerUp";
        var down = Button("↓", "Move this entire layer down", () => MoveLayer(layer, border, 1)); down.Tag = "LayerDown";
        var remove = Button("Remove", "Remove the selected file, or the last file if none is selected", () =>
        {
            if (layer.Count == 0) return;
            var selected = list.SelectedIndex >= 0 ? list.SelectedIndex : layer.Count - 1;
            layer.RemoveAt(selected);
            list.SelectedIndex = Math.Min(selected, layer.Count - 1);
            Status.Text = $"File removed. {files.Sum(f => f.Count)} file(s) ready.";
        });
        remove.IsEnabled = layer.Count > 0;
        Button("Browse…", "Choose documents or images", () => { var dialog = new OpenFileDialog { Filter = InputTypes.Filter, Multiselect = true, Title = $"Add files to layer {files.IndexOf(layer)+1}" }; if (dialog.ShowDialog(this) == true) AddFiles(layer, dialog.FileNames); });
        var title = index switch { 0 => "First layer", 1 => "Second layer", 2 => "Third layer", _ => $"Layer {index + 1}" };
        var label = new TextBlock { Text = $"{index+1:00}   " + title, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
        border.Tag = label;
        Button("Delete layer", "Remove this entire layer and its attachments", () =>
        {
            files.Remove(layer);
            Boxes.Children.Remove(border);
            RefreshLayers();
            Status.Text = $"Layer deleted. {files.Count} layer(s) remaining. Use + to add a layer.";
        });
        header.Children.Add(label); grid.Children.Add(header);
        var hint = new TextBlock { Text = "Drop PDF, Word or image files here\nPDF  ·  Word  ·  PNG  ·  JPG  ·  more", TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Foreground = (Brush)FindResource("Muted"), IsHitTestVisible = false };
        Grid.SetRow(hint,1); grid.Children.Add(hint); Grid.SetRow(list,1); grid.Children.Add(list);
        layer.CollectionChanged += (_, e) =>
        {
            hint.Visibility = layer.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            remove.IsEnabled = layer.Count > 0;
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                list.SelectedIndex = e.NewStartingIndex;
        };
        border.PreviewDragOver += (_, e) => { e.Effects = !busy && e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; e.Handled = true; };
        border.Drop += (_, e) => { if (!busy && e.Data.GetData(DataFormats.FileDrop) is string[] paths) AddFiles(layer,paths); e.Handled = true; };
    }
    void AddFiles(ObservableCollection<InputFile> layer, string[] paths)
    {
        if (paths.Any(p => !File.Exists(p) || !InputTypes.IsSupported(p)))
        { MessageBox.Show(this,"Choose PDF, Word, PNG, JPG/JPEG, BMP, GIF or TIFF files.", "Unsupported file",MessageBoxButton.OK,MessageBoxImage.Warning); return; }
        foreach (var path in paths) layer.Add(new(Path.GetFullPath(path)));
        Status.Text = $"{files.Sum(f => f.Count)} file(s) ready. Layers combine from top to bottom.";
    }
    void AddLayer()
    {
        files.Add(new());
        AddBox(files.Count - 1);
        RefreshLayers();
    }
    void RefreshLayers()
    {
        for (var i = 0; i < Boxes.Children.Count; i++)
        {
            var border = (Border)Boxes.Children[i];
            var caption = i switch { 0 => "First layer", 1 => "Second layer", 2 => "Third layer", _ => $"Layer {i + 1}" };
            ((TextBlock)border.Tag).Text = $"{i+1:00}   {caption}";
            var header = (DockPanel)((Grid)border.Child).Children[0];
            foreach (var button in ((StackPanel)header.Children[0]).Children.OfType<Button>())
            {
                if (button.Tag as string == "LayerUp") button.IsEnabled = i > 0;
                if (button.Tag as string == "LayerDown") button.IsEnabled = i < files.Count - 1;
            }
        }
    }
    void MoveLayer(ObservableCollection<InputFile> layer, Border border, int direction)
    {
        if (busy) return;
        var current = files.IndexOf(layer);
        var target = current + direction;
        if (current < 0 || target < 0 || target >= files.Count) return;
        files.RemoveAt(current);
        files.Insert(target, layer);
        Boxes.Children.Remove(border);
        Boxes.Children.Insert(target, border);
        RefreshLayers();
        border.BringIntoView();
        Status.Text = $"Layer moved to position {target + 1}. Layers combine from top to bottom.";
    }
    void AddLayer_Click(object sender, RoutedEventArgs e)
    {
        if (busy) return;
        AddLayer();
        LayersScroll.UpdateLayout();
        LayersScroll.ScrollToBottom();
        Status.Text = $"Layer {files.Count} added. Drop files or use Browse to fill it.";
    }
    void Clear_Click(object sender, RoutedEventArgs e)
    {
        if (busy) return;
        foreach (var layer in files) layer.Clear();
        finished = null;
        OpenButton.Visibility = Visibility.Collapsed;
        Status.Text = "All attached files cleared. Ready for a fresh stack.";
    }
    void UpdateHint() => SaveHint.Text = settings.UseFixedFolder ? "Saves directly to: " + settings.SaveFolder : "Save As will open when you press Combine.";
    void Settings_Click(object sender, RoutedEventArgs e) { new SettingsWindow(settings) { Owner = this }.ShowDialog(); UpdateHint(); }
    async void Combine_Click(object sender, RoutedEventArgs e)
    {
        if (busy) return;
        try
        {
            var inputs = files.SelectMany(f => f).Select(f => f.FullPath).ToArray();
            if (inputs.Length == 0) throw new InvalidOperationException("Add at least one PDF, Word document or image first.");
            var name = Combiner.ValidateName(OutputName.Text);
            string output;
            bool overwrite = false;
            if (settings.UseFixedFolder)
            {
                if (!Directory.Exists(settings.SaveFolder)) throw new InvalidOperationException("The fixed save folder is unavailable. Choose another folder in Settings.");
                output = Path.Combine(settings.SaveFolder, name);
                if (File.Exists(output))
                {
                    if (MessageBox.Show(this, $"Replace the existing PDF?\n\n{output}", "PDF already exists", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                    overwrite = true;
                }
            }
            else
            {
                var dialog = new SaveFileDialog { FileName = name, DefaultExt = ".pdf", Filter = "PDF document|*.pdf", AddExtension = true, OverwritePrompt = true, Title = "Save your Document Sandwich" };
                if (dialog.ShowDialog(this) != true) return;
                output = dialog.FileName; overwrite = File.Exists(output);
            }
            busy = true; Boxes.IsEnabled = OutputName.IsEnabled = SettingsButton.IsEnabled = CombineButton.IsEnabled = ClearButton.IsEnabled = AddLayerButton.IsEnabled = false;
            OpenButton.Visibility = Visibility.Collapsed; Progress.Visibility = Visibility.Visible;
            var count = await Combiner.CombineAsync(inputs,output,overwrite, message => Dispatcher.InvokeAsync(() => Status.Text = message));
            finished = output; Status.Text = $"Saved {count} page(s) to {output}"; OpenButton.Visibility = Visibility.Visible;
        }
        catch (Exception ex) { Status.Text = "Could not combine files. Your input files are unchanged."; MessageBox.Show(this,ex.Message,"Document Sandwich",MessageBoxButton.OK,MessageBoxImage.Warning); }
        finally { busy = false; Boxes.IsEnabled = OutputName.IsEnabled = SettingsButton.IsEnabled = CombineButton.IsEnabled = ClearButton.IsEnabled = AddLayerButton.IsEnabled = true; Progress.Visibility = Visibility.Collapsed; }
    }
    void Open_Click(object sender, RoutedEventArgs e)
    {
        try { if (finished != null) Process.Start(new ProcessStartInfo(finished) { UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not open PDF"); }
    }
}
