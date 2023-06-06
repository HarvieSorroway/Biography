using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biography.SimGameCore
{
    public static class SpriteLeaserWarpperHooks
    {
        public static void HookOn()
        {
            //On.RoomCamera.SpriteLeaser.Update += SpriteLeaser_Update;
            //On.RoomCamera.SpriteLeaser.ctor += SpriteLeaser_ctor;
        }

        private static void SpriteLeaser_ctor(On.RoomCamera.SpriteLeaser.orig_ctor orig, RoomCamera.SpriteLeaser self, IDrawable obj, RoomCamera rCam)
        {
            if(rCam.game is SimGame)
            {
                SpriteLeaserWarpper.wappers.Add(self, new SpriteLeaserWarpper(self, new UnityEngine.Vector2(300f, 300f)));
            }
            orig.Invoke(self, obj, rCam);
        }

        private static void SpriteLeaser_Update(On.RoomCamera.SpriteLeaser.orig_Update orig, RoomCamera.SpriteLeaser self, float timeStacker, RoomCamera rCam, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, timeStacker, rCam, camPos);
            if(SpriteLeaserWarpper.wappers.TryGetValue(self, out var wappers) )
            {
                wappers.PostDrawSprites(self);
            }
        }
    }
}
