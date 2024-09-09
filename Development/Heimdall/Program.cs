using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO.IsolatedStorage;
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
using VRage.Voxels.Mesh;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        // Groupings
        private string scriptGroupName;
        private string screensGroupName;
        private string soundsGroupName;
        private string mawsGroupName;
        private string flaresGroupName;
        private string logsBlockName;
        private string scanCameraName;
        private string weaponIgnoreTag;
        private int scanDistance;

        // Sounds
        private string radarLockSound;
        private string missileLaunchSound;
        private string specialContactSound;
        private string newContactSound;

        // Targeting
        private double minimumEntitySize;
        private int globalAutoFireDelaySecs;
        private int targetAutoFireDelaySecs;
        private int counterMissileFireDelaySecs;
        private bool autoAdjustRadarRange;
        private int defaultRadarRangeMeters;

        // Basic Turret
        private string turretGroupTag;
        private bool activateTurret;
        private string aimReferenceTag;
        private string azimuthRotorTag;
        private string elevationRotorTag;

        private SimpleTimerSM timerSM;
        private WCPbApi wc = null;

        // Targeting Tracking
        private List<long> entityIds = new List<long>();
        private List<long> blackListedIds = new List<long>();
        private Queue<long> targetQueue = new Queue<long>();
        private Dictionary<long, HostileEntity> targets = new Dictionary<long, HostileEntity>();
        private Dictionary<MyDetectedEntityInfo, float> entities = new Dictionary<MyDetectedEntityInfo, float>();
        private MyDetectedEntityInfo targetEntity = new MyDetectedEntityInfo();

        // Manual Fire Control
        private int manualTargetId = 0;
        private IMyTerminalBlock manualWeapon = null;
        private bool hasPendingFireRequest = false;
        private bool hasPendingFreeFireRequest = false;

        // Auto Fire Control
        private bool autoFire = false;
        private int targetFireMaxTicks;
        private int counterMissileFireMaxTicks;
        private int globalFireMaxTicks;
        private int currGlobalFireTicks;
        private int currMissileAutoFireTicks;
        private const int blacklistMaxTicks = 3600;
        private int currBlacklistTicks = 0;

        // Remote Fire Control
        private const string PASSKEY = "d6ec0b65-b173-442d-8e46-53760218f59a";
        private bool masterModeOn = false;
        private bool slaveModeOn = false;
        private Queue<long> receiverQueue = new Queue<long>();
        private const int MAX_MASTER_PING_TICKS = 1000;
        private int currMasterPingTicks = MAX_MASTER_PING_TICKS;
        private bool hasPendingRemoteFireRequest = false;
        private long remoteFireSender = 0;
        private long remoteFireTargetId = 0;
        private bool remoteManualFire = false;

        // Networking
        private const string HEIMDALL_IGC_UNI = "HEIMDALL_IGC_UNI";
        private const string HEIMDALL_IGC_BRD = "HEIMDALL_IGC_BRD";
        Networking network;

        // Active Flare
        private List<IMyTerminalBlock> wcMAWS = new List<IMyTerminalBlock>();
        private List<IMyTerminalBlock> wcFlares = new List<IMyTerminalBlock>();
        private const int lockOnWarningTicks = 600;
        private int currLockOnWarningTicks = 600;
        private bool isDispensing = false;
        private bool isBeingLockedOn = false;
        private bool isBeingFiredOn = false;

        // Script Blocks
        private List<IMySoundBlock> wcSounds = new List<IMySoundBlock>();
        private List<IMyTextSurface> wcScreens = new List<IMyTextSurface>();
        private List<IMyTerminalBlock> wcWeapons = new List<IMyTerminalBlock>();
        private List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
        private List<IMyProgrammableBlock> whipPBs = new List<IMyProgrammableBlock>();
        private List<string> sounds = new List<string>();

        // Basic Turret Handling
        private List<IMyMotorStator> rotors = new List<IMyMotorStator>();
        private MyDetectedEntityInfo turretTarget;
        private IMyTerminalBlock aimReference;
        private IMyMotorStator azimuthRotor;
        private IMyMotorStator elevationRotor;
        private float azimuthRestAngle;
        private float elevationRestAngle;
        private PIDController elPID = new PIDController(2, 0, 10);
        private PIDController azPID = new PIDController(2, 0, 10);
        private bool resetTurret = true;

        // Logs
        private StringBuilder logs = new StringBuilder();
        private StringBuilder debugLogs = new StringBuilder();
        private StringBuilder issues = new StringBuilder();
        private StringBuilder lcdLogs = new StringBuilder();
        private StringBuilder targetList = new StringBuilder();
        private IMyTerminalBlock logBlock = null;
        private IMyCameraBlock scanCamera = null;
        private bool hasPendingTargetLogRequest = false;
        private string logType = string.Empty;
        private const string DEBUG_PANEL_NAME = "Debug Panel";
        private IMyTextSurfaceProvider debugTextPanel;

        // Setup
        private const string SCRIPT_VERSION = "1.5";
        private bool isSetup = false;
        private const int SETUP_TIME = 600;
        private int currSetupTime = 300;

        // Display
        private MyIni iniTool = new MyIni();
        private const string SCREEN_SETTINGS = "Heimdall - Text Surface Config";

        // Whip Radar Interface
        private const string IGC_TAG = "IGC_IFF_MSG";
        private const string RADAR_TAG = "Radar - General";
        private const string RADAR_KEY_1 = "Use radar range override";
        private const string RADAR_KEY_2 = "Radar range override (m)";
        private MyIni iniStorage = new MyIni();
        private const int RADAR_RANGE_TICKS = 15;
        private int currRadarRangeTicks = RADAR_RANGE_TICKS;
        private const int RADAR_UPDATE_TICKS = 20;
        private int currRadarUpdateTicks = RADAR_UPDATE_TICKS;
        private int radarMaxDistance = 0;
        private int enemyMaxDistance = 0;
        private bool enemyDetected = false;
        private bool isInit = false;

        // Settings
        private const string SCRIPT_NAME = "Heimdall";
        private const string GENERAL_SETTINGS = "General";
        private const string TARGETING_SETTINGS = "Targeting";
        private const string SOUND_SETTINGS = "Sounds";
        private const string TURRET_SETTINGS = "Turret";
        private const int SETTINGS_UPDATE_TICKS = 600;
        private int currSettingsTicks = SETTINGS_UPDATE_TICKS;
        ScriptSettings settings = new ScriptSettings(SCRIPT_NAME);
        string scanProb = string.Empty;

        // Settings - Saved State
        ScriptSettingsBinary storageSettings = new ScriptSettingsBinary();
        private const int SHIFT_AUTOFIRE = 0;
        private const int SHIFT_REMOTE_MASTER = 1;
        private const int SHIFT_REMOTE_SLAVE = 2;

        public class HostileEntity
        {
            public MyDetectedEntityInfo Info = new MyDetectedEntityInfo();
            public int ShootTime = 0;
            public int EngageCount = 0;
            public float Threat = 0;
            public MyDetectedEntityType Type = MyDetectedEntityType.None;
            public int ListIndex = 0;
            public int blackListVote = 0;
        }

        // Whip's Radar Entity Relationship Values
        public enum RadarRelationship
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

        public Program()
        {
            // Load AutoFire setting
            if (!string.IsNullOrEmpty(Storage))
            {
                if(storageSettings.TryLoad(Storage))
                {
                    autoFire = storageSettings.Get(SHIFT_AUTOFIRE);
                    masterModeOn = storageSettings.Get(SHIFT_REMOTE_MASTER);
                    slaveModeOn = storageSettings.Get(SHIFT_REMOTE_SLAVE);
                }
                else
                {
                    Storage = "0";
                    autoFire = false;
                    masterModeOn = false;
                    slaveModeOn = false;
                }
            }

            network = new Networking(this, HEIMDALL_IGC_UNI, HEIMDALL_IGC_BRD);
            network.SetMessageHandler(new Action<long, object>(HandleMessages));

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            timerSM = new SimpleTimerSM(this, Update(), true);

            RefreshSettings();
        }

        public void Main(string argument, UpdateType updateSource)
        {
            try
            {
                logs.Clear();
                debugLogs.Clear();

                logs.AppendLine($"Heimdall v{SCRIPT_VERSION} by Khjin");
                logs.AppendLine($"WCMissile Director");
                logs.AppendLine($"Runtime (ms): {Runtime.LastRunTimeMs}");
                if (!isSetup)
                {
                    logs.AppendLine($"Retrying Setup in {10 - ((int)(currSetupTime / 60))}s");
                }
                logs.AppendLine("-------");
                if (masterModeOn)   { logs.AppendLine(">> MasterMode ON <<"); }
                if (slaveModeOn)    { logs.AppendLine(">> SlaveMode ON <<"); }
                if (autoFire)       { logs.AppendLine(">> Auto-Fire ON <<"); }
                if (activateTurret) { logs.AppendLine(">> Turret Active <<"); }
                logs.AppendLine($"Weapons: {wcWeapons.Count}");
                logs.AppendLine($"Screens: {wcScreens.Count}");
                logs.AppendLine($"MAWS: {wcMAWS.Count}");
                logs.AppendLine($"Flares: {wcFlares.Count}");
                logs.AppendLine($"Sound Blocks: {wcSounds.Count}");
                logs.AppendLine($"Black Box: {(logBlock != null ? "YES" : "NO")}");
                logs.AppendLine($"Scanner: {(scanCamera != null ? "YES" : "NO")}");
                logs.AppendLine(scanProb);
                if (!isSetup && IsSetupTime())
                {
                    isSetup = Setup();
                }

                if (isSetup)
                {
                    if (!string.IsNullOrEmpty(argument))
                    {
                        HandleArguments(argument);
                    }

                    // Basic turret aiming
                    AimAtTarget();
                    RefreshSettings();
                }
                else
                {
                    logs.AppendLine("\n---");
                    logs.AppendLine(issues.ToString());
                    logs.AppendLine("Script setup failed.");
                }

                UpdateTickTimers();
                timerSM.Run();

                Echo(logs.ToString());
            }
            catch(Exception ex)
            {
                isSetup = false;
                currSetupTime = SETUP_TIME;

                if (debugTextPanel != null && debugTextPanel.SurfaceCount > 0)
                {
                    debugLogs.AppendLine(ex.Message);
                    debugLogs.AppendLine("-------------");
                    debugLogs.AppendLine(ex.StackTrace);
                    debugTextPanel.GetSurface(0).WriteText(debugLogs.ToString());
                }
            }
        }

        public void Save()
        {
            Storage = storageSettings.Export();
        }

        public void RefreshSettings()
        {
            if (!IsSettingRefreshTime()) { return; }

            if (Me.CustomData != null && Me.CustomData != string.Empty)
            {
                settings.TryLoad(Me.CustomData);
            }

            // General Settings
            scriptGroupName = settings.Get("scriptGroupName", GENERAL_SETTINGS, "WCWeapons");
            screensGroupName = settings.Get("screensGroupName", GENERAL_SETTINGS, "WCScreens");
            soundsGroupName = settings.Get("soundsGroupName", GENERAL_SETTINGS, "WCSounds");
            flaresGroupName = settings.Get("flaresGroupName", GENERAL_SETTINGS, "WCFlares");
            mawsGroupName = settings.Get("mawsGroupName", GENERAL_SETTINGS, "WCMAWS");
            logsBlockName = settings.Get("logsBlockName", GENERAL_SETTINGS, "BlackBox");
            scanCameraName = settings.Get("scanCameraName", GENERAL_SETTINGS, "Scanner");
            weaponIgnoreTag = settings.Get("weaponIgnoreTag", GENERAL_SETTINGS, "Radar").ToLowerInvariant();
            scanDistance = settings.Get("scanDistance", GENERAL_SETTINGS, 12000);

            // Sounds
            radarLockSound = settings.Get("radarLockSound", SOUND_SETTINGS, "RWR-TrackingAndTargeting");
            missileLaunchSound = settings.Get("missileLaunchSound", SOUND_SETTINGS, "RWR-MissileLaunchWarning");
            specialContactSound = settings.Get("specialContactSound", SOUND_SETTINGS, "RWR-SpecialContact");
            newContactSound = settings.Get("newContactSound", SOUND_SETTINGS, "RWR-NewContact");

            // Targeting
            minimumEntitySize = settings.Get("minimumEntitySize", TARGETING_SETTINGS, 3.0f);
            globalAutoFireDelaySecs = settings.Get("globalAutoFireDelaySecs", TARGETING_SETTINGS, 2);
            targetAutoFireDelaySecs = settings.Get("targetAutoFireDelaySecs", TARGETING_SETTINGS, 5);
            counterMissileFireDelaySecs = settings.Get("counterMissileFireDelaySecs", TARGETING_SETTINGS, 2);
            autoAdjustRadarRange = settings.Get("autoAdjustRadarRange", TARGETING_SETTINGS, true);
            defaultRadarRangeMeters = settings.Get("defaultRadarRangeMeters", TARGETING_SETTINGS, 15000);

            // Basic Turret Control
            turretGroupTag = settings.Get("turretGroupTag", TURRET_SETTINGS, "Turret Group");
            activateTurret = settings.Get("activateTurret", TURRET_SETTINGS, false);
            aimReferenceTag = settings.Get("aimReferenceTag", TURRET_SETTINGS, "Aim");
            azimuthRotorTag = settings.Get("azimuthRotorTag", TURRET_SETTINGS, "Azimuth");
            elevationRotorTag = settings.Get("elevationRotorTag", TURRET_SETTINGS, "Elevation");

            // Autofire
            targetFireMaxTicks = targetAutoFireDelaySecs * 60;
            globalFireMaxTicks = globalAutoFireDelaySecs * 60;
            counterMissileFireMaxTicks = counterMissileFireDelaySecs * 60;
            currMissileAutoFireTicks = counterMissileFireMaxTicks;
            currGlobalFireTicks = globalAutoFireDelaySecs * 60;

            Me.CustomData = settings.Export();
        }

        public void HandleArguments(string args)
        {
            if (args == "Fire")
            {
                hasPendingFreeFireRequest = true;
            }
            else if (args == "Scan")
            {
                ScanTarget();
            }
            else if (args.StartsWith("Log"))
            {
                logType = args;
                hasPendingTargetLogRequest = true;
            }
            else if (args.StartsWith("Fire") && args.Contains(":"))
            {
                string num = args.Replace("Fire:", "").Trim();
                int target_num = 0;
                if (int.TryParse(num, out target_num))
                {
                    if (manualTargetId == 0)
                    {
                        manualTargetId = target_num;
                    }
                    else
                    {
                        if (target_num == manualTargetId)
                            hasPendingFireRequest = true;
                        else
                            manualTargetId = target_num;
                    }

                    autoFire = false;
                }
            }
            else if (args == "AutoFire")
            {
                autoFire = (!autoFire);
                storageSettings.Set(autoFire, SHIFT_AUTOFIRE);
                Save();
            }
            else if (args == "MasterMode")
            {
                masterModeOn = (!masterModeOn);
                slaveModeOn = masterModeOn ? false : slaveModeOn;
                storageSettings.Set(masterModeOn, SHIFT_REMOTE_MASTER);
                storageSettings.Set(slaveModeOn, SHIFT_REMOTE_SLAVE);
                Save();
            }
            else if (args == "SlaveMode")
            {
                slaveModeOn = (!slaveModeOn);
                masterModeOn = slaveModeOn ? false : masterModeOn;
                storageSettings.Set(masterModeOn, SHIFT_REMOTE_MASTER);
                storageSettings.Set(slaveModeOn, SHIFT_REMOTE_SLAVE);
                Save();
            }
        }

        public void HandleMessages(long sender, object message)
        {
            if (message is MyTuple<int, string>)
            {
                var data = (MyTuple<int, string>)message;
                if(data.Item2 == PASSKEY)
                {
                    if(slaveModeOn && data.Item1 == RemoteMessage.MASTER_PING)
                    {
                        var msg = RemoteMessage.GetRegisterMsg(Me.EntityId);
                        network.Unicast(sender, msg);
                    }
                    else if(masterModeOn && data.Item1 == RemoteMessage.SLAVE_REGISTER)
                    {
                        if (!receiverQueue.Contains(sender))
                        {
                            receiverQueue.Enqueue(sender);
                        }
                    }
                }
            }
            else if (message is MyTuple<int, string, long>)
            {
                var data = (MyTuple<int, string, long>)message;
                if (data.Item2 == PASSKEY)
                {
                    // Remote Fire Confirmation
                    if (masterModeOn && data.Item1 == RemoteMessage.REMOTE_FIRE_CONFIRM)
                    {
                        if (targets.ContainsKey(data.Item3))
                        {
                            targets[data.Item3].ShootTime = 0;
                            targets[data.Item3].EngageCount++;
                        }
                    }
                }
            }
            else if(message is MyTuple<int, string, long, bool>)
            {
                var data = (MyTuple<int, string, long, bool>)message;
                if (data.Item2 == PASSKEY)
                {
                    // Remote Fire
                    if (slaveModeOn && data.Item1 == RemoteMessage.REMOTE_FIRE)
                    {
                        if (data.Item1 == RemoteMessage.REMOTE_FIRE
                        && data.Item2 == PASSKEY
                        && !hasPendingRemoteFireRequest)
                        {
                            remoteFireTargetId = data.Item3;
                            hasPendingRemoteFireRequest = true;
                            remoteManualFire = data.Item4;
                        }
                    }
                }
            }
        }

        private void SendMasterPings()
        {
            if (masterModeOn)
            {
                if (IsMasterPingTime())
                {
                    receiverQueue.Clear();
                    var msg = RemoteMessage.GetMasterPingMsg();
                    network.Broadcast(msg);
                }
            }
        }

        private bool Setup()
        {
            // Clear Storages
            issues.Clear();
            blocks.Clear();
            wcWeapons.Clear();
            wcScreens.Clear();
            wcMAWS.Clear();
            wcFlares.Clear();
            wcSounds.Clear();
            whipPBs.Clear();

            azimuthRotor = null;
            elevationRotor = null;
            rotors.Clear();

            // Activate WeaponCore API
            wc = WCPbApi.GetInstance(Me);
            if (wc == null)
            {
                issues.AppendLine("Unable to activate WeaponCore API");
                return false;
            }

            // Check if a WeaponCore weapon is available
            if (!wc.HasGridAi(Me.CubeGrid.EntityId))
            {
                issues.AppendLine("No WeaponCore weapon installed.");
                return false;
            }

            // Get weapon blocks
            IMyBlockGroup group = GridTerminalSystem.GetBlockGroupWithName(scriptGroupName);
            if (group != null)
            {
                group.GetBlocks(wcWeapons, b => b.IsSameConstructAs(Me));
                if (wcWeapons.Count > 0)
                {
                    wcWeapons = wcWeapons.OrderBy(w => wc.GetMaxWeaponRange(w, 0)).ToList();
                }
            }
            else
            {
                issues.AppendLine($"No {scriptGroupName} group found.");
            }

            // Get Screens
            group = GridTerminalSystem.GetBlockGroupWithName(screensGroupName);
            if (group != null)
            {
                group.GetBlocks(blocks, b => b.IsSameConstructAs(Me));
                foreach (var block in blocks)
                {
                    if (block is IMyTextSurfaceProvider)
                    {
                        if (!block.CustomData.ToLowerInvariant().Contains(SCREEN_SETTINGS.ToLowerInvariant()))
                        {
                            iniTool.Clear();
                            iniTool.AddSection(SCREEN_SETTINGS);
                            var surfaceProvider = block as IMyTextSurfaceProvider;
                            for (int i = 0; i < surfaceProvider.SurfaceCount; i++)
                            {
                                iniTool.Set(SCREEN_SETTINGS, $"Show on screen {i}", false);
                            }
                            block.CustomData = iniTool.ToString();
                        }
                        else
                        {
                            if (iniTool.TryParse(block.CustomData))
                            {
                                var surfaceProvider = block as IMyTextSurfaceProvider;
                                for (int i = 0; i < surfaceProvider.SurfaceCount; i++)
                                {
                                    if (iniTool.Get(SCREEN_SETTINGS, $"Show on screen {i}").ToBoolean())
                                    {
                                        var surface = surfaceProvider.GetSurface(i);
                                        surface.ContentType = ContentType.TEXT_AND_IMAGE;
                                        wcScreens.Add(surface);
                                    }
                                }
                            }
                        }
                    }
                    if( block is IMyBatteryBlock) 
                    { isInit = true; }
                }
            }

            // Get MAWS
            group = GridTerminalSystem.GetBlockGroupWithName(mawsGroupName);
            if (group != null)
            { group.GetBlocksOfType(wcMAWS, b => b.IsSameConstructAs(Me)); }

            // Get Flares
            group = GridTerminalSystem.GetBlockGroupWithName(flaresGroupName);
            if (group != null)
            { group.GetBlocksOfType(wcFlares, b => b.IsSameConstructAs(Me)); }

            // Get Sound Blocks
            group = GridTerminalSystem.GetBlockGroupWithName(soundsGroupName);
            if (group != null)
            { group.GetBlocksOfType(wcSounds, b => b.IsSameConstructAs(Me)); }

            // Get Logs Block
            GridTerminalSystem.GetBlocksOfType(blocks, b => b.CustomName.Contains(logsBlockName) && b.IsSameConstructAs(Me));
            if (blocks.Count > 0)
            { logBlock = blocks.First(); }

            // Scan Camera Block
            List<IMyCameraBlock> cameras = new List<IMyCameraBlock>();
            GridTerminalSystem.GetBlocksOfType(cameras, b => b.CustomName.Contains(scanCameraName) && b.IsSameConstructAs(Me));
            if (cameras.Count > 0)
            {
                scanCamera = cameras.First();
                scanCamera.EnableRaycast = true;
            }

            // Basic Turret Control
            group = GridTerminalSystem.GetBlockGroupWithName(turretGroupTag);
            if (group != null)
            {
                group.GetBlocks(blocks, b => b.IsSameConstructAs(Me));
                foreach(var block in blocks)
                {
                    if(block is IMyMotorStator)
                    {
                        if (block.CustomName.ToLowerInvariant().Contains(azimuthRotorTag.ToLowerInvariant()))
                        { azimuthRotor = block as IMyMotorStator; float.TryParse(azimuthRotor.CustomData, out azimuthRestAngle); }
                        else if (block.CustomName.ToLowerInvariant().Contains(elevationRotorTag.ToLowerInvariant()))
                        { elevationRotor = block as IMyMotorStator; float.TryParse(elevationRotor.CustomData, out elevationRestAngle); }
                    }
                    else
                    {
                        if (block.CustomName.ToLowerInvariant().Contains(aimReferenceTag.ToLowerInvariant()))
                        { aimReference = block; }
                    }
                }
            }

            // Get Whiplast414's PBs
            GridTerminalSystem.GetBlocksOfType(whipPBs, b => b.IsSameConstructAs(Me) && b.CustomData.Contains(RADAR_TAG));

            // Debug Text Panel
            debugTextPanel = GridTerminalSystem.GetBlockWithName(DEBUG_PANEL_NAME) as IMyTextSurfaceProvider;

            return true;
        }

        IEnumerable<double> Update()
        {
            if (isSetup)
            {
                yield return 0;
                network.ProcessMessages();
                yield return 0;

                yield return 0;
                SendMasterPings();
                yield return 0;

                yield return 0;
                RefreshTargets();
                yield return 0;

                yield return 0;
                CheckForLockOn();
                yield return 0;

                yield return 0;
                CheckForMissiles();
                yield return 0;

                yield return 0;
                EngageTargets();
                yield return 0;

                // Display Targets
                foreach (var surface in wcScreens)
                {
                    yield return 0;

                    lcdLogs.Clear();
                    if (masterModeOn)
                    { lcdLogs.AppendLine($"[ Master Mode On, {receiverQueue.Count}]"); }
                    if (slaveModeOn)
                    { lcdLogs.AppendLine("[ Slave Mode On ]"); }
                    if (autoFire)
                    { lcdLogs.AppendLine("AutoFire On"); }
                    else
                    { lcdLogs.AppendLine($"Target: {(targets.ContainsKey(targetEntity.EntityId) ? targetEntity.Name : "")}"); }
                    lcdLogs.AppendLine("-------");
                    lcdLogs.AppendLine(targetList.ToString());
                    surface.WriteText(lcdLogs.ToString());

                    yield return 0;
                }

                // Log Targets
                if (hasPendingTargetLogRequest && logBlock != null)
                {
                    hasPendingTargetLogRequest = false;
                    StringBuilder sb = new StringBuilder(logBlock.CustomData);

                    foreach (var entity in entities.Keys)
                    {
                        if (HasGenericName(entity.Name))
                        {
                            continue;
                        }

                        if (logType == "Log"
                        || (logType == "LogSG" && entity.Type == MyDetectedEntityType.SmallGrid)
                        || (logType == "LogLG" && entity.Type == MyDetectedEntityType.LargeGrid))
                        {
                            string gps = "";
                            if (TryParseToGpsFormat(entity.BoundingBox.Center.ToString(), entity.Name, out gps))
                            {
                                sb.AppendLine(gps);
                            }
                            yield return 0;
                        }
                    }

                    sb.AppendLine("");
                    logBlock.CustomData = sb.ToString();
                }

                // Adjust PB Range
                if (autoAdjustRadarRange && IsRadarRangeUpdateTime())
                {
                    IMyProgrammableBlock[] pbs = whipPBs.ToArray();
                    foreach (var pb in pbs)
                    {
                        yield return 0;
                        iniStorage.Clear();
                        if (iniStorage.TryParse(pb.CustomData))
                        {
                            iniStorage.Set(RADAR_TAG, RADAR_KEY_1, true);
                            iniStorage.Set(RADAR_TAG, RADAR_KEY_2, radarMaxDistance);
                            pb.CustomData = iniStorage.ToString();
                        }
                        yield return 0;
                    }
                }
            }

            yield return 0;
        }

        private void RefreshTargets()
        {
            // Clear tracking storage
            targetList.Clear();
            entityIds.Clear();
            entities.Clear();
            int index = 1;

            if (isSetup && wc.HasGridAi(Me.CubeGrid.EntityId))
            {
                // Clear blacklisted entity ids (1-min)
                if(currBlacklistTicks == blacklistMaxTicks)
                {
                    currBlacklistTicks = 0;
                    blackListedIds.Clear();
                }

                // Reset radar distances
                radarMaxDistance = 0;
                enemyMaxDistance = 0;
                enemyDetected = false;

                wc.GetSortedThreats(Me, entities);
                foreach (MyDetectedEntityInfo info in entities.Keys)
                {
                    if (info.IsEmpty() || !IsValidEntity(info) || blackListedIds.Contains(info.EntityId))
                    { continue; }

                    // Get distance from entity (For Whip radar and threat calculation)
                    int dist = (int)Vector3D.Distance(Me.GetPosition(), info.BoundingBox.Center);

                    long entityId = info.EntityId;
                    float threat = CalculateThreat(info, dist, entities[info]);
                    entityIds.Add(entityId);

                    if (IsValidTarget(info, threat))
                    {
                        if (!targets.ContainsKey(entityId))
                        {
                            HostileEntity hostileEntity = ToHostileEntity(info, threat);
                            targets.Add(entityId, hostileEntity);

                            if(info.Type == MyDetectedEntityType.LargeGrid)
                            {
                                PlaySound(specialContactSound);
                            }
                            else
                            {
                                PlaySound(newContactSound);
                            }
                        }
                        else
                        {
                            HostileEntity hostileEntity = targets[entityId];
                            hostileEntity.Info = info;
                            hostileEntity.Threat = threat;
                        }
                    }

                    if (dist > radarMaxDistance)
                    { radarMaxDistance = dist; }

                    // Check of an enemy is detected
                    if (info.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies)
                    {
                        enemyDetected = true;
                        if (dist > enemyMaxDistance)
                        { enemyMaxDistance = dist; }
                    }

                    // Relay entity to WHIP radar
                    if (IsRadarUpdateTime())
                    {
                        var entityTuple = ToTuple(info);
                        IGC.SendBroadcastMessage(IGC_TAG, entityTuple);
                    }
                }

                // If no entities detected, set to default
                if (radarMaxDistance == 0)
                { radarMaxDistance = defaultRadarRangeMeters; }
                else
                {
                    if (enemyDetected && enemyMaxDistance != 0)
                    { radarMaxDistance = enemyMaxDistance; }
                    radarMaxDistance = (int)LimitMin(radarMaxDistance / 1000, 1) * 1000;
                }

                // Sort and Clean-up
                int targetIndex = 1;
                int genericTargetIndex = 5000;
                targets = targets.OrderByDescending(t => t.Value.Threat).ToDictionary(t => t.Key, t => t.Value);
                foreach (var key in targets.Keys.ToArray())
                {
                    if (!entityIds.Contains(key)
                    ||  !IsValidTarget(targets[key].Info, targets[key].Threat)
                    || blackListedIds.Contains(key))
                    { targets.Remove(key); }
                    else
                    {
                        if (HasGenericName(targets[key].Info.Name))
                        { targets[key].ListIndex = genericTargetIndex++; }
                        else
                        { targets[key].ListIndex = targetIndex++;  }
                    }
                }
                targets = targets.OrderBy(t => t.Value.ListIndex).ToDictionary(t => t.Key, t => t.Value);

                // Display Targets
                foreach (var value in targets.Values.ToArray())
                {
                    targetList.AppendLine($"[{index++}] {GetTargetType(value.Info)}  {value.Info.Name}");
                }
            }
            else
            {
                isSetup = false;
                issues.AppendLine("No WeaponCore weapon installed.");
            }
        }

        private void EngageTargets()
        {
            FreeFireTarget();

            ManualFireTargets();

            RemoteFireTarget();

            AutoFireMissiles();

            AutoFireTargets();
        }

        private void AutoFireMissiles()
        {
            if (!CanFireAtMissile()) { return; }
            foreach (var weapon in wcWeapons)
            {
                // See if our weapon is ready to fire
                if (!wc.IsWeaponReadyToFire(weapon)) { continue; }

                // This function should only fire at missiles
                MyDetectedEntityInfo? info = wc.GetWeaponTarget(weapon);
                if (!info.HasValue) { continue; }
                if (info.Value.Type != MyDetectedEntityType.Missile) { continue; }

                // Missile is detected, fire at it!
                wc.FireWeaponOnce(weapon, false);
                // Just fired, reset the counter
                currMissileAutoFireTicks = 0;
            }
        }

        private void AutoFireTargets()
        {
            if (!CanAutoFire()) { return; }

            // Queue new targets
            bool allAttacked = true;
            foreach (var id in targets.Keys)
            {
                if (targets[id].EngageCount < 1)
                { allAttacked = false; }

                if (!targetQueue.Contains(id))
                {
                    targetQueue.Enqueue(id);
                }
            }

            // Select a target (if possible)
            if (targetQueue.Count > 0)
            {
                long id = targetQueue.Peek();
                if (targets.ContainsKey(id))
                {
                    var target = targets[targetQueue.Dequeue()];

                    if (masterModeOn)
                    {
                        // Remote autofire (slaves launch)
                        long remoteId = GetNextRemoteLauncher();
                        if (remoteId > 0)
                        {
                            if (targets[id].EngageCount < 1 || allAttacked)
                            {
                                if (target.ShootTime == targetFireMaxTicks)
                                {
                                    var msg = RemoteMessage.GetRemoteFire(target.Info.EntityId, false);
                                    network.Unicast(remoteId, msg);
                                }
                            }
                        }
                    }
                    else
                    {
                        // Normal Autofire
                        IMyTerminalBlock weapon = GetNextWeapon(target.Info);
                        if (weapon != null)
                        {
                            if (targets[id].EngageCount < 1 || allAttacked)
                            {
                                if (target.ShootTime == targetFireMaxTicks)
                                {
                                    wc.SetWeaponTarget(weapon, target.Info.EntityId);
                                    wc.SetAiFocus(weapon, target.Info.EntityId);
                                    MyDetectedEntityInfo? targetLocked = wc.GetWeaponTarget(weapon);
                                    if (targetLocked != null)
                                    {
                                        wc.FireWeaponOnce(weapon, false);
                                        target.ShootTime = 0;
                                        target.EngageCount++;
                                        if (target.blackListVote > 0)
                                        { target.blackListVote--; }
                                    }
                                    else
                                    {
                                        // When there's too many failed attempts to lock at
                                        // a target, black list it to may room for other targets
                                        // (refreshes every 3,600 ticks / 1 game min)
                                        target.blackListVote++;
                                        if(target.blackListVote >= wcWeapons.Count * 2)
                                        { blackListedIds.Add(target.Info.EntityId); }
                                    }
                                }
                            }
                        }
                    }

                    targetQueue.Enqueue(id);
                }
                else
                {
                    targetQueue.Dequeue();
                }

                // Reset the engage count to redistribute missiles
                if (allAttacked)
                {
                    foreach (var entity in targets.Values)
                    {
                        entity.EngageCount = 0;
                    }
                }
            }
        }

        private void ManualFireTargets()
        {
            if (manualTargetId > 0 && manualTargetId <= targets.Count)
            {
                targetEntity = targets.Values.ElementAt(manualTargetId - 1).Info;

                if (manualWeapon == null) { manualWeapon = GetNextWeapon(targetEntity); }
                if (masterModeOn) { manualWeapon = wcWeapons[0]; }

                if (manualWeapon != null)
                {
                    if (hasPendingFireRequest)
                    {
                        if(masterModeOn)
                        {
                            long remoteId = GetNextRemoteLauncher();
                            if(remoteId > 0)
                            {
                                var data = RemoteMessage.GetRemoteFire(targetEntity.EntityId, true);
                                network.Unicast(remoteId, data);
                            }
                        }
                        else
                        {
                            wc.FireWeaponOnce(manualWeapon, false);
                        }

                        hasPendingFireRequest = false;
                        manualWeapon = null;
                        manualTargetId = 0;
                        targetEntity = new MyDetectedEntityInfo();
                    }
                    else
                    {
                        wc.SetWeaponTarget(manualWeapon, targetEntity.EntityId);
                        wc.SetAiFocus(manualWeapon, targetEntity.EntityId);
                    }
                }
            }
        }

        private void FreeFireTarget()
        {
            if (hasPendingFreeFireRequest)
            {
                targetEntity = wc.GetAiFocus(Me.CubeGrid.EntityId) ?? new MyDetectedEntityInfo();
                IMyTerminalBlock weapon = GetNextWeapon(targetEntity);
                if (masterModeOn) { weapon = wcWeapons[0]; }

                if (!targetEntity.IsEmpty() && weapon != null)
                {
                    wc.SetWeaponTarget(weapon, targetEntity.EntityId);
                    wc.SetAiFocus(weapon, targetEntity.EntityId);
                    
                    if(masterModeOn)
                    {
                        // Remote manual fire
                        long remoteId = GetNextRemoteLauncher();
                        if (!targetEntity.IsEmpty() && remoteId > 0)
                        {
                            var msg = RemoteMessage.GetRemoteFire(targetEntity.EntityId, true);
                            network.Unicast(remoteId, msg);
                        }
                    }
                    else
                    {
                        wc.FireWeaponOnce(weapon, false);
                    }

                    hasPendingFreeFireRequest = false;
                }
            }
        }

        private void RemoteFireTarget()
        {
            if (hasPendingRemoteFireRequest)
            {
                HostileEntity remoteTarget = targets.FirstOrDefault
                                             (t => t.Value.Info.EntityId == remoteFireTargetId).Value;
                if (remoteTarget != null)
                {
                    IMyTerminalBlock weapon = GetNextWeapon(remoteTarget.Info);
                    if (weapon != null)
                    {
                        wc.SetWeaponTarget(weapon, remoteFireTargetId);
                        wc.SetAiFocus(weapon, remoteFireTargetId);
                        wc.FireWeaponOnce(weapon, false);

                        if(!remoteManualFire)
                        {
                            var msg = RemoteMessage.GetRemoteFireConfirm(remoteFireTargetId);
                            network.Unicast(remoteFireSender, msg);
                        }

                        // Reset
                        hasPendingRemoteFireRequest = false;
                        remoteFireSender = 0;
                        remoteFireTargetId = 0;
                        remoteManualFire = false;
                    }
                }
                else
                {
                    // Reset
                    hasPendingRemoteFireRequest = false;
                    remoteFireSender = 0;
                    remoteFireTargetId = 0;
                    remoteManualFire = false;
                }
            }
        }

        private void AimAtTarget()
        {
            bool invalidTurret = (aimReference == null
                                  || (azimuthRotor == null
                                  && elevationRotor == null));

            if (!activateTurret || invalidTurret) { return; }

            turretTarget = new MyDetectedEntityInfo();

            // When slaved, aim at what is assigned to us as target
            if (slaveModeOn && targets.ContainsKey(remoteFireTargetId))
            { turretTarget = targets[remoteFireTargetId].Info; }
            else
            {
                // We only aim at the highest threat target
                if (targets.Count > 0)
                { turretTarget = targets.Values.First().Info; }
            }

            if (targets.Count == 0
            ||  turretTarget.IsEmpty())
            {
                if (resetTurret)
                {
                    if (azimuthRotor != null && azimuthRotor.CustomData != string.Empty && azimuthRotor.Angle != 0)
                    { azimuthRotor.TargetVelocityRad = (float)azPID.Update(MathHelper.ToRadians(azimuthRotor.Angle)); }
                    if (elevationRotor != null && elevationRotor.CustomData != string.Empty && elevationRotor.Angle != 0)
                    { elevationRotor.TargetVelocityRad = (float)elPID.Update(MathHelper.ToRadians(elevationRotor.Angle)); }

                    if((azimuthRotor != null && elevationRotor != null && azimuthRotor.Angle == 0 && elevationRotor.Angle == 0)
                    || (azimuthRotor != null && azimuthRotor.Angle == 0)
                    || (elevationRotor != null && elevationRotor.Angle == 0))
                    { resetTurret = false; }
                }
                return;
            }

            resetTurret = true;
            Vector3D aimReferencePos = aimReference.GetPosition();
            Vector3D currentForwardVector = aimReference.WorldMatrix.Forward;
            Vector3D currentUpVector = aimReference.WorldMatrix.Up;
            Vector3D currentLeftVector = aimReference.WorldMatrix.Left;
            Vector3D targetDirection = Vector3D.Normalize(turretTarget.Position - aimReferencePos);

            if (elevationRotor != null && elevationRotor.IsFunctional && elevationRotor.IsWorking)
            {
                // Calculate pitch error
                double pitchError = Math.Acos(MathHelper.Clamp(targetDirection.Dot(currentUpVector) / targetDirection.Length(), -1, 1)) - Math.PI / 2;
                elevationRotor.TargetVelocityRad = MathHelper.Clamp((float)elPID.Update(-pitchError), -1.256f, 1.256f);
            }

            if (azimuthRotor != null && azimuthRotor.IsFunctional && azimuthRotor.IsWorking)
            {
                // Calculate yaw error
                Vector3D targetRelativeLeftVec = currentUpVector.Cross(targetDirection);
                double yawError = VectorAngleBetween(currentLeftVector, targetRelativeLeftVec);
                yawError *= VectorCompareDirection(VectorProjection(currentLeftVector, targetDirection), targetDirection);
                azimuthRotor.TargetVelocityRad = MathHelper.Clamp((float)azPID.Update(yawError), -1.256f, 1.256f);
            }
        }

        private void CheckForLockOn()
        {
            MyTuple<bool, int, int> lockedStatus = wc.GetProjectilesLockedOn(Me.CubeGrid.EntityId);
            if (lockedStatus.Item1 || lockedStatus.Item2 > 0)
            {
                isBeingLockedOn = true;
                isBeingFiredOn = false;

                if (currLockOnWarningTicks == lockOnWarningTicks && !isDispensing)
                {
                    if(lockedStatus.Item2 > 0)
                    {
                        isBeingFiredOn = true;
                        PlaySound(missileLaunchSound);
                    }
                    else
                    {
                        PlaySound(radarLockSound);
                    }
                    currLockOnWarningTicks = 0;
                }
            }
            else
            {
                if (isBeingLockedOn)
                {
                    StopSounds();
                    isBeingLockedOn = false;
                    isBeingFiredOn = false;
                }
            }
        }

        private void CheckForMissiles()
        {
            if (ProjectileDetected() && isBeingFiredOn)
            {
                ShootFlares(wcFlares, true);
            }
            else
            {
                ShootFlares(wcFlares, false);
            }
        }

        private bool ProjectileDetected()
        {
            foreach (IMyTerminalBlock sensor in wcMAWS)
            {
                MyDetectedEntityInfo? info = wc.GetWeaponTarget(sensor);
                if (info.HasValue)
                {
                    MyDetectedEntityInfo ent = info.Value;
                    if (ent.Type == MyDetectedEntityType.Missile)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void ScanTarget()
        {
            if (scanCamera == null || logBlock == null) { return; }

            if (!scanCamera.CanScan(scanDistance)) { return; }
            MyDetectedEntityInfo info = scanCamera.Raycast(scanDistance);

            if (info.IsEmpty()) { return; }

            if (!info.HitPosition.HasValue) { return; }

            string name = "Scan Point";
            if (entityIds.Contains(info.EntityId))
            {
                MyDetectedEntityInfo entity = entities.Keys.ToList().Find(e => e.EntityId == info.EntityId);
                if (!entity.IsEmpty()) { name = entity.Name + " Point"; }
            }

            string gpsString = string.Empty;
            if (TryParseToGpsFormat(info.HitPosition.Value.ToString(), name, out gpsString))
            {
                StringBuilder sb = new StringBuilder(logBlock.CustomData);
                sb.AppendLine(gpsString);
                logBlock.CustomData = sb.ToString();
                PlaySound("SoundBlockAlert2");
            }
            else
            {
                scanProb += $"Error Parsing This {info.HitPosition.Value.ToString()}\n";
            }
        }

        private void ShootFlares(List<IMyTerminalBlock> blocks, bool shoot)
        {
            if (shoot && !isDispensing)
            {
                foreach (IMyTerminalBlock block in blocks)
                {
                    wc.ToggleWeaponFire(block, true, false);
                }

                isDispensing = true;
            }

            if (!shoot && isDispensing)
            {
                foreach (IMyTerminalBlock block in blocks)
                {
                    wc.ToggleWeaponFire(block, false, false);
                }

                isDispensing = false;
            }
        }

        private void PlaySound(string soundName)
        {
            bool searched = false;
            bool isValidSound = false;
            foreach (var soundBlock in wcSounds)
            {
                if (!searched)
                {
                    if (sounds.Count == 0)
                    {
                        soundBlock.GetSounds(sounds);
                    }

                    isValidSound = sounds.Contains(soundName);
                    searched = true;
                }
                if (isValidSound)
                {
                    soundBlock.SelectedSound = soundName;
                    soundBlock.LoopPeriod = 2;
                    soundBlock.Play();
                }
                else
                {
                    break;
                }
            }
        }

        private void StopSounds()
        {
            foreach (var soundBlock in wcSounds)
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

        private string GetTargetType(MyDetectedEntityInfo info)
        {
            switch (info.Type)
            {
                case MyDetectedEntityType.SmallGrid: return "S";
                case MyDetectedEntityType.LargeGrid: return "L";
                case MyDetectedEntityType.CharacterHuman: return "C";
                case MyDetectedEntityType.Missile: return "M";
                default: return "U";
            }
        }

        private bool IsSetupTime()
        {
            if (currSetupTime >= SETUP_TIME)
            {
                currSetupTime = 0;
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsSettingRefreshTime()
        {
            if (currSettingsTicks >= SETTINGS_UPDATE_TICKS)
            {
                currSettingsTicks = 0;
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsRadarRangeUpdateTime()
        {
            if (currRadarRangeTicks >= RADAR_RANGE_TICKS)
            {
                currRadarRangeTicks = 0;
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsRadarUpdateTime()
        {
            if (currRadarUpdateTicks >= RADAR_UPDATE_TICKS)
            {
                currRadarUpdateTicks = 0;
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsMasterPingTime()
        {
            if (currMasterPingTicks >= MAX_MASTER_PING_TICKS)
            {
                currMasterPingTicks = 0;
                return true;
            }
            else
            {
                return false;
            }
        }

        private void UpdateTickTimers()
        {
            if (currLockOnWarningTicks < lockOnWarningTicks)
            {
                currLockOnWarningTicks++;
            }

            if (currGlobalFireTicks < globalFireMaxTicks)
            {
                currGlobalFireTicks++;
            }

            if (currSettingsTicks < SETTINGS_UPDATE_TICKS)
            {
                currSettingsTicks++;
            }

            if (currSetupTime < SETUP_TIME)
            {
                currSetupTime++;
            }

            if (currRadarRangeTicks < RADAR_RANGE_TICKS)
            {
                currRadarRangeTicks++;
            }

            if (currRadarUpdateTicks < RADAR_UPDATE_TICKS)
            {
                currRadarUpdateTicks++;
            }

            if (currMasterPingTicks < MAX_MASTER_PING_TICKS)
            {
                currMasterPingTicks++;
            }

            if(currMissileAutoFireTicks < counterMissileFireMaxTicks)
            {
                currMasterPingTicks++;
            }

            if(currBlacklistTicks < blacklistMaxTicks && blackListedIds.Count > 0)
            {
                currBlacklistTicks++;
            }

            foreach (var value in targets.Values)
            {
                if (value.ShootTime < targetFireMaxTicks)
                {
                    value.ShootTime++;
                }
            }
        }

        private bool CanAutoFire()
        {
            if (autoFire && currGlobalFireTicks == globalFireMaxTicks)
            {
                currGlobalFireTicks = 0;
                return true;
            }

            return false;
        }

        private bool CanFireAtMissile()
        {
            if(autoFire && currMissileAutoFireTicks == counterMissileFireMaxTicks)
            {
                return true;
            }
            return false;
        }

        private bool IsEnemy(MyDetectedEntityInfo info)
        {
            switch(info.Relationship)
            {
                case MyRelationsBetweenPlayerAndBlock.Owner:        return false;
                case MyRelationsBetweenPlayerAndBlock.Friends:      return false;
                case MyRelationsBetweenPlayerAndBlock.FactionShare: return false;
                case MyRelationsBetweenPlayerAndBlock.Neutral:      return false;
                default: return true;
            }
        }
        
        private bool IsValidEntity(MyDetectedEntityInfo info)
        {
            // Check entity type
            switch (info.Type)
            {
                case MyDetectedEntityType.CharacterHuman:
                case MyDetectedEntityType.SmallGrid:
                case MyDetectedEntityType.LargeGrid:
                case MyDetectedEntityType.Missile: break;
                default: return false;
            }

            // Entity must be within minimum target size
            return (info.BoundingBox.Size.LengthSquared() >= (minimumEntitySize * minimumEntitySize));
        }

        private bool IsValidTarget(MyDetectedEntityInfo info, float threat)
        {
            // ENEMY
            if(IsEnemy(info) && threat > 0)
            {
                return true;
            }

            return false;
        }

        private float CalculateThreat(MyDetectedEntityInfo info, double dist, float wcThreat)
        {
            float threat = 0;
            if(IsEnemy(info))
            {
                // Get the final wcThreat (If NaN, set to minimum threat)
                float finalWcThreat = wc.GetMinimumOffRat(1);
                if (!float.IsNaN(wcThreat)) { finalWcThreat = (float)LimitMin(wcThreat, 0); }

                // Zero threat still seems reliable so we trust it
                if(finalWcThreat == 0)
                { return threat; }

                // The closer it is, the higher the threat. Based on highest weapon range
                float maxRange = (float)LimitMin(wc.GetMaxWeaponRange(wcWeapons.Last(), 0), 1.0);
                float entityDist = (float)LimitMin(dist, 1.0);
                float distThreat = (float)(LimitMin(maxRange - dist, 1.0) / maxRange);

                // The faster the entity is moving, the higher the threat
                float approachThreat = 0;
                float entityVel = info.Velocity.Length();
                if (entityVel > 0)
                {
                    approachThreat = (float)(entityDist / entityVel);
                    approachThreat = (float)((entityDist - approachThreat) / entityDist);
                    // Approaching enities get higher threat levels
                    approachThreat *= (isApproaching(info) ? 1.0f : 0.5f);
                }

                // Get the average threat
                threat = (finalWcThreat + distThreat + approachThreat) / 3;
            }
            return threat;
        }

        private bool isApproaching(MyDetectedEntityInfo info)
        {
            // Calculate the vector from your ship to the other ship
            Vector3D relVect = info.Position - Me.GetPosition();
            // Normalize the direction vector of the other ship
            Vector3D normVect = info.Velocity.Normalized();
            // Calculate the dot product between the relative vector and
            // the normalized other ship's direction
            double dotProd = Vector3D.Dot(relVect, normVect);
            // If the dot product is positive, the other ship is approaching
            return dotProd > 0;
        }

        private bool HasGenericName(string name)
        {
            if (name.ToLowerInvariant().StartsWith("small grid")
            || name.ToLowerInvariant().StartsWith("large grid"))
            {
                return true;
            }
            return false;
        }

        private bool TryParseToGpsFormat(string vectorFormat, string gpsName, out string gpsFormat)
        {
            bool result = true;
            gpsFormat = string.Empty;
            try
            {
                vectorFormat = vectorFormat.Replace(" ", ":");
                vectorFormat = vectorFormat.Replace("}", "");
                string[] sections = vectorFormat.Split(':');
                double xVal = Math.Round(double.Parse(sections[1].Trim()), 6);
                double yVal = Math.Round(double.Parse(sections[3].Trim()), 6);
                double zVal = Math.Round(double.Parse(sections[5].Trim()), 6);
                gpsFormat = string.Format("GPS:{0}:{1}:{2}:{3}:", gpsName, xVal, yVal, zVal);
            }
            catch
            {
                result = false;
            }
            return result;
        }

        private IMyTerminalBlock GetNextWeapon(MyDetectedEntityInfo info)
        {
            foreach (var weapon in wcWeapons)
            {
                // Skip detection only weapons like radars
                if(weapon.CustomName.ToLowerInvariant().Contains(weaponIgnoreTag.ToLowerInvariant()))
                { continue; }

                if (weapon.IsFunctional
                 && wc.IsWeaponReadyToFire(weapon, shootReady: true)
                 && wc.IsTargetValid(weapon, info.EntityId, false, true)
                 && isInit)
                {
                    double range = wc.GetMaxWeaponRange(weapon, 0);
                    double distanceSq = (info.BoundingBox.Center - Me.GetPosition()).LengthSquared();
                    if (distanceSq <= (range * range))
                    {
                        wc.ReleaseAiFocus(weapon, weapon.OwnerId);
                        weapon.ApplyAction("OnOff_Off");
                        weapon.ApplyAction("OnOff_On");
                        return weapon;
                    }
                }
            }

            return null;
        }

        private long GetNextRemoteLauncher()
        {
            long remoteId = 0;
            for (int i = 0; i < receiverQueue.Count; i++)
            {
                long id = receiverQueue.Dequeue();
                if(network.IsReachableReceiver(id))
                {
                    remoteId = id;
                    receiverQueue.Enqueue(id);
                }
                break;
            }
            return remoteId;
        }

        private MyTuple<byte, long, Vector3D, double> ToTuple(MyDetectedEntityInfo entity)
        {
            return new MyTuple<byte, long, Vector3D, double>((byte)GetRelationship(entity), entity.EntityId, entity.Position, 0);
        }

        private HostileEntity ToHostileEntity(MyDetectedEntityInfo entity, float threat)
        {
            HostileEntity hostileEntity = new HostileEntity();
            hostileEntity.Info = entity;
            hostileEntity.Threat = threat;
            hostileEntity.EngageCount = 0;
            hostileEntity.ShootTime = targetFireMaxTicks;
            hostileEntity.Type = entity.Type;
            return hostileEntity;
        }

        private double LimitMin(double value, double min)
        {
            if (value < min)
            { return min; }
            else
            { return value; }
        }

        #region ===== Whiplash141 Functions =====

        void ApplyGyroOverride(double pitchSpeed, double yawSpeed, double rollSpeed, List<IMyGyro> gyroList, MatrixD worldMatrix)
        {
            var rotationVec = new Vector3D(pitchSpeed, yawSpeed, rollSpeed);
            var relativeRotationVec = Vector3D.TransformNormal(rotationVec, worldMatrix);

            foreach (var thisGyro in gyroList)
            {
                var transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(thisGyro.WorldMatrix));

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

        #endregion ===== Whiplash141 Functions =====
    }
}
