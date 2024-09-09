// Original script from Klime
// v240802 - [Khjin] Modified to reposition bombs to the correct spot when firing

using System;
using System.Collections.Generic;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace khjin.vanillaex.MissilesReposition
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class MissilesReposition : MySessionComponentBase
    {
        Dictionary<string, Vector4D> offsets = new Dictionary<string, Vector4D>();

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                // Subtype ID | offset-x | offset-y | offset-z | infantry kill radius
                offsets.Add("KJN_KWP_BMB_an_m30A1_100lb_block", new Vector4D(0, 0, -0.5, 5));
                offsets.Add("KJN_KWP_BMB_an_m57A1_250lb_block", new Vector4D(0, 0, -0.5, 16));
                offsets.Add("KJN_KWP_BMB_an_m64A1_500lb_block", new Vector4D(0, 0, -0.5, 36));
                offsets.Add("KJN_KWP_BMB_an_m65A1_1000lb_block", new Vector4D(0, 0, -0.5, 50));
                offsets.Add("KJN_KWP_BMB_an_m66A1_2000lb_block", new Vector4D(0, 0, -1.0, 90));
                offsets.Add("KJN_KWP_ROC_hvar_block", new Vector4D(0, 0, -1.0, 5));
            }
        }

        public override void BeforeStart()
        {
            if (MyAPIGateway.Session.IsServer)
            {
                MyAPIGateway.Missiles.OnMissileAdded += MissileAdded;
                MyAPIGateway.Missiles.OnMissileRemoved += MissileRemoved;
            }
        }

        protected override void UnloadData()
        {
            if (MyAPIGateway.Session.IsServer)
            {
                MyAPIGateway.Missiles.OnMissileAdded -= MissileAdded;
                MyAPIGateway.Missiles.OnMissileCollided -= MissileRemoved;
            }
        }

        private void MissileAdded(IMyMissile missile)
        {
            if(missile.AmmoDefinition.Id.SubtypeName.ToLowerInvariant().EndsWith("_ammo"))
            {
                MyEntity entity = MyEntities.GetEntityById(missile.LauncherId);
                IMySmallMissileLauncher missileLauncher = entity as IMySmallMissileLauncher;

                if (missileLauncher != null && missileLauncher.CubeGrid.Physics != null)
                {
                    if (offsets.ContainsKey(missileLauncher.BlockDefinition.SubtypeId))
                    {
                        // Grab the offsets
                        Vector4D values = offsets[missileLauncher.BlockDefinition.SubtypeId];
                        // Reset to position of launcher
                        Vector3D origVel = missile.LinearVelocity;
                        Vector3D launcherPos = missileLauncher.GetPosition();
                        Vector3D launcherFWD = Vector3D.Normalize(missileLauncher.WorldMatrix.Forward);
                        Vector3D launcherUP = Vector3D.Normalize(missileLauncher.WorldMatrix.Up);
                        Vector3D launcherLEF = Vector3D.Normalize(missileLauncher.WorldMatrix.Left);
                        // Calculate new position
                        Vector3D newPos = launcherPos + (launcherFWD * values.Y);
                        newPos = newPos + (launcherUP * values.Z);
                        newPos = newPos + (launcherLEF * values.X);
                        // Set to new position
                        missile.SetPosition(newPos);
                        // Follow original linear velocity
                        missile.LinearVelocity = origVel;
                    }
                }
            }
        }

        private void MissileRemoved(IMyMissile missile)
        {
            string key = GetKey(missile);
            if (offsets.ContainsKey(key))
            {
                // Generate damaging blast waves
                Vector3D overPressureOrigin = missile.GetPosition();
                double overPressureRange = offsets[key].W;

                BoundingSphereD innerArea = new BoundingSphereD(overPressureOrigin, overPressureRange * 0.5);
                BoundingSphereD outerArea = new BoundingSphereD(overPressureOrigin, overPressureRange);

                MyExplosionInfo innerExpl = new MyExplosionInfo(50, 0, innerArea, MyExplosionTypeEnum.BOMB_EXPLOSION, false);
                MyExplosionInfo outerExpl = new MyExplosionInfo(50, 0, outerArea, MyExplosionTypeEnum.BOMB_EXPLOSION, false);

                MyExplosions.AddExplosion(ref innerExpl);
                MyExplosions.AddExplosion(ref outerExpl);
            }
        }

        private string GetKey(IMyMissile missile)
        {
            string subtypeId = missile.AmmoDefinition.Id.SubtypeName;
            string key = subtypeId.Replace("_ammo", "_block");
            return key;
        }

        private void ChatMessage(string message)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                MyVisualScriptLogicProvider.SendChatMessage($"SERVER: {message}");
            }
            else
            {
                MyAPIGateway.Utilities.ShowNotification($"CLIENT: {message}", 5000, "White");
            }
        }
    }
}