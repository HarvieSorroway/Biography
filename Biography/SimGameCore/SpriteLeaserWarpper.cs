using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Biography.SimGameCore
{
    public class SpriteLeaserWarpper
    {
        public static ConditionalWeakTable<RoomCamera.SpriteLeaser, SpriteLeaserWarpper> wappers = new ConditionalWeakTable<RoomCamera.SpriteLeaser, SpriteLeaserWarpper>();
        public WeakReference<RoomCamera.SpriteLeaser> spriteLeaserRef;

        public Vector2 hookPos;

        public SpriteLeaserWarpper(RoomCamera.SpriteLeaser spriteLeaser,Vector2 hookPos)
        {
            spriteLeaserRef = new WeakReference<RoomCamera.SpriteLeaser>(spriteLeaser);
            this.hookPos = hookPos;
        }

        public void PostDrawSprites(RoomCamera.SpriteLeaser spriteLeaser)
        {
            Vector2 firstSpritePos = GetRootPos(spriteLeaser.sprites[0]);
            foreach(var sprite in spriteLeaser.sprites)
            {
                MoveByDelta(firstSpritePos, hookPos, sprite);
            }
        }

        public Vector2 GetRootPos(FSprite fSprite)
        {
            if(fSprite is CustomFSprite)
            {
                CustomFSprite customFSprite = (CustomFSprite)fSprite;
                return customFSprite.vertices[0];
            }
            else if(fSprite is TriangleMesh)
            {
                TriangleMesh triangleMesh = (TriangleMesh)fSprite;
                return triangleMesh.vertices[0];
            }
            return fSprite.GetPosition();
        }

        public void MoveByDelta(Vector2 firstSpritePos, Vector2 root,FSprite fSprite)
        {
            Vector2 origRoot = GetRootPos(fSprite);
            Vector2 delta = origRoot - firstSpritePos;

            if(fSprite is CustomFSprite)
            {
                CustomFSprite customFSprite = (CustomFSprite)fSprite;
                for (int i = 0;i < 4; i++)
                {
                    customFSprite.MoveVertice(i,customFSprite.vertices[i] - origRoot + delta + root);
                }
            }
            else if (fSprite is TriangleMesh)
            {
                TriangleMesh triangleMesh = (TriangleMesh)fSprite;
                for(int i = 0;i < triangleMesh.vertices.Length;i++)
                {
                    triangleMesh.MoveVertice(i, triangleMesh.vertices[i] - origRoot + delta + root);
                }
            }
            else
                fSprite.SetPosition(root + delta);
        }
    }
}
