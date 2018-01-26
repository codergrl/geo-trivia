// /*******************************************************************************
//  * Copyright 2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace GeoTrivia.Converters
{
    class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if ((bool)value == true)
            {
                return GetSolidColorBrush("#FF008963");
            }
            else
                return GetSolidColorBrush("#FFff4f00");

        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

        private SolidColorBrush GetSolidColorBrush(string hex)
        {
            hex = hex.Replace("#", string.Empty);
            byte a = (byte)(System.Convert.ToUInt32(hex.Substring(0, 2), 16));
            byte r = (byte)(System.Convert.ToUInt32(hex.Substring(2, 2), 16));
            byte g = (byte)(System.Convert.ToUInt32(hex.Substring(4, 2), 16));
            byte b = (byte)(System.Convert.ToUInt32(hex.Substring(6, 2), 16));
            SolidColorBrush myBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(a, r, g, b));
            return myBrush;
        }
    }
}
