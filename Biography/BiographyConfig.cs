using BepInEx.Logging;
using Menu;
using Menu.Remix.MixedUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Biography
{
    public class BiographyConfig : OptionInterface
    {
        public static Configurable<bool> UnlockAll;

        public OpLabel infoLabel;
        public BiographyConfig()
        {
            UnlockAll = config.Bind<bool>("Biography_UnlockAll", false);
        }

        public override void Initialize()
        {
            base.Initialize();
            OpTab opTab = new OpTab(this, "Options");
            Tabs = new OpTab[]
            {
                opTab
            };

            float biasY = 30f;
            var elements = new UIelement[]
            {
                new OpLabel(30f,550f,Translate("Biography Options"),true),

                new OpCheckBox(UnlockAll,30f,550f - 40f),
                infoLabel = new OpLabel(160f,550f - 40f, Translate("Unlock all creature for biography(spoiler!!!)"), false)
                {
                    verticalAlignment = OpLabel.LabelVAlignment.Center,
                    color = MenuColorEffect.rgbColored
                },
            };
            opTab.AddItems(elements);
        }
    }
}
