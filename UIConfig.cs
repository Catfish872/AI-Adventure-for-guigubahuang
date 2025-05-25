using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;

namespace MOD_kqAfiU
{
    public class UIConfig : UIBase
    {
        // 构造函数，接收IntPtr参数
        public UIConfig(IntPtr ptr) : base(ptr)
        {
            Debug.Log("UIConfig构造函数被调用");
        }

        // UI组件
        public GameObject panelObj;
        public GameObject urlTextObj;
        public GameObject keyTextObj;
        public GameObject modelTextObj;
        public GameObject inputUrlObj;
        public GameObject inputKeyObj;
        public GameObject inputModelObj;
        public GameObject confirmButtonObj;
        public GameObject cancelButtonObj;
        public GameObject copyButtonObj;

        // 回调函数
        private Action<string, string, string> callback;

        // 初始化UI数据
        public void InitData(string urlDefault, string keyDefault, string modelDefault, Action<string, string, string> onConfirm)
        {
            Debug.Log("开始初始化Config UI数据");

            // 存储回调
            this.callback = onConfirm;

            try
            {
                // 查找UI组件 - 所有组件都在同一层级
                this.panelObj = transform.Find("Panel").gameObject;
                this.urlTextObj = transform.Find("url").gameObject;
                this.keyTextObj = transform.Find("Key").gameObject;
                this.modelTextObj = transform.Find("model").gameObject;
                this.inputUrlObj = transform.Find("inputurl").gameObject;
                this.inputKeyObj = transform.Find("inputkey").gameObject;
                this.inputModelObj = transform.Find("Inputmodel").gameObject;
                this.confirmButtonObj = transform.Find("Button").gameObject;
                this.cancelButtonObj = transform.Find("ButtonCancel").gameObject;
                this.copyButtonObj = transform.Find("ButtonCopy").gameObject;

                Debug.Log("组件查找结果: Panel=" + (panelObj != null) +
                         ", InputURL=" + (inputUrlObj != null) +
                         ", InputKey=" + (inputKeyObj != null) +
                         ", InputModel=" + (inputModelObj != null));

                // 设置输入框默认文本
                if (this.inputUrlObj != null && !string.IsNullOrEmpty(urlDefault))
                {
                    InputField inputField = this.inputUrlObj.GetComponent<InputField>();
                    if (inputField != null)
                    {
                        inputField.text = urlDefault;
                    }
                }

                if (this.inputKeyObj != null && !string.IsNullOrEmpty(keyDefault))
                {
                    InputField inputField = this.inputKeyObj.GetComponent<InputField>();
                    if (inputField != null)
                    {
                        inputField.text = keyDefault;
                    }
                }

                if (this.inputModelObj != null && !string.IsNullOrEmpty(modelDefault))
                {
                    InputField inputField = this.inputModelObj.GetComponent<InputField>();
                    if (inputField != null)
                    {
                        inputField.text = modelDefault;
                    }
                }

                // 绑定确认按钮事件
                if (this.confirmButtonObj != null)
                {
                    Button confirmButton = this.confirmButtonObj.GetComponent<Button>();
                    if (confirmButton != null)
                    {
                        Action confirmAction = delegate () {
                            string urlInput = inputUrlObj.GetComponent<InputField>().text;
                            string keyInput = inputKeyObj.GetComponent<InputField>().text;
                            string modelInput = inputModelObj.GetComponent<InputField>().text;

                            if (string.IsNullOrEmpty(urlInput) || string.IsNullOrEmpty(keyInput) || string.IsNullOrEmpty(modelInput))
                            {
                                UITipItem.AddTip("所有字段都不能为空！", 2f);
                            }
                            else
                            {
                                // 1. 更新本地配置文件
                                SaveConfig(urlInput, keyInput, modelInput);

                                // 2. 更新ModMain的静态变量
                                ModMain.apiUrl = urlInput;
                                ModMain.apiKey = keyInput;
                                ModMain.modelName = modelInput;

                                // 3. 调用回调函数
                                if (callback != null)
                                {
                                    callback(urlInput, keyInput, modelInput);
                                }

                                UITipItem.AddTip("配置已保存", 2f);
                                g.ui.CloseAllUI();
                            }
                        };
                        confirmButton.onClick.AddListener(confirmAction);
                    }
                }

                // 绑定取消按钮事件
                if (this.cancelButtonObj != null)
                {
                    Button cancelButton = this.cancelButtonObj.GetComponent<Button>();
                    if (cancelButton != null)
                    {
                        Action cancelAction = delegate () {
                            g.ui.CloseAllUI();
                        };
                        cancelButton.onClick.AddListener(cancelAction);
                    }
                }

                // 绑定复制按钮事件 - 从其他mod中读取数据并填充到当前输入框
                if (this.copyButtonObj != null)
                {
                    Button copyButton = this.copyButtonObj.GetComponent<Button>();
                    if (copyButton != null)
                    {
                        Action copyAction = delegate () {
                            try
                            {
                                // 从g.data.dataObj.data读取其他mod的配置
                                string apiUrl = "";
                                string apiKey = "";
                                string modelName = "";

                                if (g.data != null && g.data.dataObj != null && g.data.dataObj.data != null)
                                {
                                    // 读取API配置
                                    if (g.data.dataObj.data.ContainsKey("apiUrl"))
                                    {
                                        apiUrl = g.data.dataObj.data.GetString("apiUrl");
                                    }

                                    if (g.data.dataObj.data.ContainsKey("apiKey"))
                                    {
                                        apiKey = g.data.dataObj.data.GetString("apiKey");
                                    }

                                    if (g.data.dataObj.data.ContainsKey("modelName"))
                                    {
                                        modelName = g.data.dataObj.data.GetString("modelName");
                                    }

                                    // 填充到输入框
                                    if (!string.IsNullOrEmpty(apiUrl) && inputUrlObj != null)
                                    {
                                        InputField urlField = inputUrlObj.GetComponent<InputField>();
                                        if (urlField != null) urlField.text = apiUrl;
                                    }

                                    if (!string.IsNullOrEmpty(apiKey) && inputKeyObj != null)
                                    {
                                        InputField keyField = inputKeyObj.GetComponent<InputField>();
                                        if (keyField != null) keyField.text = apiKey;
                                    }

                                    if (!string.IsNullOrEmpty(modelName) && inputModelObj != null)
                                    {
                                        InputField modelField = inputModelObj.GetComponent<InputField>();
                                        if (modelField != null) modelField.text = modelName;
                                    }

                                    UITipItem.AddTip("已从其他mod复制配置", 2f);
                                }
                                else
                                {
                                    UITipItem.AddTip("无法获取其他mod配置", 2f);
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.Log("复制配置失败: " + ex.ToString());
                                UITipItem.AddTip("复制配置失败", 2f);
                            }
                        };
                        copyButton.onClick.AddListener(copyAction);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log("初始化Config UI失败: " + ex.ToString());
            }
        }

        // 保存配置到config.json
        private void SaveConfig(string apiUrl, string apiKey, string modelName)
        {
            try
            {
                string configPath = "config.json";

                // 读取原始JSON
                string originalJson = "";
                if (File.Exists(configPath))
                {
                    originalJson = File.ReadAllText(configPath);
                }

                // 解析为JObject
                var jsonObj = string.IsNullOrEmpty(originalJson)
                    ? new Newtonsoft.Json.Linq.JObject()
                    : JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(originalJson);

                // 只更新特定字段
                jsonObj["ApiUrl"] = apiUrl;
                jsonObj["ApiKey"] = apiKey;
                jsonObj["ModelName"] = modelName;

                // 保存回文件
                File.WriteAllText(configPath, jsonObj.ToString(Formatting.Indented));

                // 更新ModMain静态变量
                ModMain.apiUrl = apiUrl;
                ModMain.apiKey = apiKey;
                ModMain.modelName = modelName;

                Debug.Log("配置已保存到文件(保留自定义字段)");
            }
            catch (Exception ex)
            {
                Debug.LogError($"保存配置时发生错误: {ex.Message}");
            }
        }

        // 打开配置UI的静态方法
        public static void OpenConfigUI(string urlDefault, string keyDefault, string modelDefault, Action<string, string, string> callback)
        {
            Debug.Log("开始打开ConfigUI...");

            // 加载预制体
            GameObject prefab = g.res.Load<GameObject>("ui/config/config");
            Debug.Log("预制体加载结果: " + (prefab != null ? "成功" : "失败"));
            if (prefab == null) return;

            GameObject uiObj = GameObject.Instantiate(prefab);
            Debug.Log("预制体实例化完成: " + uiObj.name);

            // 设置父对象为Canvas
            Transform canvasTransform = GameObject.Find("Canvas")?.transform;
            Debug.Log("Canvas查找结果: " + (canvasTransform != null ? "成功" : "失败"));
            if (canvasTransform == null) return;

            uiObj.transform.SetParent(canvasTransform, false);
            RectTransform rectTransform = uiObj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = Vector2.zero;
                Debug.Log("UI位置设置完成");
            }

            Canvas canvas = uiObj.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = uiObj.GetComponentInChildren<Canvas>();
            }

            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 100;
                Debug.Log("UI排序顺序设置为: " + canvas.sortingOrder);
            }

            // 输出UI对象的子对象结构，帮助诊断
            Debug.Log("UI对象子级数量: " + uiObj.transform.childCount);
            for (int i = 0; i < uiObj.transform.childCount; i++)
            {
                Debug.Log("子对象 " + i + ": " + uiObj.transform.GetChild(i).name);
            }

            // 直接查找并设置组件
            InputField inputUrl = uiObj.transform.Find("inputurl")?.GetComponent<InputField>();
            InputField inputKey = uiObj.transform.Find("inputkey")?.GetComponent<InputField>();
            InputField inputModel = uiObj.transform.Find("inputmodel")?.GetComponent<InputField>();

            if (inputUrl != null) inputUrl.text = urlDefault;
            if (inputKey != null) inputKey.text = keyDefault;
            if (inputModel != null) inputModel.text = modelDefault;

            Debug.Log($"{modelDefault}");

            Button confirmButton = uiObj.transform.Find("Button")?.GetComponent<Button>();
            Button cancelButton = uiObj.transform.Find("ButtonCancel")?.GetComponent<Button>();
            Button copyButton = uiObj.transform.Find("ButtonCopy")?.GetComponent<Button>();

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                Action confirmAction = delegate () {
                    string urlInput = inputUrl?.text ?? "";
                    string keyInput = inputKey?.text ?? "";
                    string modelInput = inputModel?.text ?? "";

                    if (string.IsNullOrEmpty(urlInput) || string.IsNullOrEmpty(keyInput) || string.IsNullOrEmpty(modelInput))
                    {
                        UITipItem.AddTip("所有字段都不能为空！", 2f);
                    }
                    else
                    {
                        try
                        {
                            // 1. 读取现有配置
                            ModConfig config = Config.ReadConfig();
                            if (config == null)
                            {
                                Config.InitConfig();
                                config = Config.ReadConfig();

                                if (config == null)
                                {
                                    config = new ModConfig();
                                }
                            }

                            // 2. 更新配置
                            config.ApiUrl = urlInput;
                            config.ApiKey = keyInput;
                            config.ModelName = modelInput;

                            // 3. 保存到文件
                            string configPath = "config.json";
                            string jsonContent = JsonConvert.SerializeObject(config, Formatting.Indented);
                            File.WriteAllText(configPath, jsonContent);

                            // 4. 更新ModMain静态变量
                            ModMain.apiUrl = urlInput;
                            ModMain.apiKey = keyInput;
                            ModMain.modelName = modelInput;

                            if (callback != null)
                            {
                                callback(urlInput, keyInput, modelInput);
                            }

                            UITipItem.AddTip("配置已保存", 2f);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError("保存配置失败: " + ex.ToString());
                            UITipItem.AddTip("保存配置失败", 2f);
                        }

                        GameObject.Destroy(uiObj);
                    }
                };
                confirmButton.onClick.AddListener(confirmAction);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveAllListeners();
                Action cancelAction = delegate () {
                    GameObject.Destroy(uiObj);
                };
                cancelButton.onClick.AddListener(cancelAction);
            }

