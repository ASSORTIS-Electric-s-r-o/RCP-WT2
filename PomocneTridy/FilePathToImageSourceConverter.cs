using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Diagnostics;
using System.IO;

namespace RCP_WT1.PomocneTridy
{
    // ==========================================
    // Převod absolutní cesty k obrázku na ImageSource
    // ==========================================
    internal sealed class FilePathToImageSourceConverter : IValueConverter
    {
        // ==========================================
        // Převod String -> BitmapImage
        // ==========================================
        public object? Convert(
            object value,
            Type targetType,
            object parameter,
            string language)
        {
            try
            {
                string path = (value?.ToString() ?? "").Trim();

                if (string.IsNullOrWhiteSpace(path))
                    return null;

                if (!File.Exists(path))
                {
                    Debug.WriteLine($"Soubor obrázku neexistuje: {path}");
                    return null;
                }

                return new BitmapImage(new Uri(path, UriKind.Absolute));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Chyba při načítání obrázku:");
                Debug.WriteLine(ex.ToString());

                return null;
            }
        }

        // ==========================================
        // Převod zpět není podporován
        // ==========================================
        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            string language)
        {
            throw new NotImplementedException();
        }
    }
}