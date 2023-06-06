using Menu.Remix.MixedUI;
using Menu;
using Menu.Remix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Biography.SimGameCore;
using System.Reflection;
using RWCustom;
using Menu.Remix.MixedUI.ValueTypes;

namespace Biography.BiographyMenu
{
    public class BiographyMenu : Menu.Menu
    {
        public static ProcessManager.ProcessID BiographyMenuID = new ProcessManager.ProcessID("BiographyMenu", true);
        public static BiographyMenu instance;
        static List<CreatureTemplate.Type> templateTypeBlackList = new List<CreatureTemplate.Type>();

        public Dictionary<string, RoomCamContainerWrapper> containerWrappers = new Dictionary<string, RoomCamContainerWrapper>();

        public FSprite darkSprite;
        public MenuLabel descriptionLabel;
        public MenuTabWrapper tabWrapper;

        public OpLabel creatureTypeLabel;
        public OpLabelLong personalityLabel;
        public OpSimpleButton RespawnCreatureButton;
        public OpSimpleButton RandomIDButton;
        public OpTextBox idChangeBox;
        public OpScrollBox creatureTemplateInfoScrollBox;
        public OpRect creatureDisplayRect;
        public OpFloatSlider colorChangeSlider;
        public UIelement MouseOverElement { get; private set; }
        public Configurable<int> idConfigurable;
        public Configurable<float> colorChangeConfigurable;

        public List<UIelementWrapper> creatureTemplateInfoWrappers = new List<UIelementWrapper>();

        public string currentSelectCreature;
        public string currentID = "1000";

        public float lastSliderVal = 0f;

        public BiographyMenu(ProcessManager processManager) : base(processManager, BiographyMenuID)
        {
            instance = this;
            idConfigurable = new Configurable<int>(1000);
            colorChangeConfigurable = new Configurable<float>(0f);

            pages.Add(new Page(this, null, "main", 0));

            var sceneID = manager.rainWorld.options.TitleBackground;
            scene = new InteractiveMenuScene(this, this.pages[0], sceneID);
            pages[0].subObjects.Add(this.scene);

            darkSprite = new FSprite("pixel", true)
            {
                color = new Color(0.01f, 0.01f, 0.01f),
                anchorX = 0f,
                anchorY = 0f,
                scaleX = 1368f,
                scaleY = 770f,
                x = -1f,
                y = -1f,
                alpha = 0.85f
            };
            pages[0].Container.AddChild(darkSprite);

            float buttonWidth = Menu.MainMenu.GetButtonWidth(CurrLang);

            Vector2 pos = new Vector2(manager.rainWorld.options.ScreenSize.x - buttonWidth - 30f, 10f);
            Vector2 size = new Vector2(buttonWidth, 30f);

            tabWrapper = new MenuTabWrapper(this, pages[0]);
            InitTab(tabWrapper);
            InitContainerWrapper();

            pages[0].subObjects.Add(tabWrapper);
            pages[0].subObjects.Add(new SimpleButton(this, pages[0], Translate("EXIT"), "Exit", pos, size));

            ProcessManagerExHooks.managerEx.RequestNewSimGame();
        }

