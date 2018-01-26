using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks;
using Esri.ArcGISRuntime.UI;
using System.Windows.Input;
using GeoTrivia.Commands;

namespace GeoTrivia
{
    /// <summary>
    /// Provides map data to an application
    /// </summary>
    public class SceneViewModel : INotifyPropertyChanged
    {
        private ICommand _changeDifficultyCommand;
        private int _difficulty = 1;
        private ICommand _startGameCommand;
        private string _gameMode = "ChooseDifficulty";

        private List<Feature> _questions = null;
        private Question _currentQuestion = null;
        private int _idx = -1;

        public SceneViewModel()
        {
            InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            Scene = new Scene(new Basemap(new Uri("https://www.arcgis.com/home/webmap/viewer.html?webmap=86265e5a4bbb4187a59719cf134e0018")));
            var serviceFeatureTable = new ServiceFeatureTable(new Uri("https://services1.arcgis.com/6677msI40mnLuuLr/arcgis/rest/services/TriviaMap/FeatureServer/0"));
            await serviceFeatureTable.LoadAsync();

            var query = new QueryParameters();
            query.WhereClause = "1=1";

            var features = await serviceFeatureTable.QueryFeaturesAsync(query);
            _questions = features.ToList();
            NextQuestion();

            Scene.OperationalLayers.Add(new FeatureLayer(serviceFeatureTable));
        }

        private Scene _scene;

        /// <summary>
        /// Gets or sets the map
        /// </summary>
        public Scene Scene
        {
            get { return _scene; }
            set { _scene = value; OnPropertyChanged(); }
        }

        public string GameMode
        {
            get { return _gameMode; }
            set
            {
                if (_gameMode != value )
                {
                    _gameMode = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Difficulty
        {
            get { return _difficulty; }
            set
            {
                _difficulty = value;
                OnPropertyChanged();
            }
        }

        public ICommand ChangeDifficultyCommand
        {
            get
            {
                return _changeDifficultyCommand ?? (_changeDifficultyCommand = new DelegateCommand(
                    (x) =>
                    {
                        switch (x)
                        {
                            case "Easy":
                                Scene.Basemap = new Basemap(new Uri("https://www.arcgis.com/home/webmap/viewer.html?webmap=86265e5a4bbb4187a59719cf134e0018"));
                                Difficulty = 1;
                                break;
                            case "Medium":
                                Scene.Basemap = new Basemap(new Uri("https://www.arcgis.com/home/webmap/viewer.html?webmap=68a4f59815c745eeb2fa161f6ea0c112"));
                                Difficulty = 2;
                                break;
                            case "Hard":
                                Scene.Basemap = new Basemap(new Uri("https://www.arcgis.com/home/webmap/viewer.html?useExisting=1&layers=c4ec722a1cd34cf0a23904aadf8923a0"));
                                Difficulty = 3;
                                break;
                        }
                    }));
            }
        }

        public ICommand StartGameCommand
        {
            get
            {
                return _startGameCommand ?? (_startGameCommand = new DelegateCommand(
                    (x) =>
                    {
                        GameMode = "Playing";
                        //start loading questions
                    }));
            }
        }

        /// <summary>
        /// Raises the <see cref="SceneViewModel.PropertyChanged" /> event
        /// </summary>
        /// <param name="propertyName">The name of the property that has changed</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var propertyChangedHandler = PropertyChanged;
            if (propertyChangedHandler != null)
                propertyChangedHandler(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Question CurrentQuestion
        {
            get { return _currentQuestion; }
        }

        public delegate void NewQuestionEvent();

        public event NewQuestionEvent NewQuestion;

        public async void NextQuestion()
        {
            _idx += 1;
            if (_idx < _questions.Count)
            {
                var curQuestion = _questions[_idx] as ArcGISFeature;
                await curQuestion.LoadAsync();

                _currentQuestion = new Question(curQuestion.Attributes["Question"].ToString(), curQuestion.Attributes["Answer"].ToString(), curQuestion.Geometry);
                NewQuestion?.Invoke();
            }
        }
    }
}
