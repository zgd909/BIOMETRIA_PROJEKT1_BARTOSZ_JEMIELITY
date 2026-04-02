using System;
using System.Windows;

namespace WpfImageEditor
{
    public partial class CustomFilterWindow : Window
    {
        public double[,]? Kernel { get; private set; }

        public CustomFilterWindow()
        {
            InitializeComponent();
        }

        private void CmbSize_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (TxtKernel == null) return;
            int size = GetSelectedSize();
            var rows = new System.Text.StringBuilder();
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    rows.Append(r == size / 2 && c == size / 2 ? "1" : "0");
                    if (c < size - 1) rows.Append(' ');
                }
                if (r < size - 1) rows.AppendLine();
            }
            TxtKernel.Text = rows.ToString();
        }

        private int GetSelectedSize()
        {
            string? s = (CmbSize.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString();
            return s switch { "5x5" => 5, "7x7" => 7, _ => 3 };
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int size = GetSelectedSize();
                var lines = TxtKernel.Text.Trim().Split('\n');
                if (lines.Length < size)
                    throw new FormatException($"Oczekiwano {size} wierszy, otrzymano {lines.Length}.");

                var k = new double[size, size];
                for (int r = 0; r < size; r++)
                {
                    var parts = lines[r].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < size)
                        throw new FormatException($"Wiersz {r + 1}: oczekiwano {size} wartosci.");
                    for (int c = 0; c < size; c++)
                        k[r, c] = double.Parse(parts[c], System.Globalization.CultureInfo.InvariantCulture);
                }
                Kernel = k;
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Blad parsowania kernela:\n{ex.Message}", "Blad",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
