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
        private int _points = 0;
        private bool _isSubmitted;
        private MapPoint _userAnswer;
        private ICommand _startGameCommand;
        private ICommand _submitAnswerCommand;
        private ICommand _nextQuestionCommand;
        private string _gameMode = "ChooseDifficulty";
        private GraphicsOverlayCollection _graphicsOverlays = null;
        private GraphicsOverlay _actualAnswerOverlay = null;
        private GraphicsOverlay _correctAnswerOverlay = null;
        private GraphicsOverlay _incorrectAnswerOverlay = null;
        private List<Feature> _questions = null;
        private Question _currentQuestion = null;
        private int _idx = -1;
        private bool _isCorrect;
        private double _userErrorKM = 0;
        private Feedback _feedback;

        public SceneViewModel()
        {
            InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            Scene = new Scene(Basemap.CreateImageryWithLabels());

            var serviceFeatureTable = new ServiceFeatureTable(new Uri("https://services1.arcgis.com/6677msI40mnLuuLr/arcgis/rest/services/TriviaMap/FeatureServer/0"));
            await serviceFeatureTable.LoadAsync();

            var featureLayer = new FeatureLayer(serviceFeatureTable);
            featureLayer.DefinitionExpression = "1 = 0";
            Scene.OperationalLayers.Add(featureLayer);

            var query = new QueryParameters();
            query.WhereClause = "1=1";

            var features = await serviceFeatureTable.QueryFeaturesAsync(query);
            _questions = features.ToList();
            NextQuestion();
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

        public int Points
        {
            get { return _points; }
            set
            {
                _points = value;
                OnPropertyChanged();
            }
        }

        public bool IsSubmitted
        {
            get { return _isSubmitted; }
            set { _isSubmitted = value;
                OnPropertyChanged();
            }

        }

        public int Idx
        {
            get { return _idx; }
            set
            {
                if (_idx != value)
                {
                    _idx = value;
                    OnPropertyChanged();
                }
            }
        }

        public MapPoint UserAnswer
        {
            get { return _userAnswer; }
            set
            {
                _userAnswer = value;
                OnPropertyChanged();
                CompareAnswerToGeometry();
            }
        }

        public Feedback Feedback
        {
            get { return _feedback; }
            set
            {
                _feedback = value;
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
                                Scene.Basemap = Basemap.CreateNationalGeographic();
                                Difficulty = 100;
                                break;
                            case "Medium":
                                Scene.Basemap = new Basemap(new ArcGISTiledLayer(new Uri("https://wtb.maptiles.arcgis.com/arcgis/rest/services/World_Topo_Base/MapServer")));
                                Difficulty = 250;
                                break;
                            case "Hard":
                                Scene.Basemap = Basemap.CreateImagery();
                                Difficulty = 500;
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

        public ICommand SubmitAnswerCommand
        {
            get
            {
                return _submitAnswerCommand ?? (_submitAnswerCommand = new DelegateCommand(
                    (x) =>
                    {
                        //user submitted the answer
                        IsSubmitted = true;
                    }));
            }
        }

        public ICommand NextQuestionCommand
        {
            get
            {
                return _nextQuestionCommand ?? (_nextQuestionCommand = new DelegateCommand(
                    (x) =>
                    {
                        GameMode = "Playing";
                        NextQuestion();
                    }));
            }
        }

        public GraphicsOverlayCollection GraphicsOverlay
        {
            get { return _graphicsOverlays; }
            set { _graphicsOverlays = value; }
        }

        public double UserErrorKM
        {
            get { return _userErrorKM; }
            set {
                _userErrorKM = value;
                OnPropertyChanged();
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
            set
            {
                if (_currentQuestion != value)
                {
                    _currentQuestion = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsCorrect
        {
            get => _isCorrect;
            set { _isCorrect = value;
                OnPropertyChanged();
                Feedback = new Feedback()
                {
                    IsCorrect = value,
                    Distance = UserErrorKM,
                    Answer = CurrentQuestion.Answer,
                    FunFact = "This is where the fun fact goes if we decide to add one in."
                };
            }
        }

        public delegate void NewQuestionEvent();

        public event NewQuestionEvent NewQuestion;

        public async void NextQuestion()
        {
            if (_actualAnswerOverlay != null)
            {
                _actualAnswerOverlay.Graphics.Clear();
            }

            if (_correctAnswerOverlay != null)
            {
                _correctAnswerOverlay.Graphics.Clear();
            }

            if (_incorrectAnswerOverlay != null)
            {
                _incorrectAnswerOverlay.Graphics.Clear();
            }

            Idx += 1;
            if (Idx < _questions.Count)
            {
                var curQuestion = _questions[Idx] as ArcGISFeature;
                await curQuestion.LoadAsync();

                var question = curQuestion.Attributes["Question"];
                var answer = curQuestion.Attributes["Answer"];
                if (question != null && answer != null)
                {
                    CurrentQuestion = new Question(curQuestion.Attributes["Question"].ToString(), curQuestion.Attributes["Answer"].ToString(), curQuestion.Geometry);
                    NewQuestion?.Invoke();
                }
            }
        }

        private void CompareAnswerToGeometry()
        {
            if (_actualAnswerOverlay == null || _correctAnswerOverlay == null || _incorrectAnswerOverlay == null)
            {
                InitializeOverlays();
            }

            var actualGeometry = CurrentQuestion.Geometry;

            var minDimension = Math.Min(actualGeometry.Extent.Width, actualGeometry.Extent.Height);

            var highlightGeometry = GeometryEngine.Buffer(actualGeometry, minDimension * 0.1);

            int i = 0;
            var correct = false;
            UserErrorKM = 0;
            while (!correct && i < 3)
            {
                var bufferedGeometry = GeometryEngine.Buffer(actualGeometry, minDimension * (0.5 * i));
                correct = GeometryEngine.Contains(bufferedGeometry, UserAnswer);
                ++i;
            }

            if (correct == true)
            {
                Points += (Difficulty * (4 - i));
                _correctAnswerOverlay.Graphics.Add(new Graphic(highlightGeometry));
            }
            else
            {
                _incorrectAnswerOverlay.Graphics.Add(new Graphic(highlightGeometry));

                var distanceResult = GeometryEngine.DistanceGeodetic(UserAnswer, actualGeometry.Extent.GetCenter(), LinearUnits.Kilometers, AngularUnits.Degrees, GeodeticCurveType.Geodesic);
                UserErrorKM = distanceResult.Distance;
            }

            IsCorrect = correct;

            _actualAnswerOverlay.Graphics.Add(new Graphic(actualGeometry));

            GameMode = "AnswerSubmitted";
        }

        private void InitializeOverlays()
        {
            byte opacity = 180;
            byte outline_gray = 128;

            var outlineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Dash, Windows.UI.Color.FromArgb(255, outline_gray, outline_gray, outline_gray), 5.0);

            var correctSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, Windows.UI.Color.FromArgb(opacity, 0, 255, 128), outlineSymbol);
            _correctAnswerOverlay = new GraphicsOverlay();
            _correctAnswerOverlay.Renderer = new SimpleRenderer(correctSymbol);
            GraphicsOverlay.Add(_correctAnswerOverlay);

            var incorrectSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, Windows.UI.Color.FromArgb(opacity, 255, 0, 0), outlineSymbol);
            _incorrectAnswerOverlay = new GraphicsOverlay();
            _incorrectAnswerOverlay.Renderer = new SimpleRenderer(incorrectSymbol);
            GraphicsOverlay.Add(_incorrectAnswerOverlay);

            var actualGeometrySymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Windows.UI.Color.FromArgb(255, 255, 255, 255), 3.0);
            _actualAnswerOverlay = new GraphicsOverlay();
            _actualAnswerOverlay.Renderer = new SimpleRenderer(actualGeometrySymbol);
            GraphicsOverlay.Add(_actualAnswerOverlay);
        }
    }
}
