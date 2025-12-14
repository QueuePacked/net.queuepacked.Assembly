using net.queuepacked.Assembly.CPU;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace net.queuepacked.Assembly.UI
{
    /// <summary>
    /// Interaction logic for CodeView.xaml
    /// </summary>
    public partial class CodeView : Window
    {
        public Model Model { get; }

        public CodeView(Model model)
        {
            Model = model;
            DataContext = Model;
            InitializeComponent();
            Model.PropertyChanged += ModelOnPropertyChanged;
            Editor.TextChanged += EditorOnTextChanged;
            Editor.Options.EnableRectangularSelection = true;
            Editor.Options.EnableTextDragDrop = true;
        }

        private void EditorOnTextChanged(object? sender, EventArgs e)
        {
            Model.Code = Editor.Text;
            UpdateCompiled(Editor.Document.Text);
        }

        private void ModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(Model.Code) || Editor.Text == Model.Code)
                return;

            Editor.Document.Text = Model.Code;
            UpdateCompiled(Editor.Document.Text);
        }

        private void UpdateCompiled(string source)
        {
            try
            {
                Editor2.Document.Text = string.Empty;
                Editor3.Document.Text = string.Empty;
                Editor4.Document.Text = string.Empty;
                Editor5.Document.Text = string.Empty;
                Editor6.Document.Text = string.Empty;

                string removeCommentsAndWhitespace = AssemblyCompiler.RemoveCommentsAndWhitespace(source);
                Editor2.Document.Text = removeCommentsAndWhitespace;
                string expandSyntax = AssemblyCompiler.ExpandSyntax(removeCommentsAndWhitespace);
                Editor3.Document.Text = expandSyntax;
                string singleLineToSequence = AssemblyCompiler.SingleLineToSequence(expandSyntax);
                Editor4.Document.Text = singleLineToSequence;
                string referencesToAddresses = AssemblyCompiler.ReferencesToAddresses(singleLineToSequence, out _);
                Editor5.Document.Text = referencesToAddresses;
                string operationsToCodes = AssemblyCompiler.OperationsToCodes(referencesToAddresses);
                Editor6.Document.Text = operationsToCodes;
            }
            catch
            {
            }
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
