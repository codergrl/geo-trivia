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

using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace XInputHelper
{
    public static partial class CameraExtensions
    {
       public static Camera SetPitch(this Camera camera, double pitch)
        {
            return new Camera(camera.Location, camera.Heading, pitch, camera.Roll);
        }

        public static Camera Rotate(this Camera camera, double deltaHeading, double deltaPitch)
        {
            return new Camera(camera.Location, camera.Heading + deltaHeading, camera.Pitch + deltaPitch, camera.Roll);
        }

        public static Camera SetLocation(this Camera camera, MapPoint mapPoint)
        {
            var c = new Camera(mapPoint, camera.Heading, Double.IsNaN(camera.Pitch) ? 0:camera.Pitch, camera.Roll);
            return c;
        }
    }
}
