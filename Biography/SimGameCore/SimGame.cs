using Biography.BiographyMenu;
using RWCustom;
using Menu.Remix.MixedUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;
using MoreSlugcats;

namespace Biography.SimGameCore
{
    public class SimGame : RainWorldGame
    {
        public static Vector2 CreatureDisplayCenter = new Vector2(60f,320f);
        public static Vector2 CreatureDisplayWindowSize = new Vector2(300f, 200f);

        public static IntVector2 waterCreaturePos = new IntVector2(102, 20);
        public static IntVector2 airCreaturePos = new IntVector2(31, 90);

        public bool anyCreatureAdded = false;
        public AbstractCreature addedCreature;
        public Room Room => world.activeRooms[0];
        public static List<FContainer> banFContainers = new List<FContainer>();
        public static bool LockSpriteAdd = true;
        public static bool SimGameLoaded = false;
        public SimGame(ProcessManager manager) : base(manager)
        {
            this.ID = ProcessManager.ProcessID.Game;
            this.manager = manager;

            framesPerSecond = 1;
            this.myTimeStacker = 0f;
            BiographyPlugin.Log("SimGame ctor");
            this.rainWorld = manager.rainWorld;
            this.startingRoom = "biography";

            this.nextIssuedId = Random.Range(1000, 10000);
            this.shortcuts = new ShortcutHandler(this);
            this.globalRain = new GlobalRain(this);

            this.cameras = new RoomCamera[1];
            cameras[0] = new RoomCamera(this, 0);
            this.grafUpdateMenus = new List<Menu.Menu>();
            this.wasAnArtificerDream = false;

            BiographyPlugin.Log("SimGame : start session ctor");
            try
            {
                this.session = new SandboxGameSession(this);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            BiographyPlugin.Log("SimGame : start overworld ctor");
            this.overWorld = new OverWorld(this);
            world.game = this;
            if (this.world.singleRoomWorld)
            {
                DefaultRoomSettings.ancestor.pal = new int?(this.setupValues.palette);
            }
            if (this.IsArenaSession)
            {
                BiographyPlugin.Log("SimGame : IsArenaSession");
            }

            this.pathfinderResourceDivider = new PathfinderResourceDivider(this);

            int num = 0;
            if (this.world.GetAbstractRoom(this.overWorld.FIRSTROOM) != null)
            {
                num = this.world.GetAbstractRoom(this.overWorld.FIRSTROOM).index;
            }
            if (!this.world.IsRoomInRegion(num))
            {
                num = this.world.region.firstRoomIndex;
            }
            world.ActivateRoom(num);
            for (int camIndex = 0; camIndex < this.cameras.Length; camIndex++)
            {
                this.cameras[camIndex].MoveCamera(this.world.activeRooms[0], 0);
            }
            SimGameLoaded = true;
        }

        public override void Update()
        {
            QuickConnectivity.ResetFrameIterationQuota();
            //base.Update();

            //BiographyPlugin.Log("SimWorld : Update 1");
            if (this.IsArenaSession)
            {
                this.GetArenaGameSession.Update();
            }
            globalRain.Intensity = 0f;
            this.world.rainCycle.timer = 0;
            //if (this.pauseMenu != null)
            //{
            //    this.pauseMenu.Update();
            //}
            //if (this.GamePaused)
            //{
            //    for (int i = 0; i < this.cameras.Length; i++)
            //    {
            //        if (this.cameras[i].hud != null)
            //        {
            //            this.cameras[i].hud.Update();
            //        }
            //    }
            //    for (int j = this.world.activeRooms.Count - 1; j >= 0; j--)
            //    {
            //        this.world.activeRooms[j].PausedUpdate();
            //    }
            //    return;
            //}
            //if (!this.processActive)
            //{
            //    return;
            //}
            for (int k = 0; k < this.cameras.Length; k++)
            {
                this.cameras[k].Update();
            }
            this.clock++;
            this.evenUpdate = !this.evenUpdate;

            //BiographyPlugin.Log("SimWorld : Update 2");
            if (this.roomRealizer != null)
            {
                this.roomRealizer.Update();
            }
            if (this.AV != null)
            {
                this.AV.Update();
            }
            //BiographyPlugin.Log("SimWorld : Update 3");
            //if (this.mapVisible)
            //{
            //    this.abstractSpaceVisualizer.Update();
            //}
            //if (this.abstractSpaceVisualizer.room != this.cameras[0].room && this.cameras[0].room != null)
            //{
            //    this.abstractSpaceVisualizer.ChangeRoom(this.cameras[0].room);
            //}
            this.world.GetAbstractRoom(0).Update(1);
            if (this.world.rainCycle.timer > 100)
            {
                this.world.offScreenDen.Update(1);
            }
            //BiographyPlugin.Log("SimWorld : Update 4");
            for (int l = 0; l < this.world.worldProcesses.Count; l++)
            {
                this.world.worldProcesses[l].Update();
            }
            //BiographyPlugin.Log("SimWorld : Update 5");
            //this.world.rainCycle.Update();
            this.overWorld.Update();
            this.pathfinderResourceDivider.Update();
            this.updateShortCut++;
            if (this.updateShortCut > 2)
            {
                this.updateShortCut = 0;
                this.shortcuts.Update();
            }
            //BiographyPlugin.Log("SimWorld : Update 6");
            for (int m = this.world.activeRooms.Count - 1; m >= 0; m--)
            {
                this.world.activeRooms[m].Update();
                this.world.activeRooms[m].PausedUpdate();
            }
            if (this.world.loadingRooms.Count > 0)
            {
                for (int n = 0; n < 1; n++)
                {
                    for (int num = this.world.loadingRooms.Count - 1; num >= 0; num--)
                    {
                        if (this.world.loadingRooms[num].done)
                        {
                            BiographyPlugin.Log($"SimGame : Room-{world.loadingRooms[num].room.abstractRoom.name} orig gravity-{world.loadingRooms[num].room.gravity}");
                            world.loadingRooms[num].room.gravity = 1f;
                            this.world.loadingRooms.RemoveAt(num);
                        }
                        else
                        {
                            this.world.loadingRooms[num].Update();
                            BiographyPlugin.Log($"SimGame : LoadingRoom-{num},done-{world.loadingRooms[num].done},failed-{world.loadingRooms[num].failed},status-{world.loadingRooms[num].status}");
                        }
                    }
                }
            }
            //BiographyPlugin.Log("SimWorld : Update 7");
            if (this.cameras[0] != null)
            {
                for (int num2 = 0; num2 < 4; num2++)
                {
                    PlayerHandler playerHandler = this.rainWorld.GetPlayerHandler(num2);
                    if (playerHandler != null && this.Players.Count > num2)
                    {
                        playerHandler.ControllerHandler.AttemptScreenShakeRumble(this.cameras[0].controllerShake);
                    }
                }
            }
            this.timeInRegionThisCycle++;

            //BiographyPlugin.Log("SimWorld : Update 8");
        }

        public override void GrafUpdate(float timeStacker)
        {
            float timeStacker2 = timeStacker;
            if (this.pauseUpdate)
            {
                timeStacker2 = 1f;
            }
            for (int l = 0; l < this.cameras.Length; l++)
            {
                this.cameras[l].DrawUpdate(timeStacker2, this.TimeSpeedFac);
                this.cameras[l].PausedDrawUpdate(timeStacker2, this.TimeSpeedFac);
            }
        }

        public override void RawUpdate(float dt)
        {
            framesPerSecond = 40;
            this.myTimeStacker += dt * (float)this.framesPerSecond;
            int num = 0;


            while (this.myTimeStacker > 1)
            {
                //if (framesPerSecond == 40)
                //    BiographyPlugin.Log($"SimGame : Fast prepare room, loading process : {Room.loadingProgress},loadingRooms : {world.loadingRooms.Count}");


                this.Update();
                //BiographyPlugin.Log("SimWorld rawUpdate");

                this.myTimeStacker -= 1f;
                num++;
                if (num > 2)
                {
                    this.myTimeStacker = 0f;
                }
            }
            this.GrafUpdate(this.myTimeStacker);
        }

        public override void ShutDownProcess()
        {
            BiographyPlugin.Log("SimGame : Start shut down simgame process");
            if (devToolsLabel != null)
                devToolsLabel.RemoveFromContainer();
            
            for (int i = 0; i < this.cameras.Length; i++)
            {
                this.cameras[i].ClearAllSprites();
            }

            BiographyPlugin.Log("SimGame : finish shut down cameras");
            for (int j = 0; j < this.cameras.Length; j++)
            {
                this.cameras[j].virtualMicrophone.AllQuiet();
            }

            BiographyPlugin.Log("SimGame : finish shut down virtualMicrophone");
            if (this.pauseMenu != null)
            {
                this.pauseMenu.ShutDownProcess();
            }

            BiographyPlugin.Log("SimGame : finish shut down pauseMenu");
            if (this.arenaOverlay != null)
            {
                this.arenaOverlay.ShutDownProcess();
                this.manager.sideProcesses.Remove(this.arenaOverlay);
                this.arenaOverlay = null;
            }

            BiographyPlugin.Log("SimGame : finish shut down arenaOverlay");
            if (this.IsArenaSession)
            {
                try
                {
                    //因为没有赋值overlay，所以会触发异常，但并不影响整个进程所以没关系。
                    this.GetArenaGameSession.ProcessShutDown();
                }
                catch(Exception _)
                {
                }
            }
            BiographyPlugin.Log("SimGame : finish shut down ArenaSession");
        }

        public bool SpawnCreature(CreatureTemplate.Type type,string idNumber,OpLabelLong opLabelLong)
        {
            if (type == CreatureTemplate.Type.Slugcat)
                return false;

            try
            {
                if (addedCreature != null)
                {
                    if (addedCreature.realizedCreature != null)
                        addedCreature.realizedCreature.Destroy();
                    addedCreature.Destroy();
                    addedCreature = null;
                }

                for(int i = Room.updateList.Count - 1;i >= 0; i--)
                {
                    if (Room.updateList[i] is PhysicalObject)
                    {
                        Room.updateList[i].Destroy();
                    }
                }

                EntityID id;
                if (idNumber.Contains("."))
                {
                    int region = int.Parse(idNumber.Split('.')[0]);
                    int number = int.Parse(idNumber.Split('.')[1]);
                    id = new EntityID(region, number);
                }
                else
                    id = new EntityID(-1,int.Parse(idNumber));

                var template = StaticWorld.GetCreatureTemplate(type);
                IntVector2 pos = airCreaturePos;
                int node = -1;
                if (template.TopAncestor().type == CreatureTemplate.Type.TentaclePlant) 
                { 
                    for(int i = 0;i < 1000; i++)
                    {
                        if (Room.abstractRoom.nodes[i].type == AbstractRoomNode.Type.Den)
                        {
                            node = i;
                            break;
                        }
                    }
                    pos.y += 50;
                }
                if(template.TopAncestor().type == CreatureTemplate.Type.GarbageWorm)
                {
                    for (int i = 0; i < 1000; i++)
                    {
                        if (Room.abstractRoom.nodes[i].type == AbstractRoomNode.Type.GarbageHoles)
                        {
                            node = i;
                            break;
                        }
                    }
                    pos.y += 50;
                }
                if(template.type == MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.StowawayBug)
                {
                    pos.y = 123;
                }
                if(template.waterRelationship == CreatureTemplate.WaterRelationship.WaterOnly || template.TopAncestor().waterRelationship == CreatureTemplate.WaterRelationship.WaterOnly)
                {
                    pos = waterCreaturePos;
                }


                addedCreature = new AbstractCreature(world, StaticWorld.GetCreatureTemplate(type), null, new WorldCoordinate(Room.abstractRoom.index, pos.x, pos.y, node), id);

                if (template.type == MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.StowawayBug)
                {
                    Vector2 realPos = Room.MiddleOfTile(pos);
                    (addedCreature.state as StowawayBugState).HomePos = realPos;
                    (addedCreature.state as StowawayBugState).aimPos = realPos + Vector2.down * 40f;
                }

                Room.abstractRoom.AddEntity(addedCreature);
                addedCreature.RealizeInRoom();
                anyCreatureAdded = true;

                opLabelLong.text = BiographyMenu.BiographyMenu.ParsePersonalityInfo(manager.rainWorld.inGameTranslator, addedCreature.personality);
                return true;
            }
            catch (Exception e)
            {
                addedCreature = null;
                Debug.LogException(e);
                return false;
            }
        }
    }
}
