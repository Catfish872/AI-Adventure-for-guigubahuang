using Il2CppSystem.Collections.Generic;
using Il2CppSystem.IO;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.U2D;

namespace ReplaceSprite
{
    public static class ExportSpriteAtlas
    {
        private class SpriteData
        {
            public string atlasName;
            public Sprite sprite;
        }

        private static bool isExport;
        private static string exportPath;
        private static TimerCoroutine cor;
        private static System.Collections.Generic.List<SpriteData> spriteDatas;
        private static System.Collections.Generic.List<Texture2D> bgs;

        /// <summary>
        /// 导出游戏中所有图集文件
        /// </summary>
        public static void ExportAll()
        {
            if (isExport)
            {
                return;
            }
            isExport = true;

            var path = OpenFileDialog.OpenFolder("请选择图集导出到的文件夹");
            if (!path.t1)
            {
                return;
            }
            exportPath = path.t2;

            CallQueue cq = new CallQueue();

            cq.Add(new Action(()=> {
                g.ui.OpenUI<UICheckPopup>(UIType.CheckPopup).InitData("提示", "是否导出图集（需要几十分钟）", 2, new Action(()=> {
                    ExportAllAtlas();
                    cq.Next();
                }), new Action(cq.Next));
            }));

            cq.Add(new Action(()=> {
                g.ui.OpenUI<UICheckPopup>(UIType.CheckPopup).InitData("提示", "是否导出大图（需要几个小时）", 2, new Action(() => {
                    ExportAllBG();
                    cq.Next();
                }), new Action(cq.Next));
            }));

            cq.Add(new Action(()=> {
                FileTool.DeleteDir(exportPath + "/SpriteAtlas（导出游戏中的所有图集，仅做大小参考）");

                cor = g.timer.Frame(new Action(UpdateExport), 1, true);
            }));

            cq.Run();
        }



        /// <summary>
        /// 导出图集
        /// </summary>
        private static void ExportAllAtlas()
        {
            //此目录记录了游戏中所有图集文件
            string spriteAtlasNamesPath = Application.dataPath + "/../Mod/modFQA/资源修改教程/Resources（资源对照）/SpriteAtlas（图集，这个资源一般不需要重载）";

            var atlasFiles = FileTool.GetFiles(spriteAtlasNamesPath, "*.spriteAtlas");
            List<string> atlasNames = new List<string>();
            foreach (var item in atlasFiles)
            {
                atlasNames.Add(Path.GetFileNameWithoutExtension(item.Name));
            }

            spriteDatas = new System.Collections.Generic.List<SpriteData>();

            for (int i = 0; i < atlasNames.Count; i++)
            {
                string atlasName = atlasNames[i];

                SpriteAtlas atlas = Resources.Load<SpriteAtlas>("SpriteAtlas/" + atlasName);
                UnhollowerBaseLib.Il2CppReferenceArray<Sprite> sprites = new UnhollowerBaseLib.Il2CppReferenceArray<Sprite>(atlas.spriteCount);
                atlas.GetSprites(sprites);

                for (int j = 0; j < sprites.Length; j++)
                {
                    Sprite sprite = sprites[j];

                    spriteDatas.Add(new SpriteData() { atlasName = atlasName, sprite = sprite });
                }
            }
        }

        /// <summary>
        /// 导出大图
        /// </summary>
        private static void ExportAllBG()
        {
            List<Texture2D> objects = new List<Texture2D>();
            string[] bgPaths = new string[] { "ArtifactSpriteIcon", "BG", "Build", "FaBao", "FortuiousBigBG", "Fortuitous", "loginBG", "MobilePaperBG", "SchoolBG", "SchoolMap", "Scribbles", "Tupo", "Yuejie" };
            for (int i = 0; i < bgPaths.Length; i++)
            {
                var dirs = Resources.LoadAll("Texture");
                foreach (var item in dirs)
                {
                    Texture2D tex = item.TryCast<Texture2D>();

                    if (tex != null)
                    {
                        objects.Add(tex);
                    }
                }
            }

            bgs = new System.Collections.Generic.List<Texture2D>();
            foreach (var item in objects)
            {
                bgs.Add(item);
            }
        }

