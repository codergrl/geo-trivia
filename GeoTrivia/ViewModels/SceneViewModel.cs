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
        private const int DIFFICULTY_EASY = 100;
        private const int DIFFICULTY_MEDIUM = 250;
        private const int DIFFICULTY_HARD = 500;

        // Amount we will buffer the actual answer geometry at each step to
        // determine if the user got close enough to the answer to cound as
        // a correct answer
        private const double BUFFER_FRACTION = 0.25;

        // Number of times we will try to buffer the feature by BUFFER_FRACTION
        // before considering the guess incorrect
        private const int NUM_BUFFER_ATTEMPTS = 3;

        private Scene _scene;
        private ICommand _changeDifficultyCommand;
        private int _difficulty = DIFFICULTY_EASY;
        private int _points = 0;
        private bool _isSubmitted;
        private MapPoint _userAnswer;
        private ICommand _startGameCommand;
        private ICommand _submitAnswerCommand;
        private ICommand _nextQuestionCommand;
        private string _gameMode = "ChooseDifficulty";
        private GraphicsOverlayCollection _graphicsOverlays = null;
        private GraphicsOverlay _guessOverlay = null;
        private GraphicsOverlay _actualAnswerOverlay = null;
        private GraphicsOverlay _correctAnswerOverlay = null;
        private GraphicsOverlay _incorrectAnswerOverlay = null;
        private GraphicsOverlay _incorrectLineOverlay = null;
        private GraphicsOverlay _debugOverlay = null;
        private List<Question> _questions = null;
        private Question _currentQuestion = null;
        private int _idx = -1;
        private bool _isCorrect;
        private double _userErrorKM = 0;
        private Feedback _feedback;
        private Geometry _zoomToGeometry = null;
        private String _endOfGameMessage = "";
        private bool _debugOverlayEnabled = false;

        public SceneViewModel()
        {
            InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            Scene = new Scene(Basemap.CreateImageryWithLabels());
            LoadQuestions();
        }

        private async void LoadQuestions()
        {
            var serviceFeatureTable = new ServiceFeatureTable(new Uri("https://services1.arcgis.com/6677msI40mnLuuLr/arcgis/rest/services/TriviaMap/FeatureServer/0"));
            await serviceFeatureTable.LoadAsync();

            var query = new QueryParameters();
            query.WhereClause = "1=1";

            var features = await serviceFeatureTable.QueryFeaturesAsync(query);
            var questions = features.ToList();

            _questions = new List<Question>();
            foreach (ArcGISFeature question in questions)
            {
                await question.LoadAsync();

                var text = question.Attributes["Question"];
                var answer = question.Attributes["Answer"];
                if (question != null && answer != null)
                {
                    _questions.Add(new Question(text.ToString(), answer.ToString(), question.Geometry));
                }
            }

            NextQuestion();
        }

        public int MaximumPossiblePoints()
        {
            return _questions.Count * NUM_BUFFER_ATTEMPTS * Difficulty;
        }

        /// <summary>
        /// Gets or sets the map
        /// </summary>
        public Scene Scene
        {
            get => _scene;
            set { _scene = value; OnPropertyChanged(); }
        }

        public string EndOfGameMessage
        {
            get => _endOfGameMessage;
            set { _endOfGameMessage = value; OnPropertyChanged(); }
        }

        public string GameMode
        {
            get => _gameMode;
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
            get => _difficulty;
            set { _difficulty = value; OnPropertyChanged(); }
        }

        public int Points
        {
            get => _points;
            set { _points = value; OnPropertyChanged(); }
        }

        public bool IsSubmitted
        {
            get => _isSubmitted;
            set { _isSubmitted = value; OnPropertyChanged(); }

        }

        public int Idx
        {
            get => _idx;
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
            get => _userAnswer;
            set
            {
                _userAnswer = value;
                OnPropertyChanged();
                CompareAnswerToGeometry();
            }
        }

        public Feedback Feedback
        {
            get => _feedback;
            set { _feedback = value; OnPropertyChanged(); }
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
                                Difficulty = DIFFICULTY_EASY;
                                break;
                            case "Medium":
                                Scene.Basemap = new Basemap(new ArcGISTiledLayer(new Uri("https://wtb.maptiles.arcgis.com/arcgis/rest/services/World_Topo_Base/MapServer")));
                                Difficulty = DIFFICULTY_MEDIUM;
                                break;
                            case "Hard":
                                Scene.Basemap = Basemap.CreateImagery();
                                Difficulty = DIFFICULTY_HARD;
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
            get => _graphicsOverlays;
            set => _graphicsOverlays = value;
        }

        public double UserErrorKM
        {
            get => _userErrorKM;
            set => _userErrorKM = value;
        }

        public Question CurrentQuestion
        {
            get => _currentQuestion;
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
            set
            {
                _isCorrect = value;
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

        public Geometry ZoomToGeometry
        {
            get => _zoomToGeometry;
            set => _zoomToGeometry = value;
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

        public void NextQuestion()
        {
            if (_guessOverlay != null)
            {
                _guessOverlay.Graphics.Clear();
            }

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

            if (_incorrectLineOverlay != null)
            {
                _incorrectLineOverlay.Graphics.Clear();
            }

            Idx += 1;
            if (Idx < _questions.Count)
            {
                CurrentQuestion = _questions[Idx];
            }
            else
            {
                var percentCorrect = (double)Points / (double)MaximumPossiblePoints() * 100.0;

                if (percentCorrect <= 30.0)
                {
                    EndOfGameMessage = "Maybe try harder next time.  You got " + Convert.ToString(Points) + " Points.";
                }
                else if (percentCorrect <= 80.0)
                {
                    EndOfGameMessage = "OK, you know your way around.  You got " + Convert.ToString(Points) + " Points!  That's " + Convert.ToString(Math.Round(percentCorrect)) + "%";
                }
                else
                {
                    EndOfGameMessage = "Do you want a job?  You got " + Convert.ToString(Points) + " Points!  That's " + Convert.ToString(Math.Round(percentCorrect)) + "%";
                }

                GameMode = "GameOver";
            }
        }

        private void CompareAnswerToGeometry()
        {
            if (_guessOverlay == null ||
                _actualAnswerOverlay == null || 
                _correctAnswerOverlay == null || 
                _incorrectAnswerOverlay == null ||
                _incorrectLineOverlay == null)
            {
                InitializeOverlays();
            }

            var actualGeometry = CurrentQuestion.Geometry;

            var minDimension = Math.Min(actualGeometry.Extent.Width, actualGeometry.Extent.Height);

            // Determine if the user was correct or not by testing the point inside the polygon, first
            // unbuffered, then by an increasing amout of buffer
            int i = 0;
            var correct = false;
            UserErrorKM = 0;
            while (!correct && i < NUM_BUFFER_ATTEMPTS)
            {
                var bufferedGeometry = GeometryEngine.Buffer(actualGeometry, minDimension * (BUFFER_FRACTION * i));
                correct = GeometryEngine.Contains(bufferedGeometry, UserAnswer);
                if (_debugOverlayEnabled)
                {
                    _debugOverlay.Graphics.Add(new Graphic(bufferedGeometry));
                }
                ++i;
            }

            if (correct == true)
            {
                var currentHightlight = actualGeometry;
                for (int j = 0; j < Math.Max(i, 2); ++j)
                {
                    currentHightlight = GeometryEngine.Buffer(actualGeometry, minDimension * (BUFFER_FRACTION * j));
                    _correctAnswerOverlay.Graphics.Add(new Graphic(currentHightlight));
                }

                Points += (Difficulty * ((NUM_BUFFER_ATTEMPTS + 1) - i));
                _zoomToGeometry = GeometryEngine.Buffer(actualGeometry, actualGeometry.Extent.Width);
            }
            else
            {
                // Highlight the correct answer
                var highlightGeometry = GeometryEngine.Buffer(actualGeometry, minDimension * 0.1);
                _incorrectAnswerOverlay.Graphics.Add(new Graphic(highlightGeometry));

                _guessOverlay.Graphics.Add(new Graphic(UserAnswer));

                // Build a polyline between the user's guess and the actual answer
                var actualCenter = actualGeometry.Extent.GetCenter();

                var builder = new PolylineBuilder(SpatialReference.Create(4326));
                builder.AddPoint(UserAnswer);
                builder.AddPoint(actualCenter);

                var line = builder.ToGeometry();
                _zoomToGeometry = GeometryEngine.Buffer(line, line.Extent.Width * 0.25);
                _incorrectLineOverlay.Graphics.Add(new Graphic(line));

                // Calculate how far they were off in Kilometers
                var distanceResult = GeometryEngine.DistanceGeodetic(UserAnswer, actualCenter, LinearUnits.Kilometers, AngularUnits.Degrees, GeodeticCurveType.Geodesic);
                UserErrorKM = distanceResult.Distance;
            }

            _actualAnswerOverlay.Graphics.Add(new Graphic(actualGeometry));

            IsCorrect = correct;
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

            var guessSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.X, Windows.UI.Color.FromArgb(255, 255, 0, 0), 80.0);
            _guessOverlay = new GraphicsOverlay();
            _guessOverlay.Renderer = new SimpleRenderer(guessSymbol);
            GraphicsOverlay.Add(_guessOverlay);

            var incorrectLineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Dot, Windows.UI.Color.FromArgb(200, 0, 0, 0), 5.0);
            _incorrectLineOverlay = new GraphicsOverlay();
            _incorrectLineOverlay.Renderer = new SimpleRenderer(incorrectLineSymbol);
            GraphicsOverlay.Add(_incorrectLineOverlay);

            if (_debugOverlayEnabled)
            {
                var debugOutline = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Windows.UI.Colors.AliceBlue, 10.0);
                var debugSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, Windows.UI.Color.FromArgb(128, outline_gray, outline_gray, outline_gray), debugOutline);
                _debugOverlay = new GraphicsOverlay();
                _debugOverlay.Renderer = new SimpleRenderer(debugSymbol);
                GraphicsOverlay.Add(_debugOverlay);
            }
        }
    }
}
