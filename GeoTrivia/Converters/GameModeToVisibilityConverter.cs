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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace GeoTrivia.Converters
{
    class GameModeToVisibilityConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
        {
            switch (parameter)
            {
                case "DifficultyScreen":
                    if (value.ToString() == "ChooseDifficulty")
                        return Visibility.Visible;
                    else
                        return Visibility.Collapsed;
                case "GameScreen":
                    if (value.ToString() == "Playing")
                        return Visibility.Visible;
                    else
                        return Visibility.Collapsed;
                case "FeedbackScreen":
                    if (value.ToString() == "AnswerSubmitted")
                        return Visibility.Visible;
                    else
                        return Visibility.Collapsed;
                case "PointsBanner":
                    if (value.ToString() == "AnswerSubmitted" || value.ToString() == "Playing")
                        return Visibility.Visible;
                    else
                        return Visibility.Collapsed;
                case "GameOverScreen":
                    if (value.ToString() == "GameOver")
                        return Visibility.Visible;
                    else
                        return Visibility.Collapsed;
                default:
                    return Visibility.Collapsed;
            }
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
