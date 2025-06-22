using System.Windows;
using StarFox.Interop.GFX;
using StarFox.Interop.GFX.CONVERT;

namespace StarFoxMapVisualizer.Dialogs
{
    /// <summary>
    /// Interaction logic for GFXCanvasSizeDialog.xaml
    /// </summary>
    public partial class GFXCanvasSizeDialog : Window
    {
        public CanvasSizeDefinition SelectedCanvasSize { get; private set; }
        private CanvasSizeDefinition CalculatedCanvasSize {
            get {
                int.TryParse(WBox.Text, out int width);
                int.TryParse(HBox.Text, out int height);
                int.TryParse(CWBox.Text, out int cwidth);
                int.TryParse(CHBox.Text, out int cheight);
                return new CanvasSizeDefinition()
                {
                    Width = width,
                    Height = height,
                    CharHeight= cheight,
                    CharWidth= cwidth,
                };
            }
        }

        public GFXCanvasSizeDialog() :
            this(FXConvertConstraints.GetDefinition(FXConvertConstraints.FXCanvasTemplates.CGX))
        {


        }
        public GFXCanvasSizeDialog(CanvasSizeDefinition ExistingDefinition)
        {
            InitializeComponent();
            FromExisting(ExistingDefinition);
        }

        public void FromExisting(CanvasSizeDefinition Canvas)
        {
            SelectedCanvasSize = Canvas;
            WBox.Text = Canvas.Width.ToString();
            HBox.Text = Canvas.Height.ToString();
            CWBox.Text = Canvas.CharWidth.ToString();
            CHBox.Text = Canvas.CharHeight.ToString();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Apply();
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TemplateCGX_Click(object sender, RoutedEventArgs e) =>
            FromExisting(FXConvertConstraints.GetDefinition(FXConvertConstraints.FXCanvasTemplates.CGX));

        private void Template3DM_Click(object sender, RoutedEventArgs e) =>
            FromExisting(FXConvertConstraints.GetDefinition(FXConvertConstraints.FXCanvasTemplates.MSPRITES));

        private void Apply() => FromExisting(CalculatedCanvasSize);

        private void ApplyButton_Click(object sender, RoutedEventArgs e) => Apply();
    }
}
