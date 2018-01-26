using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using Windows.UI.Xaml.Controls;

namespace GeoTrivia
{
    /// <summary>
    /// A map page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
	{
		public MainPage()
		{
			this.InitializeComponent();
            ViewModel.GraphicsOverlay = SceneView.GraphicsOverlays;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
		}

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SceneViewModel.IsSubmitted))
            {
                var viewpoint = SceneView.GetCurrentViewpoint(ViewpointType.CenterAndScale);
                ViewModel.UserAnswer = viewpoint.TargetGeometry as MapPoint;
                SceneView.SetViewpointAsync(new Viewpoint(ViewModel.ZoomToGeometry), new System.TimeSpan(0, 0, 1));
            }
            else if (e.PropertyName == nameof(SceneViewModel.GameMode))
            {
                if (ViewModel.GameMode == "Playing")
                {
                    SceneView.SetViewpointAsync(new Viewpoint(0.0, 0.0, 50000000.0), new System.TimeSpan(0, 0, 1));
                }
            }
        }

        /// <summary>
        /// Gets the view-model that provides mapping capabilities to the view
        /// </summary>
        public SceneViewModel ViewModel { get; } = new SceneViewModel();
    }
}
