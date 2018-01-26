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
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
		}

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SceneViewModel.IsSubmitted))
            {
                var viewpoint = SceneView.GetCurrentViewpoint(ViewpointType.CenterAndScale);
                ViewModel.UserAnswer = viewpoint.TargetGeometry as MapPoint;
            }
        }

        /// <summary>
        /// Gets the view-model that provides mapping capabilities to the view
        /// </summary>
        public SceneViewModel ViewModel { get; } = new SceneViewModel();
    }
}
