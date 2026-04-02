using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace WpfImageEditor
{
    public partial class MainWindow : Window
    {
        private readonly FilterHistory _history = new();

        public MainWindow()
        {
            InitializeComponent();
            SliderBrightness.ValueChanged += (_, e) =>
                LblBrightness.Content = ((int)e.NewValue).ToString();
            SliderContrast.ValueChanged += (_, e) =>
                LblContrast.Content = e.NewValue.ToString("F2");
            SliderThreshold.ValueChanged += (_, e) =>
                LblThreshold.Content = ((int)e.NewValue).ToString();
        }

        private bool HasImage => ImgOriginal.Source != null;

        private void UpdateEditedDisplay(BitmapSource bmp)
        {
            _history.Push(bmp);
            ImgEdited.Source = bmp;
            RefreshUndoState();
        }

        private void RefreshUndoState()
        {
            BtnUndo.IsEnabled = _history.CanUndo;
            LblHistory.Content = $"Historia: {_history.HistoryDepth - 1} kroków";
        }

        private BitmapSource CurrentEdited => (BitmapSource)ImgEdited.Source;

        private int GetFilterSize()
        {
            string? s = (CmbFilterSize.SelectedItem as ComboBoxItem)?.Content?.ToString();
            return s switch { "5x5" => 5, "7x7" => 7, _ => 3 };
        }

        private void OpenImage_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Obrazy|*.png;*.jpg;*.jpeg;*.bmp;*.tiff;*.gif|Wszystkie|*.*"
            };
            if (dlg.ShowDialog() != true) return;

            var bmp = new BitmapImage(new Uri(dlg.FileName));
            ImgOriginal.Source = bmp;
            _history.Initialize(bmp);
            ImgEdited.Source = bmp;
            RefreshUndoState();
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            if (!HasImage) { NoImageMsg(); return; }

            var dlg = new SaveFileDialog
            {
                Filter = "PNG|*.png|BMP|*.bmp|JPEG|*.jpg",
                DefaultExt = ".png"
            };
            if (dlg.ShowDialog() != true) return;

            BitmapEncoder encoder = Path.GetExtension(dlg.FileName).ToLower() switch
            {
                ".bmp"            => new BmpBitmapEncoder(),
                ".jpg" or ".jpeg" => new JpegBitmapEncoder(),
                _                 => new PngBitmapEncoder()
            };
            encoder.Frames.Add(BitmapFrame.Create(CurrentEdited));
            using var fs = File.OpenWrite(dlg.FileName);
            encoder.Save(fs);
            MessageBox.Show("Obraz zapisany.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            if (!_history.CanUndo) return;
            ImgEdited.Source = _history.Undo();
            RefreshUndoState();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            if (!HasImage) return;
            ImgEdited.Source = _history.Reset();
            RefreshUndoState();
        }

        private void ShowHistogram_Click(object sender, RoutedEventArgs e)
        {
            if (!HasImage) { NoImageMsg(); return; }
            new HistogramWindow(CurrentEdited) { Owner = this }.Show();
        }

        private void ShowProjections_Click(object sender, RoutedEventArgs e)
        {
            if (!HasImage) { NoImageMsg(); return; }
            new ProjectionsWindow(CurrentEdited) { Owner = this }.Show();
        }

        private void Grayscale_Click(object sender, RoutedEventArgs e)
        {
            if (!HasImage) { NoImageMsg(); return; }
            UpdateEditedDisplay(ImageProcessor.ToGrayscale(CurrentEdited));
        }

        private void Negative_Click(object sender, RoutedEventArgs e)
        {
            if (!HasImage) { NoImageMsg(); return; }
            UpdateEditedDisplay(ImageProcessor.Negative(CurrentEdited));
        }

        private void ApplyBrightness_Click(object sender, RoutedEventArgs e)
        {
            if (!HasImage) { NoImageMsg(); return; }
            UpdateEditedDisplay(ImageProcessor.AdjustBrightness(CurrentEdited, (int)SliderBrightness.Value));
        }

        private void ApplyContrast_Click(object sender, RoutedEventArgs e)
        {
            if (!HasImage) { NoImageMsg(); return; }
            UpdateEditedDisplay(ImageProcessor.AdjustContrast(CurrentEdited, SliderContrast.Value));
        }

        private void Binarize_Click(object sender, RoutedEventArgs e)
        {
            if (!HasImage) { NoImageMsg(); return; }
            UpdateEditedDisplay(ImageProcessor.Binarize(CurrentEdited, (int)SliderThreshold.Value));
        }

        private void Average_Click(object sender, RoutedEventArgs e)
        {
            if (!HasImage) { NoImageMsg(); return; }
            UpdateEditedDisplay(ImageProcessor.AverageFilter(CurrentEdited, GetFilterSize()));
        }

        private void Gaussian_Click(object sender, RoutedEventArgs e)
        {
            if (!HasImage) { NoImageMsg(); return; }
            UpdateEditedDisplay(ImageProcessor.GaussianFilter(CurrentEdited, GetFilterSize()));
        }

        private void Sharpen_Click(object sender, RoutedEventArgs e)
        {
            if (!HasImage) { NoImageMsg(); return; }
            UpdateEditedDisplay(ImageProcessor.SharpenFilter(CurrentEdited, GetFilterSize()));
        }

        private void Roberts_Click(object sender, RoutedEventArgs e)
        {
            if (!HasImage) { NoImageMsg(); return; }
            UpdateEditedDisplay(ImageProcessor.RobertsEdge(CurrentEdited));
        }

        private void Sobel_Click(object sender, RoutedEventArgs e)
        {
            if (!HasImage) { NoImageMsg(); return; }
            UpdateEditedDisplay(ImageProcessor.SobelEdge(CurrentEdited));
        }

        private void Laplacian_Click(object sender, RoutedEventArgs e)
        {
            if (!HasImage) { NoImageMsg(); return; }
            UpdateEditedDisplay(ImageProcessor.LaplacianEdge(CurrentEdited));
        }

        private int GetMorphSize()
        {
            string? s = (CmbMorphSize.SelectedItem as ComboBoxItem)?.Content?.ToString();
            return s switch { "5x5" => 5, "7x7" => 7, _ => 3 };
        }

        private void Erode_Click(object sender, RoutedEventArgs e)
        {
            if (!HasImage) { NoImageMsg(); return; }
            UpdateEditedDisplay(ImageProcessor.Erode(CurrentEdited, GetMorphSize()));
        }

        private void Dilate_Click(object sender, RoutedEventArgs e)
        {
            if (!HasImage) { NoImageMsg(); return; }
            UpdateEditedDisplay(ImageProcessor.Dilate(CurrentEdited, GetMorphSize()));
        }

        private void CustomFilter_Click(object sender, RoutedEventArgs e)
        {
            if (!HasImage) { NoImageMsg(); return; }
            var dlg = new CustomFilterWindow { Owner = this };
            if (dlg.ShowDialog() == true && dlg.Kernel != null)
                UpdateEditedDisplay(ImageProcessor.ApplyKernelUnscaled(CurrentEdited, dlg.Kernel));
        }

        private static void NoImageMsg() =>
            MessageBox.Show("Najpierw otwórz obraz.", "Brak obrazu",
                MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
