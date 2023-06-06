using BepInEx;
using Biography;
using Biography.BiographyMenu;
using Biography.SimGameCore;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618
[BepInPlugin(ModID, "Biography", "1.0.0")]
public class BiographyPlugin : BaseUnityPlugin
{
    const string ModID = "harvie.biography";
    public static bool UsingSandboxUnlock
    {
        get
        {
            if (BiographyConfig.UnlockAll != null)
                return !BiographyConfig.UnlockAll.Value;
            return true;
        }
    }

    void OnEnable()
    {
        On.RainWorld.OnModsInit += RainWorld_OnModsInit;
    }

    private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig.Invoke(self);
        try
        {
            BiographyMenuHooks.HookOn();
            SimGameHook.HookOn();
            ProcessManagerExHooks.HookOn(self);
            SpriteLeaserWarpperHooks.HookOn();
            InGameTrasnlatorHook.HookOn();

            BiographyMenu.AddTypeToBlackList(new CreatureTemplate.Type("LaserDrone"));
            MachineConnector.SetRegisteredOI(ModID, new BiographyConfig());

            On.Room.Update += Room_Update;

            InGameTrasnlatorHook.LoadResource();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    private void Room_Update(On.Room.orig_Update orig, Room self)
    {
        orig.Invoke(self);
        if (Input.GetKeyDown(KeyCode.C))
        {
            var addedCreature = new AbstractCreature(self.world, StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.SlugNPC), null, new WorldCoordinate(self.abstractRoom.index, 20, 10, -1), new EntityID(-1,1000));
            self.abstractRoom.AddEntity(addedCreature);
            addedCreature.RealizeInRoom();
        }
    }

    public static void Log(object obj)
    {
        Log($"{obj}");
    }

    public static void Log(string msg)
    {
        Debug.Log($"[Biography]{msg}");
    }

    public static void Log(string pattern, params object[] vars)
    {
        Debug.Log($"[Biography]" + string.Format(pattern, vars));
    }
}