using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace ProjectMediaPlayer
{
    public class FilePathToTitleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path)
            {
                string title = GetTitleFromPath(path);
                return string.IsNullOrEmpty(title) ? Path.GetFileNameWithoutExtension(path) : title;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private string GetTitleFromPath(string path)
        {
            var file = TagLib.File.Create(path);
            var title = file.Tag.Title;
            var singer = file.Tag.FirstPerformer;

            var result = string.Empty;
            if (singer != null && title != null)
            {
                result = $"{title} - {singer}";
            }

            return result;
        }
    }

}
