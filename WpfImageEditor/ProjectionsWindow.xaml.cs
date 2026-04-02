using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WpfImageEditor
{
    public partial class ProjectionsWindow : Window
    {
        private readonly double[] _horiz, _vert;

        public ProjectionsWindow(BitmapSource src)
        {
            InitializeComponent();
            _horiz = ImageProcessor.HorizontalProjection(src);
            _vert  = ImageProcessor.VerticalProjection(src);
            HorizCanvas.SizeChanged += (_, _) => DrawHoriz();
            VertCanvas.SizeChanged  += (_, _) => DrawVert();
        }

        private void DrawProfile(Canvas canvas, double[] data, Color color, bool vertical)
        {
            canvas.Children.Clear();
            if (canvas.ActualWidth <= 0 || data.Length == 0) return;

            double cw = canvas.ActualWidth;
            double ch = canvas.ActualHeight;

            double maxVal = 1;
            foreach (var v in data) if (v > maxVal) maxVal = v;

            var poly = new Polyline
            {
                Stroke          = new SolidColorBrush(color),
                StrokeThickness = 1.5
            };

            if (!vertical)
            {
                double stepY = ch / data.Length;
                for (int i = 0; i < data.Length; i++)
                    poly.Points.Add(new Point((data[i] / maxVal) * cw, i * stepY));
            }
            else
            {
                double stepX = cw / data.Length;
                for (int i = 0; i < data.Length; i++)
                    poly.Points.Add(new Point(i * stepX, ch - (data[i] / maxVal) * ch));
            }

            canvas.Children.Add(poly);
        }

        private void DrawHoriz() =>
            DrawProfile(HorizCanvas, _horiz, Color.FromRgb(40, 80, 180), vertical: false);

        private void DrawVert() =>
            DrawProfile(VertCanvas, _vert, Color.FromRgb(40, 140, 40), vertical: true);
    }
}
