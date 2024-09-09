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
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        // =====================================================
        //  SCRIPT SETTINGS
        // =====================================================
        private const string scriptGroupName = "GPS Scanner";
        private const string blackBoxTag = "Blackbox";
        private const double scanDistance = 5000; // in meters

        // ======================================================
        //  DON'T EDIT BEYOND THIS LINE (UNLESS YOU KNOW HOW TO)
        // ======================================================
        private List<IMyCameraBlock> cameras = new List<IMyCameraBlock>();
        private List<IMyTerminalBlock> blackBoxes = new List<IMyTerminalBlock>();
        private IMyTerminalBlock blackBox;
        private StringBuilder logs = new StringBuilder();
        private bool isSetup = false;
        private const string scriptVersion = "1.0";

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.None;
        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {
            logs.Clear();
            logs.AppendLine($"GPS Scanner Script v{scriptVersion} by Khjin");

            if (!isSetup)
            {
                IMyBlockGroup group = GridTerminalSystem.GetBlockGroupWithName(scriptGroupName);
                if (group != null)
                {
                    group.GetBlocksOfType(cameras, b => b.IsSameConstructAs(Me));
                    group.GetBlocksOfType(blackBoxes, b => b.CustomName.ToLowerInvariant()
                        .Contains(blackBoxTag.ToLowerInvariant()));

                    if(cameras.Count == 0)
                    {
                        logs.AppendLine($"No cameras in {scriptGroupName} group.");
                        return;
                    }
                    if (blackBoxes.Count == 0)
                    {
                        logs.AppendLine($"No black box in {scriptGroupName} group.");
                        return;
                    }

                    //  Enable raycasting on cameras
                    foreach(var camera in  cameras)
                    {
                        camera.EnableRaycast = true;
                    }

                    // Select a black box
                    blackBox = blackBoxes[0];

                    isSetup = true;
                }
                else
                {
                    logs.AppendLine($"No group called {scriptGroupName} found.");
                }
            }

            if(isSetup)
            {
                logs.AppendLine("Script is setup. Run <Scan> command to scan.");

                if (argument.ToLowerInvariant() == "scan")
                {
                    IMyCameraBlock camera = GetUsedCamera();
                    if (camera != null && camera.CanScan(scanDistance))
                    {
                        MyDetectedEntityInfo scanned = camera.Raycast(scanDistance);
                        logs.AppendLine($"Scanned: {scanned.Name}");

                        if (!scanned.IsEmpty() && scanned.HitPosition != null && scanned.HitPosition.HasValue)
                        {
                            MyWaypointInfo info = new MyWaypointInfo(scanned.Name, scanned.HitPosition.Value);
                            string data = $"Name: {scanned.Name}, " +
                                          $"Relationship: {scanned.Relationship}," +
                                          $"Type: {scanned.Type}, " +
                                          $"{info}";

                            blackBox.CustomData += data + "\n";
                        }
                    }
                }
            }

            Echo(logs.ToString());
        }

        private IMyCameraBlock GetUsedCamera()
        {
            foreach(var camera in cameras)
            {
                if(camera.IsActive)
                {
                    return camera;
                }
            }
            return null;
        }
    }
}