        public void InitTab(MenuTabWrapper menuTabWrapper)
        {
            var OnValueChangeField = typeof(UIconfig).GetField("OnValueChanged", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            var OnClickField = typeof(OpSimpleButton).GetField("OnClick", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            float buttomGap = 50f;
            float topGapFromLabel = 100f;
            float leftGap = 30f;

            float symbolScrollWidth = 100f;

            float rectLeftGap = leftGap + symbolScrollWidth + leftGap;

            var allSymbols = GetAllCreatureSymbolButtons();

            var creatureSymbolButtons = new OpScrollBox(new Vector2(leftGap, buttomGap), new Vector2(symbolScrollWidth, manager.rainWorld.options.ScreenSize.y - topGapFromLabel - buttomGap), allSymbols.Length * 80f + 40f);
            creatureTemplateInfoScrollBox = new OpScrollBox(new Vector2(rectLeftGap + (manager.rainWorld.options.ScreenSize.x - rectLeftGap - leftGap) / 2f - leftGap * 2f, (manager.rainWorld.options.ScreenSize.y - topGapFromLabel - buttomGap) - buttomGap * 2f - 250f), new Vector2(500f, 150f), 1000f, false, true, false);
            OpRect rightRect;
            UIelement[] uiElements = new UIelement[]
            {
                new OpLabel(leftGap,manager.rainWorld.options.ScreenSize.y - 40f,$"{Translate("Biography")}" + (BiographyPlugin.UsingSandboxUnlock ? $" - {Translate("saveslot")} {manager.rainWorld.options.saveSlot + 1}" : ""),true),
                creatureSymbolButtons,
                rightRect = new OpRect(new Vector2(rectLeftGap,buttomGap),new Vector2(manager.rainWorld.options.ScreenSize.x - rectLeftGap - leftGap,manager.rainWorld.options.ScreenSize.y - topGapFromLabel - buttomGap)),
                creatureDisplayRect = new OpRect(new Vector2(SimGame.CreatureDisplayCenter.x,SimGame.CreatureDisplayCenter.y) + SimGame.CreatureDisplayWindowSize / 2f,SimGame.CreatureDisplayWindowSize)
                {
                    colorFill = Color.black,
                    fillAlpha = 0.7f
                },
                colorChangeSlider = new OpFloatSlider(colorChangeConfigurable, new Vector2(SimGame.CreatureDisplayCenter.x,SimGame.CreatureDisplayCenter.y) + Vector2.right * SimGame.CreatureDisplayWindowSize.x / 2f + Vector2.up * 30f, (int)SimGame.CreatureDisplayWindowSize.x)
                {
                    description = Translate("Drag to change background color from black to white")
                },
                creatureTypeLabel = new OpLabel(rectLeftGap + rightRect.rect.size.x / 2f,rightRect.rect.size.y, "", true)
                {
                    alignment = FLabelAlignment.Center
                },
                new OpLabel(rectLeftGap + rightRect.rect.size.x / 2f - leftGap * 2f,rightRect.rect.size.y - buttomGap,$"{Translate("EntityID")} : ",true),
                idChangeBox = new OpTextBox(idConfigurable,new Vector2(rectLeftGap + rightRect.rect.size.x / 2f + leftGap,rightRect.rect.size.y - buttomGap),200f),
                RespawnCreatureButton = new OpSimpleButton(new Vector2(rectLeftGap + rightRect.rect.size.x / 2f + leftGap * 2 + 200f,rightRect.rect.size.y - buttomGap),new Vector2(100f,30f),Translate("Refresh")),
                RandomIDButton = new OpSimpleButton(new Vector2(rectLeftGap + rightRect.rect.size.x / 2f + leftGap * 2 + 200f + 130f,rightRect.rect.size.y - buttomGap),new Vector2(100f,30f),Translate("Random")),
                new OpLabel(rectLeftGap + rightRect.rect.size.x / 2f - leftGap * 2f,rightRect.rect.size.y - buttomGap * 2f,$"{Translate("Personality")} : ",true),
                personalityLabel = new OpLabelLong(new Vector2(rectLeftGap + rightRect.rect.size.x / 2f, rightRect.rect.size.y - buttomGap * 2f - 300f), new Vector2(300f,300f),""),
                creatureTemplateInfoScrollBox
            };

            foreach (var uiElement in uiElements)
            {
                new UIelementWrapper(tabWrapper, uiElement);
            }   
            
            foreach(var symbol in allSymbols)
            {
                creatureSymbolButtons.AddItemToWrapped(symbol);
            }
            SetupCreatureTemplateInfo();

            var OnClick = OnClickField.GetValue(RespawnCreatureButton) as OnSignalHandler;
            OnClick += RespawnCreatureButton_OnClick;
            OnClickField.SetValue(RespawnCreatureButton, OnClick);

            OnClick = OnClickField.GetValue(RandomIDButton) as OnSignalHandler;
            OnClick += RandomIDButton_OnClick;
            OnClickField.SetValue(RandomIDButton, OnClick);

            (uiElements[0] as OpLabel).label.shader = manager.rainWorld.Shaders["MenuText"];
            creatureTypeLabel.label.shader = manager.rainWorld.Shaders["MenuText"];
        }
        public void InitContainerWrapper()
        {
            string[] layerNames = new string[]
            {
                "Shadows",
                "BackgroundShortcuts",
                "Background",
                "Midground",
                "Items",
                "Foreground",
                "ForegroundLights",
                "Shortcuts",
                "Water",
                "GrabShaders",
                "Bloom",
                "HUD",
                "HUD2"
            };
            foreach(var layerName in layerNames)
            {
                var containerWrapper = new RoomCamContainerWrapper(this, pages[0], layerName);
                containerWrappers.Add(layerName, containerWrapper);
                pages[0].subObjects.Add(containerWrapper);
            }
        }

        public UIelement[] GetAllCreatureSymbolButtons()
        {
            List<UIelement> symbols = new List<UIelement>();

            var OnClickField = typeof(OpSimpleButton).GetField("OnClick", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            foreach (var creatureType in CreatureTemplate.Type.values.entries)
            {
                CreatureTemplate.Type type = new CreatureTemplate.Type(creatureType);

                if (type == CreatureTemplate.Type.StandardGroundCreature ||
                   type == CreatureTemplate.Type.LizardTemplate ||
                   type == CreatureTemplate.Type.Slugcat ||
                   templateTypeBlackList.Contains(type))
                {
                    continue;
                }

                if (BiographyPlugin.UsingSandboxUnlock)
                {
                    bool unlocked = false;
                    MultiplayerUnlocks multiplayerUnlocks = new MultiplayerUnlocks(manager.rainWorld.progression, new List<string>());
                    foreach (MultiplayerUnlocks.SandboxUnlockID unlockID in MultiplayerUnlocks.CreatureUnlockList)
                    {
                        if (multiplayerUnlocks.SandboxItemUnlocked(unlockID))
                        {
                            if(MultiplayerUnlocks.SymbolDataForSandboxUnlock(unlockID).critType == type)
                            {
                                unlocked = true;
                            }
                        }
                    }
                    if (!unlocked)
                        continue;
                }

                IconSymbol.IconSymbolData iconSymbol = new IconSymbol.IconSymbolData(type, AbstractPhysicalObject.AbstractObjectType.Creature, 2);

                OpSimpleImageButton symbolButton = new OpSimpleImageButton(new Vector2(20f, symbols.Count * 80f + 20f), new Vector2(60f, 60f), CreatureSymbol.SpriteNameOfCreature(iconSymbol))
                {
                    description = creatureType,
                    colorEdge = CreatureSymbol.ColorOfCreature(iconSymbol)
                };

                if (BiographyPlugin.UsingSandboxUnlock)
                {
                    symbolButton.sprite.shader = manager.rainWorld.Shaders["MenuTextCustom"];
                }
                var OnClickHandle = OnClickField.GetValue(symbolButton) as OnSignalHandler;
                OnClickHandle += SymbolButton_OnClick;
                OnClickField.SetValue(symbolButton, OnClickHandle);

                
                symbols.Add(symbolButton);
            }

            return symbols.ToArray();
        }

        private void SymbolButton_OnClick(UIfocusable trigger)
        {
            currentSelectCreature = trigger.description;
            BiographyPlugin.Log($"Select {trigger.description}");
            creatureTypeLabel.text = currentSelectCreature;
            if (!ProcessManagerExHooks.managerEx.simGame.SpawnCreature(new CreatureTemplate.Type(trigger.description), currentID, personalityLabel))
            {
                PlaySound(SoundID.MENU_Error_Ping);
            }
            else
                SetupCreatureTemplateInfo();
        }

        private void RespawnCreatureButton_OnClick(UIfocusable trigger)
        {
            BiographyPlugin.Log($"ID text-{idChangeBox.label.text} parsed-{int.Parse(idChangeBox.label.text)}");
            currentID = idChangeBox.label.text;
            if (!ProcessManagerExHooks.managerEx.simGame.SpawnCreature(new CreatureTemplate.Type(currentSelectCreature), currentID, personalityLabel))
            {
                PlaySound(SoundID.MENU_Error_Ping);
            }
        }

        private void RandomIDButton_OnClick(UIfocusable trigger)
        {
            currentID = UnityEngine.Random.Range(0, 100000).ToString();
            idChangeBox.value = currentID;
            RespawnCreatureButton_OnClick(trigger);
        }

        private void OpTextBox_OnValueChange(UIconfig iconfig,string value,string oldvalue)
        {
            if (value == oldvalue)
                return;
            try
            {
                currentID = value;
            }
            catch(Exception ex)
            {
                Debug.LogException(ex);
                PlaySound(SoundID.MENU_Error_Ping);
                return;
            }
            if (!ProcessManagerExHooks.managerEx.simGame.SpawnCreature(new CreatureTemplate.Type(currentSelectCreature), currentID, personalityLabel))
            {
                PlaySound(SoundID.MENU_Error_Ping);
            }
        }
        public override void Singal(MenuObject sender, string message)
        {
            if(message == "Exit")
            {
                PlaySound(SoundID.MENU_Switch_Page_Out);
                manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
            }
        }

        public override void Update()
        {
            try
            {
                if(colorChangeSlider.GetValueFloat() != lastSliderVal)
                {
                    lastSliderVal = colorChangeSlider.GetValueFloat();
                    creatureDisplayRect.colorFill = Color.Lerp(Color.black, Color.white, lastSliderVal);
                }
                base.Update();
            }
            catch(Exception e)
            {
                Debug.LogException(e);
                PlaySound(SoundID.MENU_Error_Ping);
            }
        }

        public override void ShutDownProcess()
        {
            try
            {
                BiographyPlugin.Log("SimGame : start BiographyMenu ShutDown");
                ProcessManagerExHooks.managerEx.ClearOutSideProcesses();
                BiographyPlugin.Log("SimGame : SimGame shutdown success");
                base.ShutDownProcess();
                instance = null;
                BiographyPlugin.Log("SimGame : finish BiographyMenu ShutDown");
            }
            catch(Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public void SetupCreatureTemplateInfo()
        {
            foreach (var item in creatureTemplateInfoScrollBox.items)
            {
                item.Deactivate();
            }

            foreach (var wrapper in creatureTemplateInfoWrappers)
            {
                try
                {
                    wrapper.tabWrapper.wrappers.Remove(wrapper.thisElement);
                    wrapper.tabWrapper._tab.RemoveItems(wrapper.thisElement);

                    wrapper.RemoveSprites();
                    wrapper.owner.RemoveSubObject(wrapper);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
            creatureTemplateInfoWrappers.Clear();
            OpScrollBox.RemoveItemsFromScrollBox(creatureTemplateInfoScrollBox.items.ToArray());


            float yBias = 30f;
            List<UIelement> result = new List<UIelement>();

            result.Add(new OpLabel(5f,creatureTemplateInfoScrollBox.contentSize - yBias, Translate("CreatureTemplate Infos"),true));
            (result[0] as OpLabel).label.shader = manager.rainWorld.Shaders["MenuText"];
            yBias += 30f;
            BiographyPlugin.Log("Menu : Add main info title");

            CreatureTemplate template = StaticWorld.GetCreatureTemplate(new CreatureTemplate.Type(currentSelectCreature));
            if (template != null)
            {
                //Basic
                result.Add(new OpLabel(15f, creatureTemplateInfoScrollBox.contentSize - yBias, $"{Translate("Basic")} :", true));
                yBias += 45f;

                string basicInfo = $"{Translate("MeatPoint")} : {template.meatPoints}\n" +
                                   $"{Translate("DangerousToPlayer")} : {template.dangerousToPlayer}\n" +
                                   $"{Translate("VisualRadius")} : {template.visualRadius}\n" +
                                   $"{Translate("WaterVision")} : {template.waterVision}\n" +
                                   $"{Translate("BlizzardAdapted")} : {template.BlizzardAdapted}";
                result.Add(new OpLabel(45f, creatureTemplateInfoScrollBox.contentSize - yBias, basicInfo));
                yBias += 60f;


                //DamageResistance
                result.Add(new OpLabel(15f, creatureTemplateInfoScrollBox.contentSize - yBias, $"{Translate("Damage Resistances")} :", true));
                yBias += 9 * (ExtEnum<Creature.DamageType>.values.Count);

                string damageResistanceInfo = $"{Translate("Base")} : {template.baseDamageResistance}";
                for (int i = 0; i < ExtEnum<Creature.DamageType>.values.Count - 1; i++)
                {
                    string name = ExtEnum<Creature.DamageType>.values.entries[i];
                    damageResistanceInfo += $"\n{Translate(name)} : {template.damageRestistances[i, 0]} | {template.damageRestistances[i, 1]}";
                }
                result.Add(new OpLabel(45f, creatureTemplateInfoScrollBox.contentSize - yBias, damageResistanceInfo));
                yBias += 9 * (ExtEnum<Creature.DamageType>.values.Count + 1);

                //Relationships
                result.Add(new OpLabel(15f, creatureTemplateInfoScrollBox.contentSize - yBias, $"{Translate("Relationships")} :", true));
                yBias += 45f;
                Dictionary<CreatureTemplate.Relationship.Type, List<int>> typeToRelationship = new Dictionary<CreatureTemplate.Relationship.Type, List<int>>();
                for(int i = 0;i < template.relationships.Length;i++)
                {
                    if (!typeToRelationship.ContainsKey(template.relationships[i].type))
                        typeToRelationship.Add(template.relationships[i].type, new List<int>());
                    typeToRelationship[template.relationships[i].type].Add(i);
                }

                
                foreach(var pair in typeToRelationship)
                {
                    int xIndex = 0;
                    int total = 0;
                    if (pair.Value.Count == 0)
                        continue;

                    result.Add(new OpLabel(45f, creatureTemplateInfoScrollBox.contentSize - yBias, pair.Key.ToString()));
                    while(total < pair.Value.Count)
                    {
                        CreatureTemplate.Type type = new CreatureTemplate.Type(ExtEnum<CreatureTemplate.Type>.values.entries[pair.Value[total]]);
                        if (type == CreatureTemplate.Type.StandardGroundCreature || 
                            type == CreatureTemplate.Type.LizardTemplate ||
                            templateTypeBlackList.Contains(type))
                        {
                            total++;
                            continue;
                        }
                        //Sandbox_SmallQuestionmark
                        IconSymbol.IconSymbolData iconSymbol = new IconSymbol.IconSymbolData(type, AbstractPhysicalObject.AbstractObjectType.Creature, 2);
                        string spriteName = CreatureSymbol.SpriteNameOfCreature(iconSymbol);
                        bool unlocked = !BiographyPlugin.UsingSandboxUnlock;
                        if (BiographyPlugin.UsingSandboxUnlock)
                        {
                            MultiplayerUnlocks multiplayerUnlocks = new MultiplayerUnlocks(manager.rainWorld.progression, new List<string>());
                            foreach (MultiplayerUnlocks.SandboxUnlockID unlockID in MultiplayerUnlocks.CreatureUnlockList)
                            {
                                if (multiplayerUnlocks.SandboxItemUnlocked(unlockID))
                                {
                                    if (MultiplayerUnlocks.SymbolDataForSandboxUnlock(unlockID).critType == type)
                                    {
                                        unlocked = true;
                                    }
                                }
                            }
                        }
                        if(!unlocked)
                            spriteName = "Sandbox_SmallQuestionmark";

                        OpSimpleImageButton symbolButton = new OpSimpleImageButton(new Vector2(110f + 40f * (xIndex + 1), creatureTemplateInfoScrollBox.contentSize - yBias), new Vector2(30f, 30f), spriteName)
                        {
                            description = unlocked ? $"{type} intensity : {template.relationships[pair.Value[total]].intensity}" : Translate("This creature is still locked"),
                            colorEdge = unlocked ? CreatureSymbol.ColorOfCreature(iconSymbol) : MenuColorEffect.rgbMediumGrey
                        };

                        result.Add(symbolButton);

                        xIndex++;
                        total++;
                        if (xIndex > 6)
                        {
                            xIndex = 0;
                            yBias += 40f;            
                        }
                        
                    }
                    if (xIndex == 0 && total > 0)
                        yBias -= 40;
                    yBias += 80f;
                }
            }


            foreach(var element in result)
                creatureTemplateInfoWrappers.Add(creatureTemplateInfoScrollBox.AddItemToWrapped(element));
        }

        public static string ParsePersonalityInfo(InGameTranslator translator,AbstractCreature.Personality personality)
        {
            string result = $"{translator.Translate("Aggression")} : {personality.aggression}\n{translator.Translate("Bravery")} : {personality.bravery}\n{translator.Translate("Dominance")} : {personality.dominance}\n{translator.Translate("Energy")} : {personality.energy}\n{translator.Translate("Nervous")} : {personality.nervous}\n{translator.Translate("Sympathy")} : {personality.sympathy}";
            return result;
        }

        public static void AddTypeToBlackList(CreatureTemplate.Type type)
        {
            if(templateTypeBlackList.Contains(type)) 
                return;
            templateTypeBlackList.Add(type);
        }
    }

    public class RoomCamContainerWrapper : MenuObject
    {
        public string layerName;
        public RoomCamContainerWrapper(Menu.Menu menu, MenuObject menuObject,string layerName) : base(menu, menuObject)
        {
            Container = new FContainer();
            menuObject.Container.AddChild(Container);
            this.layerName = layerName;
        }
    }
}
