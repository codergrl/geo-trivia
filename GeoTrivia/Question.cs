using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Geometry;

namespace GeoTrivia
{
    public class Question
    {
        private String _text = "";
        private String _answer = "";
        private Geometry _geom = null;

        public String Text
        {
            get { return _text; }
        }

        public String Answer
        {
            get { return _answer; }
        }

        public Geometry Geometry
        {
            get { return _geom; }
        }

        public Question(String text, String answer, Geometry geom)
        {
            _text = text;
            _answer = answer;
            _geom = GeometryEngine.Project(geom, new SpatialReference(4326));
        }
    }
}
