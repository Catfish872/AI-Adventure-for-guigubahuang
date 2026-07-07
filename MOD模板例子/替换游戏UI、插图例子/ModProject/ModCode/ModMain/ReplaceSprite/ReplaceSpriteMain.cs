using HarmonyLib;
using Il2CppSystem.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ReplaceSprite
{
    public class ReplaceSpriteMain
    {
        private static ReplaceSpriteMain instance;
        private ReplaceSprite replaceSprite;
        
        /// <summary>
        /// 游戏初始化时，初始化替换UI代码
        /// </summary>
        [HarmonyPatch(typeof(SceneLogin), "Init")]
        class Patch_SceneLogin_Init
        {
            [HarmonyPrefix]
            private static bool Init(SceneLogin __instance)
            {
                instance = new ReplaceSpriteMain();
                instance.replaceSprite = ReplaceSprite.instance;
                instance.replaceSprite.InitAtlasSprite();

                g.timer.Frame(new Action(instance.OnUpdate), 1, true);

                return true;
            }
        }
        
        /// <summary>
        /// 每帧调用的函数
        /// </summary>
        private void OnUpdate()
        {
            //MOD调试模式下
            if (GameConf.debugErrorMod && ModMgr.debugModExportData != null)
            {
                if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.F6)) // 加密目录的图片
                {
                    EncryptTexture.Encrypt();
                }
                if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.F5))
                {
                    ExportSpriteAtlas.ExportAll();
                }
                if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.F11))
                {
                    replaceSprite.UpdateAtlasSprite();
                }
            }
        }
    }
}
