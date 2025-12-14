using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace net.queuepacked.Assembly.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class CpuView : Window
    {
        public Model Model { get; }

        public CpuView(Model model)
        {
            Model = model;

            InitializeComponent();
            DataContext = Model;
        }

        public void SaveAsScreenshot(Stream targetStream)
        {
            DrawingVisual drawingVisual = new();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
                drawingContext.DrawRectangle(new VisualBrush(this), null, new Rect(new Point(), new Size(Width, Height)));

            RenderTargetBitmap renderTargetBitmap = new((int)Width, (int)Height, 96, 96, PixelFormats.Pbgra32);
            renderTargetBitmap.Render(drawingVisual);

            PngBitmapEncoder pngBitmapEncoder = new();
            pngBitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
            pngBitmapEncoder.Save(targetStream);
        }
    }
}