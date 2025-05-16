using Microsoft.Win32;
using QRCoder;
using System;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using static QRCoder.QRCodeGenerator;

namespace QRCodeGenerator
{
    public partial class MainWindow : Window
    {
        private QRCodeService _qrCodeService;
        private QRCodeHistoryService _historyService;

        private Color _qrColor = Color.Black;
        private Color _bgColor = Color.White;
        private ECCLevel _eccLevel = ECCLevel.Q;
        private bool _isDark = false;

        public MainWindow()
        {
            InitializeComponent();
            ApplySystemTheme();
            _qrCodeService = new QRCodeService();
            _historyService = new QRCodeHistoryService();
        }

        private void UpdateStats(string lastAction = "")
        {
            var history = _historyService.LoadHistory();
            txtStats.Text = $"Згенеровано QR-кодів: {history.Count}" +
                (string.IsNullOrEmpty(lastAction) ? "" : $" | Остання дія: {lastAction}");
        }

        // Генерація QR-коду з шифруванням, якщо задано пароль
        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            if (txtInput == null)
            {
                MessageBox.Show("Input textbox not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string text = txtInput.Text;
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show("Please enter some text or URL.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // Перевірка на URL (простий варіант)
            if (text.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !Uri.IsWellFormedUriString(text, UriKind.Absolute))
            {
                MessageBox.Show("Введено некоректне посилання.", "URL Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // Перевірка на довжину (до 1000 символів)
            if (text.Length > 1000)
            {
                MessageBox.Show("Текст занадто довгий для QR-коду (максимум 1000 символів).", "Length Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Вибір рівня корекції помилок
            ECCLevel level = _eccLevel;

            // Генерація QR-коду з вибраними кольорами
            var qrCodeImage = _qrCodeService.GenerateQRCode(text, level, null, _qrColor, _bgColor);

            imgQRCode.Source = BitmapToImageSource(qrCodeImage);

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png|JPEG Image|*.jpg",
                FileName = "qrcode"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                ImageSaver.SaveImage(qrCodeImage, saveFileDialog.FileName);

                // Збереження в історії
                var history = _historyService.LoadHistory();
                history.Add(new QRCodeHistory { Text = text, FilePath = saveFileDialog.FileName });
                _historyService.SaveHistory(history);
            }

            UpdateStats("Генерація QR-коду");
        }

        // Генерація QR-коду для соціальних мереж
        private void btnGenerateSocial_Click(object sender, RoutedEventArgs e)
        {
            if (cbSocialType.SelectedItem is ComboBoxItem selectedItem && !string.IsNullOrWhiteSpace(txtSocialUser.Text))
            {
                string userInput = txtSocialUser.Text.Trim();
                string url = userInput;

                switch (selectedItem.Content.ToString())
                {
                    case "Facebook":
                        url = userInput.StartsWith("http") ? userInput : $"https://facebook.com/{userInput}";
                        break;
                    case "Instagram":
                        url = userInput.StartsWith("http") ? userInput : $"https://instagram.com/{userInput}";
                        break;
                    case "Twitter":
                        url = userInput.StartsWith("http") ? userInput : $"https://twitter.com/{userInput}";
                        break;
                    case "LinkedIn":
                        url = userInput.StartsWith("http") ? userInput : $"https://linkedin.com/in/{userInput}";
                        break;
                    case "YouTube":
                        url = userInput.StartsWith("http") ? userInput : $"https://youtube.com/{userInput}";
                        break;
                    case "TikTok":
                        url = userInput.StartsWith("http") ? userInput : $"https://tiktok.com/@{userInput}";
                        break;
                }

                var qrCodeImage = _qrCodeService.GenerateQRCode(url, ECCLevel.Q, null, _qrColor, _bgColor);
                imgQRCode.Source = BitmapToImageSource(qrCodeImage);

                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "PNG Image|*.png|JPEG Image|*.jpg",
                    FileName = "social_qrcode"
                };
                if (saveFileDialog.ShowDialog() == true)
                {
                    ImageSaver.SaveImage(qrCodeImage, saveFileDialog.FileName);
                    var history = _historyService.LoadHistory();
                    history.Add(new QRCodeHistory { Text = url, FilePath = saveFileDialog.FileName });
                    _historyService.SaveHistory(history);
                }

                UpdateStats("Генерація QR-коду для соціальних мереж");
            }
            else
            {
                MessageBox.Show("Введіть ім'я користувача або посилання!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Генерація QR-коду для Wi-Fi
        private void btnGenerateWifi_Click(object sender, RoutedEventArgs e)
        {
            string ssid = txtWifiSSID.Text.Trim();
            string password = txtWifiPassword.Password;
            string auth = ((ComboBoxItem)cbWifiAuth.SelectedItem).Content.ToString();

            if (string.IsNullOrEmpty(ssid))
            {
                MessageBox.Show("Введіть SSID Wi-Fi.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string wifiPayload = $"WIFI:T:{auth};S:{ssid};P:{password};H:false;;";
            var qrCodeImage = _qrCodeService.GenerateQRCode(wifiPayload, ECCLevel.Q, null, _qrColor, _bgColor);
            imgQRCode.Source = BitmapToImageSource(qrCodeImage);

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png|JPEG Image|*.jpg",
                FileName = "wifi_qrcode"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                ImageSaver.SaveImage(qrCodeImage, saveFileDialog.FileName);
                var history = _historyService.LoadHistory();
                history.Add(new QRCodeHistory { Text = wifiPayload, FilePath = saveFileDialog.FileName });
                _historyService.SaveHistory(history);
            }

            UpdateStats("Генерація QR-коду для Wi-Fi");
        }

        // Обробник вибору кольору QR
        private void cbQrColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var colorName = ((ComboBoxItem)cbQrColor.SelectedItem).Content.ToString();
            _qrColor = Color.FromName(colorName);
        }

        // Обробник вибору кольору фону
        private void cbBgColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var colorName = ((ComboBoxItem)cbBgColor.SelectedItem).Content.ToString();
            _bgColor = Color.FromName(colorName);
        }

        // Перетворення Bitmap в ImageSource для WPF
        private static BitmapSource BitmapToImageSource(Bitmap bitmap)
        {
            using (var memory = new System.IO.MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;
                return BitmapFrame.Create(memory, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            }
        }

        private void btnShowHistory_Click(object sender, RoutedEventArgs e)
        {
            lbHistory.Items.Clear();
            var history = _historyService.LoadHistory();
            foreach (var item in history)
                lbHistory.Items.Add($"{item.Text} | {item.FilePath}");
            lbHistory.Visibility = lbHistory.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void lbHistory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbHistory.SelectedItem is string selected)
            {
                var parts = selected.Split('|');
                if (parts.Length > 1)
                {
                    string filePath = parts[1].Trim();
                    if (System.IO.File.Exists(filePath))
                    {
                        using (var bmp = new Bitmap(filePath))
                        {
                            imgQRCode.Source = BitmapToImageSource(new Bitmap(bmp));
                        }
                    }
                }
            }
        }

        private void btnDeleteHistory_Click(object sender, RoutedEventArgs e)
        {
            if (lbHistory.SelectedItems.Count == 0)
                return;

            var selectedItems = lbHistory.SelectedItems.Cast<string>().ToList();
            var history = _historyService.LoadHistory();

            foreach (var selected in selectedItems)
            {
                var parts = selected.Split('|');
                if (parts.Length > 1)
                {
                    string filePath = parts[1].Trim();
                    var itemToRemove = history.Find(h => h.FilePath == filePath);
                    if (itemToRemove != null)
                        history.Remove(itemToRemove);
                }
            }

            _historyService.SaveHistory(history);
            // Оновлюємо список, не змінюючи видимість
            lbHistory.Items.Clear();
            foreach (var item in history)
                lbHistory.Items.Add($"{item.Text} | {item.FilePath}");
        }

        private void btnRenameHistory_Click(object sender, RoutedEventArgs e)
        {
            if (lbHistory.SelectedItem is string selected)
            {
                var parts = selected.Split('|');
                if (parts.Length > 1)
                {
                    string filePath = parts[1].Trim();
                    var history = _historyService.LoadHistory();
                    var itemToRename = history.Find(h => h.FilePath == filePath);
                    if (itemToRename != null)
                    {
                        var input = Microsoft.VisualBasic.Interaction.InputBox("Введіть новий опис:", "Перейменування", itemToRename.Text);
                        if (!string.IsNullOrWhiteSpace(input))
                        {
                            itemToRename.Text = input;
                            _historyService.SaveHistory(history);
                            btnShowHistory_Click(null, null); // Оновити список
                        }
                    }
                }
            }
        }

        private void txtHistorySearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var search = txtHistorySearch.Text.Trim().ToLower();
            lbHistory.Items.Clear();
            var history = _historyService.LoadHistory();
            foreach (var item in history)
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
            if (_isDark)
            {
                Application.Current.Resources["WindowBackgroundColor"] = System.Windows.Media.Brushes.White;
                Application.Current.Resources["ForegroundColor"] = System.Windows.Media.Brushes.Black;
            }
            else
            {
                Application.Current.Resources["WindowBackgroundColor"] = System.Windows.Media.Brushes.Black;
                Application.Current.Resources["ForegroundColor"] = System.Windows.Media.Brushes.White;
            }
            _isDark = !_isDark;
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
                MessageBox.Show("Немає тексту для копіювання.", "Копіювання", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ApplySystemTheme()
        {
            try
            {
                var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key != null)
                {
                    var value = key.GetValue("AppsUseLightTheme");
                    bool isLight = value == null || (int)value != 0;
                    Application.Current.Resources["WindowBackgroundColor"] = isLight ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.Black;
                    Application.Current.Resources["ForegroundColor"] = isLight ? System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.White;
                    this.Background = (System.Windows.Media.Brush)Application.Current.Resources["WindowBackgroundColor"];
                    Foreground = (System.Windows.Media.Brush)Application.Current.Resources["ForegroundColor"];
                    _isDark = !isLight;
                }
            }
            catch { /* Можна додати логування */ }
        }
    }
}