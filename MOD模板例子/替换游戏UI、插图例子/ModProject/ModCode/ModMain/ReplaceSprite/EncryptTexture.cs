using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReplaceSprite
{
    public static class EncryptTexture
    {

        static bool isEncrypt;
        static string encryptPath;
        static string savePath;
        /// <summary>
        /// 加密图片
        /// </summary>
        public static void Encrypt()
        {
            Console.WriteLine("想要加密图片");
            if (isEncrypt)
            {
                if (g.ui.GetUI<UICheckPopup>(UIType.CheckPopup) == null)
                {
                    g.ui.OpenUI<UICheckPopup>(UIType.CheckPopup).InitData(GameTool.LS("common_tishi"), "正在加密图片！", 1);
                }
                return;
            }
            encryptPath = null;
            savePath = null;
            isEncrypt = true;

            CallQueue cq = new CallQueue();
            cq.Add(new Action(() =>
            {
                var action = new Action(() =>
                {
                    DataStruct<bool, string> path = OpenFileDialog.OpenFolder("请选择要加密图片目录");
                    if (!path.t1)
                    {
                        Console.WriteLine("没有选择要加密的目录");
                        isEncrypt = false;
                        return;
                    }
                    encryptPath = path.t2;
                    cq.Next();
                });
                g.ui.OpenUI<UICheckPopup>(UIType.CheckPopup).InitData(GameTool.LS("common_tishi"), "请选择要加密图片目录。", 1, action);
            }));

            cq.Add(new Action(() =>
            {
                var action = new Action(() =>
                {
                    DataStruct<bool, string> topath = OpenFileDialog.OpenFolder("请选择保存加密图片的文件夹");
                    if (!topath.t1)
                    {
                        Console.WriteLine("没有选择保存加密图片的文件夹");
                        isEncrypt = false;
                        return;
                    }
                    savePath = topath.t2;
                    cq.Next();
                });
                g.ui.OpenUI<UICheckPopup>(UIType.CheckPopup).InitData(GameTool.LS("common_tishi"), "请选择保存加密图片的文件夹。", 1, action);
            }));


            cq.Add(new Action(() =>
            {
                Thread thread = new Thread(() =>
                {
                    string[] files = Directory.GetFiles(encryptPath, "*.png", SearchOption.AllDirectories);
                    foreach (string file in files)
                    {
                        UnhollowerBaseLib.Il2CppStructArray<byte> bytes = FileTool.ReadByte(file);
                        var toBytes = EncryptTool.EncryptMult(bytes, GameConf.modEncryPassword);
                        var saveFile = file.Replace("\\", "/").Replace(encryptPath.Replace("\\", "/"), savePath);
                        var toDir = Path.GetDirectoryName(saveFile);
                        if (!Directory.Exists(toDir))
                        {
                            Directory.CreateDirectory(toDir);
                        }
                        Console.WriteLine(file + " >> " + saveFile);
                        FileTool.WriteByte(saveFile, toBytes);
                    }
                    Console.WriteLine("加密完成");
                    UnityEngine.Application.OpenURL(savePath);
                    isEncrypt = false;
                });
                thread.Start();
            }));

            cq.Run();
        }

        public static bool IsEncrypt(byte[] bytes)
        {
            string encryptKey = "2a;ad.,&fSf^SX.,:12@D";
            for (int i = 0; i < encryptKey.Length && i < bytes.Length; i++)
            {
                byte k = (byte)encryptKey[i];
                k = (byte)(k ^ 2 * 5 / 3);
                if (bytes[i] != k)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
