using Microsoft.Win32;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using static QRCoder.QRCodeGenerator;
using QRCodeGenerator.Validators;

namespace QRCodeGenerator
{
    public partial class MainWindow : Window
    {
        private readonly QRCodeService _qrCodeService = new();
        private readonly QRCodeHistoryService _historyService = new();

        private Color _qrColor = Color.Black;
        private Color _bgColor = Color.White;
        private ECCLevel _eccLevel = ECCLevel.Q;
        private bool _isDark = false;

        private readonly Dictionary<string, string> _socialTemplates = new()
        {
            { "Facebook", "https://facebook.com/{0}" },
            { "Instagram", "https://instagram.com/{0}" },
            { "Twitter", "https://twitter.com/{0}" },
            { "LinkedIn", "https://linkedin.com/in/{0}" },
            { "YouTube", "https://youtube.com/{0}" },
            { "TikTok", "https://tiktok.com/@{0}" }
        };

        public MainWindow()
        {
            InitializeComponent();
            ApplySystemTheme();
        }

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string input = txtInput.Text.Trim();
                InputValidator.ValidateText(input);
                GenerateAndDisplayQRCode(input, "qrcode", "Генерація QR-коду");
            }
            catch (ArgumentException ex)
            {
                ShowError(ex.Message, ex.GetType().Name);
            }
        }

        private void btnGenerateSocial_Click(object sender, RoutedEventArgs e)
        {
            if (cbSocialType.SelectedItem is not ComboBoxItem selectedItem || string.IsNullOrWhiteSpace(txtSocialUser.Text))
            {
                ShowError("Введіть ім'я користувача або посилання!", "Помилка");
                return;
            }

            string userInput = txtSocialUser.Text.Trim();
            string platform = selectedItem.Content.ToString();
            string url = userInput.StartsWith("http") ? userInput : string.Format(_socialTemplates[platform], userInput);

            GenerateAndDisplayQRCode(url, "social_qrcode", "Генерація QR-коду для соціальних мереж");
        }

        private void btnGenerateWifi_Click(object sender, RoutedEventArgs e)
        {
            string ssid = txtWifiSSID.Text.Trim();
            string password = txtWifiPassword.Password;
            string auth = ((ComboBoxItem)cbWifiAuth.SelectedItem).Content.ToString();

            if (string.IsNullOrEmpty(ssid))
            {
                ShowError("Введіть SSID Wi-Fi.", "Помилка");
                return;
            }

            string wifiPayload = $"WIFI:T:{auth};S:{ssid};P:{password};H:false;;";
            GenerateAndDisplayQRCode(wifiPayload, "wifi_qrcode", "Генерація QR-коду для Wi-Fi");
        }

        private void GenerateAndDisplayQRCode(string text, string defaultFileName, string action)
        {
            var qrCodeImage = _qrCodeService.GenerateQRCode(text, _eccLevel, null, _qrColor, _bgColor);
            imgQRCode.Source = BitmapToImageSource(qrCodeImage);
            if (TrySaveQRCode(qrCodeImage, defaultFileName, text))
            {
                UpdateStats(action);
            }
        }

        private bool TrySaveQRCode(Bitmap qrCodeImage, string defaultFileName, string text)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png|JPEG Image|*.jpg",
                FileName = defaultFileName
            };

            if (dialog.ShowDialog() == true)
            {
                ImageSaver.SaveImage(qrCodeImage, dialog.FileName);
                _historyService.AddToHistory(text, dialog.FileName);
                return true;
            }
            return false;
        }

        private void cbQrColor_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
            _qrColor = Color.FromName(((ComboBoxItem)cbQrColor.SelectedItem).Content.ToString());

        private void cbBgColor_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
            _bgColor = Color.FromName(((ComboBoxItem)cbBgColor.SelectedItem).Content.ToString());

        private static BitmapSource BitmapToImageSource(Bitmap bitmap)
        {
            using var memory = new System.IO.MemoryStream();
            bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
            memory.Position = 0;
            return BitmapFrame.Create(memory, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        }

        private void btnShowHistory_Click(object sender, RoutedEventArgs e)
        {
            lbHistory.Items.Clear();
            foreach (var item in _historyService.LoadHistory())
                lbHistory.Items.Add($"{item.Text} | {item.FilePath}");

            lbHistory.Visibility = lbHistory.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void lbHistory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbHistory.SelectedItem is not string selected || selected.Split('|').Length < 2) return;
            string filePath = selected.Split('|')[1].Trim();

            if (System.IO.File.Exists(filePath))
                using (var bmp = new Bitmap(filePath))
                    imgQRCode.Source = BitmapToImageSource(new Bitmap(bmp));
        }

        private void btnDeleteHistory_Click(object sender, RoutedEventArgs e)
        {
            if (lbHistory.SelectedItems.Count == 0) return;

            var selectedItems = lbHistory.SelectedItems.Cast<string>().ToList();
            var history = _historyService.LoadHistory();

            foreach (var selected in selectedItems)
            {
                var parts = selected.Split('|');
                if (parts.Length < 2) continue;
                string filePath = parts[1].Trim();
                var item = history.Find(h => h.FilePath == filePath);
                if (item != null) history.Remove(item);
            }

            _historyService.SaveHistory(history);
            btnShowHistory_Click(null, null);
        }

        private void btnRenameHistory_Click(object sender, RoutedEventArgs e)
        {
            if (lbHistory.SelectedItem is not string selected || selected.Split('|').Length < 2) return;
            string filePath = selected.Split('|')[1].Trim();

            var history = _historyService.LoadHistory();
            var item = history.Find(h => h.FilePath == filePath);
            if (item != null)
            {
                string input = Microsoft.VisualBasic.Interaction.InputBox("Введіть новий опис:", "Перейменування", item.Text);
                if (!string.IsNullOrWhiteSpace(input))
                {
                    item.Text = input;
                    _historyService.SaveHistory(history);
                    btnShowHistory_Click(null, null);
                }
            }
        }

        private void txtHistorySearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string search = txtHistorySearch.Text.Trim().ToLower();
            lbHistory.Items.Clear();
            foreach (var item in _historyService.LoadHistory())
            {
                if (item.Text.ToLower().Contains(search) || item.FilePath.ToLower().Contains(search))
                    lbHistory.Items.Add($"{item.Text} | {item.FilePath}");
            }
        }

        private void ECC_Checked(object sender, RoutedEventArgs e)
        {
            if (rbECCL.IsChecked == true) _eccLevel = ECCLevel.L;
            else if (rbECCM.IsChecked == true) _eccLevel = ECCLevel.M;
            else if (rbECCQ.IsChecked == true) _eccLevel = ECCLevel.Q;
            else if (rbECCH.IsChecked == true) _eccLevel = ECCLevel.H;
        }

        private void btnToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            _isDark = !_isDark;
            Application.Current.Resources["WindowBackgroundColor"] = _isDark ? System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.White;
            Application.Current.Resources["ForegroundColor"] = _isDark ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.Black;
            this.Background = (System.Windows.Media.Brush)Application.Current.Resources["WindowBackgroundColor"];
            Foreground = (System.Windows.Media.Brush)Application.Current.Resources["ForegroundColor"];
        }

        private void btnCopyInput_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtInput.Text))
            {
                Clipboard.SetText(txtInput.Text);
                MessageBox.Show("Текст скопійовано в буфер обміну.", "Копіювання", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                ShowError("Немає тексту для копіювання.", "Копіювання");
            }
        }

        private void ApplySystemTheme()
        {
            try
            {
                var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                bool isLight = key?.GetValue("AppsUseLightTheme") is not int v || v != 0;

                Application.Current.Resources["WindowBackgroundColor"] = isLight ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.Black;
                Application.Current.Resources["ForegroundColor"] = isLight ? System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.White;

                this.Background = (System.Windows.Media.Brush)Application.Current.Resources["WindowBackgroundColor"];
                Foreground = (System.Windows.Media.Brush)Application.Current.Resources["ForegroundColor"];
                _isDark = !isLight;
            }
            catch { }
        }

        private void ShowError(string message, string caption) =>
            MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Warning);

        private void UpdateStats(string lastAction = "")
        {
            var history = _historyService.LoadHistory();
            txtStats.Text = $"Згенеровано QR-кодів: {history.Count}" +
                (string.IsNullOrEmpty(lastAction) ? "" : $" | Остання дія: {lastAction}");
        }
    }
}