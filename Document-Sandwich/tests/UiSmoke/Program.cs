using System.Collections;
using System.Reflection;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using DocumentSandwich;

static class Program
{
    [STAThread]
    static void Main()
    {
        var app = new App();
        app.InitializeComponent();
        var window = new MainWindow();
        void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        var name = (TextBox)window.FindName("OutputName");
        Check(name.Text == "", "Filename must start blank.");
        foreach (var blank in new[] { "", "   ", ".pdf", " .pdf " })
        {
            var blocked = false;
            try { Combiner.ValidateName(blank); } catch (InvalidOperationException) { blocked = true; }
            Check(blocked, "Blank filename must be blocked.");
        }
        var layers = (StackPanel)window.FindName("Boxes");
        Check(layers.Children.Count == 3, "Must start with three layers.");
        var surface = (FrameworkElement)window.Content;
        var clientSize = new Size(window.Width - 16,window.Height - 40);
        surface.Measure(clientSize); surface.Arrange(new Rect(clientSize)); surface.UpdateLayout();
        var scroll = (ScrollViewer)window.FindName("LayersScroll");
        Check(scroll.ScrollableHeight < 1, $"Starting layers must fit without scrolling; overflow {scroll.ScrollableHeight}.");
        var add = (Button)window.FindName("AddLayerButton");
        for (int i = 0; i < 7; i++) add.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(layers.Children.Count == 10, "Adding layers beyond three failed.");
        var collections = (IList)typeof(MainWindow).GetField("files",BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(window)!;
        foreach (IList collection in collections) collection.Add(new InputFile(@"C:\sample.pdf"));
        name.Text = "Keep this name";
        var clear = (Button)window.FindName("ClearButton");
        clear.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(collections.Cast<IList>().All(c => c.Count == 0), "Clear left files attached.");
        Check(layers.Children.Count == 10 && name.Text == "Keep this name", "Clear must preserve layers and entered name.");
        Check(((ScrollViewer)window.FindName("LayersScroll")).VerticalScrollBarVisibility == ScrollBarVisibility.Auto, "Extra layers must scroll.");
        Check(clear.Background.ToString() == "#FFFFFFFF", "Clear must be white.");
        Button LayerButton(int index,string text)
        {
            var header = (DockPanel)((Grid)((Border)layers.Children[index]).Child).Children[0];
            return ((StackPanel)header.Children[0]).Children.OfType<Button>().Single(b => (string)b.Content == text);
        }
        ListBox LayerList(int index) => ((Grid)((Border)layers.Children[index]).Child).Children.OfType<ListBox>().Single();
        var movedLayer = (IList)collections[0];
        var movedBox = layers.Children[0];
        movedLayer.Add(new InputFile(@"C:\move-first.pdf"));
        movedLayer.Add(new InputFile(@"C:\move-second.pdf"));
        Check(!LayerButton(0,"↑").IsEnabled && !LayerButton(9,"↓").IsEnabled,"Boundary arrows must be disabled.");
        LayerButton(0,"↓").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(ReferenceEquals(collections[1],movedLayer) && ReferenceEquals(layers.Children[1],movedBox),"Down must move the entire layer in both UI and combine order.");
        Check(movedLayer.Count == 2 && ((InputFile)movedLayer[0]).FullPath.EndsWith("move-first.pdf"),"Layer move must preserve its files and their internal order.");
        Check(((TextBlock)((Border)movedBox).Tag).Text == "02   Second layer","Moving must renumber the labels.");
        LayerButton(1,"↑").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(ReferenceEquals(collections[0],movedLayer) && ReferenceEquals(layers.Children[0],movedBox),"Up must restore the layer and combine order.");
        movedLayer.Clear();
        Check(!LayerButton(0,"Remove").IsEnabled,"Remove must be disabled on empty layers.");
        var firstLayer = (IList)collections[0];
        firstLayer.Add(new InputFile(@"C:\first.pdf")); firstLayer.Add(new InputFile(@"C:\second.pdf"));
        Check(LayerList(0).SelectedIndex == 1,"New files must be selected automatically.");
        LayerList(0).SelectedIndex = -1;
        LayerButton(0,"Remove").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(firstLayer.Count == 1 && ((InputFile)firstLayer[0]).FullPath.EndsWith("first.pdf"),"Remove without selection must remove last attachment.");
        LayerButton(0,"Remove").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(firstLayer.Count == 0 && !LayerButton(0,"Remove").IsEnabled,"Remove selected file must empty the layer.");
        var survivingLayer = (IList)collections[2]; survivingLayer.Add(new InputFile(@"C:\survivor.pdf"));
        LayerButton(1,"Delete layer").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(layers.Children.Count == 9 && ReferenceEquals(collections[1],survivingLayer),"Deleting a middle layer must preserve subsequent attachments.");
        Check(((TextBlock)((Border)layers.Children[1]).Tag).Text == "02   Second layer","Layer labels must renumber.");
        LayerButton(1,"↓").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(ReferenceEquals(collections[2],survivingLayer),"Layer movement must still work after deleting another layer.");
        LayerButton(2,"↑").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        LayerButton(1,"Remove").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(survivingLayer.Count == 0,"Remove must still target the right layer after deletion.");
        var addFiles = typeof(MainWindow).GetMethod("AddFiles",BindingFlags.NonPublic | BindingFlags.Instance)!;
        addFiles.Invoke(window,[survivingLayer,new[] {Path.GetFullPath("assets/sandwich.png")}]);
        Check(survivingLayer.Count == 1 && LayerList(1).SelectedIndex == 0,"Adding to a surviving layer must work after deletion.");
        LayerButton(1,"Delete layer").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(!collections.Cast<object>().Any(c => ReferenceEquals(c,survivingLayer)),"Deleting a populated layer must remove its attachments from the combine list.");
        while (layers.Children.Count > 0) LayerButton(layers.Children.Count-1,"Delete layer").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(collections.Count == 0,"All layers must be deletable.");
        add.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(layers.Children.Count == 1 && collections.Count == 1,"Must be able to add a layer after deleting every layer.");
        Check(!LayerButton(0,"↑").IsEnabled && !LayerButton(0,"↓").IsEnabled,"A single layer cannot move up or down.");
        Console.WriteLine("PASS: whole-layer up/down movement; attachments preserved in order; combine order matches UI; renumbering; boundary arrows; movement after deletion.");
        Console.WriteLine("PASS: Remove with/without selection; empty Remove disabled; delete populated/middle/all layers; renumbering; add/remove after deletion.");
        window.Close();
        var root = Path.GetFullPath(Path.Combine("tests","results-images-" + DateTime.Now.ToString("yyyyMMdd-HHmmss")));
        Directory.CreateDirectory(root);
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
        {
            drawing.DrawRectangle(Brushes.OrangeRed,null,new Rect(0,0,120,80));
            drawing.DrawRectangle(Brushes.ForestGreen,null,new Rect(10,10,40,60));
        }
        var bitmap = new RenderTargetBitmap(120,80,96,96,PixelFormats.Pbgra32); bitmap.Render(visual);
        var encoders = new BitmapEncoder[] { new PngBitmapEncoder(), new JpegBitmapEncoder(), new BmpBitmapEncoder(), new GifBitmapEncoder(), new TiffBitmapEncoder() };
        var extensions = new[] { "png", "jpg", "bmp", "gif", "tiff" };
        var inputs = new List<string>();
        for (var i = 0; i < encoders.Length; i++)
        {
            var file = Path.Combine(root,"sample." + extensions[i]);
            encoders[i].Frames.Add(BitmapFrame.Create(bitmap));
            if (encoders[i] is TiffBitmapEncoder) encoders[i].Frames.Add(BitmapFrame.Create(bitmap));
            using (var output = File.Create(file)) encoders[i].Save(output);
            inputs.Add(file);
        }
        var first = Path.Combine(root,"first.pdf");
        using (var pdf = new PdfDocument()) { var page = pdf.AddPage(); page.Width = PdfSharp.Drawing.XUnit.FromPoint(321); pdf.Save(first); }
        inputs.Insert(0,first); inputs.Add(first);
        var combined = Path.Combine(root,"images-combined.pdf");
        var count = Combiner.Combine(inputs.ToArray(),combined,false,_=>{});
        using (var pdf = PdfReader.Open(combined,PdfDocumentOpenMode.Import))
        {
            Check(count == 8 && pdf.PageCount == 8,"Image formats and multi-page TIFF must produce eight mixed pages.");
            Check(Math.Abs(pdf.Pages[0].Width.Point-321)<1 && Math.Abs(pdf.Pages[7].Width.Point-321)<1,"Mixed image/PDF order changed.");
            for (var i = 1; i < 7; i++)
            {
                Check(pdf.Pages[i].Width.Point > pdf.Pages[i].Height.Point,"Landscape image must use landscape page.");
                Check(pdf.Pages[i].Elements.GetDictionary("/Resources")?.Elements.GetDictionary("/XObject")?.Elements.Count > 0,"Image was not embedded.");
            }
        }
        var original = File.ReadAllBytes(combined);
        var corrupt = Path.Combine(root,"corrupt.png"); File.WriteAllText(corrupt,"broken");
        var failed = false; try { Combiner.Combine([corrupt],combined,true,_=>{}); } catch { failed = true; }
        Check(failed && original.SequenceEqual(File.ReadAllBytes(combined)),"Corrupt image must preserve existing output.");
        Console.WriteLine("PASS: initial layers fit without scrolling; blank names blocked; dynamic layers and Clear; PNG/JPG/BMP/GIF/TIFF; multi-page TIFF; mixed PDF/image order; embedded images; corrupt-image output protection.");
    }
}
