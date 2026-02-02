using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using static Il2CppSystem.Net.ServicePointManager;
using System.Linq;
using System.Collections.Generic;

namespace MOD_kqAfiU
{
    public class UIConfig : UIBase
    {
        public static UIConfig Instance { get; private set; }
        // 构造函数，接收IntPtr参数
        public static InputField GeneralEventInput;
        public static InputField GenderEventInput;
        public static InputField LoverSpouseEventInput;
        public static InputField ParentChildEventInput;
        public static InputField MasterStudentEventInput;
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
        public GameObject inputProbObj;
        public GameObject confirmButtonObj;
        public GameObject cancelButtonObj;
        public GameObject copyButtonObj;
        public GameObject inputPromptPrefixObj;
        public GameObject inputPromptSuffixObj;
        public GameObject inputGeneralEventObj;
        public GameObject inputGenderEventObj;
        public GameObject inputLoverSpouseEventObj;
        public GameObject inputParentChildEventObj;
        public GameObject inputMasterStudentEventObj;
        public GameObject promptPrefixResetObj;
        public GameObject promptSuffixResetObj;
        public GameObject generalEventResetObj;
        public GameObject genderEventResetObj;
        public GameObject loverSpouseEventResetObj;
        public GameObject parentChildEventResetObj;
        public GameObject masterStudentEventResetObj;
        public GameObject inputShortProbObj; 
        public GameObject rewardAndShortButtonObj;

        // 回调函数
        private Action<string, string, string> callback;

        public static float CalculateUIScale()
        {
            // 基准：1920分辨率下scale=1.0
            // 更高分辨率下按比例缩小，保持视觉一致
            if (Screen.width <= 2560)
                return 0.95f; // 低于等于1920时保持原大小
            else
                return 2560f / Screen.width; // 高于2560时按比例缩小
        }

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
                this.inputProbObj = transform.Find("inputprob").gameObject;
                this.confirmButtonObj = transform.Find("Button").gameObject;
                this.cancelButtonObj = transform.Find("ButtonCancel").gameObject;
                this.copyButtonObj = transform.Find("ButtonCopy").gameObject;
                this.inputPromptPrefixObj = transform.Find("inputpromptPrefix").gameObject;
                this.inputPromptSuffixObj = transform.Find("inputpromptSuffix").gameObject;
                this.inputGeneralEventObj = transform.Find("inputGeneralEvent").gameObject;
                this.inputGenderEventObj = transform.Find("inputGenderEvent").gameObject;
                this.inputLoverSpouseEventObj = transform.Find("inputLoverSpouseEvent").gameObject;
                this.inputParentChildEventObj = transform.Find("inputParentChildEvent").gameObject;
                this.inputMasterStudentEventObj = transform.Find("inputMasterStudentEvent").gameObject;
                this.promptPrefixResetObj = transform.Find("promptPrefixReset").gameObject;
                this.promptSuffixResetObj = transform.Find("promptSuffixReset").gameObject;
                this.generalEventResetObj = transform.Find("GeneralEventReset").gameObject;
                this.genderEventResetObj = transform.Find("GenderEventReset").gameObject;
                this.loverSpouseEventResetObj = transform.Find("LoverSpouseEventReset").gameObject;
                this.parentChildEventResetObj = transform.Find("ParentChildEventReset").gameObject;
                this.masterStudentEventResetObj = transform.Find("MasterStudentEventReset").gameObject;
                this.inputShortProbObj = transform.Find("inputshortprob").gameObject;
                this.rewardAndShortButtonObj = transform.Find("rewardandshort").gameObject;

  

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

