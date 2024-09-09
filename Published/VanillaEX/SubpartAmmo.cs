// Original script from Klime
// v240802 - [Khjin] Added null guard and clean-up, updated logic
using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace khjin.vanillaex.SubpartAmmo
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallMissileLauncher), false,
        "KJN_KWP_BMB_an_m30A1_100lb_block",
        "KJN_KWP_BMB_an_m57A1_250lb_block",
        "KJN_KWP_BMB_an_m64A1_500lb_block",
        "KJN_KWP_BMB_an_m65A1_1000lb_block",
        "KJN_KWP_BMB_an_m66A1_2000lb_block",
        "KJN_KWP_ROC_hvar_block"
    )]
    public class SubpartAmmo : MyGameLogicComponent
    {
        IMySmallMissileLauncher launcher;
        IMyMissileGunObject launcherObj; //Required for CanShoot
        List<MyEntitySubpart> subparts = new List<MyEntitySubpart>();
        bool subpartVis = true; //Optimization

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            launcher = Entity as IMySmallMissileLauncher;
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (launcher.CubeGrid.Physics != null)
            {
                //Get subparts
                if (launcher.Components.Has<MyHierarchyComponentBase>())
                {
                    var comp = launcher.Components.Get<MyHierarchyComponentBase>();
                    HashSet<IMyEntity> result = new HashSet<IMyEntity>();
                    comp.GetChildrenRecursive(result);
                    foreach (var child in result)
                    {
                        MyEntitySubpart subpart = child as MyEntitySubpart;
                        if (subpart != null)
                        {
                            subparts.Add(subpart);
                        }
                    }
                }

                launcherObj = launcher as IMyMissileGunObject;
                NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
            }
        }

        public override void UpdateAfterSimulation10()
        {
            MyGunStatusEnum status;
            launcherObj.CanShoot(MyShootActionEnum.PrimaryAction, 0, out status);

            if (status == MyGunStatusEnum.Reloading
            ||  status == MyGunStatusEnum.OutOfAmmo)
            {
                if (subpartVis)
                {
                    for (int i = (subparts.Count - 1); i >= 0; i--)
                    {
                        MyEntitySubpart subpart = subparts[i];
                        if (subpart != null)
                        { subpart.Render.Visible = false; }
                        else
                        { subparts.Remove(subpart); }
                    }
                    subpartVis = false;
                }
            }
            else
            {
                if (!subpartVis)
                {
                    for (int i = (subparts.Count - 1); i >= 0; i--)
                    {
                        MyEntitySubpart subpart = subparts[i];
                        if (subpart != null)
                        { subpart.Render.Visible = true; }
                        else
                        { subparts.Remove(subpart); }
                    }
                    subpartVis = true;
                }
            }
        }

        public override void Close()
        {

        }
    }
}