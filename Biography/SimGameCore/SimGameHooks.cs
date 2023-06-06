using Biography.BiographyMenu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using static Room;
using Debug = UnityEngine.Debug;

namespace Biography.SimGameCore
{
    public static class SimGameHook
    {
        public static T GetUninit<T>()
        {
            return (T)(FormatterServices.GetSafeUninitializedObject(typeof(T)));
        }

        public static void HookOn()
        {
            try
            {
                Hook hook1 = new Hook(typeof(RainCycle).GetProperty("ScreenShake", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetGetMethod(), typeof(SimGameHook).GetMethod("RainCycle_Get_ScreenShake", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
                Hook hook2 = new Hook(typeof(RainCycle).GetProperty("MicroScreenShake", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetGetMethod(), typeof(SimGameHook).GetMethod("RainCycle_Get_MicroScreenShake", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
                Hook hook3 = new Hook(typeof(RainCycle).GetProperty("ShaderLight", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetGetMethod(), typeof(SimGameHook).GetMethod("RainCycle_Get_ShaderLight", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
                Hook hook4 = new Hook(typeof(RainCycle).GetProperty("TimeUntilRain", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetGetMethod(), typeof(SimGameHook).GetMethod("RainCycle_Get_TimeUntilRain", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
                Hook hook5 = new Hook(typeof(RainCycle).GetProperty("RainApproaching", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetGetMethod(), typeof(SimGameHook).GetMethod("RainCycle_Get_RainApproaching", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            On.RainWorldGame.ctor += RainWorldGame_ctor1;
            On.SandboxGameSession.ctor += SandboxGameSession_ctor;
            On.SandboxGameSession.Initiate += SandboxGameSession_Initiate;
            On.ArenaGameSession.AddHUD += ArenaGameSession_AddHUD;
            On.GameSession.ctor += GameSession_ctor;

            On.RoomCamera.ctor += RoomCamera_ctor;
            On.RoomCamera.ApplyPalette += RoomCamera_ApplyPalette;
            On.RoomCamera.ApplyFade += RoomCamera_ApplyFade;
            //On.RoomCamera.NewObjectInRoom += RoomCamera_NewObjectInRoom;
            On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;
            On.RoomCamera.ReturnFContainer += RoomCamera_ReturnFContainer;
            On.RoomCamera.PositionCurrentlyVisible += RoomCamera_PositionCurrentlyVisible;

            On.OverWorld.LoadFirstWorld += OverWorld_LoadFirstWorld;
            On.RainWorldGame.AllowRainCounterToTick += RainWorldGame_AllowRainCounterToTick;

            On.World.ctor += World_ctor;

            On.Room.ctor += Room_ctor;
            On.Room.Loaded += Room_Loaded;
            On.Room.LoadFromDataString += Room_LoadFromDataString;

            On.Water.AddToContainer += Water_AddToContainer;

            On.VirtualMicrophone.Update += VirtualMicrophone_Update;
            On.RoomRain.Update += RoomRain_Update;


            //On.FContainer.AddChild += FContainer_AddChild;
            On.VultureGraphics.ExitShadowMode += VultureGraphics_ExitShadowMode;

            On.ShortcutGraphics.Update += ShortcutGraphics_Update;
            On.ShortcutGraphics.Draw += ShortcutGraphics_Draw;
            On.ShortcutGraphics.NewRoom += ShortcutGraphics_NewRoom;
        }

        private static bool RoomCamera_PositionCurrentlyVisible(On.RoomCamera.orig_PositionCurrentlyVisible orig, RoomCamera self, Vector2 testPos, float margin, bool widescreen)
        {
            bool result = orig.Invoke(self,testPos, margin, widescreen);
            if (self.room.game is SimGame)
                result = true;
            return result;
        }

        private static void Water_AddToContainer(On.Water.orig_AddToContainer orig, Water self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (self.room.game is SimGame)
                return;
            orig.Invoke(self, sLeaser, rCam, newContatiner);
        }

        private static void ShortcutGraphics_NewRoom(On.ShortcutGraphics.orig_NewRoom orig, ShortcutGraphics self)
        {
            if (self.camera.game is SimGame)
                return;
            orig.Invoke(self);
        }

        private static void ShortcutGraphics_Draw(On.ShortcutGraphics.orig_Draw orig, ShortcutGraphics self, float timeStacker, Vector2 camPos)
        {
            if (self.camera.game is SimGame)
                return;
            orig.Invoke(self,timeStacker, camPos);
        }

        private static void ShortcutGraphics_Update(On.ShortcutGraphics.orig_Update orig, ShortcutGraphics self)
        {
            if (self.camera.game is SimGame)
                return;
            orig.Invoke(self);
        }

        //vulture graphics fix
        private static void VultureGraphics_ExitShadowMode(On.VultureGraphics.orig_ExitShadowMode orig, VultureGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, bool changeContainer)
        {
            if(self.vulture != null && self.vulture.room != null && self.vulture.room.game != null)
            {
                if (self.vulture.room.game is SimGame)
                    if (self.spritesInShadowMode)
                        return;
            }
            orig.Invoke(self, sLeaser, rCam, changeContainer);
        }

        #region RuntimeDetour
        private static float RainCycle_Get_RainApproaching(Func<RainCycle, float> orig, RainCycle self)
        {
            if (self.world == null || self.world.game is SimGame)
                return 1f;
            else
                return orig.Invoke(self);
        }
        private static int RainCycle_Get_TimeUntilRain(Func<RainCycle, int> orig, RainCycle self)
        {
            if (self.world == null || self.world.game is SimGame)
                return 1000000;
            return orig.Invoke(self);
        }
        private static float RainCycle_Get_ShaderLight(Func<RainCycle, float> orig, RainCycle self)
        {
            if (self.world == null || self.world.game is SimGame)
                return 0f;
            else
                return orig.Invoke(self);
        }
        private static float RainCycle_Get_MicroScreenShake(Func<RainCycle, float> orig, RainCycle self)
        {
            if (self.world == null || self.world.game is SimGame)
                return 0f;
            else
                return orig.Invoke(self);
        }
        private static float RainCycle_Get_ScreenShake(Func<RainCycle, float> orig, RainCycle self)
        {
            if (self.world == null || self.world.game is SimGame)
                return 0f;
            else
                return orig.Invoke(self);
        }
        #endregion

        #region RoomCamera
        private static void RoomCamera_NewObjectInRoom(On.RoomCamera.orig_NewObjectInRoom orig, RoomCamera self, IDrawable obj)
        {
            if (self.game is SimGame)
            {
                if (obj is RoomRain)
                    return;
            }

            BiographyPlugin.Log($"SimGame : new {obj} in room");
            orig.Invoke(self, obj);
            if (self.game is SimGame)
            {
                if (obj is GraphicsModule)
                {
                    foreach (var sleaser in self.spriteLeasers)
                    {
                        if (sleaser.drawableObject == obj)
                        {
                            foreach (var sprite in sleaser.sprites)
                            {
                                Futile.stage.AddChild(sprite);
                            }
                            SpriteLeaserWarpper.wappers.Add(sleaser, new SpriteLeaserWarpper(sleaser, new Vector2(300f, 600f)));
                        }
                    }
                }
            }
        }

        private static void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
        {
            if (self.game is SimGame)
            {
                var simGame = self.game as SimGame;
                if (simGame.addedCreature != null && simGame.addedCreature.realizedCreature != null)
                {
                    Vector2 vector = Vector2.Lerp(self.lastPos, self.pos, timeStacker);
                    Vector2 goal = simGame.addedCreature.realizedCreature.DangerPos - vector - SimGame.CreatureDisplayCenter - SimGame.CreatureDisplayWindowSize;
                    Vector2 delta = goal - self.offset;
                    float t = delta.magnitude > 100f ? Time.deltaTime * 10f : Time.deltaTime * 3f;
                    self.offset = Vector2.Lerp(self.offset, goal, t);
                }
            }
            orig.Invoke(self, timeStacker, timeSpeed);
        }

        private static void RoomCamera_ApplyFade(On.RoomCamera.orig_ApplyFade orig, RoomCamera self)
        {
            if (self.game is SimGame)
            {
                BiographyPlugin.Log($"SimGame : PaletteB-{self.paletteB}");
                if (self.paletteB > -1)
                {
                    BiographyPlugin.Log("SimGame : RoomCam_ApplyFade 0");
                    for (int i = 0; i < 32; i++)
                    {
                        for (int j = 8; j < 16; j++)
                        {
                            self.paletteTexture.SetPixel(i, j - 8, Color.Lerp(Color.Lerp(self.fadeTexA.GetPixel(i, j), self.fadeTexA.GetPixel(i, j - 8), self.fadeCoord.y), Color.Lerp(self.fadeTexB.GetPixel(i, j), self.fadeTexB.GetPixel(i, j - 8), self.fadeCoord.y), self.fadeCoord.x));
                        }
                    }
                    BiographyPlugin.Log("SimGame : RoomCam_ApplyFade 1");
                }
                else
                {
                    BiographyPlugin.Log("SimGame : RoomCam_ApplyFade 2");
                    for (int k = 0; k < 32; k++)
                    {
                        for (int l = 8; l < 16; l++)
                        {
                            self.paletteTexture.SetPixel(k, l - 8, Color.Lerp(self.fadeTexA.GetPixel(k, l), self.fadeTexA.GetPixel(k, l - 8), self.fadeCoord.y));
                        }
                    }
                    BiographyPlugin.Log("SimGame : RoomCam_ApplyFade 3");
                }

                self.paletteTexture.Apply(false);
                self.ApplyPalette();
            }
            else
                orig.Invoke(self);
        }

        private static void RoomCamera_ctor(On.RoomCamera.orig_ctor orig, RoomCamera self, RainWorldGame game, int cameraNumber)
        {
            if (game is SimGame)
            {
                self.game = game;
                self.followAbstractCreature = null;
                self.room = null;
                self.pos = new Vector2(0f, 0f);
                self.lastPos = self.pos;
                self.leanPos = new Vector2(0f, 0f);
                self.screenShake = 0f;
                self.screenMovementShake = 0f;
                self.microShake = 0f;
                self.cameraNumber = cameraNumber;
                self.offset = new Vector2((float)cameraNumber * 6000f, 0f);
                self.followCreatureInputForward = new Vector2(0f, 0f);
                self.singleCameraDrawables = new List<ISingleCameraDrawable>();
                self.virtualMicrophone = new VirtualMicrophone(self);

                self.paletteB = -1;
                self.spriteLeasers = new List<RoomCamera.SpriteLeaser>();

                if (RoomCamera.allEffectColorsTexture == null)
                {
                    RoomCamera.allEffectColorsTexture = new Texture2D(40, 4, TextureFormat.ARGB32, false);
                    string str = AssetManager.ResolveFilePath("Palettes" + Path.DirectorySeparatorChar.ToString() + "effectColors.png");
                    AssetManager.SafeWWWLoadTexture(ref RoomCamera.allEffectColorsTexture, "file:///" + str, false, true);
                }
                self.SpriteLayers = new FContainer[13];
                for (int i = 0; i < self.SpriteLayers.Length; i++)
                {
                    self.SpriteLayers[i] = new FContainer();
                    SimGame.banFContainers.Add(self.SpriteLayers[i]);
                    //Futile.stage.AddChild(self.SpriteLayers[i]);
                }
                self.SpriteLayerIndex = new Dictionary<string, int>
                {
                    { "Shadows", 0 },
                    { "BackgroundShortcuts", 1 },
                    { "Background", 2 },
                    { "Midground", 3 },
                    { "Items", 4 },
                    { "Foreground", 5 },
                    { "ForegroundLights", 6 },
                    { "Shortcuts", 7 },
                    { "Water", 8 },
                    { "GrabShaders", 9 },
                    { "Bloom", 10 },
                    { "HUD", 11 },
                    { "HUD2", 12 }
                };
                self.levelGraphic = new FSprite("LevelTexture", true);
                self.levelGraphic.anchorX = 0f;
                self.levelGraphic.anchorY = 0f;
                //self.ReturnFContainer("Foreground").AddChild(self.levelGraphic);
                self.backgroundGraphic = new FSprite("BackgroundTexture", true);
                self.backgroundGraphic.shader = game.rainWorld.Shaders["Background"];
                self.backgroundGraphic.anchorX = 0f;
                self.backgroundGraphic.anchorY = 0f;
                //self.ReturnFContainer("Foreground").AddChild(self.backgroundGraphic);
                self.shortcutGraphics = new ShortcutGraphics(self, game.shortcuts, new FShader[]
                {
                    game.rainWorld.Shaders["Shortcuts"]
                });
                self.paletteTexture = new Texture2D(32, 8, TextureFormat.ARGB32, false);
                self.paletteTexture.anisoLevel = 0;
                self.paletteTexture.filterMode = FilterMode.Point;
                self.paletteTexture.wrapMode = TextureWrapMode.Clamp;
                self.SnowTexture = new RenderTexture(1400, 800, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                self.SnowTexture.filterMode = FilterMode.Point;
                Shader.DisableKeyword("SNOW_ON");
                self.paletteA = -1;
                self.empty = new Color[49];
                Color color = new Color(0f, 0f, 0f, 0f);
                for (int j = 0; j < self.empty.Length; j++)
                {
                    self.empty[j] = color;
                }
                self.snowLightTex = new Texture2D(7, 7, TextureFormat.RGBA32, false);
                self.snowLightTex.filterMode = FilterMode.Point;
                self.fullscreenSync = Screen.fullScreen;

                self.LoadGhostPalette(32);
                self.ChangeMainPalette(0);
            }
            else
                orig.Invoke(self, game, cameraNumber);
        }

        private static void RoomCamera_ApplyPalette(On.RoomCamera.orig_ApplyPalette orig, RoomCamera self)
        {
            if (self.game is SimGame)
            {
                self.currentPalette = new RoomPalette(self.paletteTexture, 1f - self.paletteTexture.GetPixel(9, 7).r, 1f - self.paletteTexture.GetPixel(30, 7).r, self.paletteTexture.GetPixel(2, 7), self.paletteTexture.GetPixel(4, 7), self.paletteTexture.GetPixel(5, 7), self.paletteTexture.GetPixel(6, 7), self.paletteTexture.GetPixel(7, 7), self.paletteTexture.GetPixel(8, 7), self.paletteTexture.GetPixel(1, 7), self.paletteTexture.GetPixel(0, 7), self.paletteTexture.GetPixel(10, 7), self.paletteTexture.GetPixel(11, 7), self.paletteTexture.GetPixel(12, 7), self.paletteTexture.GetPixel(13, 7));
                Color pixel = self.paletteTexture.GetPixel(9, 7);
                float value = 1f - pixel.r;
                if (pixel.r == 0f && pixel.g == 0f && pixel.b > 0f)
                {
                    value = 1f + pixel.b;
                }
                for (int i = 0; i < self.spriteLeasers.Count; i++)
                {
                    self.spriteLeasers[i].UpdatePalette(self, self.currentPalette);
                }
            }
            else
                orig.Invoke(self);
        }

        private static FContainer RoomCamera_ReturnFContainer(On.RoomCamera.orig_ReturnFContainer orig, RoomCamera self, string layerName)
        {
            if (self.game is SimGame)
            {
                return BiographyMenu.BiographyMenu.instance.containerWrappers[layerName].Container;
            }
            else
                return orig.Invoke(self, layerName);
        }
        #endregion

        #region Room
        private static void Room_Loaded(On.Room.orig_Loaded orig, Room self)
        {
            orig.Invoke(self);
            if(self.game is SimGame)
            {
                self.roomRain = null;
                self.roomSettings.DangerType = RoomRain.DangerType.None;
            }
        }
        private static void Room_LoadFromDataString(On.Room.orig_LoadFromDataString orig, Room self, string[] lines)
        {
            if (self.game is SimGame)
            {
                RoomPreprocessor.VersionFix(ref lines);
                string[] array = lines[1].Split('*');
                if (lines[1].Split('|')[1] == "-1")
                {
                    self.water = false;
                    self.defaultWaterLevel = -1;
                    self.floatWaterLevel = float.MinValue;
                }
                else
                {
                    self.water = true;
                    self.defaultWaterLevel = Convert.ToInt32(lines[1].Split('|')[1], CultureInfo.InvariantCulture);
                    self.floatWaterLevel = self.MiddleOfTile(new IntVector2(0, self.defaultWaterLevel)).y;
                    self.waterInFrontOfTerrain = Convert.ToInt32(lines[1].Split('|')[2], CultureInfo.InvariantCulture) == 1;
                }

                BiographyPlugin.Log("SimGame : Room_LoadFromDataString 1");

                array = lines[1].Split('|')[0].Split('*');
                self.Width = Convert.ToInt32(array[0], CultureInfo.InvariantCulture);
                self.Height = Convert.ToInt32(array[1], CultureInfo.InvariantCulture);
                string[] array2 = lines[2].Split('|')[0].Split('*');
                self.lightAngle = new Vector2(Convert.ToSingle(array2[0], CultureInfo.InvariantCulture), Convert.ToSingle(array2[1], CultureInfo.InvariantCulture));
                string[] array3 = lines[3].Split('|');
                self.cameraPositions = new Vector2[array3.Length];
                for (int i = 0; i < array3.Length; i++)
                {
                    self.cameraPositions[i] = new Vector2(Convert.ToSingle(array3[i].Split(',')[0], CultureInfo.InvariantCulture), 0f - (800f - (float)self.Height * 20f + Convert.ToSingle(array3[i].Split(',')[1])));
                }

                BiographyPlugin.Log("SimGame : Room_LoadFromDataString 2");
                BiographyPlugin.Log($"{self.world},{self.game},{self.abstractRoom},{self.game.IsArenaSession},{self.game.GetArenaGameSession}");

                self.DefaultTile = null;
                if (self.world != null && self.game != null && self.abstractRoom.firstTimeRealized && (!self.game.IsArenaSession || self.game.GetArenaGameSession.GameTypeSetup.levelItems))
                {
                    string[] array4 = lines[5].Split('|');
                    for (int j = 0; j < array4.Length - 1; j++)
                    {
                        IntVector2 intVector = new IntVector2(Convert.ToInt32(array4[j].Split(',')[1], CultureInfo.InvariantCulture) - 1, self.Height - Convert.ToInt32(array4[j].Split(',')[2], CultureInfo.InvariantCulture));
                        bool flag = true;
                        if ((Convert.ToInt32(array4[j].Split(',')[0], CultureInfo.InvariantCulture) != 1) ? (UnityEngine.Random.value < 0.75f) : (UnityEngine.Random.value < 0.6f))
                        {
                            EntityID newID = self.game.GetNewID(-self.abstractRoom.index);
                            switch (Convert.ToInt32(array4[j].Split(',')[0], CultureInfo.InvariantCulture))
                            {
                                case 0:
                                    self.abstractRoom.AddEntity(new AbstractPhysicalObject(self.world, AbstractPhysicalObject.AbstractObjectType.Rock, null, new WorldCoordinate(self.abstractRoom.index, intVector.x, intVector.y, -1), newID));
                                    break;
                                case 1:
                                    self.abstractRoom.AddEntity(new AbstractSpear(self.world, null, new WorldCoordinate(self.abstractRoom.index, intVector.x, intVector.y, -1), newID, explosive: false));
                                    break;
                            }
                        }
                    }
                }

                BiographyPlugin.Log("SimGame : Room_LoadFromDataString 3");


                self.Tiles = new Tile[self.Width, self.Height];
                for (int k = 0; k < self.Width; k++)
                {
                    for (int l = 0; l < self.Height; l++)
                    {
                        self.Tiles[k, l] = new Tile(k, l, Tile.TerrainType.Air, vBeam: false, hBeam: false, wbhnd: false, 0, ((l <= self.defaultWaterLevel) ? 1 : 0) + ((l == self.defaultWaterLevel) ? 1 : 0));
                    }
                }

                BiographyPlugin.Log("SimGame : Room_LoadFromDataString 4");


                List<IntVector2> hiveTiles = new List<IntVector2>();
                List<IntVector2> list = new List<IntVector2>();
                List<int> hiveTileIndexes = new List<int>();
                int currentHiveTileIndex = -1;
                List<IntVector2> list2 = new List<IntVector2>();
                IntVector2 intVector2 = new IntVector2(0, self.Height - 1);
                List<IntVector2> list3 = new List<IntVector2>();
                string[] array5 = lines[11].Split('|');
                for (int m = 0; m < array5.Length - 1; m++)
                {
                    string[] array6 = array5[m].Split(',');
                    self.Tiles[intVector2.x, intVector2.y].Terrain = (Tile.TerrainType)int.Parse(array6[0], NumberStyles.Any, CultureInfo.InvariantCulture);
                    for (int n = 1; n < array6.Length; n++)
                    {
                        switch (array6[n])
                        {
                            case "1":
                                self.Tiles[intVector2.x, intVector2.y].verticalBeam = true;
                                break;
                            case "2":
                                self.Tiles[intVector2.x, intVector2.y].horizontalBeam = true;
                                break;
                            case "3":
                                if (self.Tiles[intVector2.x, intVector2.y].shortCut < 1)
                                {
                                    self.Tiles[intVector2.x, intVector2.y].shortCut = 1;
                                }
                                break;
                            case "4":
                                self.Tiles[intVector2.x, intVector2.y].shortCut = 2;
                                break;
                            case "5":
                                self.Tiles[intVector2.x, intVector2.y].shortCut = 3;
                                break;
                            case "9":
                                self.Tiles[intVector2.x, intVector2.y].shortCut = 4;
                                break;
                            case "12":
                                self.Tiles[intVector2.x, intVector2.y].shortCut = 5;
                                break;
                            case "6":
                                self.Tiles[intVector2.x, intVector2.y].wallbehind = true;
                                break;
                            case "7":
                                AddHiveTile(ref hiveTiles, ref hiveTileIndexes, ref currentHiveTileIndex, intVector2);
                                self.Tiles[intVector2.x, intVector2.y].hive = true;
                                break;
                            case "8":
                                list2.Add(intVector2);
                                break;
                            case "10":
                                list.Add(intVector2);
                                break;
                            case "11":
                                list3.Add(intVector2);
                                break;
                        }
                    }
                    intVector2.y--;
                    if (intVector2.y < 0)
                    {
                        intVector2.x++;
                        intVector2.y = self.Height - 1;
                    }
                }

                BiographyPlugin.Log("SimGame : Room_LoadFromDataString 5");


                if (list.Count > 0)
                {
                    self.garbageHoles = list.ToArray();
                }
                if (currentHiveTileIndex > -1)
                {
                    self.hives = new IntVector2[currentHiveTileIndex + 1][];
                    for (int num = 0; num <= currentHiveTileIndex; num++)
                    {
                        int num2 = 0;
                        for (int num3 = 0; num3 < hiveTileIndexes.Count; num3++)
                        {
                            if (hiveTileIndexes[num3] == num)
                            {
                                num2++;
                            }
                        }
                        self.hives[num] = new IntVector2[num2];
                        int num4 = 0;
                        for (int num5 = 0; num5 < hiveTileIndexes.Count; num5++)
                        {
                            if (hiveTileIndexes[num5] == num)
                            {
                                self.hives[num][num4] = hiveTiles[num5];
                                num4++;
                            }
                        }
                    }
                }
                else
                {
                    self.hives = new IntVector2[0][];
                }

                BiographyPlugin.Log("SimGame : Room_LoadFromDataString 6");


                List<WaterFall> list4 = new List<WaterFall>();
                while (list2.Count > 0)
                {
                    IntVector2 intVector3 = list2[0];
                    for (int num6 = 0; num6 < list2.Count; num6++)
                    {
                        intVector3 = list2[num6];
                        if (!list2.Contains(intVector3 + new IntVector2(-1, 0)) && !list2.Contains(intVector3 + new IntVector2(0, 1)))
                        {
                            break;
                        }
                    }
                    bool flag2 = list2.Contains(intVector3 + new IntVector2(0, -1));
                    int num7 = 0;
                    for (int num8 = intVector3.x; num8 < self.TileWidth && list2.Contains(new IntVector2(num8, intVector3.y)) && list2.Contains(new IntVector2(num8, intVector3.y - 1)) == flag2; num8++)
                    {
                        num7++;
                        list2.Remove(new IntVector2(num8, intVector3.y));
                        if (flag2)
                        {
                            list2.Remove(new IntVector2(num8, intVector3.y - 1));
                        }
                    }
                    list4.Add(new WaterFall(self, intVector3, flag2 ? 1f : 0.5f, num7));
                }
                self.waterFalls = list4.ToArray();
                for (int num9 = 0; num9 < self.waterFalls.Length; num9++)
                {
                    self.AddObject(self.waterFalls[num9]);
                }
                if (list3.Count > 0)
                {
                    self.AddObject(new WormGrass(self, list3));
                }
                self.Loaded();


                BiographyPlugin.Log("SimGame : Room_LoadFromDataString 7");

            }
            else
                orig.Invoke(self, lines);
        }

        private static void Room_ctor(On.Room.orig_ctor orig, Room self, RainWorldGame game, World world, AbstractRoom abstractRoom)
        {
            if (game is SimGame)
            {
                self.game = game;
                self.world = world;
                self.abstractRoom = abstractRoom;


                BiographyPlugin.Log("SimWorld : RoomLoad 1");

                self.roomSettings = new RoomSettings(abstractRoom.name, world.region, false, false, (game != null) ? game.StoryCharacter : null);

                BiographyPlugin.Log("SimWorld : RoomLoad 2");

                if (game != null)
                {
                    self.SlugcatGamemodeUniqueRoomSettings(game);
                    if (self.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.WaterFluxMaxLevel) > 0f)
                    {
                        self.waterFlux = new Room.WaterFluxController(self, game.globalRain);
                    }
                    if (ModManager.MSC && self.roomSettings.GetEffect(MoreSlugcatsEnums.RoomEffectType.InvertedWater) != null)
                    {
                        self.waterInverted = true;
                    }
                }

                BiographyPlugin.Log("SimWorld : RoomLoad 3");

                if ((world.region != null && world.region.name == "HR") || self.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.LavaSurface) != null)
                {
                    Shader.EnableKeyword("HR");
                }
                else
                {
                    Shader.DisableKeyword("HR");
                }
                self.physicalObjects = new List<PhysicalObject>[3];
                for (int i = 0; i < self.physicalObjects.Length; i++)
                {
                    self.physicalObjects[i] = new List<PhysicalObject>();
                }
                self.drawableObjects = new List<IDrawable>();
                self.accessModifiers = new List<IAccessibilityModifier>();
                self.updateList = new List<UpdatableAndDeletable>();
                self.lightSources = new List<LightSource>();
                self.waterFalls = new WaterFall[0];
                self.visionObscurers = new List<VisionObscurer>();
                self.socialEventRecognizer = new SocialEventRecognizer(self);
                self.cellDistortions = new List<CellDistortion>();
                self.cosmeticLightSources = new List<LightSource>();
                self.zapCoils = new List<ZapCoil>();
                self.lightningMachines = new List<LightningMachine>();
                self.energySwirls = new List<EnergySwirl>();
                self.snowSources = new List<SnowSource>();
                self.localBlizzards = new List<LocalBlizzard>();
                self.oeSpheres = new List<OEsphere>();
                self.deathFallFocalPoints = new List<Vector2>();
                self.blizzard = false;
                self.dustStorm = false;
                self.blizzardHeatSources = new List<IProvideWarmth>();
                self.lockedShortcuts = new List<IntVector2>();

                BiographyPlugin.Log("SimWorld : RoomLoad 4");
            }
            else
                orig.Invoke(self, game, world, abstractRoom);
        }
        #endregion

        #region Session
        private static void SandboxGameSession_Initiate(On.SandboxGameSession.orig_Initiate orig, SandboxGameSession self)
        {
            if (self.game is SimGame)
            {
                self.initiated = true;
                self.sandboxInitiated = true;
            }
            else
                orig.Invoke(self);
        }

        private static void ArenaGameSession_AddHUD(On.ArenaGameSession.orig_AddHUD orig, ArenaGameSession self)
        {
            if (self.game is SimGame)
            {
                BiographyPlugin.Log($"SimGame : Block arenaGameSession_AddHUD");
                return;
            }
            else
                orig.Invoke(self);
        }

        private static void SandboxGameSession_Update(On.SandboxGameSession.orig_Update orig, SandboxGameSession self)
        {
            if (self.game is SimGame)
            {
                BiographyPlugin.Log("SimWorld : Enter SandboxGameSession Update");

                if (!self.overlaySpawned && self.room != null)
                {
                    self.overlaySpawned = true;
                    self.room.AddObject(new SandboxOverlayOwner(self.room, self, !self.PlayMode));
                }

                BiographyPlugin.Log("SimWorld : stage 1 finish");

                if (self.arenaSitting.attempLoadInGame && self.arenaSitting.gameTypeSetup.savingAndLoadingSession)
                {
                    self.arenaSitting.attempLoadInGame = false;
                    self.arenaSitting.LoadFromFile(self, self.game.world, self.game.rainWorld);
                }

                BiographyPlugin.Log("SimWorld : stage 2 finish");

                if (self.initiated)
                {
                    self.counter++;
                }
                else if (self.room != null && self.room.shortCutsReady)
                {
                    self.Initiate();
                }
                BiographyPlugin.Log("SimWorld : stage 3 finish");

                if (self.room != null && self.chMeta != null && self.chMeta.deferred)
                {
                    self.room.deferred = true;
                }
                self.thisFrameActivePlayers = self.PlayersStillActive(true, false);

                BiographyPlugin.Log("SimWorld : stage 4 finish");

                if (!self.sessionEnded)
                {
                    if (self.endSessionCounter > 0)
                    {
                        if (self.ShouldSessionEnd())
                        {
                            self.endSessionCounter--;
                            if (self.endSessionCounter == 0)
                            {
                                self.EndSession();
                            }
                        }
                        else
                        {
                            self.endSessionCounter = -1;
                        }
                    }
                    else if (self.endSessionCounter == -1 && self.ShouldSessionEnd())
                    {
                        self.endSessionCounter = 30;
                    }
                }

                BiographyPlugin.Log("SimWorld : stage 5 finish");

                for (int i = 0; i < self.behaviors.Count; i++)
                {
                    if (self.behaviors[i].slatedForDeletion)
                    {
                        self.RemoveBehavior(i);
                    }
                    else
                    {
                        self.behaviors[i].Update();
                    }
                }

                BiographyPlugin.Log("SimWorld : stage 6 finish");

                if (self.game.world.rainCycle.TimeUntilRain < -1000 && !self.sessionEnded)
                {
                    Debug.Log("Rain end session");
                    self.outsidePlayersCountAsDead = true;
                    self.EndSession();
                }

                BiographyPlugin.Log("SimWorld : stage 7 finish");

                if (self.checkWinLose && self.playersSpawned)
                {
                    if (self.Players.Count == 1)
                    {
                        if (self.arenaSitting.players[0].sandboxWin != 0)
                        {
                            self.CustomGameOver();
                        }
                    }
                    else
                    {
                        if (self.PlayersStillActive(false, true) < 2)
                        {
                            self.CustomGameOver();
                        }
                        for (int i = 0; i < self.arenaSitting.players.Count; i++)
                        {
                            if (self.arenaSitting.players[i].sandboxWin > 0)
                            {
                                self.CustomGameOver();
                                break;
                            }
                        }
                    }

                    BiographyPlugin.Log("SimWorld : stage 8 finish");
                }
            }
            else
                orig.Invoke(self);
        }

        private static void GameSession_ctor(On.GameSession.orig_ctor orig, GameSession self, RainWorldGame game)
        {
            BiographyPlugin.Log("Start GameSession_ctor");
            if (game is SimGame)
            {
                BiographyPlugin.Log("Into SimGame session ctor");

            }
            else
                orig.Invoke(self, game);
            BiographyPlugin.Log("End GameSession_ctor");
        }

        private static void SandboxGameSession_ctor(On.SandboxGameSession.orig_ctor orig, SandboxGameSession self, RainWorldGame game)
        {
            BiographyPlugin.Log("SimGame : Start SandboxGameSession_ctor");
            if (game is SimGame)
            {
                self.game = game;
                self.Players = new List<AbstractCreature>();
                self.creatureCommunities = new CreatureCommunities(self);
                self.PlayMode = false;
                self.characterStats = new SlugcatStats(SlugcatStats.Name.White, false);
                self.behaviors = new List<ArenaBehaviors.ArenaGameBehavior>();
                self.rainCycleTimeInMinutes = 10000000f;
                self.overlaySpawned = true;
                BiographyPlugin.Log("SimGame : SandboxGameSession_ctor 1");

                string[] array = AssetManager.ListDirectory("Levels", false, false);
                var allLevels = new List<string>();
                for (int j = 0; j < array.Length; j++)
                {
                    if (array[j].Substring(array[j].Length - 4, 4) == ".txt" && array[j].Substring(array[j].Length - 13, 13) != "_settings.txt" && array[j].Substring(array[j].Length - 10, 10) != "_arena.txt" && !array[j].Contains("unlockall"))
                    {
                        string[] array2 = array[j].Substring(0, array[j].Length - 4).Split(new char[]
                        {
                        Path.DirectorySeparatorChar
                        });
                        allLevels.Add(array2[array2.Length - 1]);
                    }
                }
                BiographyPlugin.Log("SimGame : SandboxGameSession_ctor 2");
                BiographyPlugin.Log("SimGame : SandboxGameSession_ctor 2-0");
                var setup = new ArenaSetup(self.game.manager);
                BiographyPlugin.Log("SimGame : SandboxGameSession_ctor 2-1");
                self.arenaSitting = new ArenaSitting(setup.GetOrInitiateGameTypeSetup(ArenaSetup.GameTypeID.Sandbox), new MultiplayerUnlocks(self.game.manager.rainWorld.progression, allLevels));
                BiographyPlugin.Log("SimGame : SandboxGameSession_ctor 2-2");
                self.arenaSitting.SessionStartReset();
                self.arenaSitting.levelPlaylist.Add(game.startingRoom);
                BiographyPlugin.Log("SimGame : SandboxGameSession_ctor 2-3");
                BiographyPlugin.Log(self.arenaSitting.gameTypeSetup);
                BiographyPlugin.Log("SimGame : SandboxGameSession_ctor 3");
                if (!game.manager.rainWorld.progression.miscProgressionData.everPlayedArenaLevels.Contains(self.arenaSitting.GetCurrentLevel))
                {
                    game.rainWorld.progression.miscProgressionData.everPlayedArenaLevels.Add(self.arenaSitting.GetCurrentLevel);
                    game.rainWorld.progression.SaveProgression(false, true);
                    Debug.Log("saved level as played to misc prog");
                }
                BiographyPlugin.Log("SimGame : SandboxGameSession_ctor 4");
            }
            else
                orig.Invoke(self, game);
            BiographyPlugin.Log("End SandboxGameSession_ctor");
        }
        #endregion

        private static void FContainer_AddChild(On.FContainer.orig_AddChild orig, FContainer self, FNode node)
        {
            //if (node is FSprite)
            //{
            //    BiographyPlugin.Log($"{(node as FSprite).element.name}");
            //}
            //if (SimGame.banFContainers.Contains(self))
            //{
            //    BiographyPlugin.Log("Add to banned fcontainer");
            //    return;
            //}
            //else if (SimGame.SimGameLoaded && SimGame.LockSpriteAdd)
            //{
            //    BiographyPlugin.Log("Lock sprite add");
            //    return;
            //}
            //else
            //    orig.Invoke(self, node);

            StackTrace trace = new StackTrace();
            var stringBuilder = new StringBuilder();
            // GetFrame()获取是哪个类来调用的
            // 1:第一层，也就是当前类
            // 2:第二层，也就是调用类
            // 3:第三层，多层调用类
            // n：以此类推
            Type type = trace.GetFrame(2)?.GetMethod()?.DeclaringType;
            // 获取是类中的那个方法调用的
            string method = trace.GetFrame(2)?.GetMethod()?.ToString();
            if (type != null)
            {
                stringBuilder.Append(" - ");
                stringBuilder.Append(type);
            }

            if (method != null)
            {
                stringBuilder.Append(" - ");
                stringBuilder.Append(method);
            }

            if(SimGame.SimGameLoaded)
                BiographyPlugin.Log("SimGame : FContainer.addChild calling from : " + stringBuilder);
            orig.Invoke(self, node);
        }

        private static void RoomRain_Update(On.RoomRain.orig_Update orig, RoomRain self, bool eu)
        {
            if (self.room.game is SimGame)
                return;
            else
                orig.Invoke(self, eu);
        }

        private static void VirtualMicrophone_Update(On.VirtualMicrophone.orig_Update orig, VirtualMicrophone self)
        {
            if (self.camera.game is SimGame)
                return;
            else
                orig.Invoke(self);
        }

        private static void World_ctor(On.World.orig_ctor orig, World self, RainWorldGame game, Region region, string name, bool singleRoomWorld)
        {
            if (game is SimGame)
            {
                self.game = game;
                self.region = region;
                self.name = name;
                self.singleRoomWorld = singleRoomWorld;
                self.activeRooms = new List<Room>();
                self.loadingRooms = new List<RoomPreparer>();
                self.DisabledMapRooms = new List<string>();
                self.worldProcesses = new List<World.WorldProcess>();
                self.GetType().GetField("preProcessingGeneration", BindingFlags.Instance | BindingFlags.Public).SetValue(self, 20);

                self.rainCycle = GetUninit<RainCycle>();
            }
            else
                orig.Invoke(self, game, region, name, singleRoomWorld);
        }

        private static bool RainWorldGame_AllowRainCounterToTick(On.RainWorldGame.orig_AllowRainCounterToTick orig, RainWorldGame self)
        {
            if (self is SimGame)
                return false;
            return orig.Invoke(self);
        }

        private static void OverWorld_LoadFirstWorld(On.OverWorld.orig_LoadFirstWorld orig, OverWorld self)
        {
            if (self.game is SimGame)
            {
                string text = self.game.startingRoom;
                string text3 = Regex.Split(text, "_")[0];

                self.LoadWorld(text, self.PlayerCharacterNumber, true);
                self.FIRSTROOM = text;
            }
            else
                orig.Invoke(self);
        }

        private static void RainWorldGame_ctor1(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            if (self is SimGame)
                return;
            orig.Invoke(self, manager);
        }
        
    }
}