                // 设置概率输入框默认值
                if (this.inputProbObj != null)
                {
                    InputField inputField = this.inputProbObj.GetComponent<InputField>();
                    if (inputField != null)
                    {
                        // 从config.json读取EncounterProbability
                        try
                        {
                            string configPath = "config.json";
                            if (File.Exists(configPath))
                            {
                                string jsonContent = File.ReadAllText(configPath);
                                var jsonObj = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(jsonContent);
                                if (jsonObj != null && jsonObj.ContainsKey("EncounterProbability"))
                                {
                                    float probability = jsonObj["EncounterProbability"].ToObject<float>();
                                    inputField.text = probability.ToString();
                                }
                                else
                                {
                                    inputField.text = "0.05";
                                }
                            }
                            else
                            {
                                inputField.text = "0.05";
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.Log("读取概率配置失败: " + ex.ToString());
                            inputField.text = "0.05";
                        }
                    }
                }

                if (this.inputShortProbObj != null)
                {
                    InputField inputField = this.inputShortProbObj.GetComponent<InputField>();
                    if (inputField != null)
                    {
                        try
                        {
                            string configPath = "config.json";
                            if (File.Exists(configPath))
                            {
                                string jsonContent = File.ReadAllText(configPath);
                                var jsonObj = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(jsonContent);
                                if (jsonObj != null && jsonObj.ContainsKey("ShortEventProbability"))
                                {
                                    float shortProbability = jsonObj["ShortEventProbability"].ToObject<float>();
                                    inputField.text = shortProbability.ToString();
                                }
                                else
                                {
                                    inputField.text = "0.015";
                                }
                            }
                            else
                            {
                                inputField.text = "0.015";
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.Log("读取短奇遇概率配置失败: " + ex.ToString());
                            inputField.text = "0.015";
                        }
                    }
                }

                if (this.inputPromptPrefixObj != null)
                {
                    InputField inputField = this.inputPromptPrefixObj.GetComponent<InputField>();
                    if (inputField != null)
                    {
                        try
                        {
                            string configPath = "config.json";
                            if (File.Exists(configPath))
                            {
                                string jsonContent = File.ReadAllText(configPath);
                                var jsonObj = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(jsonContent);
                                if (jsonObj != null && jsonObj.ContainsKey("PromptPrefix"))
                                {
                                    inputField.text = jsonObj["PromptPrefix"].ToString();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.Log("读取前缀提示词配置失败: " + ex.ToString());
                        }
                    }
                }

                // 设置后缀提示词默认值
                if (this.inputPromptSuffixObj != null)
                {
                    InputField inputField = this.inputPromptSuffixObj.GetComponent<InputField>();
                    if (inputField != null)
                    {
                        try
                        {
                            string configPath = "config.json";
                            if (File.Exists(configPath))
                            {
                                string jsonContent = File.ReadAllText(configPath);
                                var jsonObj = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(jsonContent);
                                if (jsonObj != null && jsonObj.ContainsKey("PromptSuffix"))
                                {
                                    inputField.text = jsonObj["PromptSuffix"].ToString();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.Log("读取后缀提示词配置失败: " + ex.ToString());
                        }
                    }
                }

                // 设置事件素材库默认值
                SetEventListDefaultValue(this.inputGeneralEventObj, "EventTypes");
                SetEventListDefaultValue(this.inputGenderEventObj, "OppositeGenderEventTypes");
                SetEventListDefaultValue(this.inputLoverSpouseEventObj, "LoverSpouseEventTypes");
                SetEventListDefaultValue(this.inputParentChildEventObj, "ParentChildEventTypes");
                SetEventListDefaultValue(this.inputMasterStudentEventObj, "MasterStudentEventTypes");

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
                            string probInput = inputProbObj.GetComponent<InputField>().text;
                            string shortProbInput = inputShortProbObj != null ? inputShortProbObj.GetComponent<InputField>().text : "0.015";
                            string promptPrefixInput = inputPromptPrefixObj != null ? inputPromptPrefixObj.GetComponent<InputField>().text : "";
                            string promptSuffixInput = inputPromptSuffixObj != null ? inputPromptSuffixObj.GetComponent<InputField>().text : "";
                            string generalEventInput = inputGeneralEventObj != null ? inputGeneralEventObj.GetComponent<InputField>().text : "";
                            string genderEventInput = inputGenderEventObj != null ? inputGenderEventObj.GetComponent<InputField>().text : "";
                            string loverSpouseEventInput = inputLoverSpouseEventObj != null ? inputLoverSpouseEventObj.GetComponent<InputField>().text : "";
                            string parentChildEventInput = inputParentChildEventObj != null ? inputParentChildEventObj.GetComponent<InputField>().text : "";
                            string masterStudentEventInput = inputMasterStudentEventObj != null ? inputMasterStudentEventObj.GetComponent<InputField>().text : "";


                            if (string.IsNullOrEmpty(urlInput) || string.IsNullOrEmpty(keyInput) || string.IsNullOrEmpty(modelInput) || string.IsNullOrEmpty(probInput))
                            {
                                UITipItem.AddTip("所有字段都不能为空！", 2f);
                            }
                            else
                            {
                                // 验证概率输入
                                float probability;
                                if (!float.TryParse(probInput, out probability) || probability < 0 || probability > 1)
                                {
                                    UITipItem.AddTip("概率值必须是0到1之间的数字！", 2f);
                                    return;
                                }

                                // 验证短奇遇概率输入
                                float shortProbability;
                                if (!float.TryParse(shortProbInput, out shortProbability) || shortProbability < 0 || shortProbability > 1)
                                {
                                    UITipItem.AddTip("短奇遇概率值必须是0到1之间的数字！", 2f);
                                    return;
                                }

                                // 1. 更新本地配置文件
                                SaveConfigWithNewFields(urlInput, keyInput, modelInput, probability, shortProbability,
     promptPrefixInput, promptSuffixInput, generalEventInput, genderEventInput,
     loverSpouseEventInput, parentChildEventInput, masterStudentEventInput);

                                // 2. 更新ModMain的静态变量
                                ModMain.apiUrl = urlInput;
                                ModMain.apiKey = keyInput;
                                ModMain.modelName = modelInput;
                                ModMain.encounterProbability = probability;
                                ModMain.shortEventProbability = shortProbability;
                             

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
                        BindResetButton(this.promptPrefixResetObj, this.inputPromptPrefixObj, () => Tools.HardcodedDefaultPromptPrefix);
                        BindResetButton(this.promptSuffixResetObj, this.inputPromptSuffixObj, () => Tools.HardcodedDefaultPromptSuffix);
                        BindResetButton(this.generalEventResetObj, this.inputGeneralEventObj, () => string.Join("\n", Tools.HardcodedDefaultGeneralEventTypes));
                        BindResetButton(this.genderEventResetObj, this.inputGenderEventObj, () => string.Join("\n", Tools.HardcodedDefaultOppositeGenderEventTypes));
                        BindResetButton(this.loverSpouseEventResetObj, this.inputLoverSpouseEventObj, () => string.Join("\n", Tools.HardcodedDefaultLoverSpouseEventTypes));
                        BindResetButton(this.parentChildEventResetObj, this.inputParentChildEventObj, () => string.Join("\n", Tools.HardcodedDefaultParentChildEventTypes));
                        BindResetButton(this.masterStudentEventResetObj, this.inputMasterStudentEventObj, () => string.Join("\n", Tools.HardcodedDefaultMasterStudentEventTypes));
                    }
                }
                
            }
            catch (Exception ex)
            {
                Debug.Log("初始化Config UI失败: " + ex.ToString());
            }
        }

        public void RefreshEventInputsFromMemory()
        {
            Debug.Log("UIConfig 正在从内存数据刷新事件列表...");
            try
            {
                Transform uiTransform = UIConfig.Instance.transform;

                var generalEventInput = uiTransform.Find("inputGeneralEvent")?.GetComponent<InputField>();
                if (generalEventInput != null && Tools.DefaultGeneralEventTypes != null)
                    generalEventInput.text = string.Join("\n", Tools.DefaultGeneralEventTypes);

                var genderEventInput = uiTransform.Find("inputGenderEvent")?.GetComponent<InputField>();
                if (genderEventInput != null && Tools.DefaultOppositeGenderEventTypes != null)
                    genderEventInput.text = string.Join("\n", Tools.DefaultOppositeGenderEventTypes);

                var loverSpouseEventInput = uiTransform.Find("inputLoverSpouseEvent")?.GetComponent<InputField>();
                if (loverSpouseEventInput != null && Tools.DefaultLoverSpouseEventTypes != null)
                    loverSpouseEventInput.text = string.Join("\n", Tools.DefaultLoverSpouseEventTypes);

                var parentChildEventInput = uiTransform.Find("inputParentChildEvent")?.GetComponent<InputField>();
                if (parentChildEventInput != null && Tools.DefaultParentChildEventTypes != null)
                    parentChildEventInput.text = string.Join("\n", Tools.DefaultParentChildEventTypes);

                var masterStudentEventInput = uiTransform.Find("inputMasterStudentEvent")?.GetComponent<InputField>();
                if (masterStudentEventInput != null && Tools.DefaultMasterStudentEventTypes != null)
                    masterStudentEventInput.text = string.Join("\n", Tools.DefaultMasterStudentEventTypes);
            }
            catch (Exception ex)
            {
                Debug.LogError("从内存刷新UIConfig事件输入框时出错: " + ex.ToString());
            }
        }

        private void BindResetButton(GameObject resetButtonObj, GameObject inputObj, System.Func<string> getDefaultValue)
        {
            if (resetButtonObj != null && inputObj != null)
            {
                Button resetButton = resetButtonObj.GetComponent<Button>();
                if (resetButton != null)
                {
                    Action resetAction = delegate () {
                        InputField inputField = inputObj.GetComponent<InputField>();
                        if (inputField != null)
                        {
                            inputField.text = getDefaultValue();
                            UITipItem.AddTip("已重置为默认值", 1f);
                        }
                    };
                    resetButton.onClick.AddListener(resetAction);
                }
            }
        }

        private void SetEventListDefaultValue(GameObject inputObj, string configKey)
        {
            if (inputObj != null)
            {
                InputField inputField = inputObj.GetComponent<InputField>();
                if (inputField != null)
                {
                    try
                    {
                        string configPath = "config.json";
                        if (File.Exists(configPath))
                        {
                            string jsonContent = File.ReadAllText(configPath);
                            var jsonObj = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(jsonContent);
                            if (jsonObj != null && jsonObj.ContainsKey(configKey))
                            {
                                var eventList = jsonObj[configKey].ToObject<List<string>>();
                                if (eventList != null)
                                {
                                    inputField.text = string.Join("\n", eventList);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"读取{configKey}配置失败: " + ex.ToString());
                    }
                }
            }
        }

        // 保存配置到config.json
        private void SaveConfigWithNewFields(string apiUrl, string apiKey, string modelName, float encounterProbability, float shortEventProbability,
     string promptPrefix, string promptSuffix, string generalEvent, string genderEvent,
     string loverSpouseEvent, string parentChildEvent, string masterStudentEvent)
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

                // 更新基本字段
                jsonObj["ApiUrl"] = apiUrl;
                jsonObj["ApiKey"] = apiKey;
                jsonObj["ModelName"] = modelName;
                jsonObj["EncounterProbability"] = encounterProbability;
                jsonObj["ShortEventProbability"] = shortEventProbability;

                // 更新提示词字段（只有非空时才保存）
                jsonObj["PromptPrefix"] = promptPrefix ?? "";
                jsonObj["PromptSuffix"] = promptSuffix ?? "";

                // 更新事件列表字段（始终保存，空值保存为[]）
                var generalEventList = string.IsNullOrEmpty(generalEvent) ? new List<string>() :
                    generalEvent.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                jsonObj["EventTypes"] = Newtonsoft.Json.Linq.JArray.FromObject(generalEventList);

                var genderEventList = string.IsNullOrEmpty(genderEvent) ? new List<string>() :
                    genderEvent.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                jsonObj["OppositeGenderEventTypes"] = Newtonsoft.Json.Linq.JArray.FromObject(genderEventList);

                var loverSpouseEventList = string.IsNullOrEmpty(loverSpouseEvent) ? new List<string>() :
                    loverSpouseEvent.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                jsonObj["LoverSpouseEventTypes"] = Newtonsoft.Json.Linq.JArray.FromObject(loverSpouseEventList);

                var parentChildEventList = string.IsNullOrEmpty(parentChildEvent) ? new List<string>() :
                    parentChildEvent.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                jsonObj["ParentChildEventTypes"] = Newtonsoft.Json.Linq.JArray.FromObject(parentChildEventList);

                var masterStudentEventList = string.IsNullOrEmpty(masterStudentEvent) ? new List<string>() :
                    masterStudentEvent.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                jsonObj["MasterStudentEventTypes"] = Newtonsoft.Json.Linq.JArray.FromObject(masterStudentEventList);

                // 保存回文件
                File.WriteAllText(configPath, jsonObj.ToString(Formatting.Indented));

                // 更新ModMain静态变量
                ModMain.apiUrl = apiUrl;
                ModMain.apiKey = apiKey;
                ModMain.modelName = modelName;
                ModMain.encounterProbability = encounterProbability;

                // 更新Tools中的变量
                if (!string.IsNullOrEmpty(promptPrefix))
                    Tools.DefaultPromptPrefix = promptPrefix;
                if (!string.IsNullOrEmpty(promptSuffix))
                    Tools.DefaultPromptSuffix = promptSuffix;
                if (!string.IsNullOrEmpty(generalEvent))
                    Tools.DefaultGeneralEventTypes = generalEvent.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                if (!string.IsNullOrEmpty(genderEvent))
                    Tools.DefaultOppositeGenderEventTypes = genderEvent.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                if (!string.IsNullOrEmpty(loverSpouseEvent))
                    Tools.DefaultLoverSpouseEventTypes = loverSpouseEvent.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                if (!string.IsNullOrEmpty(parentChildEvent))
                    Tools.DefaultParentChildEventTypes = parentChildEvent.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                if (!string.IsNullOrEmpty(masterStudentEvent))
                    Tools.DefaultMasterStudentEventTypes = masterStudentEvent.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

             
            }
            catch (Exception ex)
            {
                Debug.LogError($"保存配置时发生错误: {ex.Message}");
            }
        }
        private static void SetStaticEventListDefaultValue(InputField inputField, string configKey, bool isStringField)
        {
            if (inputField != null)
            {
                try
                {
                    string configPath = "config.json";
                    if (File.Exists(configPath))
                    {
                        string jsonContent = File.ReadAllText(configPath);
                        var jsonObj = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(jsonContent);
                        if (jsonObj != null && jsonObj.ContainsKey(configKey))
                        {
                            if (isStringField)
                            {
                                inputField.text = jsonObj[configKey].ToString();
                            }
                            else
                            {
                                var eventList = jsonObj[configKey].ToObject<List<string>>();
                                if (eventList != null)
                                {
                                    inputField.text = string.Join("\n", eventList);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log($"读取{configKey}配置失败: " + ex.ToString());
                }
            }
        }

        private static void BindStaticResetButton(Button resetButton, InputField inputField, System.Func<string> getDefaultValue)
        {
            if (resetButton != null && inputField != null)
            {
                resetButton.onClick.RemoveAllListeners();
                Action resetAction = delegate () {
                    inputField.text = getDefaultValue();
                    UITipItem.AddTip("已重置为默认值", 1f);
                };
                resetButton.onClick.AddListener(resetAction);
            }
        }

        

        // 打开配置UI的静态方法
        public static void OpenConfigUI(string urlDefault, string keyDefault, string modelDefault, Action<string, string, string> callback)
        {
        

            // 加载预制体
            GameObject prefab = g.res.Load<GameObject>("ui/config/config");
   
            if (prefab == null) return;

            GameObject uiObj = GameObject.Instantiate(prefab);

            //Instance = uiObj.GetComponent<UIConfig>();


            // 设置父对象为Canvas
            Transform canvasTransform = GameObject.Find("Canvas")?.transform;

            if (canvasTransform == null) return;

            uiObj.transform.SetParent(canvasTransform, false);



            RectTransform rectTransform = uiObj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = Vector2.zero;

                float scale = CalculateUIScale();
                rectTransform.localScale = Vector3.one * scale;

                //UITipItem.AddTip($"分辨率: {Screen.width}x{Screen.height}, DPI: {Screen.dpi}, UI缩放: {scale}",10f);

            }

            Canvas canvas = uiObj.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = uiObj.GetComponentInChildren<Canvas>();
            }

            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 9999;
                Debug.Log("UI排序顺序设置为: " + canvas.sortingOrder);
            }


            // 直接查找并设置组件
            InputField inputUrl = uiObj.transform.Find("inputurl")?.GetComponent<InputField>();
            InputField inputKey = uiObj.transform.Find("inputkey")?.GetComponent<InputField>();
            InputField inputModel = uiObj.transform.Find("inputmodel")?.GetComponent<InputField>();
            InputField inputProb = uiObj.transform.Find("inputprob")?.GetComponent<InputField>();
            InputField inputPromptPrefix = uiObj.transform.Find("inputpromptPrefix")?.GetComponent<InputField>();
            InputField inputPromptSuffix = uiObj.transform.Find("inputpromptSuffix")?.GetComponent<InputField>();
            InputField inputGeneralEvent = uiObj.transform.Find("inputGeneralEvent")?.GetComponent<InputField>();
            GeneralEventInput = inputGeneralEvent;

            InputField inputGenderEvent = uiObj.transform.Find("inputGenderEvent")?.GetComponent<InputField>();
            GenderEventInput = inputGenderEvent;

            InputField inputLoverSpouseEvent = uiObj.transform.Find("inputLoverSpouseEvent")?.GetComponent<InputField>();
            LoverSpouseEventInput = inputLoverSpouseEvent;

            InputField inputParentChildEvent = uiObj.transform.Find("inputParentChildEvent")?.GetComponent<InputField>();
            ParentChildEventInput = inputParentChildEvent;

            InputField inputMasterStudentEvent = uiObj.transform.Find("inputMasterStudentEvent")?.GetComponent<InputField>();
            MasterStudentEventInput = inputMasterStudentEvent;

            InputField inputShortProb = uiObj.transform.Find("inputshortprob")?.GetComponent<InputField>();
            Toggle autoColoringToggle = uiObj.transform.Find("Toggle")?.GetComponent<Toggle>();


            if (inputUrl != null) inputUrl.text = urlDefault;
            if (inputKey != null) inputKey.text = keyDefault;
            if (inputModel != null) inputModel.text = modelDefault;

            // 设置概率输入框默认值
            if (inputProb != null)
            {
                try
                {
                    string configPath = "config.json";
                    if (File.Exists(configPath))
                    {
                        string jsonContent = File.ReadAllText(configPath);
                        var jsonObj = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(jsonContent);
                        if (jsonObj != null && jsonObj.ContainsKey("EncounterProbability"))
                        {
                            float probability = jsonObj["EncounterProbability"].ToObject<float>();
                            inputProb.text = probability.ToString();
                        }
                        else
                        {
                            inputProb.text = "0.05";
                        }
                    }
                    else
                    {
                        inputProb.text = "0.05";
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log("读取概率配置失败: " + ex.ToString());
                    inputProb.text = "0.05";
                }
            }

            if (inputShortProb != null)
            {
                try
                {
                    string configPath = "config.json";
                    if (File.Exists(configPath))
                    {
                        string jsonContent = File.ReadAllText(configPath);
                        var jsonObj = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(jsonContent);
                        if (jsonObj != null && jsonObj.ContainsKey("ShortEventProbability"))
                        {
                            float shortProbability = jsonObj["ShortEventProbability"].ToObject<float>();
                            inputShortProb.text = shortProbability.ToString();
                        }
                        else
                        {
                            inputShortProb.text = "0.015";
                        }
                    }
                    else
                    {
                        inputShortProb.text = "0.015";
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log("读取短奇遇概率配置失败: " + ex.ToString());
                    inputShortProb.text = "0.015";
                }
            }

            SetStaticEventListDefaultValue(inputPromptPrefix, "PromptPrefix", true);
            SetStaticEventListDefaultValue(inputPromptSuffix, "PromptSuffix", true);
            SetStaticEventListDefaultValue(inputGeneralEvent, "EventTypes", false);
            SetStaticEventListDefaultValue(inputGenderEvent, "OppositeGenderEventTypes", false);
            SetStaticEventListDefaultValue(inputLoverSpouseEvent, "LoverSpouseEventTypes", false);
            SetStaticEventListDefaultValue(inputParentChildEvent, "ParentChildEventTypes", false);
            SetStaticEventListDefaultValue(inputMasterStudentEvent, "MasterStudentEventTypes", false);
            ModConfig config = Config.ReadConfig();
            if (config != null && autoColoringToggle != null)
            {
                autoColoringToggle.isOn = config.AutoColoringEnabled;
            }

            Debug.Log($"{modelDefault}");

            Button confirmButton = uiObj.transform.Find("Button")?.GetComponent<Button>();
            Button cancelButton = uiObj.transform.Find("ButtonCancel")?.GetComponent<Button>();
            Button copyButton = uiObj.transform.Find("ButtonCopy")?.GetComponent<Button>();
            Button promptPrefixResetButton = uiObj.transform.Find("promptPrefixReset")?.GetComponent<Button>();
            Button promptSuffixResetButton = uiObj.transform.Find("promptSuffixReset")?.GetComponent<Button>();
            Button generalEventResetButton = uiObj.transform.Find("GeneralEventReset")?.GetComponent<Button>();
            Button genderEventResetButton = uiObj.transform.Find("GenderEventReset")?.GetComponent<Button>();
            Button loverSpouseEventResetButton = uiObj.transform.Find("LoverSpouseEventReset")?.GetComponent<Button>();
            Button parentChildEventResetButton = uiObj.transform.Find("ParentChildEventReset")?.GetComponent<Button>();
            Button masterStudentEventResetButton = uiObj.transform.Find("MasterStudentEventReset")?.GetComponent<Button>();
            Button rewardAndShortButton = uiObj.transform.Find("rewardandshort")?.GetComponent<Button>();


            if (rewardAndShortButton != null)
            {
                rewardAndShortButton.onClick.RemoveAllListeners(); // 这一行很重要，防止重复添加监听器
                Action rewardAndShortAction = delegate () {
                    try
                    {
                        // 降低当前UI的优先级，以便新打开的UI在上面
                        Canvas currentCanvas = uiObj.GetComponent<Canvas>(); // 注意这里使用 uiObj 的 Canvas
                        if (currentCanvas == null)
                        {
                            currentCanvas = uiObj.GetComponentInChildren<Canvas>();
                        }
                        if (currentCanvas != null)
                        {
                            currentCanvas.sortingOrder = 5000;
                        }
                        UIRewardAndShortConfig.OpenRewardAndShortConfigUI(() => {
                            // 当子UI关闭后，通过静态实例刷新主UI的事件列表
                            if (UIConfig.Instance != null)
                            {
                                UIConfig.Instance.RefreshEventInputsFromMemory();
                            }
                        });

                    }
                    catch (Exception ex)
                    {
                        Debug.Log("打开奖励和短奇遇配置UI失败: " + ex.ToString());
                        UITipItem.AddTip("打开配置UI失败", 2f);
                    }
                };
                rewardAndShortButton.onClick.AddListener(rewardAndShortAction);
            }
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                Action confirmAction = delegate () {
                    string urlInput = inputUrl?.text ?? "";
                    string keyInput = inputKey?.text ?? "";
                    string modelInput = inputModel?.text ?? "";
                    string probInput = inputProb?.text ?? "";
                    string shortProbInput = inputShortProb?.text ?? "";
                    string promptPrefixInput = inputPromptPrefix?.text ?? "";
                    string promptSuffixInput = inputPromptSuffix?.text ?? "";
                    string generalEventInput = inputGeneralEvent?.text ?? "";
                    string genderEventInput = inputGenderEvent?.text ?? "";
                    string loverSpouseEventInput = inputLoverSpouseEvent?.text ?? "";
                    string parentChildEventInput = inputParentChildEvent?.text ?? "";
                    string masterStudentEventInput = inputMasterStudentEvent?.text ?? "";
                    bool autoColoringEnabled = autoColoringToggle?.isOn ?? true;

                    if (string.IsNullOrEmpty(urlInput) || string.IsNullOrEmpty(keyInput) || string.IsNullOrEmpty(modelInput) || string.IsNullOrEmpty(probInput))
                    {
                        UITipItem.AddTip("所有字段都不能为空！", 2f);
                    }
                    else
                    {
                        // 验证概率输入
                        float probability;
                        if (!float.TryParse(probInput, out probability) || probability < 0 || probability > 1)
                        {
                            UITipItem.AddTip("概率值必须是0到1之间的数字！", 2f);
                            return;
                        }

                        // 验证短奇遇概率输入
                        float shortProbability;
                        if (!float.TryParse(shortProbInput, out shortProbability) || shortProbability < 0 || shortProbability > 1)
                        {
                            UITipItem.AddTip("短奇遇概率值必须是0到1之间的数字！", 2f);
                            return;
                        }

                        try
                        {
                            // 1. 在委托执行的瞬间，读取最新的配置
                            string configPath = "config.json";
                            ModConfig currentConfig = Config.ReadConfig();
                            if (currentConfig == null)
                            {
                                currentConfig = new ModConfig();
                            }

                            // 2. 将UI界面上的所有值，赋给这个全新的、干净的 currentConfig 对象
                            currentConfig.ApiUrl = urlInput;
                            currentConfig.ApiKey = keyInput;
                            currentConfig.ModelName = modelInput;
                            currentConfig.EncounterProbability = probability;
                            currentConfig.ShortEventProbability = shortProbability;
                            currentConfig.PromptPrefix = promptPrefixInput ?? "";
                            currentConfig.PromptSuffix = promptSuffixInput ?? "";
                            currentConfig.AutoColoringEnabled = autoColoringEnabled; // 直接从UI读取toggle的值

                            // 更新事件列表
                            currentConfig.EventTypes = string.IsNullOrEmpty(generalEventInput) ? new List<string>() :
                                generalEventInput.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

                            currentConfig.OppositeGenderEventTypes = string.IsNullOrEmpty(genderEventInput) ? new List<string>() :
                                genderEventInput.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

                            currentConfig.LoverSpouseEventTypes = string.IsNullOrEmpty(loverSpouseEventInput) ? new List<string>() :
                                loverSpouseEventInput.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

                            currentConfig.ParentChildEventTypes = string.IsNullOrEmpty(parentChildEventInput) ? new List<string>() :
                                parentChildEventInput.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

                            currentConfig.MasterStudentEventTypes = string.IsNullOrEmpty(masterStudentEventInput) ? new List<string>() :
                                masterStudentEventInput.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

                            // 3. 将这个干净的、更新后的对象序列化并保存
                            string jsonContent = JsonConvert.SerializeObject(currentConfig, Formatting.Indented);
                            File.WriteAllText(configPath, jsonContent);

                            // 更新ModMain静态变量
                            ModMain.apiUrl = urlInput;
                            ModMain.apiKey = keyInput;
                            ModMain.modelName = modelInput;
                            ModMain.encounterProbability = probability;
                            ModMain.shortEventProbability = shortProbability;
                            ModMain.autoColoringEnabled = autoColoringEnabled;
                            Tools.Initialize(ModMain.apiUrl, ModMain.apiKey, ModMain.modelName);
                            if (!string.IsNullOrEmpty(promptPrefixInput))
                                Tools.DefaultPromptPrefix = promptPrefixInput;
                            if (!string.IsNullOrEmpty(promptSuffixInput))
                                Tools.DefaultPromptSuffix = promptSuffixInput;
                            if (currentConfig.EventTypes.Any())
                                Tools.DefaultGeneralEventTypes = currentConfig.EventTypes;
                            if (currentConfig.OppositeGenderEventTypes.Any())
                                Tools.DefaultOppositeGenderEventTypes = currentConfig.OppositeGenderEventTypes;
                            if (currentConfig.LoverSpouseEventTypes.Any())
                                Tools.DefaultLoverSpouseEventTypes = currentConfig.LoverSpouseEventTypes;
                            if (currentConfig.ParentChildEventTypes.Any())
                                Tools.DefaultParentChildEventTypes = currentConfig.ParentChildEventTypes;
                            if (currentConfig.MasterStudentEventTypes.Any())
                                Tools.DefaultMasterStudentEventTypes = currentConfig.MasterStudentEventTypes;

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
                        GeneralEventInput = null;
                        GenderEventInput = null;
                        LoverSpouseEventInput = null;
                        ParentChildEventInput = null;
                        MasterStudentEventInput = null;
                        Instance = null;
                        GameObject.Destroy(uiObj);
                    }
                };
                confirmButton.onClick.AddListener(confirmAction);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveAllListeners();
                Action cancelAction = delegate () {
                    GeneralEventInput = null;
                    GenderEventInput = null;
                    LoverSpouseEventInput = null;
                    ParentChildEventInput = null;
                    MasterStudentEventInput = null;
                    Instance = null;
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
                BindStaticResetButton(promptPrefixResetButton, inputPromptPrefix, () => Tools.HardcodedDefaultPromptPrefix);
                BindStaticResetButton(promptSuffixResetButton, inputPromptSuffix, () => Tools.HardcodedDefaultPromptSuffix);
                BindStaticResetButton(generalEventResetButton, inputGeneralEvent, () => string.Join("\n", Tools.HardcodedDefaultGeneralEventTypes));
                BindStaticResetButton(genderEventResetButton, inputGenderEvent, () => string.Join("\n", Tools.HardcodedDefaultOppositeGenderEventTypes));
                BindStaticResetButton(loverSpouseEventResetButton, inputLoverSpouseEvent, () => string.Join("\n", Tools.HardcodedDefaultLoverSpouseEventTypes));
                BindStaticResetButton(parentChildEventResetButton, inputParentChildEvent, () => string.Join("\n", Tools.HardcodedDefaultParentChildEventTypes));
                BindStaticResetButton(masterStudentEventResetButton, inputMasterStudentEvent, () => string.Join("\n", Tools.HardcodedDefaultMasterStudentEventTypes));
                Instance = uiObj.GetComponent<UIConfig>();
            }

        }
    }
}