            if (copyButton != null)
            {
                copyButton.onClick.RemoveAllListeners();
                Action copyAction = delegate () {
                    try
                    {
                        // 从g.data.dataObj.data读取其他mod的配置
                        string apiUrl = "";
                        string apiKey = "";
                        string modelName = "";

                        if (g.data != null && g.data.dataObj != null && g.data.dataObj.data != null)
                        {
                            // 读取API配置
                            if (g.data.dataObj.data.ContainsKey("apiUrl"))
                            {
                                apiUrl = g.data.dataObj.data.GetString("apiUrl");
                            }

                            if (g.data.dataObj.data.ContainsKey("apiKey"))
                            {
                                apiKey = g.data.dataObj.data.GetString("apiKey");
                            }

                            if (g.data.dataObj.data.ContainsKey("modelName"))
                            {
                                modelName = g.data.dataObj.data.GetString("modelName");
                            }

                            // 填充到输入框
                            if (!string.IsNullOrEmpty(apiUrl) && inputUrl != null)
                            {
                                inputUrl.text = apiUrl;
                            }

                            if (!string.IsNullOrEmpty(apiKey) && inputKey != null)
                            {
                                inputKey.text = apiKey;
                            }

                            if (!string.IsNullOrEmpty(modelName) && inputModel != null)
                            {
                                inputModel.text = modelName;
                            }

                            UITipItem.AddTip("已从神识传音复制配置", 2f);
                        }
                        else
                        {
                            UITipItem.AddTip("无法获取神识传音配置，请检查神识配置", 2f);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Log("复制配置失败: " + ex.ToString());
                        UITipItem.AddTip("复制配置失败", 2f);
                    }
                };
                copyButton.onClick.AddListener(copyAction);
            }

            Debug.Log("UI设置完成");
        }
    }
}