        /// <summary>
        /// 导出所有的图片
        /// </summary>
        private static void UpdateExport()
        {
            if (Time.frameCount%2 == 0)
            {
                return;
            }
            if (spriteDatas == null && bgs == null)
            {
                cor?.Stop();
                g.ui.CloseUI(UIType.Loading);
                return;
            }
            if ((spriteDatas == null || spriteDatas.Count == 0) && (bgs == null || bgs.Count == 0))
            {
                cor?.Stop();
                g.ui.CloseUI(UIType.Loading);
                Application.OpenURL(exportPath + "/SpriteAtlas（导出游戏中的所有图集，仅做大小参考");
                return;
            }
            string log = "导出图集中，剩余：" + ((spriteDatas == null ? 0 : spriteDatas.Count) + (bgs == null ? 0 : bgs.Count));

            g.ui.OpenUI<UILoading>(UIType.Loading).InitData(log);

            if (spriteDatas != null && spriteDatas.Count > 0)
            {
                SpriteData spriteData = spriteDatas[spriteDatas.Count - 1];
                Texture2D tex = DuplicateTexture(spriteData.sprite);
                string spritePath = exportPath + "/SpriteAtlas（导出游戏中的所有图集，仅做大小参考）/" + spriteData.atlasName + "/" + spriteData.sprite.name.Replace("(Clone)", "");
                FileTool.WriteByte(spritePath + ".png", ImageConversion.EncodeToPNG(tex));
                GameObject.Destroy(tex);
                spriteDatas.RemoveAt(spriteDatas.Count - 1);
            }
            else if (bgs != null && bgs.Count > 0)
            {
                Texture2D bgTex = bgs[bgs.Count - 1];
                Texture2D tex = DuplicateTexture(bgTex);
                string spritePath = exportPath + "/SpriteAtlas（导出游戏中的所有图集，仅做大小参考）/BG/" + bgTex.name;
                FileTool.WriteByte(spritePath + ".png", ImageConversion.EncodeToPNG(tex));
                GameObject.Destroy(tex);
                GameObject.Destroy(bgTex);
                bgs.RemoveAt(bgs.Count - 1);
            }
        }

        /// <summary>
        /// 使贴图变为可读取的
        /// </summary>
        private static Texture2D DuplicateTexture(Sprite sprite)
        {
            RenderTexture renderTex = RenderTexture.GetTemporary(sprite.texture.width, sprite.texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(sprite.texture, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableText = new Texture2D(sprite.texture.width, sprite.texture.height);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);

            int texWid = (int)sprite.rect.width;
            int texHei = (int)sprite.rect.height;
            Texture2D newTex = new Texture2D(texWid, texHei);
            Color[] defaultPixels = Enumerable.Repeat<Color>(new Color(0, 0, 0, 0), texWid * texHei).ToArray();
            Color[] pixels = readableText.GetPixels((int)sprite.textureRect.x
            , (int)sprite.textureRect.y
            , (int)sprite.textureRect.width
            , (int)sprite.textureRect.height);
            newTex.SetPixels(defaultPixels);
            newTex.SetPixels((int)sprite.textureRectOffset.x, (int)sprite.textureRectOffset.y, (int)sprite.textureRect.width, (int)sprite.textureRect.height, pixels);
            for (int i = 0; i < newTex.width; i++)
            {
                for (int j = 0; j < newTex.height; j++)
                {
                    var col = newTex.GetPixel(i, j);
                    float v = (col.r + col.g + col.b) / 3f;
                    col = new Color(v, v, v, col.a);
                    newTex.SetPixel(i, j, col);
                }
            }
            newTex.Apply();

            GameObject.Destroy(readableText);

            return newTex;
        }

        /// <summary>
        /// 使贴图变为可读取的
        /// </summary>
        private static Texture2D DuplicateTexture(Texture2D tex)
        {
            RenderTexture renderTex = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(tex, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableText = new Texture2D(tex.width, tex.height);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            for (int i = 0; i < readableText.width; i++)
            {
                for (int j = 0; j < readableText.height; j++)
                {
                    var col = readableText.GetPixel(i, j);
                    float v = (col.r + col.g + col.b) / 3f;
                    col = new Color(v, v, v, col.a);
                    readableText.SetPixel(i, j, col);
                }
            }
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);

            return readableText;
        }
    }
}
