using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        private Vector3D GetPositionAboveInGravity(Vector3D refPos, double distFromReference, IMyShipController controller)
        {
            double offset = (distFromReference / 10.0) * -1;
            Vector3D hoverPosition = refPos + controller.GetTotalGravity() * offset;
            return hoverPosition;
        }

        private Vector3D GetPositionBelowInGravity(Vector3D refPos, double distFromReference, IMyShipController controller)
        {
            double offset = (distFromReference / 10.0) * -1;
            Vector3D hoverPosition = refPos - controller.GetTotalGravity() * offset;
            return hoverPosition;
        }

        public Vector3D GetPositionFromTarget(Vector3D refPos, Vector3D targetPos, double distFromTarget = 0)
        {
            Vector3D positionFromTarget = new Vector3D(0, 0, 0);
            double length = (refPos - targetPos).Length();
            double percent = 1.0;
            if (distFromTarget > 0)
            {
                percent = distFromTarget / length;
            }
            positionFromTarget = targetPos + percent * (refPos - targetPos);
            return positionFromTarget;
        }

        public Vector3D GetRotatedPosition(Vector3D refPos, Vector3D initPos, float degrees)
        {
            MatrixD matrix = Matrix.CreateFromAxisAngle(refPos, MathHelper.ToRadians(degrees));
            Vector3D rotated = Vector3D.Transform(initPos, matrix);
            return rotated;
        }

        public Vector3D GetLocalDirection(Vector3D blockPos, MatrixD refMatrix)
        {
            // Get the world-relative direction of blockPos from the reference blockGrid
            Vector3D blockDir = blockPos - refMatrix.Translation;
            // Get the cubegrid-relative direction of blockPos from the reference blockGrid
            Vector3D localDir = Vector3D.TransformNormal(blockDir,
                MatrixD.Transpose(refMatrix));
            return localDir;
        }

        public Vector3D GetMovedPosition(Vector3D localDir, MatrixD refMatrix)
        {
            // Get the world direction using the local direction
            Vector3D worldDir = Vector3D.TransformNormal(localDir, refMatrix);
            // Get the world-relative position using the world direction
            Vector3D movedPos = worldDir + refMatrix.Translation;
            return movedPos;
        }

        public double GetShipDirectionalSpeed(IMyShipController controller, Base6Directions.Direction direction)
        {
            //get the velocity of the ship as a vector
            Vector3D velocity = controller.GetShipVelocities().LinearVelocity;
            //given a direction calculate the "length" for that direction, lenght is the speed in this case
            return velocity.Dot(controller.WorldMatrix.GetDirectionVector(direction));
        }

    }
}
