using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Scripting;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public bool TryParseToGpsFormat(string vectorFormat, string gpsName, out string gpsFormat)
        {
            Vector3D coords = Vector3D.Zero;
            gpsFormat = string.Empty;
            if (Vector3D.TryParse(vectorFormat, out coords))
            {
                MyWaypointInfo info = new MyWaypointInfo(gpsName, coords);
                gpsFormat = info.ToString();
                return true;
            }
            return false;
        }

        public bool TryParseToVectorFormat(string gpsFormat, out string vectorFormat, out string gpsName)
        {
            MyWaypointInfo info;
            vectorFormat = string.Empty;
            gpsName = string.Empty;

            // GPS:Fuel Storage:45367.65:229935.4:-123361.87:#FF75C9F1:
            string[] parts = gpsFormat.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            gpsFormat = string.Empty;
            for(int i = 0; i < 5; i++)
            { gpsFormat += (parts[i] + ":"); }

            if(MyWaypointInfo.TryParse(gpsFormat, out info))
            {
                vectorFormat = info.Coords.ToString();
                gpsName = info.Name;
                return true;
            }

            return false;
        }

        public bool TryParseToVector(string gpsFormat, out Vector3D vector)
        {
            vector = Vector3D.Zero;
            MyWaypointInfo info;
            if(MyWaypointInfo.TryParse(gpsFormat, out info))
            {
                vector = info.Coords;
                return true;
            }
            return false;
        }
    }
}
