using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Biography.BiographyMenu
{
    public static class BiographyMenuHooks
    {
        public static void HookOn()
        {
            On.ProcessManager.PostSwitchMainProcess += ProcessManager_PostSwitchMainProcess;
            On.Menu.MainMenu.ctor += MainMenu_ctor;
        }

        private static void MainMenu_ctor(On.Menu.MainMenu.orig_ctor orig, Menu.MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
        {
            orig.Invoke(self,manager,showRegionSpecificBkg);

            float buttonWidth = Menu.MainMenu.GetButtonWidth(self.CurrLang);
            Vector2 pos = new Vector2(683f - buttonWidth / 2f, 0f);
            Vector2 size = new Vector2(buttonWidth, 30f);

            MenuTabWrapper menuTabWrapper = new MenuTabWrapper(self, self.pages[0]);
            self.pages[0].subObjects.Add(menuTabWrapper);

            SimpleButton collectionButton = null;
            foreach(var button in self.mainMenuButtons)
            {
                if (button.signalText == "COLLECTION")
                {
                    collectionButton = button;
                    break;
                }
            }

            var biographyButton = new OpSimpleImageButton(new Vector2(collectionButton.pos.x + MainMenu.GetButtonWidth(self.CurrLang) + 10f, collectionButton.pos.y), new Vector2(30f, 30f), CreatureSymbol.SpriteNameOfCreature(new IconSymbol.IconSymbolData(CreatureTemplate.Type.PinkLizard, AbstractPhysicalObject.AbstractObjectType.Creature, 2)))
            {
                description = self.Translate("BIOGRAPHY")
            };
            new UIelementWrapper(menuTabWrapper, biographyButton);

            var OnClickField = typeof(OpSimpleButton).GetField("OnClick", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            var OnClick = OnClickField.GetValue(biographyButton) as OnSignalHandler;
            OnClick += BiographyButton_OnClick;
            OnClickField.SetValue(biographyButton, OnClick);
        }

        private static void ProcessManager_PostSwitchMainProcess(On.ProcessManager.orig_PostSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            if(ID == BiographyMenu.BiographyMenuID)
            {
                self.currentMainLoop = new BiographyMenu(self);
            }
            orig.Invoke(self, ID);
        }

        private static void BiographyButton_OnClick(UIfocusable trigger)
        {
            trigger.wrapper.tabWrapper.menu.PlaySound(SoundID.MENU_Switch_Page_In);
            trigger.wrapper.tabWrapper.menu.manager.RequestMainProcessSwitch(BiographyMenu.BiographyMenuID);
        }
    }
}
