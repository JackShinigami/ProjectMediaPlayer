using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ProjectMediaPlayer
{
    class DoubleToPercentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double percent = (double)value;
            return percent.ToString("P1");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string percent = value.ToString();
            percent = percent.Substring(0, percent.Length - 1);
            return double.Parse(percent);
        }
    }
}
