using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ModifDressIDEditor : EditorWindow
{
    private int id;

    [MenuItem("游戏工具/更新/修改88888的ID为其他ID")]
    public static void OpenWindow()
    {
        EditorWindow.GetWindow<ModifDressIDEditor>();
    }

    void OnGUI()
    {
        id = EditorGUILayout.IntField("自定义ID（必须是5位数）：", id);

        if (GUILayout.Button("开始修改"))
        {
            ModifID(id);
        }
    }

    private void ModifID(int id)
    {
        if (id.ToString().Length != 5)
        {
            Debug.LogError("ID必须为5位数！");
            return;
        }
       
        while (true)
        {
            bool isModifFolder = false;

            List<string> dirs = new List<string>();
            dirs.AddRange(Directory.GetDirectories(Application.dataPath + "/立绘资源", "*", SearchOption.AllDirectories));
            dirs.AddRange(Directory.GetDirectories(Application.dataPath + "/Resources/Game/Portrait", "*", SearchOption.AllDirectories));
            dirs.AddRange(Directory.GetDirectories(Application.dataPath + "/Resources/Game/PortraitDynamic", "*", SearchOption.AllDirectories));

            foreach (var item in dirs)
            {
                int folderID = 0;
                int.TryParse(Path.GetFileName(item), out folderID);

                if (folderID.ToString().Length == 8 && folderID.ToString().IndexOf(id.ToString()) == -1)
                {
                    isModifFolder = true;
                    string newFolderName = Path.GetDirectoryName(item) + "/" + id.ToString() + folderID.ToString().Substring(5, folderID.ToString().Length - 5);
                    Directory.Move(item, newFolderName);
                    break;
                }
            }

            if (!isModifFolder)
            {
                break;
            }
        }
        
        List<FileInfo> fileInfos = new List<FileInfo>();
        fileInfos.AddRange(FileTool.GetFiles(Application.dataPath + "/立绘资源"));
        fileInfos.AddRange(FileTool.GetFiles(Application.dataPath + "/Resources/Game/Portrait"));
        fileInfos.AddRange(FileTool.GetFiles(Application.dataPath + "/Resources/Game/PortraitDynamic"));

        foreach (var item in fileInfos)
        {
            int folderID = 0;
            string fullName = item.FullName;
            if (fullName.LastIndexOf(".") != -1)
            {
                fullName = fullName.Substring(0, fullName.LastIndexOf("."));
            }

            int.TryParse(Path.GetFileNameWithoutExtension(fullName), out folderID);

            if (folderID.ToString().Length == 8)
            {
                string oldFileName = Path.GetFileName(item.FullName);
                string newFileName = Path.GetDirectoryName(item.FullName) + "/" + id.ToString() + oldFileName.Substring(5, oldFileName.Length - 5);

                File.Move(item.FullName, newFileName);
            }
        }

        AssetDatabase.Refresh();

        Debug.Log("修改ID成功：" + id);
    }
}
