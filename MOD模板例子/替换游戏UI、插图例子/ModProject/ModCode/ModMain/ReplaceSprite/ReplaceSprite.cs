using HarmonyLib;
using Il2CppSystem.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace ReplaceSprite
{
    public class ReplaceSprite
    {
        private static ReplaceSprite _instance;
        public static ReplaceSprite instance {
            get 
            { 
                if (_instance == null)
                {
                    _instance = new ReplaceSprite();
                    _instance.Init();
                }
                return _instance;
            }
        }

        private Dictionary<string, Dictionary<string, string>> spritePaths = new Dictionary<string, Dictionary<string, string>>();
        private Dictionary<string, Dictionary<string, Sprite>> spriteCache = new Dictionary<string, Dictionary<string, Sprite>>();

        public void Init()
        {
            InitAtlasSprite();

            g.events.On(EGameType.OpenUIEnd, new Action<ETypeData>(OnOpenUI));
        }

        public void Destroy()
        {
            g.events.Off(EGameType.OpenUIEnd, new Action<ETypeData>(OnOpenUI));
        }

        private void OnOpenUI(ETypeData e)
        {
            //打开UI时替换所有精灵
            EGameTypeData.OpenUIEnd edata = e.Cast<EGameTypeData.OpenUIEnd>();

            var images = edata.ui.transform.GetComponentsInChildren<Image>(true);

            foreach (var item in images)
            {
                if (item == null || item.sprite == null || item.sprite.texture == null || item.mainTexture == null)
                {
                    continue;
                }

                string spriteName = item.sprite.name.Replace("(Clone)", "");

                //大图
                if (spriteName == item.mainTexture.name)
                {
                    Sprite spr2 = GetSprite("BG", spriteName);
                    if (spr2 != null)
                    {
                        item.sprite = spr2;
                    }
                    continue;
                }

                //图集
                string[] atlasNames = item.mainTexture.name.Split('-');
                if (atlasNames.Length != 6)
                {
                    continue;
                }
                //图片倒数第二位是图集名字
                string atlasName = atlasNames[atlasNames.Length-2];
                Sprite spr = GetSprite(atlasName, spriteName);
                if (spr != null)
                {
                    item.sprite = spr;
                }
            }
        }

        /// <summary>
        /// 重载获取精灵脚本，返回替换的资源
        /// </summary>
        [HarmonyPatch(typeof(SpriteAtlas), "GetSprite")]
        class Patch_SpriteAtlas_GetSprite
        {
            [HarmonyPrefix]
            private static bool GetSprite(ref Sprite __result, SpriteAtlas __instance, ref string name)
            {
                Sprite spr = instance.GetSprite(__instance.name, name);
                if (spr != null)
                {
                    __result = spr;
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// 重载获取大图脚本，返回替换的资源
        /// </summary>
        [HarmonyPatch(typeof(SpriteTool), "GetSpriteBigTex")]
        class Patch_SpriteTool_GetSpriteBigTex
        {
            [HarmonyPrefix]
            private static bool GetSpriteBigTex(ref Sprite __result, ref string path)
            {
                Sprite spr = instance.GetSprite("BG", Path.GetFileName(path));
                if (spr != null)
                {
                    __result = spr;
                    return false;
                }
                return true;
            }
        }
        
        /// <summary>
        /// 刷新图集
        /// </summary>
        public void UpdateAtlasSprite()
        {
            InitAtlasSprite();
        }

        /// <summary>
        /// 初始所有图集精灵
        /// </summary>
        public void InitAtlasSprite()
        {
            for (int i = 0; i < g.mod.allModPaths.Count; i++)
            {
                string direPath = g.mod.allModPaths[i].t2 + "/ModAssets/SpriteAtlas";
                if (!System.IO.Directory.Exists(direPath))
                {
                    continue;
                }
                var dirs = System.IO.Directory.GetDirectories(direPath);

                foreach (var item in dirs)
                {
                    string atlasName = Path.GetFileName(item);

                    var filesPNG = FileTool.GetFiles(item, "*.png");
                    var filesJPG = FileTool.GetFiles(item, "*.jpg");

                    if (!spritePaths.ContainsKey(atlasName))
                    {
                        spritePaths[atlasName] = new Dictionary<string, string>();
                    }
                    foreach (var item2 in filesPNG)
                    {
                        spritePaths[atlasName][Path.GetFileNameWithoutExtension(item2.FullName)] = item2.FullName;
                    }
                    foreach (var item2 in filesJPG)
                    {
                        spritePaths[atlasName][Path.GetFileNameWithoutExtension(item2.FullName)] = item2.FullName;
                    }
                }
            }
        }

        /// <summary>
        /// 获取精灵图片
        /// </summary>
        private Sprite GetSprite(string atlasName, string spriteName)
        {
            if (spritePaths.ContainsKey(atlasName) && spritePaths[atlasName].ContainsKey(spriteName))
            {
                if (spriteCache.ContainsKey(atlasName) && spriteCache[atlasName].TryGetValue(spriteName, out var sp) && sp != null)
                {
                    return sp;
                }

                string path = spritePaths[atlasName][spriteName];
                if (!spriteCache.ContainsKey(atlasName))
                {
                    spriteCache[atlasName] = new Dictionary<string, Sprite>();
                }
                var isEncrypt = EncryptTexture.IsEncrypt(FileTool.ReadByte(path));
                Sprite spr = ModTool.LoadSpriteInFile(path, isEncrypt);
                spriteCache[atlasName][spriteName] = spr;

                return spr;
            }

            return null;
        }
    }
}
