using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Policy;
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
        public class RemoteMessage
        {
            public const int MASTER_PING = 1;
            public const int SLAVE_REGISTER = 2;
            public const int REMOTE_FIRE = 3;
            public const int REMOTE_FIRE_CONFIRM = 4;

            public static MyTuple<int, string> GetMasterPingMsg()
            {
                return new MyTuple<int, string>(MASTER_PING, 
                                                PASSKEY);
            }

            public static MyTuple<int, string> GetRegisterMsg(long entityId)
            {
                return new MyTuple<int, string>(SLAVE_REGISTER, 
                                                PASSKEY);
            }

            public static MyTuple<int, string, long, bool> GetRemoteFire(long targetId, bool remoteManualFire)
            {
                return new MyTuple<int, string, long, bool>(REMOTE_FIRE, 
                                                      PASSKEY,
                                                      targetId,
                                                      remoteManualFire);
            }

            public static MyTuple<int, string, long> GetRemoteFireConfirm(long targetId)
            {
                return new MyTuple<int, string, long>(REMOTE_FIRE_CONFIRM, 
                                                      PASSKEY,
                                                      targetId);
            }
        }
    }
}
