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
        #region ======= SCRIPT START =======
        #region Settings
        private class DefaultSettings
        {
            // Screen Tag
            public const string screenTag = "SFCS";
            // Airbrakes
            public const bool useReverseForAirbrakes = false;
            // Auto-Throttle
            public const double autoThrottleSpeed = 300;
            public const int autoThrottleActivationTime = 5;
            public const double autoThrottleMinThrustPercent = 15;
            public const string thrusterTag = "SFCS";
            // Auto-Cruise
            public const double cruiseYawMultiplier = 1.0;
            public const double minCruiseElevation = 100;
            public const int autoCruiseTriggerTime = 10;
            public const double autoCruiseTriggerAltitude = 200;
            public const double autoCruiseTriggerSpeed = 80;
            // Weaponcore Support
            public const bool weaponCoreSupport = true;
            // Radar Support
            public const bool radarIFSupport = true;
            public const string radarIgcTag = "IGC_IFF_MSG";
            // Virtual Hard Deck
            public const double virtualSeaLevel = 0;
            // Status Monitor - Bingo Fuel
            public const double bingoFuelPercent = 10;
            // Status Monitor - Sounds
            public const string soundBlockTag = "SFCS";
            public const string soundAirTarget = "CAP_F-16_NewContact_Air";
            public const string soundGroundTarget = "CAP_F-16_NewContact_Ground";
            public const string soundAltitude = "CAP_F-16_Altitude";
            public const string soundPullUp = "CAP_F-16_PullUp";
            public const string soundCaution = "CAP_F-16_Caution";
            public const string soundChaffFlare = "CAP_F-16_ChaffFlare";
            public const string soundMissileLaunch = "CAP_F-16_MissileLaunchWarning";
            public const string soundBingoFuel = "CAP_F-16_Bingo";
            // Display Colors
            public const string headerFgText = "SFC System";
            public static readonly Color headerBgColor = new Color(0, 150, 0, 5);
            public static readonly Color headerFgColor = new Color(0, 250, 0, 255);
            public static readonly Color statusOffBgColor = new Color(0, 150, 0, 5);
            public static readonly Color statusOffFgColor = new Color(0, 150, 0, 60);
            public static readonly Color statusOnBgColor = new Color(0, 150, 0, 30);
            public static readonly Color statusOnFgColor = new Color(0, 250, 0, 255);
            public static readonly Color alertBgColor = new Color(150, 0, 0, 50);
            public static readonly Color alertFgColor = new Color(250, 0, 0, 255);
            public static readonly Color contactBgColor = new Color(150, 150, 0, 50);
            public static readonly Color contactFgColor = new Color(250, 250, 0, 255);
            public static readonly Color clearBgColor = new Color(0, 150, 0, 10);
            public static readonly Color clearFgColor = new Color(0, 250, 0, 255);
            public static readonly Color noSupportBgColor = new Color(10, 10, 10, 20);
            public static readonly Color noSupportFgColor = new Color(100, 100, 100, 150);
        }
        
        // Text and Tags
        private const string sectionGeneral = "General";
        private const string section3rdParty = "Mod & Script Support";
        private const string sectionSounds = "Sounds";
        private const string sectionColors = "Display";
        private string screenTag = "SFCS";
        // Airbrakes
        private bool useReverseForAirbrakes;
        // Auto Throttle
        private double autoThrottleSpeed;
        private int autoThrottleActivationTime;
        public double autoThrottleMinThrustPercent;
        public string thrusterTag;
        // Cruise and Auto-Cruise
        private double minCruiseElevation;
        private double cruiseYawMultiplier;
        private int autoCruiseTriggerTime;
        private double autoCruiseTriggerAltitude;
        private double autoCruiseTriggerSpeed;
        // WC Support
        private bool weaponCoreSupport;
        private bool radarIFSupport;
        private string radarIgcTag;
        // Water Mod Support
        public double virtualSeaLevel;
        // Bingo Fuel
        private double bingoFuelPercent;
        // Sounds
        private string soundBlockTag;
        private string soundAirTarget;
        private string soundGroundTarget;
        private string soundAltitude;
        private string soundPullUp;
        private string soundCaution;
        private string soundChaffFlare;
        private string soundMissileLaunch;
        private string soundBingoFuel;
        // Display Colors
        public const string headerFgText = "SFC System";
        public Color headerBgColor;
        public Color headerFgColor;
        public Color statusOffBgColor;
        public Color statusOffFgColor;
        public Color statusOnBgColor;
        public Color statusOnFgColor;
        public Color noSupportBgColor;
        public Color noSupportFgColor;
        public Color alertBgColor;
        public Color alertFgColor;
        public Color contactBgColor;
        public Color contactFgColor;
        public Color clearBgColor;
        public Color clearFgColor;
        #endregion

        #region Variables

        // Autopilot and control
        private double currShipSpeed = 0;
        private double currSeaElevation = 0;
        private double currSurfaceElevation = 0;
        private IMyShipController currController;
        private Input input = new Input();
        private List<IMyGyro> gyros = new List<IMyGyro>();
        private List<IMyShipController> controllers = new List<IMyShipController>();
        private TickTimer autoCruiseTimer = new TickTimer(0, 0);
        private PIDController pitchPID;
        private PIDController rollPID;

        // Cruise and Auto-Cruise
        private bool cruiseMode = false;
        private double cruiseSpeed;
        private double cruiseElevation;
        private const float cruiseYawForce = 3.5f;

        // Airbrakes
        private List<IMyAdvancedDoor> airbrakes = new List<IMyAdvancedDoor>();

        // Thrusters
        private List<IMyThrust> thrusters = new List<IMyThrust>();
        private TickTimer autoThrottleTimer = new TickTimer(0, 0);
        private bool autoThrottle = false;

        // Status Monitor
        private WcPbApi wc = null;
        private bool showSeaElevation = false;
        private bool wcActivated = false;
        private bool isSoundReset = false;
        private bool isPlaying = false;
        private bool warnedBingo = false;
        private bool missileDetected = false;
        private bool enemyDetected = false;
        private bool lowFlightMode = false;
        private const float minAirTargetSize = 2.5f;
        private const float minAirTargetSpeed = 60 * 60;
        private TickTimer wcTimer = new TickTimer(0, 300);
        private TickTimer soundTimer = new TickTimer(120, 120);
        private TickTimer entityTimer = new TickTimer(10, 10);
        private TickTimer bingoTimer = new TickTimer(300, 300);
        private List<string> sounds = new List<string>();
        private List<IMyGasTank> fuelTanks = new List<IMyGasTank>();
        private List<IMyTerminalBlock> maws = new List<IMyTerminalBlock>();
        private List<IMySoundBlock> soundBlocks = new List<IMySoundBlock>();
        private List<long> wcEntityIds = new List<long>();
        private Dictionary<MyDetectedEntityInfo, float> wcEntities = new Dictionary<MyDetectedEntityInfo, float>();
        private Dictionary<long, MyDetectedEntityInfo> entities = new Dictionary<long, MyDetectedEntityInfo>();
        private enum MissileStatus
        {
            Safe,
            Launched,
            Critical
        }
        private enum TargetNotif
        {
            Unknown,
            AirTarget,
            GroundTarget
        }
        private enum GroundStatus
        {
            Safe,
            FastLanding,
            CriticalAltitude,
            LowAltitude
        }

        // Custom Status Monitor
        // Save sim, don't change this
        private int maxCustomMonitors = 6;
        private const string typeVelocity = "VEL";
        private const string typeFunctional = "FUNC";
        private const string typeWorking = "WRK";
        private TickTimer blinkerOffTimer = new TickTimer(0, 5);
        private TickTimer blinkerOnTimer = new TickTimer(60, 60);
        private TickTimer customTimer = new TickTimer(10, 10);
        private TickTimer customRenderTimer = new TickTimer(10, 10);
        Queue<CustomMonitor> customMonitors = new Queue<CustomMonitor>();
        Dictionary<string, bool> customMonitorFlags = new Dictionary<string, bool>();
        public class CustomMonitor
        {
            public string Type;
            public string Tag;
            public string Value;
            public List<IMyTerminalBlock> Blocks = new List<IMyTerminalBlock>();
        }

        // Custom Status Rendering
        private MyIni screenIni = new MyIni();
        private const string screenSection = "SFCS - Text Surface Config";
        private List<IMyTextSurface> screenSurfaces = new List<IMyTextSurface>();
        private Dictionary<string, MySprite> sprites = new Dictionary<string, MySprite>();
        private int updatedScreens = 0;

        // Radar I/F
        private enum RadarRelationship
        {
            Neutral = 0,
            Enemy = 1,
            Friendly = 2,
            Locked = 4,
            LargeGrid = 8,
            SmallGrid = 16,
            Missile = 32,
            Asteroid = 64
        }

        // Script
        private bool isSetup = false;
        private SimpleTimerSM setupSM;
        private SimpleTimerSM routineSM;
        private SetupSteps currSetupStep = SetupSteps.Init;
        private TickTimer setupTimer = new TickTimer(600, 600);
        private List<TickTimer> tickTimers = new List<TickTimer>();
        private List<IMyBlockGroup> blockGroups = new List<IMyBlockGroup>();
        private List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
        private enum SetupSteps
        {
            Init,
            WeaponCore,
            AutoPilot,
            Airbrakes,
            Thrusters,
            FuelMonitor,
            StatusMonitor,
            CustomMonitor,
            MonitorDisplay,
            Complete,
            Error
        }
        private int runs = 0;
        private double total = 0;

        // Script Settings
        private const int SHIFT_AUTO_THROTTLE = 0;
        private const int SHIFT_CRUISE = 1;
        private const int SHIFT_LOW_FLIGHT = 2;
        private ScriptSettings settings = new ScriptSettings("SFCS");
        private ScriptSettingsBinary storage = new ScriptSettingsBinary();

        // Logs
        private const string VERSION = "2.0.0";
        private bool debugMode = true;
        private StringBuilder logs = new StringBuilder();
        private StringBuilder setupLogs = new StringBuilder();

        #endregion

        #region Main

        public Program()
        {
            try
            {
                // Load AutoFire setting
                if (!string.IsNullOrEmpty(Storage))
                {
                    if (storage.TryLoad(Storage))
                    {
                        autoThrottle = storage.Get(SHIFT_AUTO_THROTTLE);
                        cruiseMode = storage.Get(SHIFT_CRUISE);
                        lowFlightMode = storage.Get(SHIFT_LOW_FLIGHT);
                    }
                    else
                    {
                        Storage = "0";
                        storage.Set(false, SHIFT_AUTO_THROTTLE);
                        storage.Set(false, SHIFT_CRUISE);
                        storage.Set(false, SHIFT_LOW_FLIGHT);
                    }
                }

                // PID Controllers
                pitchPID = new PIDController(2, 0, 5);
                rollPID = new PIDController(2, 0, 5);

                // Register timers
                RegisterTimer(setupTimer, true);
                RegisterTimer(wcTimer, false);
                RegisterTimer(entityTimer, true);

                RegisterTimer(autoCruiseTimer, false);
                RegisterTimer(autoThrottleTimer, false);
                RegisterTimer(soundTimer, false);
                RegisterTimer(bingoTimer, true);
                RegisterTimer(customTimer, true);
                RegisterTimer(customRenderTimer, true);
                RegisterTimer(blinkerOffTimer, false);
                RegisterTimer(blinkerOnTimer, true);

                // Setup state machines
                setupSM = new SimpleTimerSM(this, Setup(), true);
                routineSM = new SimpleTimerSM(this, RunRoutines(), true);
                Runtime.UpdateFrequency = UpdateFrequency.Update1;
            }
            catch
            {
                Storage = "0";
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            try
            {
                logs.Clear();
                logs.AppendLine($"SFC Script v{VERSION} by Khjin");
                logs.AppendLine($"Average Runtime: {GetAverageRuntime():0.0000}");
                if (weaponCoreSupport && !wcActivated)
                { logs.AppendLine($"Waiting for WeaponCore...{wcTimer.GetRemainingSecs()} secs"); }
                else
                { logs.AppendLine($"Blocks refresh in {setupTimer.GetRemainingSecs()} secs"); }
                logs.AppendLine("-------");

                if (debugMode)
                {
                    logs.AppendLine($"Controllers: {controllers.Count}");
                    logs.AppendLine($"Airbrakes: {airbrakes.Count}");
                    logs.AppendLine($"Thrusters: {thrusters.Count}");
                    logs.AppendLine($"Fuel Tanks: {fuelTanks.Count}");
                    logs.AppendLine($"Sound Blocks: {soundBlocks.Count}");
                    logs.AppendLine($"Screens: {screenSurfaces.Count}");
                    logs.AppendLine($"Custom Status: {customMonitorFlags.Count}/6");

                    if (weaponCoreSupport)
                    {
                        logs.AppendLine($"MAWS: {maws.Count}");
                    }
                }

                if (!isSetup || setupTimer.Elapsed())
                {
                    setupSM.Run();
                    logs.AppendLine(setupLogs.ToString());
                }
                if (isSetup)
                {
                    if (argument != string.Empty)
                    { HandleCommands(argument); }

                    // Get the main controller and its inputs
                    currController = GetController();
                    input.Update(currController);

                    // Run Real-time Routines
                    RunAutoCruiseRoutines();

                    // Run yield-able routines
                    routineSM.Run();
                }

                UpdateTickTimers();
                Echo(logs.ToString());
            }
            catch
            {
                Storage = "0";
            }
        }

        private void HandleCommands(string args)
        {
            string command = args.ToLowerInvariant();

            // Cruise Mode
            if (command == "toggle_cruise")
            { 
                cruiseMode = !cruiseMode; 
                storage.Set(cruiseMode, SHIFT_CRUISE);

                if (cruiseMode) {
                    autoThrottle = true;
                    storage.Set(autoThrottle, SHIFT_AUTO_THROTTLE);
                    SaveCurrShipStatus(); 
                }
                else {

                    autoThrottle = false;
                    storage.Set(autoThrottle, SHIFT_AUTO_THROTTLE);

                    cruiseElevation = 0;
                    cruiseSpeed = 0;
                    ClearGyroOverrides(); 
                }
            }
            else if (command == "cruise_on")
            { 
                cruiseMode = true;
                autoThrottle = true;
                storage.Set(cruiseMode, SHIFT_CRUISE);
                storage.Set(autoThrottle, SHIFT_AUTO_THROTTLE);
                SaveCurrShipStatus(); 
            }
            else if (command == "cruise_off")
            { 
                cruiseMode = false;
                storage.Set(cruiseMode, SHIFT_CRUISE);

                autoThrottle = false;
                storage.Set(autoThrottle, SHIFT_AUTO_THROTTLE);

                cruiseElevation = 0;
                cruiseSpeed = 0;

                ClearGyroOverrides();
            }

            // Low Flight
            else if (command == "toggle_low_flight")
            { lowFlightMode = !lowFlightMode; storage.Set(lowFlightMode, SHIFT_LOW_FLIGHT); }
            else if (command == "low_flight_on")
            { lowFlightMode = true; storage.Set(lowFlightMode, SHIFT_LOW_FLIGHT); }
            else if (command == "low_flight_off")
            { lowFlightMode = false; storage.Set(lowFlightMode, SHIFT_LOW_FLIGHT); }
            
            // Sea Elevation
            else if (command == "toggle_se")
            { showSeaElevation = !showSeaElevation; }
        }

        public void Save()
        {
            Storage = storage.Export();
        }

        private IEnumerable<double> RunRoutines()
        {
            RunAirbrakeRoutines();
            yield return 0.01;
            RunThrusterRoutines();
            yield return 0.01;

            // Check standard monitors
            if (currController.IsUnderControl)
            {
                string soundToPlay = string.Empty;

                // Check status then yield before trying the next so we don't hold sim
                MissileStatus missileStatus = CheckMissileStatus();   yield return 0.01;
                GroundStatus groundStatus = CheckAltitudeStatus();    yield return 0.01;
                TargetNotif entityStatus = CheckEntities();           yield return 0.01;
                bool isBingoFuel = (!warnedBingo && IsBingoFuel());   yield return 0.01;

                if (groundStatus == GroundStatus.CriticalAltitude)  { soundToPlay = soundPullUp; }
                else if (missileStatus == MissileStatus.Critical)   { soundToPlay = soundChaffFlare; }
                else if (missileStatus == MissileStatus.Launched)   { soundToPlay = soundMissileLaunch; }
                else if (groundStatus == GroundStatus.FastLanding)  { soundToPlay = soundCaution; }
                else if (groundStatus == GroundStatus.LowAltitude)  { soundToPlay = soundAltitude; }
                else if (entityStatus == TargetNotif.AirTarget)     { soundToPlay = soundAirTarget; }
                else if (entityStatus == TargetNotif.GroundTarget)  { soundToPlay = soundGroundTarget; }
                else if (isBingoFuel)                               { soundToPlay = soundBingoFuel; warnedBingo = true; }
                else { /* DO NOTHING */ }

                PlayNotifSound(soundToPlay);
                yield return 0.01;
            }
            
            RunCustomMonitorRoutines();
            yield return 0.01;

            // Render standard and custom monitors
            RunMonitorRendering();
            yield return 0.01;
        }

        private void UpdateTickTimers()
        {
            foreach (TickTimer tickTimer in tickTimers)
            { if (tickTimer.CanUpdate) { tickTimer.Update(); } }
        }

        private double GetAverageRuntime()
        {
            if(runs < 60)
            { total += Runtime.LastRunTimeMs; ++runs; return(total / runs); }
            else
            { total = 0; runs = 0; return Runtime.LastRunTimeMs; }
        }

        #endregion

        #region Setup

        private IEnumerable<double> Setup()
        {
            switch(currSetupStep)
            {
                case SetupSteps.Init: SetupInit(); break;
                case SetupSteps.WeaponCore: SetupWeaponCore(); break;
                case SetupSteps.AutoPilot: SetupAutopilot(); break;
                case SetupSteps.Airbrakes: SetupAirbrakes(); break;
                case SetupSteps.Thrusters: SetupThrusters(); break;
                case SetupSteps.StatusMonitor: SetupStatusMonitor(); break;
                case SetupSteps.CustomMonitor: SetupCustomMonitor(); break;
                case SetupSteps.MonitorDisplay: SetupMonitorDisplay(); break;
                case SetupSteps.Complete: SetupComplete(); break;
                case SetupSteps.Error: SetupError(); break;
                default: SetupError(); break;
            }

            if(!isSetup)
            { yield return 0.3; }
            else
            { yield return 0.1; }
        }

        private void SetupInit()
        {
            RefreshSettings();

            setupTimer.CanUpdate = false;
            currSetupStep = SetupSteps.WeaponCore;
        }

        private void SetupWeaponCore()
        {
            if (currSetupStep != SetupSteps.WeaponCore) { return; }

            if (weaponCoreSupport)
            {
                if (wc == null)
                {
                    try
                    {
                        wc = new WcPbApi();
                        if (!wc.Activate(Me))
                        { wc = null; }
                    }
                    catch
                    { wc = null; }
                }
                else
                {
                    if (!wcActivated)
                    {
                        wcTimer.CanUpdate = true;
                        if (wcTimer.Elapsed())
                        {
                            wcActivated = true;
                            wcTimer.Reset(canUpdate: false);
                        }
                    }
                    else
                    // WeaponCore is Activated
                    {
                        // MAWS
                        GridTerminalSystem.GetBlocksOfType(maws,
                            b => b.IsSameConstructAs(Me)
                            && (b.BlockDefinition.SubtypeId.ToLowerInvariant().Contains("maws")
                            || b.BlockDefinition.SubtypeId.ToLowerInvariant().Contains("missileapproach")));

                        currSetupStep = SetupSteps.AutoPilot;
                    }
                }
            }
            else
            { currSetupStep = SetupSteps.AutoPilot; }
        }

        private void SetupAutopilot()
        {
            if (currSetupStep != SetupSteps.AutoPilot) { return; }

            setupLogs.Clear();

            // Grab the controllers
            GridTerminalSystem.GetBlocksOfType(controllers,
                b => b.IsSameConstructAs(Me)
                && b.CanControlShip);

            // Grab the gyros
            GridTerminalSystem.GetBlocksOfType(gyros, b => b.IsSameConstructAs(Me));

            if (controllers.Count > 0)
            {
                currController = GetController();
                currSetupStep = SetupSteps.Airbrakes;
            }
            else
            {
                setupLogs.Append("No ship controllers found.");
                if (isSetup) { isSetup = false; }
                currSetupStep = SetupSteps.Error;
            }
        }

        private void SetupAirbrakes()
        {
            if (currSetupStep != SetupSteps.Airbrakes) { return; }

            GridTerminalSystem.GetBlocksOfType(airbrakes,
                b => b.IsSameConstructAs(Me)
                && b.BlockDefinition.SubtypeId.ToLowerInvariant().Contains("brake"));
            currSetupStep = SetupSteps.Thrusters;
        }

        private void SetupThrusters()
        {
            if (currSetupStep != SetupSteps.Thrusters) { return; }

            // Get forward facing thrusters
            GridTerminalSystem.GetBlocksOfType(thrusters,
                b => b.IsSameConstructAs(Me) && 
                (  (b.CubeGrid == Me.CubeGrid && b.WorldMatrix.Forward == currController.WorldMatrix.Backward)
                || (b.CubeGrid != Me.CubeGrid && b.CustomName.Contains(thrusterTag))
                ));
            currSetupStep = SetupSteps.StatusMonitor;
        }

        private void SetupStatusMonitor()
        {
            if (currSetupStep != SetupSteps.StatusMonitor) { return; }

            // Fuel Tank
            GridTerminalSystem.GetBlocksOfType(fuelTanks,
                b => b.IsSameConstructAs(Me)
                && !b.BlockDefinition.SubtypeId.ToLowerInvariant().Contains("oxy"));

            // Sound Blocks
            GridTerminalSystem.GetBlocksOfType(soundBlocks,
                b => b.IsSameConstructAs(Me)
                && b.CustomName.Contains(soundBlockTag));

            if (soundBlocks.Count > 0)
            { soundBlocks[0].GetSounds(sounds); }

            currSetupStep = SetupSteps.CustomMonitor;
        }

        private void SetupCustomMonitor()
        {
            if (currSetupStep != SetupSteps.CustomMonitor) { return; }

            blockGroups.Clear();
            customMonitors.Clear();
            customMonitorFlags.Clear();

            // Get custom monitors
            GridTerminalSystem.GetBlockGroups(blockGroups, g => g.Name.StartsWith(screenTag));
            foreach(IMyBlockGroup group in blockGroups)
            {
                string[] parts = group.Name.Split(new char[] {'_'}, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3 || !parts[2].Contains(":")) { continue; }

                // Monitor details
                string[] pair = parts[2].Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                CustomMonitor monitor = new CustomMonitor();
                monitor.Type = parts[1].Trim();
                monitor.Tag = pair[0].Trim();
                monitor.Value = pair[1].Trim();
                group.GetBlocks(monitor.Blocks, b=> b.IsSameConstructAs(Me));

                if (!customMonitorFlags.ContainsKey(monitor.Tag))
                {
                    if (customMonitorFlags.Count < maxCustomMonitors)
                    {
                        customMonitors.Enqueue(monitor);
                        customMonitorFlags.Add(monitor.Tag, false);
                    }
                    else
                    {
                        setupLogs.AppendLine($"Exceeded maximum number of monitors ({maxCustomMonitors})");
                        currSetupStep = SetupSteps.Error; return;
                    }
                }
                else
                {
                    setupLogs.AppendLine($"Duplicate monitor tag {monitor.Tag}");
                    currSetupStep = SetupSteps.Error; return;
                }
            }

            currSetupStep = SetupSteps.MonitorDisplay;
        }

        private void SetupMonitorDisplay()
        {
            if (currSetupStep != SetupSteps.MonitorDisplay) { return; }

            GridTerminalSystem.GetBlocksOfType(blocks,
                b => b.IsSameConstructAs(Me)
                &&   b.CustomName.Contains(screenTag));

            if(blocks.Count > 0)
            {
                screenSurfaces.Clear();
                foreach(IMyTerminalBlock block in blocks)
                {
                    if(block is IMyTextSurfaceProvider)
                    {
                        IMyTextSurfaceProvider displayBlock = block as IMyTextSurfaceProvider;
                        if (displayBlock.SurfaceCount == 0) { continue; }

                        if (block.CustomData == string.Empty)
                        {
                            screenIni.Clear();
                            screenIni.AddSection(screenSection);
                            for (int i = 0; i < displayBlock.SurfaceCount; i++)
                            { screenIni.Set(screenSection, $"Show on screen {i}", false); }
                            block.CustomData = screenIni.ToString();
                        }
                        else
                        {
                            if (screenIni.TryParse(block.CustomData))
                            {
                                if (screenIni.ContainsSection(screenSection))
                                {
                                    for (int i = 0; i < displayBlock.SurfaceCount; i++)
                                    {
                                        MyIniValue value = screenIni.Get(screenSection, $"Show on screen {i}");
                                        if (!value.IsEmpty && value.ToBoolean() == true)
                                        {
                                            var screen = displayBlock.GetSurface(i);
                                            screen.ContentType = ContentType.SCRIPT;
                                            screen.Script = string.Empty;
                                            screenSurfaces.Add(screen);
                                        }
                                    }
                                }
                                else
                                {
                                    screenIni.AddSection(screenSection);
                                    for (int i = 0; i < displayBlock.SurfaceCount; i++)
                                    { screenIni.Set(screenSection, $"Show on screen {i}", false); }
                                    block.CustomData = screenIni.ToString();
                                }
                            }
                        }
                    }
                }
            }

            currSetupStep = SetupSteps.Complete;
        }

        private void SetupComplete()
        {
            if (currSetupStep != SetupSteps.Complete) { return; }

            isSetup = true;
            setupTimer.Reset();
            currSetupStep = SetupSteps.Init;
        }

        private void SetupError()
        {
            if (currSetupStep != SetupSteps.Error) { return; }

            isSetup = false;
            setupTimer.Reset();
            currSetupStep = SetupSteps.Init;
        }

        private void RefreshSettings()
        {
            if (Me.CustomData != null && Me.CustomData != string.Empty)
            {
                settings.TryLoad(Me.CustomData);
            }

            // General Settings
            screenTag = settings.Get("Screen Tag", sectionGeneral, DefaultSettings.screenTag);
            useReverseForAirbrakes = settings.Get("User S for airbrakes", sectionGeneral, DefaultSettings.useReverseForAirbrakes);
            autoThrottleSpeed = settings.Get("Auto-throttle speed (m/s)", sectionGeneral, DefaultSettings.autoThrottleSpeed);
            autoThrottleActivationTime = settings.Get("Auto-throttle activation time (s)", sectionGeneral, DefaultSettings.autoThrottleActivationTime);
            autoThrottleMinThrustPercent = settings.Get("Auto-throttle minimum thrust percent (%)", sectionGeneral, DefaultSettings.autoThrottleMinThrustPercent);
            thrusterTag = settings.Get("Thruster Tag", sectionGeneral, DefaultSettings.thrusterTag); 
            cruiseYawMultiplier = settings.Get("Cruise yaw multiplier", sectionGeneral, DefaultSettings.cruiseYawMultiplier);
            minCruiseElevation = settings.Get("Minimum cruise elevation", sectionGeneral, DefaultSettings.minCruiseElevation);
            autoCruiseTriggerTime = settings.Get("Auto-cruise trigger time (s)", sectionGeneral, DefaultSettings.autoCruiseTriggerTime);
            autoCruiseTriggerAltitude = settings.Get("Auto-cruise trigger altitude (m)", sectionGeneral, DefaultSettings.autoCruiseTriggerAltitude);
            autoCruiseTriggerSpeed = settings.Get("Auto-cruise trigger speed (m/s)", sectionGeneral, DefaultSettings.autoCruiseTriggerSpeed);
            bingoFuelPercent = settings.Get("Bingo fuel percent (%)", sectionGeneral, DefaultSettings.bingoFuelPercent);

            // Mod and Script Support
            weaponCoreSupport = settings.Get("WeaponCore support", section3rdParty, DefaultSettings.weaponCoreSupport);
            radarIFSupport = settings.Get("Radar I/F support (Whip's Radar)", section3rdParty, DefaultSettings.radarIFSupport);
            radarIgcTag = settings.Get("Radar IGC tag (Whip's Radar)", section3rdParty, DefaultSettings.radarIgcTag);
            virtualSeaLevel = settings.Get("Virtual Sea Level (m)", section3rdParty, DefaultSettings.virtualSeaLevel);

            // Status Monitor - Sounds
            soundBlockTag = settings.Get("Sound Block Tag", sectionSounds, DefaultSettings.soundBlockTag);
            soundAirTarget = settings.Get("Sound - Air Target", sectionSounds, DefaultSettings.soundAirTarget);
            soundGroundTarget = settings.Get("Sound - Ground Target", sectionSounds, DefaultSettings.soundGroundTarget);
            soundAltitude = settings.Get("Sound - Low Altitude", sectionSounds, DefaultSettings.soundAltitude);
            soundPullUp = settings.Get("Sound - Critical Altitude", sectionSounds, DefaultSettings.soundPullUp);
            soundCaution = settings.Get("Sound - Caution", sectionSounds, DefaultSettings.soundCaution);
            soundChaffFlare = settings.Get("Sound - Countermeasures", sectionSounds, DefaultSettings.soundChaffFlare);
            soundMissileLaunch = settings.Get("Sound - Missile Launch", sectionSounds, DefaultSettings.soundMissileLaunch);
            soundBingoFuel = settings.Get("Sound - Bingo Fuel", sectionSounds, DefaultSettings.soundBingoFuel);

            // Display Colors
            headerBgColor = settings.GetColor("Header background color", sectionColors, DefaultSettings.headerBgColor);
            headerFgColor = settings.GetColor("Header text color", sectionColors, DefaultSettings.headerFgColor);
            statusOffBgColor = settings.GetColor("Status OFF background color", sectionColors, DefaultSettings.statusOffBgColor);
            statusOffFgColor = settings.GetColor("Status OFF text color", sectionColors, DefaultSettings.statusOffFgColor);
            statusOnBgColor = settings.GetColor("Status ON background color", sectionColors, DefaultSettings.statusOnBgColor);
            statusOnFgColor = settings.GetColor("Status ON text color", sectionColors, DefaultSettings.statusOnFgColor);
            alertBgColor = settings.GetColor("Enemy alert background color", sectionColors, DefaultSettings.alertBgColor);
            alertFgColor = settings.GetColor("Enemy alert text color", sectionColors, DefaultSettings.alertFgColor);
            contactBgColor = settings.GetColor("Contact alert background color", sectionColors, DefaultSettings.contactBgColor);
            contactFgColor = settings.GetColor("Contact alert text color", sectionColors, DefaultSettings.contactFgColor);
            clearBgColor = settings.GetColor("Clear background color", sectionColors, DefaultSettings.clearBgColor);
            clearFgColor = settings.GetColor("Clear text color", sectionColors, DefaultSettings.clearFgColor);
            noSupportBgColor = settings.GetColor("No support background color", sectionColors, DefaultSettings.noSupportBgColor);
            noSupportFgColor = settings.GetColor("No support text color", sectionColors, DefaultSettings.noSupportFgColor);

            // Update level and throttle timers
            autoCruiseTimer.Configure(0, autoCruiseTriggerTime * 60);
            autoThrottleTimer.Configure(0, autoThrottleActivationTime * 60);

            // Pre-calculate percentages
            autoThrottleMinThrustPercent /= 100;
            bingoFuelPercent /= 100;

            Me.CustomData = settings.Export();
        }

        #endregion

        #region Autopilot

        private void RunAutoCruiseRoutines()
        {
            currShipSpeed = currController.GetShipSpeed();
            currController.TryGetPlanetElevation(MyPlanetElevation.Surface, out currSurfaceElevation);
            currController.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out currSeaElevation);

            // Reload check for when reloading a script or the game
            SaveCurrShipStatus();

            // Check if the ship should engage Cruise Mode
            CheckAutoCruiseStatus();

            // Check if the pilot overrides cruise
            CheckPilotOverride();

            // Do cruise operations
            if(cruiseMode)
            {
                Cruise();
            }
        }

        private void SaveCurrShipStatus()
        {
            if(cruiseMode)
            {
                if(cruiseElevation == 0 || cruiseSpeed == 0)
                {
                    cruiseElevation = currSeaElevation;
                    cruiseSpeed = currShipSpeed;
                }
            }
        }

        private void CheckPilotOverride()
        {
            if (cruiseMode && currController.IsUnderControl)
            {
                if (input.Roll != 0 || input.Pitch != 0 || input.Yaw != 0)
                {
                    cruiseMode = false;
                    storage.Set(cruiseMode, SHIFT_CRUISE);
                    autoCruiseTimer.Reset(canUpdate: false);

                    autoThrottle = false;
                    storage.Set(autoThrottle, SHIFT_AUTO_THROTTLE);

                    cruiseElevation = 0;
                    cruiseSpeed = 0;
                    ClearGyroOverrides();
                }
            }
        }

        private void CheckAutoCruiseStatus()
        {
            if(!cruiseMode)
            {
                if(!currController.IsUnderControl
                && currShipSpeed >= autoCruiseTriggerSpeed
                && currSeaElevation >= autoCruiseTriggerAltitude)
                {
                    autoCruiseTimer.CanUpdate = true;
                    if (autoCruiseTimer.Elapsed())
                    {
                        cruiseMode = true;
                        storage.Set(cruiseMode, SHIFT_CRUISE);
                        autoCruiseTimer.Reset(canUpdate: false);

                        // Take note of the current state
                        cruiseSpeed = currShipSpeed;
                        cruiseElevation = currSeaElevation;
                    }
                }
                else
                {
                    cruiseMode = false;
                    storage.Set(cruiseMode, SHIFT_CRUISE);
                    autoCruiseTimer.Reset(canUpdate: false);
                    ClearGyroOverrides();
                }
            }
        }

        private void Cruise()
        {
            // Get the ship's vectors
            Vector3D currentDownVect = currController.WorldMatrix.Down;
            Vector3D currentForwardVect = currController.WorldMatrix.Forward;
            Vector3D currentLeftVect = currController.WorldMatrix.Left;
            Vector3D gravityVec = currController.GetNaturalGravity();

            // Calculate yaw output based on user input
            double yawOutput = input.LeftRight * cruiseYawForce * cruiseYawMultiplier;

            double targetElevation = cruiseElevation;
            double currElevation = currSeaElevation;
            if (currSurfaceElevation < minCruiseElevation)
            {
                targetElevation = minCruiseElevation;
                currElevation = currSurfaceElevation;
            }

            double elevationDiff = targetElevation - currElevation;

            // Calculate elevation adjustment based on elevation difference
            double pitchError = Math.Acos(MathHelper.Clamp(gravityVec.Dot(currentForwardVect) / 
                                                      gravityVec.Length(), -1, 1)) - Math.PI / 2;

            // Add artificial error to adjust elevation
            double elevationAdj = 0;
            if(Math.Abs(elevationDiff) > 0.1 && MathHelper.ToDegrees(pitchError) < 30)
            {
                double errorMultiplier = Math.Min(Math.Abs(elevationDiff), 450);
                elevationAdj = errorMultiplier * 0.1 * GetSign(elevationDiff, true);
                elevationAdj = MathHelper.ToRadians(elevationAdj);
            }
            double pitchOutput = pitchPID.Update(pitchError + elevationAdj);

            // Calculate the roll error (only in gravity)
            Vector3D planetRelativeLeftVec = currentForwardVect.Cross(gravityVec);
            double rollError = VectorAngleBetween(currentLeftVect, planetRelativeLeftVec);
            rollError *= VectorCompareDirection(VectorProjection(currentLeftVect, gravityVec), gravityVec);
            double rollOutput = rollPID.Update(rollError);

            // Apply the control outputs to the gyroscope
            ApplyGyroOverride(pitchOutput, yawOutput, -rollOutput, gyros, currController);
        }

        #endregion

        #region Airbrakes

        private void RunAirbrakeRoutines()
        {
            if (currController.IsUnderControl)
            {
                if ((!useReverseForAirbrakes && input.UpDown > 0)
                || (useReverseForAirbrakes && input.ForwardBack > 0))
                { SetAirbrakeStatus(true); }
                else
                { SetAirbrakeStatus(false); }
            }
        }

        private void SetAirbrakeStatus(bool open)
        {
            foreach (IMyAdvancedDoor airbrake in airbrakes)
            {
                if (open && airbrake.Status != DoorStatus.Opening && airbrake.Status != DoorStatus.Open)
                { airbrake.OpenDoor(); }
                else if (!open && airbrake.Status != DoorStatus.Closing && airbrake.Status != DoorStatus.Closed)
                { airbrake.CloseDoor(); }
            }
        }

        #endregion

        #region Thrusters

        private void RunThrusterRoutines()
        {
            if (currController.IsUnderControl)
            {
                // Activate Auto-throttle by pressing W
                if(input.ForwardBack < 0)
                {
                    if (!autoThrottle && currShipSpeed > 60)
                    {
                        autoThrottleTimer.CanUpdate = true;
                        if (autoThrottleTimer.Elapsed())
                        {
                            autoThrottle = true;
                            storage.Set(autoThrottle, SHIFT_AUTO_THROTTLE);
                            autoThrottleTimer.Reset(canUpdate: false);
                        }
                    }

                    // Subgrid Thruster Support
                    SetThrustersOverridePercent(1, true);
                }
                else
                {
                    if(!autoThrottle)
                    {
                        if(autoThrottleTimer.CanUpdate)
                        {
                            autoThrottle = false;
                            storage.Set(autoThrottle, SHIFT_AUTO_THROTTLE);
                            autoThrottleTimer.Reset(canUpdate: false);
                        }

                        // Subgrid Thruster Support
                        SetThrustersOverridePercent(0, true);
                    }
                }

                if(autoThrottle)
                {
                    // Pressed S or Space while on auto-throttle
                    if (input.ForwardBack > 0
                    || autoThrottle && input.UpDown > 0)
                    {
                        autoThrottle = false;
                        storage.Set(autoThrottle, SHIFT_AUTO_THROTTLE);
                        SetThrustersOverridePercent(0);
                        autoThrottleTimer.Reset(canUpdate: false);
                    }
                }
            }
            else
            {
                if(!cruiseMode)
                {
                    autoThrottle = false;
                    storage.Set(autoThrottle, SHIFT_AUTO_THROTTLE);
                    SetThrustersOverridePercent(0);
                    autoThrottleTimer.Reset(canUpdate: false);
                }
            }

            if (autoThrottle && input.ForwardBack == 0)
            {
                if (cruiseMode)
                {
                    // Softer adjustment on cruise mode
                    double speedDiff = Math.Max(cruiseSpeed - currShipSpeed, 0);
                    double overrideVal = Math.Max(speedDiff / cruiseSpeed, (autoThrottleMinThrustPercent / 100));
                    SetThrustersOverridePercent((float)overrideVal);
                }
                else
                {
                    // Harder adjustment on non-cruise for more power
                    double speedDiff = Math.Max(autoThrottleSpeed - currShipSpeed, 0);
                    double overrideVal = speedDiff > 1 ? 1.0f : 0.5f;
                    SetThrustersOverridePercent((float)overrideVal);
                }
            }
        }

        #endregion

        #region Status Monitors

        private void PlayNotifSound(string soundToPlay)
        {
            if (soundToPlay != string.Empty || isPlaying)
            {
                if (!isPlaying)
                {
                    PlaySound(soundToPlay);
                    isPlaying = true;
                    isSoundReset = false;
                    soundTimer.Reset();
                }
                else
                {
                    if (soundTimer.Elapsed())
                    {
                        isPlaying = false;
                    }
                }
            }
            else
            {
                if (!isSoundReset)
                {
                    StopSound();
                    soundTimer.Reset(true, false);
                    isSoundReset = true;
                }
            }
        }

        private MissileStatus CheckMissileStatus()
        {
            if (weaponCoreSupport)
            {
                // Check if ship is being locked
                var lockedStatus = wc.GetProjectilesLockedOn(Me.CubeGrid.EntityId);
                if (lockedStatus.Item2 > 0 && lockedStatus.Item3 > 0)
                { return MissileStatus.Launched; }
                else if (lockedStatus.Item2 > 0 && lockedStatus.Item3 > 90)
                { return MissileStatus.Critical; }
                else
                { return MissileStatus.Safe; }
            }
            return MissileStatus.Safe ;
        }

        private GroundStatus CheckAltitudeStatus()
        {
            // Get ship linear velocity and derive the vector aligned with gravity
            Vector3D shipVelocity = currController.GetShipVelocities().LinearVelocity;
            Vector3D gravityDirection = Vector3D.Normalize(currController.GetNaturalGravity());
            double downwardVelocity = Math.Max(0, Vector3D.Dot(shipVelocity, gravityDirection));
            
            // Calculate the time (secs) before ship hits the ground or the virtual hard deck (When Set)
            double timeToImpact = currSurfaceElevation / downwardVelocity; ;

            // Check possible collision with hard deck (if set)
            if(virtualSeaLevel != 0)
            {
                double virtualSeaElevation = currSeaElevation - virtualSeaLevel;
                double virtualTimeToImpact = virtualSeaElevation / downwardVelocity;
                timeToImpact = virtualTimeToImpact < timeToImpact ? virtualTimeToImpact : timeToImpact;
            }

            if (!lowFlightMode)
            {
                if (timeToImpact < 5)
                { return GroundStatus.CriticalAltitude; }
                else if (timeToImpact < 10)
                { return GroundStatus.LowAltitude; }
                else
                { return GroundStatus.Safe; }
            }
            else
            { 
                if(currSurfaceElevation < 100 && downwardVelocity > 10)
                { return GroundStatus.FastLanding; }
                else
                { return GroundStatus.Safe; }
            }
        }

        private TargetNotif CheckEntities()
        {
            TargetNotif notif = TargetNotif.Unknown;
            if (weaponCoreSupport)
            {
                if (wc.HasGridAi(Me.EntityId) && entityTimer.Elapsed())
                {
                    // Note if enemies were found
                    enemyDetected = false;

                    // Refresh Entities
                    wcEntities.Clear();
                    wcEntityIds.Clear();
                    wc.GetSortedThreats(Me, wcEntities);

                    foreach(MyDetectedEntityInfo info in wcEntities.Keys)
                    {
                        wcEntityIds.Add(info.EntityId);
                        double size = info.BoundingBox.Size.LengthSquared();

                        if(!entities.ContainsKey(info.EntityId) && size >= minAirTargetSize)
                        {
                            entities.Add(info.EntityId, info);
                            notif = IdentifyTargetType(info);
                        }

                        // Radar I/F Support
                        if (radarIFSupport && weaponCoreSupport)
                        {
                            var entityTuple = ToTuple(info);
                            IGC.SendBroadcastMessage(radarIgcTag, entityTuple, TransmissionDistance.CurrentConstruct);
                        }

                        if(info.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies)
                        { enemyDetected = true; }
                    }

                    // Remove out of range entities
                    var entityIds = entities.Keys.ToArray();
                    for(int i = entityIds.Length - 1; i >= 0; i--)
                    {
                        if(!wcEntityIds.Contains(entityIds[i]))
                        {
                            entities.Remove(entityIds[i]);
                        }
                    }
                    entityTimer.Reset();
                }
            }
            return notif;
        }

        private TargetNotif IdentifyTargetType(MyDetectedEntityInfo info)
        {
            TargetNotif notif = TargetNotif.Unknown;

            // Buildings and ships
            if(info.Type == MyDetectedEntityType.LargeGrid
            && info.Velocity.LengthSquared() < 60)
            { 
                notif = TargetNotif.GroundTarget; 
            }
            else if(info.Type == MyDetectedEntityType.SmallGrid)
            {
                if (info.Velocity.LengthSquared() >= minAirTargetSpeed)
                { notif = TargetNotif.AirTarget; }
                else
                { notif = TargetNotif.GroundTarget; }
            }

            return notif;
        }

        private bool IsBingoFuel()
        {
            bool status = false;
            if (bingoTimer.Elapsed())
            {
                int countedTanks = 0;
                double averageFilledRatio = 0;
                foreach (IMyGasTank fuelTank in fuelTanks)
                {
                    if (fuelTank.IsWorking)
                    {
                        countedTanks++;
                        averageFilledRatio += fuelTank.FilledRatio;
                    }
                }

                averageFilledRatio /= countedTanks;
                status = (averageFilledRatio <= (bingoFuelPercent));
                if (!status) { warnedBingo = false; }
                bingoTimer.Reset();
            }
            return status;
        }

        #endregion

        #region Custom Monitors

        private void RunCustomMonitorRoutines()
        {
            if(customTimer.Elapsed() && customMonitorFlags.Count > 0)
            {
                CustomMonitor monitor = customMonitors.Dequeue();
                customMonitors.Enqueue(monitor);

                if(monitor.Type == typeVelocity)
                { MonitorVelocity(monitor); }
                else if (monitor.Type == typeFunctional)
                { MonitorFunctional(monitor); }
                else if (monitor.Type == typeWorking)
                { MonitorWorking(monitor); }

                customTimer.Reset();
            }
        }

        private void MonitorVelocity(CustomMonitor monitor)
        {
            double value = 0;
            if(double.TryParse(monitor.Value, out value))
            {
                foreach(IMyTerminalBlock block in monitor.Blocks)
                {
                    string f1 = $"{block.GetValueFloat("Velocity"):0.##}";
                    string f2 = $"{monitor.Value:0.##}";
                    if(f1 == f2) 
                    { customMonitorFlags[monitor.Tag] = true; return; }
                }
            }
            customMonitorFlags[monitor.Tag] = false;
        }

        private void MonitorFunctional(CustomMonitor monitor)
        {
            foreach (IMyTerminalBlock block in monitor.Blocks)
            {
                if (monitor.Value.ToLowerInvariant() == "false"
                && !block.IsFunctional) 
                { customMonitorFlags[monitor.Tag] = true; return; }
                else if (monitor.Value.ToLowerInvariant() == "true"
                && block.IsFunctional) 
                { customMonitorFlags[monitor.Tag] = true; return; }
            }
            customMonitorFlags[monitor.Tag] = false;
        }

        private void MonitorWorking(CustomMonitor monitor)
        {
            foreach (IMyTerminalBlock block in monitor.Blocks)
            {
                if (monitor.Value.ToLowerInvariant() == "false"
                && !block.IsWorking)
                { customMonitorFlags[monitor.Tag] = true; return; }
                else if (monitor.Value.ToLowerInvariant() == "true"
                && block.IsWorking)
                { customMonitorFlags[monitor.Tag] = true; return; }
            }
            customMonitorFlags[monitor.Tag] = false;
        }

        #endregion

        #region Render Monitors

        private void RunMonitorRendering()
        {
            if (customRenderTimer.Elapsed() && screenSurfaces.Count > 0)
            {
                if (updatedScreens < screenSurfaces.Count)
                {
                    var screen = screenSurfaces[updatedScreens];

                    var frame = screen.DrawFrame();
                    var viewPort = new RectangleF((screen.TextureSize - screen.SurfaceSize) / 2f, screen.SurfaceSize);
                    RenderMonitors(ref frame, viewPort);
                    frame.Dispose();

                    updatedScreens++;
                }
                else
                { updatedScreens = 0; }
                
                customRenderTimer.Reset();
            }
        }

        private void RenderMonitors(ref MySpriteDrawFrame frame, RectangleF viewPort)
        {
            var position = new Vector2(256, 20) + viewPort.Position;

            // Draw header rectangle
            var sprite = ShapeSprite("headerbg", Shapes.Square, headerBgColor, 
                                     position, new Vector2(viewPort.Width, 110), TextAlignment.CENTER);
            sprite.Color = headerBgColor;
            frame.Add(sprite);

            // Draw header text
            sprite = TextSprite("headertext", headerFgText, headerFgColor, position, TextAlignment.CENTER);
            sprite.Data = headerFgText;
            sprite.Color = headerFgColor;
            frame.Add(sprite);

            // Alert
            RenderStandardAlert(ref frame, position);

            // Status
            RenderStatus(ref frame, position + new Vector2(0, 200));
        }

        private void RenderStandardAlert(ref MySpriteDrawFrame frame, Vector2 position)
        {
            if(showSeaElevation)
            {
                var sprite = TextSprite("sealevelfg", string.Empty, headerFgColor,
                                    position + new Vector2(0, 75), TextAlignment.CENTER);
                sprite.Data = $"SEA ELEV: {currSeaElevation:0.00}";
                frame.Add(sprite);
                return;
            }

            // Alerts
            if(weaponCoreSupport)
            {
                if (missileDetected || enemyDetected)
                {
                    if(CanDrawBlinker())
                    {
                        // Red background
                        var sprite = ShapeSprite("alertbg", Shapes.Square, alertBgColor,
                                                 position + new Vector2(0, 100), new Vector2(240, 60), TextAlignment.CENTER);
                        sprite.Color = alertBgColor;
                        frame.Add(sprite);

                        string alertText = string.Empty;
                        if(missileDetected)
                        { alertText = "M I S S I L E"; }
                        else if(enemyDetected)
                        { alertText = "H O S T I L E"; }

                        sprite = TextSprite("alerttext", string.Empty, alertFgColor,
                                            position + new Vector2(0, 75), TextAlignment.CENTER);
                        sprite.Color = alertFgColor;
                        sprite.Data = alertText;
                        frame.Add(sprite);
                    }
                }
                else if(entities.Count > 0)
                {
                    if (CanDrawBlinker())
                    {
                        // Yellow background
                        var sprite = ShapeSprite("contactbg", Shapes.Square, contactBgColor,
                                                 position + new Vector2(0, 100), new Vector2(240, 60), TextAlignment.CENTER);
                        sprite.Color = contactBgColor;
                        frame.Add(sprite);
 
                        sprite = TextSprite("contacttext", "C O N T A C T", contactFgColor,
                                            position + new Vector2(0, 75), TextAlignment.CENTER);
                        sprite.Color = contactFgColor;
                        frame.Add(sprite);
                    }
                }
                else
                {
                    // Green background
                    var sprite = ShapeSprite("clearbg", Shapes.Square, clearBgColor,
                                              position + new Vector2(0, 100), new Vector2(240, 60), TextAlignment.CENTER);
                    sprite.Color = clearBgColor;
                    frame.Add(sprite);
                    // Netural text
                    sprite = TextSprite("cleartext", "C L E A R", clearFgColor, 
                                        position + new Vector2(0, 75), TextAlignment.CENTER);
                    sprite.Color = clearFgColor;
                    frame.Add(sprite);
                }
            }
            else
            {
                // Gray background
                var sprite = ShapeSprite("nowcbg", Shapes.Square, noSupportBgColor,
                                          position + new Vector2(0, 100), new Vector2(360, 60), TextAlignment.CENTER);
                sprite.Color = noSupportBgColor;
                frame.Add(sprite);
                // Gray text
                sprite = TextSprite("nowctext", "WC SUPPORT OFF", noSupportFgColor,
                                    position + new Vector2(0, 75), TextAlignment.CENTER);
                sprite.Color = noSupportFgColor;
                frame.Add(sprite);
            }

        }

        private void RenderStatus(ref MySpriteDrawFrame frame, Vector2 position)
        {
            int cmIdx = 0;
            string[] customTags = customMonitorFlags.Keys.ToArray();

            for(int row = 0; row <= 3; row++)
            {
                for(int col = 0; col <= 1; col++)
                {
                    string text = string.Empty;
                    bool isActive = false;

                    if (row == 0 && col == 0) // Auto-throttle
                    {
                        isActive = autoThrottle;
                        text = "THRUST";
                    }
                    else if (row == 1 && col == 0)  // Low-fly
                    {
                        isActive = lowFlightMode;
                        text = "LOW FLY";
                    }
                    else if (row == 2 && col == 0)  // Cruise
                    {
                        isActive = (cruiseMode);
                        text = "CRUISE";
                    }
                    else
                    {
                        if(cmIdx < customMonitorFlags.Count)
                        {
                            isActive = customMonitorFlags[customTags[cmIdx]];
                            text = customTags[cmIdx];
                            cmIdx++;
                        }
                    }

                    // Calculate offset
                    Vector2 pos = position + new Vector2((col == 0 ? -200 : 20), 70 * row);

                    // Green background
                    var sprite = ShapeSprite($"statusbg{row}{col}", Shapes.Square, statusOffBgColor,
                                              pos, new Vector2(180, 50), TextAlignment.LEFT);
                    sprite.Color = isActive ? statusOnBgColor : statusOffBgColor;
                    frame.Add(sprite);

                    // Text foregrund
                    sprite = TextSprite($"statustext{row}{col}", string.Empty, statusOffBgColor,
                                        pos + new Vector2(17,-22), TextAlignment.LEFT);
                    sprite.Color = isActive ? statusOnFgColor : statusOffFgColor;
                    sprite.Data = text;
                    frame.Add(sprite);
                }
            }
        }

        private bool CanDrawBlinker()
        {
            bool render = false;
            if(blinkerOnTimer.Elapsed())
            { blinkerOnTimer.Reset(canUpdate: false); blinkerOffTimer.CanUpdate = true; }
            else { render = true; }
            if (blinkerOffTimer.Elapsed())
            { blinkerOffTimer.Reset(canUpdate: false); blinkerOnTimer.CanUpdate = true; }
            return render;
        }

        #endregion

        #region Common

        private void RegisterTimer(TickTimer timer, bool canUpdateByDefault)
        {
            if(timer != null)
            {
                timer.CanUpdate = canUpdateByDefault;
                tickTimers.Add(timer);
            }
            else
            {
                Me.CustomData += timer.ToString() + "\n";
            }
        }

        private IMyShipController GetController()
        {
            IMyShipController foundController = null;
            foreach (IMyShipController controller in controllers)
            {
                if (controller.IsWorking && controller.IsUnderControl)
                {
                    if (foundController == null || controller.IsMainCockpit)
                    {
                        foundController = controller;
                        if (controller.IsMainCockpit)
                        { break; }
                    }
                }
            }
            if (foundController == null)
            { foundController = controllers[0]; }
            return foundController;
        }

        private void SetThrustersOverridePercent(float value, bool subgridsOnly = false)
        {
            foreach (IMyThrust thruster in thrusters)
            {
                if (subgridsOnly && (thruster.CubeGrid == Me.CubeGrid))
                { continue; }

                if (thruster.ThrustOverridePercentage != value)
                { thruster.ThrustOverridePercentage = value; }
            }
        }

        private void PlaySound(string sound)
        {
            foreach (IMySoundBlock soundBlock in soundBlocks)
            {
                if (soundBlock.IsWorking)
                {
                    soundBlock.Stop();
                    soundBlock.SelectedSound = sound;
                    soundBlock.LoopPeriod = 2;
                    soundBlock.Play();
                }
            }
        }

        private void StopSound()
        {
            foreach (IMySoundBlock soundBlock in soundBlocks)
            {
                soundBlock.Stop();
            }
        }

        private int GetRelationship(MyDetectedEntityInfo info)
        {
            RadarRelationship sizeVal = RadarRelationship.Neutral;
            if (info.Type == MyDetectedEntityType.SmallGrid) { sizeVal = RadarRelationship.SmallGrid; }
            if (info.Type == MyDetectedEntityType.LargeGrid) { sizeVal = RadarRelationship.LargeGrid; }
            if (info.Type == MyDetectedEntityType.Missile) { sizeVal = RadarRelationship.Missile; }

            RadarRelationship relVal = RadarRelationship.Enemy;
            switch (info.Relationship)
            {
                case MyRelationsBetweenPlayerAndBlock.Enemies: relVal = RadarRelationship.Enemy; break;
                case MyRelationsBetweenPlayerAndBlock.Friends: relVal = RadarRelationship.Friendly; break;
                case MyRelationsBetweenPlayerAndBlock.FactionShare: relVal = RadarRelationship.Friendly; break;
                case MyRelationsBetweenPlayerAndBlock.Owner: relVal = RadarRelationship.Friendly; break;
                case MyRelationsBetweenPlayerAndBlock.Neutral: relVal = RadarRelationship.Neutral; break;
                case MyRelationsBetweenPlayerAndBlock.NoOwnership: relVal = RadarRelationship.Neutral; break;
                default: relVal = RadarRelationship.Enemy; break;
            }

            RadarRelationship lockVal = RadarRelationship.Neutral;
            MyDetectedEntityInfo? locked = wc.GetAiFocus(Me.CubeGrid.EntityId);
            if (locked.HasValue && info.EntityId == locked.Value.EntityId)
            {
                lockVal = RadarRelationship.Locked;
            }

            int result = (int)sizeVal + (int)relVal + (int)lockVal;
            return result;
        }

        private void ClearGyroOverrides()
        {
            foreach (IMyGyro gyro in gyros)
            {
                gyro.Pitch = 0;
                gyro.Yaw = 0;
                gyro.Roll = 0;
                gyro.GyroOverride = false;
            }
        }

        private double GetSign(double value, bool flip = false)
        {
            if (!flip)
            { return value > 0 ? 1 : value < 0 ? -1 : 0; }
            else 
            { return value > 0 ? -1 : value < 0 ? 1 : 0; }
        }

        private MyTuple<byte, long, Vector3D, double> ToTuple(MyDetectedEntityInfo entity)
        {
            return new MyTuple<byte, long, Vector3D, double>((byte)GetRelationship(entity), entity.EntityId, entity.Position, 0);
        }

        private enum Shapes
        {
            Square,
            Circle,
            Triangle,
            SquareHollow,
            CircleHollow,
            Cross,
            Arrow
        }

        private MySprite ShapeSprite(string name, Shapes shape, Color color,
                                     Vector2 position, Vector2 size, TextAlignment alignment)
        {
            string data = string.Empty;
            switch (shape)
            {
                case Shapes.Square: data = "SquareSimple"; break;
                case Shapes.Circle: data = "Circle"; break;
                case Shapes.Triangle: data = "Triangle"; break;
                case Shapes.SquareHollow: data = "SquareHollow"; break;
                case Shapes.CircleHollow: data = "CircleHollow"; break;
                case Shapes.Cross: data = "Cross"; break;
                case Shapes.Arrow: data = "Arrow"; break;
                default: data = "SquareSimple"; break;
            }

            return MakeSprite(name, SpriteType.TEXTURE, data, color, position, alignment, size);
        }

        private MySprite TextSprite(string name, string message, Color color,
                                    Vector2 position, TextAlignment alignment)
        {
            return MakeSprite(name, SpriteType.TEXT, message, color, position, alignment, scale: 1.5f);
        }

        private MySprite MakeSprite(string name, SpriteType type, string data, Color color, 
                                    Vector2 position, TextAlignment alignment, 
                                    Vector2 size = new Vector2(), float scale = 0)
        {
            if(sprites.ContainsKey(name))
            {
                return sprites[name]; 
            }
            else
            {
                var sprite = new MySprite()
                {
                    Type = type,
                    Data = data,
                    Color = color,
                    Position = position,
                    Alignment = alignment,
                    Size = size,
                    RotationOrScale = scale,
                    FontId = "White"
                };
                sprites.Add(name, sprite);
                return sprite;
            }
        }
        
        #endregion

        #region Whip's Functions 

        /* ================================================================
         *  The succeeding functions are generously provided by Whiplash141
         *  https://github.com/Whiplash141/SpaceEngineersScripts
         * ================================================================
         */

        //Whip's ApplyGyroOverride Method v9 - 8/19/17
        void ApplyGyroOverride(double pitch_speed, double yaw_speed, double roll_speed, List<IMyGyro> gyro_list, IMyTerminalBlock reference)
        {
            var rotationVec = new Vector3D(-pitch_speed, yaw_speed, roll_speed); //because keen does some weird stuff with signs 
            var shipMatrix = reference.WorldMatrix;
            var relativeRotationVec = Vector3D.TransformNormal(rotationVec, shipMatrix);

            foreach (var thisGyro in gyro_list)
            {
                var gyroMatrix = thisGyro.WorldMatrix;
                var transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(gyroMatrix));

                thisGyro.Pitch = (float)transformedRotationVec.X;
                thisGyro.Yaw = (float)transformedRotationVec.Y;
                thisGyro.Roll = (float)transformedRotationVec.Z;
                thisGyro.GyroOverride = true;
            }
        }

        int VectorCompareDirection(Vector3D a, Vector3D b) //returns -1 if vectors return negative dot product
        {
            double check = a.Dot(b);
            if (check < 0)
                return -1;
            else
                return 1;
        }

        double VectorAngleBetween(Vector3D a, Vector3D b) //returns radians
        {
            if (a.LengthSquared() == 0 || b.LengthSquared() == 0)
                return 0;
            else
                return Math.Acos(MathHelper.Clamp(a.Dot(b) / a.Length() / b.Length(), -1, 1));
        }

        Vector3D VectorProjection(Vector3D a, Vector3D b) //proj a on b
        {
            Vector3D projection = a.Dot(b) / b.LengthSquared() * b;
            return projection;
        }

        #endregion

        #endregion ======= SCRIPT END =======
    }
}
