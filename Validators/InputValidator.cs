using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QRCodeGenerator.Validators
{
    public static class InputValidator
    {
        public static void ValidateText(string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("Будь ласка, введіть текст або URL.", "Input Error");

            if (text.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !Uri.IsWellFormedUriString(text, UriKind.Absolute))
                throw new ArgumentException("Введено некоректне посилання.", "URL Error");

            if (text.Length > 1000)
                throw new ArgumentException("Текст занадто довгий (максимум 1000 символів).", "Length Error");
        }
    }
}
