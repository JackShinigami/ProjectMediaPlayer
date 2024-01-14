using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ProjectMediaPlayer
{
    class SecondToTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int secondInt = 0;
            double secondDouble = 0;
            int second = 0;
            try
            {
                secondDouble = (double)value;
                second = (int)secondDouble;
            } catch (Exception e)
            {
                secondInt = (int)value;
                second = secondInt;
            }
            TimeSpan time = TimeSpan.FromSeconds(second);
            if(time.Hours == 0)
            {
                return time.ToString(@"mm\:ss");
            }
            return time.ToString(@"hh\:mm\:ss");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string time = value.ToString();
            string[] timeArray = time.Split(':');
            double second = 0;
            if (timeArray.Length == 3)
            {
                second = double.Parse(timeArray[0]) * 3600 + double.Parse(timeArray[1]) * 60 + double.Parse(timeArray[2]);
            }
            else if (timeArray.Length == 2)
            {
                second = double.Parse(timeArray[0]) * 60 + double.Parse(timeArray[1]);
            }
            else if (timeArray.Length == 1)
            {
                second = double.Parse(timeArray[0]);
            }
            return second;
        }
    }
}
