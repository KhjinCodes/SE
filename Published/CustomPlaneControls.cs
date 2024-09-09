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
        /*
         * How to Use
         * -----------
         * 
         * This script allows you to control rotors that will control
         * custom surfaces (Plane Parts, Randa's Thin wings) using normal
         * roll, pitch and yaw motions. To setup:
         * 
         * 
         * 1. The script will autodetect controlled seats as reference.
         *    If you want a specific set of seats, add them to a group
         *    called "Pilot Seats". You can use remote controls too.
         * 2. Add roll related rotors to a group called "Roll".
         * 3. Add pitch related rotors to a group called "Pitch".
         * 4. Add yaw related rotors to a group called "Yaw".
         * 5. The script can auto throttle forward thrust to automatically
         *    lower it when the plane's max speed is reached. If you want
         *    this behavior, add the forward thrusters to a group called
         *    "Forward Thrusters".
         * 6. The script also supports Plane Parts airbrakes activation
         *    using SPACE key. To do this, add airbrake to a group called
         *    "Airbrakes".
         * 7. Compile and Run the Script.
         * 8. To fine tune your controls, check out the settings in this PB's
         *    custom data.
         * 
         * 
         * 
         */

        // ==============================
        //  DO NOT EDIT BEYOND THIS LINE
        // ==============================
        //
        double worldMaxSpeed = 105;
        string rollGroup = "Roll";
        string pitchGroup = "Pitch";
        string yawGroup = "Yaw";
        string invertRollGroup = "InvertRoll";
        string invertPitchGroup = "InvertPitch";
        string invertYawGroup = "InvertYaw";
        string airbrakesGroup = "Airbrakes";
        string invertAirbrakesGroup = "InvertAirbrake";
        string controllersGroup = "Pilot Seats";
        string thrustersGroup = "Forward Thrusters";
        bool resetWhenNoInput = true;
        bool useReverseForBrakes = false;
        bool controlThrusters = true;

        const float DEFAULT_CONTROL_SPEED = 15.0f;
        const float DEFAULT_RESET_SPEED = 15.0f;
        const float DEFAULT_RESET_ANGLE = 0.0f;
        const float DEFAULT_MAX_VELOCITY = 100.0f;
        const int ROLL_RESET_WAIT = 10;
        const int PITCH_RESET_WAIT = 10;
        const int YAW_RESET_WAIT = 10;
        const int AIRBRAKE_RESET_WAIT = 5;
        const float YAW_SPEED_MULTIPLIER = 0.05f;
        const float PITCH_SPEED_MULTIPLIER = 0.05f;
        const float AIRBRAKE_SPEED_MULTIPLIER = 1.0f;
        const float MAX_THRUSTER_RAMPUP = 1.0f;
        const float THRUST_RAMPUP_FACTOR = 1.0f / 30; // 100% thrust over 30 ticks.
        const float MIN_THRUST = 0.0000000001f;
        const float MAX_ROLL_RAMPUP = 1.0f;
        const float ROLL_RAMPUP_FACTOR = 1.0f / 60; // 100% RPM over 60 ticks.
        const string DEBUG_TEXTPANEL = "CPC_DEBUG";

        int currRollTime = 0;
        int currPitchTime = 0;
        int currYawTime = 0;
        int currAirbrakeTime = 0;
        float currThrusterRampUp = 0;
        float currRollRampUp = 0;

        Dictionary<IMyMotorStator, RotorSettings> allRotors = new Dictionary<IMyMotorStator, RotorSettings>();
        List<IMyMotorStator> rollRotors = new List<IMyMotorStator>();
        List<IMyMotorStator> pitchRotors = new List<IMyMotorStator>();
        List<IMyMotorStator> yawRotors = new List<IMyMotorStator>();
        List<IMyAdvancedDoor> airbrakes = new List<IMyAdvancedDoor>();
        List<IMyMotorStator> airbrakeRotors = new List<IMyMotorStator>();
        List<IMyMotorStator> invertAirbrakeRotors = new List<IMyMotorStator>();
        List<IMyMotorStator> invertRollRotors = new List<IMyMotorStator>();
        List<IMyMotorStator> invertPitchRotors = new List<IMyMotorStator>();
        List<IMyMotorStator> invertYawRotors = new List<IMyMotorStator>();
        List<IMyThrust> thrusters = new List<IMyThrust>();
        List<IMyShipController> controllers = new List<IMyShipController>();

        IMyBlockGroup blockGroup = null;
        IMyShipController controller = null;
        IMyTextSurface debugTextSufrace = null;
        ScriptSettings settings = null;
        StringBuilder logs = new StringBuilder();
        StringBuilder statsText = new StringBuilder();
        bool isSetup = false;

        public Program()
        {
            settings = new ScriptSettings(Me);
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            logs.Clear();
            logs.AppendLine($"Khjin's Custom Plane Controls v1.0...{Spinner}");

            if (!isSetup)
            {
                isSetup = PerformSetup();
            }

            if (isSetup)
            {
                logs.AppendLine($"Roll Rotors: {rollRotors.Count}");
                logs.AppendLine($"Pitch Rotors: {pitchRotors.Count}");
                logs.AppendLine($"Yaw Rotors: {yawRotors.Count}");
                logs.AppendLine($"Airbrakes: {((airbrakes.Count > 0 || airbrakeRotors.Count > 0) ? "YES" : "NO")}");
                logs.AppendLine($"Thruster Control: {(thrusters.Count > 0 ? "YES" : "NO")}");

                controller = GetController();
                if (controller != null)
                {
                    Vector3 wasdInputs = controller.MoveIndicator;
                    Vector3 rotationInputs = new Vector3(controller.RotationIndicator.X,
                                                         controller.RotationIndicator.Y,
                                                         controller.RollIndicator);

                    HandleCommands(argument);

                    HandleRoll(rotationInputs);
                    HandlePitch(rotationInputs);
                    HandleYaw(rotationInputs);

                    HandleAirbrakeRotors(wasdInputs);
                    HandleAirbrakes(wasdInputs);
                    HandleThrusters(wasdInputs);
                }
                else
                {
                    controller = GetFreeController();
                    foreach (IMyMotorStator rotor in allRotors.Keys.ToList())
                    {
                        ResetRotor(rotor);
                    }
                    logs.AppendLine("Ship is not contolled, standing by...");
                }

                if (debugTextSufrace != null)
                {
                    debugTextSufrace.WriteText(logs.ToString());
                }
            }

            Echo(logs.ToString());
        }

        void HandleCommands(string args)
        {
            if (args == "thrust_standby_on")
            {
                controlThrusters = false;
            }
            else if (args == "thrust_standby_off")
            {
                controlThrusters = true;
            }
            else if (args == "thrust_standby_onoff")
            {
                controlThrusters = !controlThrusters;
            }
            else if (args == "reset_rotor_settings")
            {
                ResetRotorSettings();
            }
        }

        void HandleRoll(Vector3 rotationInputs)
        {
            if (rollRotors.Count == 0)
                return;

            float roll = rotationInputs.Z;

            // Roll
            if (roll != 0)
            {
                currRollTime = ROLL_RESET_WAIT;

                // Smoothen micro roll adjustments
                currRollRampUp += ROLL_RAMPUP_FACTOR;
                if (currRollRampUp >= MAX_ROLL_RAMPUP)
                    currRollRampUp = MAX_ROLL_RAMPUP;

                float rollFactor = currRollRampUp * roll;
                foreach (IMyMotorStator rotor in rollRotors)
                {
                    if (IsActiveRotor(rotor, rotationInputs))
                    {
                        SetRotorRPM(rotor, invertRollRotors, rollFactor);
                    }
                    else
                    {
                        ResetRotor(rotor);
                    }
                }
            }
            else
            {
                currRollRampUp = 0;
                currRollTime--;
                if (currRollTime <= 0)
                {
                    currRollTime = 0;
                    foreach (IMyMotorStator rotor in rollRotors)
                    {
                        if (!IsActiveRotor(rotor, rotationInputs))
                        {
                            if (resetWhenNoInput)
                                ResetRotor(rotor);
                            else
                                StopRotor(rotor);
                        }
                    }
                }
            }
        }

        void HandlePitch(Vector3 rotationInputs)
        {
            if (pitchRotors.Count == 0)
                return;

            float pitch = rotationInputs.X;
            float pitchFactor = PITCH_SPEED_MULTIPLIER * pitch;

            if (pitch != 0)
            {
                currPitchTime = PITCH_RESET_WAIT;
                foreach (IMyMotorStator rotor in pitchRotors)
                {
                    if (IsActiveRotor(rotor, rotationInputs))
                    {
                        SetRotorRPM(rotor, invertPitchRotors, pitchFactor);
                    }
                    else
                    {
                        ResetRotor(rotor);
                    }
                }
            }
            else
            {
                currPitchTime--;
                if (currPitchTime <= 0)
                {
                    currPitchTime = 0;
                    foreach (IMyMotorStator rotor in pitchRotors)
                    {
                        if (!IsActiveRotor(rotor, rotationInputs))
                        {
                            if (resetWhenNoInput)
                                ResetRotor(rotor);
                            else
                                StopRotor(rotor);
                        }
                    }
                }
            }
        }

        void HandleYaw(Vector3 rotationInputs)
        {
            if (yawRotors.Count == 0)
                return;

            float yaw = rotationInputs.Y;
            float yawFactor = YAW_SPEED_MULTIPLIER * yaw;

            // Yaw
            if (yaw != 0)
            {
                currYawTime = YAW_RESET_WAIT;
                foreach (IMyMotorStator rotor in yawRotors)
                {
                    if (IsActiveRotor(rotor, rotationInputs))
                    {
                        SetRotorRPM(rotor, invertYawRotors, yawFactor);
                    }
                    else
                    {
                        ResetRotor(rotor);
                    }
                }
            }
            else
            {
                currYawTime--;
                if (currYawTime <= 0)
                {
                    currYawTime = 0;
                    foreach (IMyMotorStator rotor in yawRotors)
                    {
                        if (!IsActiveRotor(rotor, rotationInputs))
                        {
                            if (resetWhenNoInput)
                                ResetRotor(rotor);
                            else
                                StopRotor(rotor);
                        }
                    }
                }
            }
        }

        void HandleAirbrakeRotors(Vector3 wasdInputs)
        {
            if (airbrakeRotors.Count == 0)
                return;

            float space = wasdInputs.Y;
            float back = wasdInputs.Z;
            float keyFactor = space;
            if (useReverseForBrakes)
            {
                keyFactor = back;
            }
            float airbrakeFactor = AIRBRAKE_SPEED_MULTIPLIER * keyFactor;

            if (space == 1 || (back == 1 && useReverseForBrakes))
            {
                currAirbrakeTime = AIRBRAKE_RESET_WAIT;
                foreach (IMyMotorStator rotor in airbrakeRotors)
                {
                    SetRotorRPM(rotor, invertAirbrakeRotors, airbrakeFactor);
                }
            }
            else
            {
                currAirbrakeTime--;
                if(currAirbrakeTime <= 0)
                {
                    currAirbrakeTime = 0;
                    foreach (IMyMotorStator rotor in airbrakeRotors)
                    {
                        ResetRotor(rotor);
                    }
                }
            }
        }

        void HandleAirbrakes(Vector3 wasdInputs)
        {
            if (airbrakes.Count == 0)
                return;

            float space = wasdInputs.Y;
            float back = wasdInputs.Z;

            if (space == 1 || (back == 1 && useReverseForBrakes))
            {
                foreach (IMyAdvancedDoor airbrake in airbrakes)
                {
                    if (airbrake.Status != DoorStatus.Opening
                    || airbrake.Status != DoorStatus.Open)
                    {
                        airbrake.OpenDoor();
                    }
                }
            }
            else
            {
                foreach (IMyAdvancedDoor airbrake in airbrakes)
                {
                    if (airbrake.Status != DoorStatus.Closing
                    || airbrake.Status != DoorStatus.Closed)
                    {
                        airbrake.CloseDoor();
                    }
                }
            }
        }

        void HandleThrusters(Vector3 wasdInputs)
        {
            if (thrusters.Count > 0 && controlThrusters)
            {
                if (wasdInputs.Z == -1) // W
                {
                    // Gradaually increase thrust over 1 sec
                    currThrusterRampUp += THRUST_RAMPUP_FACTOR;
                    if (currThrusterRampUp >= MAX_THRUSTER_RAMPUP)
                        currThrusterRampUp = MAX_THRUSTER_RAMPUP;

                    // Really basic thrust calculation based on speed difference
                    double shipMaxSpeed = GetShipMaxSpeed();
                    double shipForwardSpeed = GetShipDirectionalSpeed(controller, Base6Directions.Direction.Forward);
                    double speedDiff = MathHelper.Clamp(shipMaxSpeed - shipForwardSpeed, 0, shipMaxSpeed);
                    float thrustPercent = speedDiff <= 1 ? MIN_THRUST : currThrusterRampUp; // Gives props a nice idle spin
                    SetThrusterOverrides(thrusters, thrustPercent);
                }
                else
                {
                    currThrusterRampUp = 0;
                    SetThrusterOverrides(thrusters, MIN_THRUST);
                }
            }
        }

        void SetRotorRPM(IMyMotorStator rotor, List<IMyMotorStator> invertRotors, float rpmFactor)
        {
            RotorSettings rotorSettings = allRotors[rotor];
            float finalRPM = rotorSettings.controlSpeed * rpmFactor;
            finalRPM = invertRotors.Contains(rotor) ? (finalRPM * -1) : finalRPM;
            rotor.TargetVelocityRPM = finalRPM;
        }

        void ResetRotor(IMyMotorStator rotor)
        {
            try
            {
                RotorSettings stats = allRotors[rotor];
                double forwardSpeed = GetShipDirectionalSpeed(controller, Base6Directions.Direction.Forward);
                float resetAngle = forwardSpeed >= stats.maxVelocity ? stats.resetAngleAtMaxVelocity : stats.resetAngle;

                float angleDiffRad = MathHelper.ToRadians(resetAngle) - rotor.Angle;
                angleDiffRad %= MathHelper.TwoPi;

                if (angleDiffRad > MathHelper.Pi)
                {
                    angleDiffRad = -MathHelper.TwoPi + angleDiffRad;
                }
                else if (angleDiffRad < -MathHelper.Pi)
                {
                    angleDiffRad = MathHelper.TwoPi + angleDiffRad;
                }

                rotor.TargetVelocityRPM = stats.resetSpeed * angleDiffRad * 2.0f;

                // if (angleDiffRad >= -0.001f && angleDiffRad <= 0.001f)
                if (angleDiffRad == 0)
                {
                    if (rotor.TargetVelocityRPM != 0)
                    {
                        rotor.TargetVelocityRPM = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                logs.AppendLine(ex.Message + " " + ex.StackTrace);
            }
        }

        void StopRotor(IMyMotorStator rotor)
        {
            rotor.TargetVelocityRPM = 0;
        }

        void SetThrusterOverrides(List<IMyThrust> thrusters, float overridePercent)
        {
            foreach (IMyThrust thruster in thrusters)
            {
                thruster.ThrustOverridePercentage = overridePercent;
            }
        }

        bool IsActiveRotor(IMyMotorStator rotor, Vector3 rotationInputs)
        {
            bool result = true;

            float roll = rotationInputs.Z > 0 ? 1 : (rotationInputs.Z < 0 ? -1 : 0);
            float pitch = rotationInputs.X > 0 ? 1 : (rotationInputs.X < 0 ? -1 : 0);
            float yaw = rotationInputs.Y > 0 ? 1 : (rotationInputs.Y < 0 ? -1 : 0);

            List<float> inputValues = new List<float>();
            if (roll != 0 && rollRotors.Contains(rotor))
            {
                if (invertRollRotors.Contains(rotor))
                    roll *= -1;
                inputValues.Add(roll);
            }

            if (pitch != 0 && pitchRotors.Contains(rotor))
            {
                if (invertPitchRotors.Contains(rotor))
                    pitch *= -1;
                inputValues.Add(pitch);
            }

            if (yaw != 0 && yawRotors.Contains(rotor))
            {
                if (invertYawRotors.Contains(rotor))
                    yaw *= -1;
                inputValues.Add(yaw);
            }

            // If an input is live for the rotor, it is active
            result = (inputValues.Count > 0);

            float prevVal = 0;
            int idx = 0;
            foreach (float value in inputValues)
            {
                idx++;
                if (prevVal == 0)
                {
                    if (value != 0)
                        prevVal = value;
                }
                else
                {
                    if (prevVal != value && prevVal != 0)
                    {
                        // If there is a conflict however, it is inactive
                        result = false;
                    }
                }
            }
            return result;
        }

        bool PerformSetup()
        {
            bool result = true;

            // Clear Storages
            allRotors.Clear();
            rollRotors.Clear();
            pitchRotors.Clear();
            yawRotors.Clear();
            airbrakes.Clear();
            airbrakeRotors.Clear();
            invertRollRotors.Clear();
            invertPitchRotors.Clear();
            invertYawRotors.Clear();
            thrusters.Clear();
            controllers.Clear();

            result = LoadSettings();

            blockGroup = GridTerminalSystem.GetBlockGroupWithName(controllersGroup);
            if (blockGroup != null)
            {
                blockGroup.GetBlocksOfType(controllers);
            }
            else
            {
                GridTerminalSystem.GetBlocksOfType(controllers, b => b.IsSameConstructAs(Me));
            }

            if (controllers.Count > 0)
            {
                // Debug Window
                IMyTerminalBlock debugBlock = GridTerminalSystem.GetBlockWithName(DEBUG_TEXTPANEL);
                if (debugBlock != null)
                {
                    debugTextSufrace = (debugBlock as IMyTextSurfaceProvider).GetSurface(0);
                }

                // Roll
                blockGroup = GridTerminalSystem.GetBlockGroupWithName(rollGroup);
                if (blockGroup != null)
                {
                    blockGroup.GetBlocksOfType(rollRotors, b => b.IsSameConstructAs(Me));
                    foreach (IMyMotorStator rotor in rollRotors)
                    {
                        if (!allRotors.ContainsKey(rotor))
                            allRotors.Add(rotor, null);
                    }
                }
                // Pitch
                blockGroup = GridTerminalSystem.GetBlockGroupWithName(pitchGroup);
                if (blockGroup != null)
                {
                    blockGroup.GetBlocksOfType(pitchRotors, b => b.IsSameConstructAs(Me));
                    foreach (IMyMotorStator rotor in pitchRotors)
                    {
                        if (!allRotors.ContainsKey(rotor))
                            allRotors.Add(rotor, null);
                    }
                }
                // Yaw
                blockGroup = GridTerminalSystem.GetBlockGroupWithName(yawGroup);
                if (blockGroup != null)
                {
                    blockGroup.GetBlocksOfType(yawRotors, b => b.IsSameConstructAs(Me));
                    foreach (IMyMotorStator rotor in yawRotors)
                    {
                        if (!allRotors.ContainsKey(rotor))
                            allRotors.Add(rotor, null);
                    }
                }

                // Airbrakes
                blockGroup = GridTerminalSystem.GetBlockGroupWithName(airbrakesGroup);
                if (blockGroup != null)
                {
                    blockGroup.GetBlocksOfType(airbrakeRotors, b => b.IsSameConstructAs(Me));
                    foreach (IMyMotorStator rotor in airbrakeRotors)
                    {
                        if(!allRotors.ContainsKey(rotor))
                        {
                            allRotors.Add(rotor, null);
                        }
                    }
                }

                // Invert Airbrakes
                blockGroup = GridTerminalSystem.GetBlockGroupWithName(invertAirbrakesGroup);
                if(blockGroup != null)
                {
                    blockGroup.GetBlocksOfType(invertAirbrakeRotors, b => b.IsSameConstructAs(Me));
                }

                // Invert Roll
                blockGroup = GridTerminalSystem.GetBlockGroupWithName(invertRollGroup);
                if (blockGroup != null)
                {
                    blockGroup.GetBlocksOfType(invertRollRotors, b => b.IsSameConstructAs(Me));
                }

                // Invert Pitch
                blockGroup = GridTerminalSystem.GetBlockGroupWithName(invertPitchGroup);
                if (blockGroup != null)
                {
                    blockGroup.GetBlocksOfType(invertPitchRotors, b => b.IsSameConstructAs(Me));
                }

                // Invert Yaw
                blockGroup = GridTerminalSystem.GetBlockGroupWithName(invertYawGroup);
                if (blockGroup != null)
                {
                    blockGroup.GetBlocksOfType(invertYawRotors, b => b.IsSameConstructAs(Me));
                }

                // Aibrakes
                blockGroup = GridTerminalSystem.GetBlockGroupWithName(airbrakesGroup);
                if (blockGroup != null)
                {
                    blockGroup.GetBlocksOfType(airbrakes, b => b.IsSameConstructAs(Me)
                                                            && b.DefinitionDisplayNameText.ToLower().Contains("air brake"));
                }

                // Thrusters
                blockGroup = GridTerminalSystem.GetBlockGroupWithName(thrustersGroup);
                if (blockGroup != null)
                {
                    blockGroup.GetBlocksOfType(thrusters, b => b.IsSameConstructAs(Me));
                }

                // Update Rotor Settings
                foreach (IMyMotorStator rotor in allRotors.Keys.ToList())
                {
                    RotorSettings stats = new RotorSettings();
                    if (rotor.CustomData.Trim() == string.Empty)
                    {
                        WriteRotorSettings(rotor, stats);
                        allRotors[rotor] = stats;
                    }
                    else if (rotor.CustomData.Trim() != string.Empty)
                    {
                        result = ReadRotorSettings(rotor, out stats);
                        if (result) // successful reading
                        {
                            WriteRotorSettings(rotor, stats);
                            allRotors[rotor] = stats;
                        }
                    }
                    else
                    {
                        // Do nothing
                    }
                }
            }
            else
            {
                logs.AppendLine("No controllers found on ship.");
            }

            return result;
        }

        bool LoadSettings()
        {
            bool result = true;

            try
            {
                if (Me.CustomData.Trim() != string.Empty)
                {
                    settings.Read();
                    rollGroup = ReadOrAddSetting(nameof(rollGroup), rollGroup);
                    pitchGroup = ReadOrAddSetting(nameof(pitchGroup), pitchGroup);
                    yawGroup = ReadOrAddSetting(nameof(yawGroup), yawGroup);
                    invertAirbrakesGroup = ReadOrAddSetting(nameof(invertAirbrakesGroup), invertAirbrakesGroup);
                    invertRollGroup = ReadOrAddSetting(nameof(invertRollGroup), invertRollGroup);
                    invertPitchGroup = ReadOrAddSetting(nameof(invertPitchGroup), invertPitchGroup);
                    invertYawGroup = ReadOrAddSetting(nameof(invertYawGroup), invertYawGroup);
                    airbrakesGroup = ReadOrAddSetting(nameof(airbrakesGroup), airbrakesGroup);
                    controllersGroup = ReadOrAddSetting(nameof(controllersGroup), controllersGroup);
                    thrustersGroup = ReadOrAddSetting(nameof(thrustersGroup), thrustersGroup);

                    result = double.TryParse(ReadOrAddSetting(nameof(worldMaxSpeed),
                                                                     worldMaxSpeed.ToString()), out worldMaxSpeed) == false ? false : result;
                    result = bool.TryParse(ReadOrAddSetting(nameof(resetWhenNoInput),
                                                                   resetWhenNoInput.ToString()), out resetWhenNoInput) == false ? false : result;
                    result = bool.TryParse(ReadOrAddSetting(nameof(useReverseForBrakes),
                                                                   useReverseForBrakes.ToString()), out useReverseForBrakes) == false ? false : result;
                    result = bool.TryParse(ReadOrAddSetting(nameof(controlThrusters),
                                               controlThrusters.ToString()), out controlThrusters) == false ? false : result;
                    settings.Write();
                }
                else
                {
                    settings.Add(nameof(worldMaxSpeed), worldMaxSpeed);
                    settings.Add(nameof(rollGroup), rollGroup);
                    settings.Add(nameof(pitchGroup), pitchGroup);
                    settings.Add(nameof(yawGroup), yawGroup);
                    settings.Add(nameof(invertAirbrakesGroup), invertAirbrakesGroup);
                    settings.Add(nameof(invertRollGroup), invertRollGroup);
                    settings.Add(nameof(invertPitchGroup), invertPitchGroup);
                    settings.Add(nameof(invertYawGroup), invertYawGroup);
                    settings.Add(nameof(airbrakesGroup), airbrakesGroup);
                    settings.Add(nameof(controllersGroup), controllersGroup);
                    settings.Add(nameof(thrustersGroup), thrustersGroup);
                    settings.Add(nameof(resetWhenNoInput), resetWhenNoInput);
                    settings.Add(nameof(useReverseForBrakes), useReverseForBrakes);
                    settings.Add(nameof(controlThrusters), controlThrusters);
                    settings.Write();
                }
            }
            catch
            {
                logs.AppendLine("- Error loading settings. Please clear PB custom data and recompile.");
                result = false;
            }

            return result;
        }

        double GetShipDirectionalSpeed(IMyShipController controller, Base6Directions.Direction direction)
        {
            if(controller == null) { return 0; }
            Vector3D velocity = controller.GetShipVelocities().LinearVelocity;
            return velocity.Dot(controller.WorldMatrix.GetDirectionVector(direction));
        }

        double GetShipMaxSpeed()
        {
            double maxSpeed = worldMaxSpeed;
            try
            {
                // Try to get custom max speed using Naval Aviation Physics API
                float navalSpeed = Me.GetValueFloat("NavalMaxSpeed");
                maxSpeed = navalSpeed > -1 ? navalSpeed : worldMaxSpeed;
            }
            catch { }
            return maxSpeed;
        }

        float ToPositive(float value)
        {
            return (value < 0 ? (value * -1) : value);
        }

        IMyShipController GetController()
        {
            IMyShipController currController = null;
            foreach (IMyShipController controller in controllers)
            {
                if (controller.CanControlShip && controller.IsUnderControl)
                {
                    if (currController == null)
                    {
                        currController = controller;
                    }
                    else
                    {
                        if (currController.IsMainCockpit)
                        {
                            currController = controller;
                            break;
                        }
                    }
                }
            }
            return currController;
        }

        IMyShipController GetFreeController()
        {
            IMyShipController currController = null;
            foreach (IMyShipController controller in controllers)
            {
                if (controller.CanControlShip)
                {
                    if(currController == null)
                    {
                        currController = controller;
                    }
                    else
                    {
                        if (currController.IsMainCockpit)
                        {
                            currController = controller;
                            break;
                        }
                    }
                }
            }
            return currController;
        }

        void ResetRotorSettings()
        {
            foreach (IMyMotorStator rotor in allRotors.Keys.ToList())
            {
                RotorSettings stats = new RotorSettings();
                WriteRotorSettings(rotor, stats);
            }
        }

        void WriteRotorSettings(IMyMotorStator rotor, RotorSettings stats)
        {
            statsText.Clear();
            statsText.AppendLine($"{nameof(stats.controlSpeed)}={stats.controlSpeed}");
            statsText.AppendLine($"{nameof(stats.resetSpeed)}={stats.resetSpeed}");
            statsText.AppendLine($"{nameof(stats.resetAngle)}={stats.resetAngle}");
            statsText.AppendLine($"{nameof(stats.resetAngleAtMaxVelocity)}={stats.resetAngleAtMaxVelocity}");
            statsText.AppendLine($"{nameof(stats.maxVelocity)}={stats.maxVelocity}");
            rotor.CustomData = statsText.ToString();
            statsText.Clear();
        }

        bool ReadRotorSettings(IMyMotorStator rotor, out RotorSettings stats)
        {
            bool result = true;

            stats = new RotorSettings();
            string[] lines = rotor.CustomData.Trim().Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                string[] parts = line.Trim().Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    string stat = parts[0].Trim();
                    string value = parts[1].Trim();

                    try
                    {
                        if (stat == nameof(stats.controlSpeed))
                        {
                            stats.controlSpeed = float.Parse(value);
                        }
                        else if (stat == nameof(stats.resetSpeed))
                        {
                            stats.resetSpeed = float.Parse(value);
                        }
                        else if (stat == nameof(stats.resetAngle))
                        {
                            stats.resetAngle = float.Parse(value);
                        }
                        else if (stat == nameof(stats.resetAngleAtMaxVelocity))
                        {
                            stats.resetAngleAtMaxVelocity = float.Parse(value);
                        }
                        else if (stat == nameof(stats.maxVelocity))
                        {
                            stats.maxVelocity = float.Parse(value);
                        }
                        else
                        {
                            // Do nothing
                        }
                    }
                    catch
                    {
                        logs.AppendLine($"-Invalid settings for rotor {rotor.CustomName}");
                        result = false;
                        break;
                    }
                }
                else
                {
                    logs.AppendLine($"-Invalid settings for rotor {rotor.CustomName}");
                    result = false;
                    break;
                }
            }
            return result;
        }

        string ReadOrAddSetting(string key, string addValue)
        {
            string result = "";
            if (settings.ContainsKey(key))
            {
                result = settings[key];
            }
            else
            {
                settings.Add(key, addValue);
                result = addValue;
            }
            return result;
        }

        class RotorSettings
        {
            public float controlSpeed = DEFAULT_CONTROL_SPEED;
            public float resetSpeed = DEFAULT_RESET_SPEED;
            public float resetAngle = DEFAULT_RESET_ANGLE;
            public float resetAngleAtMaxVelocity = DEFAULT_RESET_ANGLE;
            public float maxVelocity = DEFAULT_MAX_VELOCITY;
        }

        private string[] _spinners = { "--", "\\", "|", "/" };
        private int _spinnerIndex = 0;
        private int _spinTicks = 0;
        private string Spinner
        {
            get
            {
                _spinTicks++;
                if (_spinTicks >= 20)
                {
                    _spinTicks = 0;
                    _spinnerIndex++;
                    if (_spinnerIndex >= _spinners.Length)
                        _spinnerIndex = 0;
                }
                return _spinners[_spinnerIndex];
            }
        }

        private class ScriptSettings
        {
            private IMyProgrammableBlock pb = null;
            private Dictionary<string, string> dctSettings = new Dictionary<string, string>();

            public ScriptSettings(IMyProgrammableBlock pb)
            {
                this.pb = pb;
            }

            public void Add(string key, object value)
            {
                if (!dctSettings.ContainsKey(key))
                {
                    dctSettings.Add(key, value.ToString());
                }
            }

            public void Read()
            {
                string[] lines = pb.CustomData.Trim('\n').Split('\n');
                foreach (string line in lines)
                {
                    string[] parts = line.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    dctSettings[parts[0].Trim()] = parts[1].Trim();
                }
            }

            public void Write()
            {
                pb.CustomData = "";
                foreach (string key in dctSettings.Keys)
                {
                    pb.CustomData += $"{key}={dctSettings[key]}\n";
                }
                pb.CustomData = pb.CustomData.TrimEnd('\n');
            }

            public string this[string key]
            {
                get { return dctSettings[key]; }
            }

            public bool ContainsKey(string key)
            {
                return dctSettings.ContainsKey(key);
            }
        }
    }
}
