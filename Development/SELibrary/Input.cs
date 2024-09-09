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
    partial class Program
    {
        public class Input
        {
            private IMyShipController controller;
            public float ForwardBack { get; private set; }
            public float LeftRight { get; private set; }
            public float UpDown { get; private set; }
            public float Pitch { get; private set; }
            public float Roll { get; private set; }
            public float Yaw { get; private set; }
            public Input() { }
            public void Update(IMyShipController controller)
            {
                if(controller != null)
                {
                    this.controller = controller;
                    ForwardBack = controller.MoveIndicator.Z;
                    LeftRight = controller.MoveIndicator.X;
                    UpDown = controller.MoveIndicator.Y;
                    Pitch = controller.RotationIndicator.Y;
                    Roll = controller.RollIndicator;
                    Yaw = controller.RotationIndicator.X;
                }
                else
                {
                    this.controller = controller;
                    ForwardBack = 0;
                    LeftRight = 0;
                    UpDown = 0;
                    Pitch = 0;
                    Roll = 0;
                    Yaw = 0;
                }
            }
            public bool IsEmpty { get { return (controller == null); } }
        }
    }
}
