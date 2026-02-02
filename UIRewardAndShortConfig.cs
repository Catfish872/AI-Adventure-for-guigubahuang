using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using System.Linq;
using TMPro;
using System.Collections.Generic;

namespace MOD_kqAfiU
{
    public class UIRewardAndShortConfig : UIBase
    {
        public static bool isGenerating = false;
        // 构造函数，接收IntPtr参数
        public UIRewardAndShortConfig(IntPtr ptr) : base(ptr)
        {
            Debug.Log("UIRewardAndShortConfig构造函数被调用");
        }

        // UI组件
        public GameObject panelObj;
        public GameObject confirmButtonObj;
        public GameObject cancelButtonObj;
        public GameObject inputPromptSuffixObj;
        public GameObject promptSuffixResetObj;
        public GameObject inputGeneralEventObj;
        public GameObject generalEventResetObj;
        public GameObject inputPool1Obj;
        public GameObject pool1ResetObj;
        public GameObject inputPool2Obj;
        public GameObject pool2ResetObj;
        public GameObject inputLuckPoolObj;
        public GameObject luckPoolResetObj;
        public GameObject inputLuckPool2Obj;
        public GameObject luckPool2ResetObj;
        public GameObject eventGeneratorInputObj;
        public GameObject eventGeneratorDropdownObj;
        public GameObject eventGeneratorNumInputObj;
        public GameObject eventGeneratorButtonObj;

        // 回调函数
        private Action callback;

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
        public void InitData(Action onConfirm)
        {
            Debug.Log("开始初始化RewardAndShortConfig UI数据");

            // 存储回调
            this.callback = onConfirm;

            try
            {

                // 查找UI组件 - 所有组件都在同一层级
                this.panelObj = transform.Find("Panel").gameObject;
                this.confirmButtonObj = transform.Find("Button").gameObject;
                this.cancelButtonObj = transform.Find("ButtonCancel").gameObject;
                this.inputPromptSuffixObj = transform.Find("inputpromptSuffix").gameObject;
                this.promptSuffixResetObj = transform.Find("promptSuffixReset").gameObject;
                this.inputGeneralEventObj = transform.Find("inputGeneralEvent").gameObject;
                this.generalEventResetObj = transform.Find("GeneralEventReset").gameObject;
                this.inputPool1Obj = transform.Find("inputpool1").gameObject;
                this.pool1ResetObj = transform.Find("pool1reset").gameObject;
                this.inputPool2Obj = transform.Find("inputpool2").gameObject;
                this.pool2ResetObj = transform.Find("pool2reset").gameObject;
                this.inputLuckPoolObj = transform.Find("inputluckpool").gameObject;
                this.luckPoolResetObj = transform.Find("luckpoolreset").gameObject;
                this.inputLuckPool2Obj = transform.Find("inputluckpool2").gameObject;
                this.luckPool2ResetObj = transform.Find("luckpoolreset2").gameObject;
                this.eventGeneratorInputObj = transform.Find("EventGeneratorInput").gameObject;
                this.eventGeneratorDropdownObj = transform.Find("EventGeneratorDropdown").gameObject;
                this.eventGeneratorNumInputObj = transform.Find("EventGeneratorNumInput").gameObject;
                this.eventGeneratorButtonObj = transform.Find("EventGeneratorButton").gameObject;

                Debug.Log("组件查找结果: Panel=" + (panelObj != null) +
                         ", InputPromptSuffix=" + (inputPromptSuffixObj != null) +
                         ", InputGeneralEvent=" + (inputGeneralEventObj != null) +
                         ", InputPool1=" + (inputPool1Obj != null));


                if (this.eventGeneratorDropdownObj != null && this.eventGeneratorButtonObj != null)
                {
                    // 1. 填充下拉菜单
                    var dropdown = this.eventGeneratorDropdownObj.GetComponent<Dropdown>();
                    if (dropdown != null)
                    {
                        dropdown.ClearOptions();
                        var options = new List<string>
                        {
                            "通用事件",
                            "异性事件",
                            "道侣/配偶事件",
                            "父母/子女事件",
                            "师徒事件",
                            "短奇遇事件"
                        };
                        var il2cppOptions = new Il2CppSystem.Collections.Generic.List<Dropdown.OptionData>();
                        foreach (var option in options)
                        {
                            il2cppOptions.Add(new Dropdown.OptionData(option));
                        }
                        dropdown.AddOptions(il2cppOptions);
                    }

                    // 2. 设置默认生成数量
                    var numInput = this.eventGeneratorNumInputObj.GetComponent<InputField>();
                    if (numInput != null)
                    {
                        numInput.text = "10";
                    }

                    // 3. 绑定生成按钮的点击事件
                    var genButton = this.eventGeneratorButtonObj.GetComponent<Button>();
                    if (genButton != null)
                    {
                        genButton.onClick.RemoveAllListeners();
                        genButton.onClick.AddListener(new Action(() => { OnGenerateEventsClicked(); }));
                    }
                }


                // 设置短奇遇提示词默认值
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
                                if (jsonObj != null && jsonObj.ContainsKey("ShortEventPrompt"))
                                {
                                    inputField.text = jsonObj["ShortEventPrompt"].ToString();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.Log("读取短奇遇提示词配置失败: " + ex.ToString());
                        }
                    }
                }

                // 设置短奇遇事件素材库默认值
                SetEventListDefaultValue(this.inputGeneralEventObj.GetComponent<InputField>(), "ShortEventTypes", false);


                SetPoolDefaultValue(this.inputPool1Obj.GetComponent<InputField>(), "PropDict1");
                SetPoolDefaultValue(this.inputPool2Obj.GetComponent<InputField>(), "PropDict2_10");
                SetPoolDefaultValue(this.inputLuckPoolObj.GetComponent<InputField>(), "LuckDict");
                SetPoolDefaultValue(this.inputLuckPool2Obj.GetComponent<InputField>(), "LuckDict2");

                // 绑定确认按钮事件
                if (this.confirmButtonObj != null)
                {
                    Button confirmButton = this.confirmButtonObj.GetComponent<Button>();
                    if (confirmButton != null)
                    {
                        Action confirmAction = delegate () {
                            string shortEventPromptInput = inputPromptSuffixObj != null ? inputPromptSuffixObj.GetComponent<InputField>().text : "";
                            string shortEventTypesInput = inputGeneralEventObj != null ? inputGeneralEventObj.GetComponent<InputField>().text : "";
                            string pool1Input = inputPool1Obj != null ? inputPool1Obj.GetComponent<InputField>().text : "";
                            string pool2Input = inputPool2Obj != null ? inputPool2Obj.GetComponent<InputField>().text : "";
                            string luckPool1Input = inputLuckPoolObj != null ? inputLuckPoolObj.GetComponent<InputField>().text : "";
                            string luckPool2Input = inputLuckPool2Obj != null ? inputLuckPool2Obj.GetComponent<InputField>().text : "";

                            // 保存配置
                            SaveRewardAndShortConfig(shortEventPromptInput, shortEventTypesInput,
    pool1Input, pool2Input, luckPool1Input, luckPool2Input);

                            // 更新ShortEvent中的静态变量
                            if (!string.IsNullOrEmpty(shortEventPromptInput))
                                ShortEvent.ShortEventPrompt = shortEventPromptInput;
                            if (!string.IsNullOrEmpty(shortEventTypesInput))
                                ShortEvent.ShortEventTypes = shortEventTypesInput.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                            var propDict1Data = ProcessPoolInput(pool1Input, "prop1").ToObject<Dictionary<string, string>>();
                            if (propDict1Data != null)
                                Prop.propDict1 = propDict1Data;

                            var propDict2Data = ProcessPoolInput(pool2Input, "prop2").ToObject<Dictionary<string, string>>();
                            if (propDict2Data != null)
                                Prop.propDict2_10 = propDict2Data;

                            var luckDict1Data = ProcessPoolInput(luckPool1Input, "luck1").ToObject<Dictionary<string, string>>();
                            if (luckDict1Data != null)
                                Prop.luckDict = luckDict1Data;

                            var luckDict2Data = ProcessPoolInput(luckPool2Input, "luck2").ToObject<Dictionary<string, string>>();
                            if (luckDict2Data != null)
                                Prop.luckDict2 = luckDict2Data;

                            // 调用回调函数
                            if (callback != null)
                            {
                                callback();
                            }

                            UITipItem.AddTip("奖励和短奇遇配置已保存", 2f);
                            g.ui.CloseAllUI();
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

                // 绑定重置按钮事件
                BindResetButton(this.promptSuffixResetObj?.GetComponent<Button>(), this.inputPromptSuffixObj?.GetComponent<InputField>(), () => ShortEvent.HardcodedShortEventPrompt);
                BindResetButton(this.generalEventResetObj?.GetComponent<Button>(), this.inputGeneralEventObj?.GetComponent<InputField>(), () => string.Join("\n", ShortEvent.HardcodedShortEventTypes));
                BindResetButton(this.pool1ResetObj?.GetComponent<Button>(), this.inputPool1Obj?.GetComponent<InputField>(), () => GetDefaultPoolNames("prop1"));
                BindResetButton(this.pool2ResetObj?.GetComponent<Button>(), this.inputPool2Obj?.GetComponent<InputField>(), () => GetDefaultPoolNames("prop2"));
                BindResetButton(this.luckPoolResetObj?.GetComponent<Button>(), this.inputLuckPoolObj?.GetComponent<InputField>(), () => GetDefaultPoolNames("luck1"));
                BindResetButton(this.luckPool2ResetObj?.GetComponent<Button>(), this.inputLuckPool2Obj?.GetComponent<InputField>(), () => GetDefaultPoolNames("luck2"));

            }
            catch (Exception ex)
            {
                Debug.Log("初始化RewardAndShortConfig UI失败: " + ex.ToString());
            }
        }


        private void OnGenerateEventsClicked()
        {
            // 1. 获取UI输入
            var dropdown = this.eventGeneratorDropdownObj.GetComponent<Dropdown>();
            var flavorInput = this.eventGeneratorInputObj.GetComponent<InputField>();
            var numInput = this.eventGeneratorNumInputObj.GetComponent<InputField>();

            if (dropdown == null || flavorInput == null || numInput == null)
            {
                UITipItem.AddTip("UI组件缺失，无法生成", 2f);
                return;
            }

            string selectedEventTypeText = dropdown.options[dropdown.value].text;
            string flavorText = flavorInput.text;
            int eventCount = 10;
            if (!int.TryParse(numInput.text, out eventCount) || eventCount <= 0)
            {
                eventCount = 10;
                numInput.text = "10";
            }

            UITipItem.AddTip($"正在为[{selectedEventTypeText}]生成{eventCount}个事件...", 2f);

            // 2. 构建提示词 (Prompt)
            string systemPrompt = @"你是一个《鬼谷八荒》修仙世界的事件设计大师。你的任务是根据要求生成指定类型的奇遇事件。
            返回的每个事件都必须严格遵循格式：'事件名称(主题1/主题2/主题3)'，每个事件占一行，不要有任何多余的解释、编号或修饰。

            例如:
            秘境探索(古修洞府/灵药园/地下遗迹)
            英雄救美(路遇截杀/恶霸欺凌/妖兽围攻)
            神兵认主(古剑择主/法器契合/丹炉共鸣)";

            string userPrompt = $"请为游戏《鬼谷八荒》生成{eventCount}个关于“{selectedEventTypeText}”的奇遇事件。";
            if (!string.IsNullOrWhiteSpace(flavorText))
            {
                userPrompt += $"事件的风格和元素请参考以下描述：'{flavorText}'。";
            }
            userPrompt += "\n请严格按照 '事件名称(主题1/主题2/主题3)' 的格式，每行一个，直接开始输出事件列表。";

            // 3. 准备并发送LLM请求
            LLMDialogueRequest request = new LLMDialogueRequest();
            request.AddSystemMessage(systemPrompt);
            request.AddUserMessage(userPrompt);

            Tools.SendLLMRequest(request, (response) => {
                ModMain.RunOnMainThread(() => {
                    if (response.StartsWith("错误："))
                    {
                        UITipItem.AddTip("事件生成失败: " + response, 3f);
                        return;
                    }

                    // 4. 解析、保存并刷新
                    try
                    {
                        var generatedEvents = response.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                                      .Select(line => line.Trim())
                                                      .Where(line => !string.IsNullOrEmpty(line))
                                                      .ToList();

                        if (generatedEvents.Count == 0)
                        {
                            UITipItem.AddTip("未能解析出任何事件", 2f);
                            return;
                        }

                        string configKey = "";
                        InputField selfInputField = null;

                        switch (selectedEventTypeText)
                        {
                            case "通用事件": configKey = "EventTypes"; break;
                            case "异性事件": configKey = "OppositeGenderEventTypes"; break;
                            case "道侣/配偶事件": configKey = "LoverSpouseEventTypes"; break;
                            case "父母/子女事件": configKey = "ParentChildEventTypes"; break;
                            case "师徒事件": configKey = "MasterStudentEventTypes"; break;
                            case "短奇遇事件":
                                configKey = "ShortEventTypes";
                                selfInputField = this.inputGeneralEventObj.GetComponent<InputField>();
                                break;
                        }

                        if (string.IsNullOrEmpty(configKey)) return;

                        string configPath = "config.json";
                        string originalJson = File.Exists(configPath) ? File.ReadAllText(configPath) : "{}";
                        var jsonObj = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(originalJson);

                        var eventList = jsonObj[configKey]?.ToObject<List<string>>() ?? new List<string>();
                        eventList.AddRange(generatedEvents);

                        jsonObj[configKey] = Newtonsoft.Json.Linq.JArray.FromObject(eventList);
                        File.WriteAllText(configPath, jsonObj.ToString(Formatting.Indented));

                        if (selfInputField != null)
                        {
                            selfInputField.text = string.Join("\n", eventList);
                        }
                        else
                        {
                            var uiConfig = UIConfig.Instance;
                            if (uiConfig != null) // 实例存在即表示UI是打开的
                            {
                                uiConfig.RefreshEventInputsFromMemory();
                            }

                        }

                        UITipItem.AddTip($"成功生成并保存了{generatedEvents.Count}个事件！", 2f);
                    }
                    catch (Exception ex)
                    {
                        UITipItem.AddTip("处理事件时出错: " + ex.Message, 3f);
                        Debug.LogError("处理LLM事件生成响应失败: " + ex.ToString());
                    }
                });
            });
        }





        // 保存奖励和短奇遇配置到config.json
        private void SaveRewardAndShortConfig(string shortEventPrompt, string shortEventTypes,
    string pool1Input, string pool2Input, string luckPool1Input, string luckPool2Input)
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

                // 更新短奇遇配置
                jsonObj["ShortEventPrompt"] = shortEventPrompt ?? "";
                var shortEventTypesList = string.IsNullOrEmpty(shortEventTypes) ? new List<string>() :
                    shortEventTypes.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                jsonObj["ShortEventTypes"] = Newtonsoft.Json.Linq.JArray.FromObject(shortEventTypesList);

                // 处理奖池配置
                jsonObj["PropDict1"] = ProcessPoolInput(pool1Input, "prop1");
                jsonObj["PropDict2_10"] = ProcessPoolInput(pool2Input, "prop2");
                jsonObj["LuckDict"] = ProcessPoolInput(luckPool1Input, "luck1");
                jsonObj["LuckDict2"] = ProcessPoolInput(luckPool2Input, "luck2");

                // 保存回文件
                File.WriteAllText(configPath, jsonObj.ToString(Formatting.Indented));

                Debug.Log("奖励和短奇遇配置已保存到文件");
            }
            catch (Exception ex)
            {
                Debug.LogError($"保存奖励和短奇遇配置时发生错误: {ex.Message}");
            }
        }







        // 保留一个静态的提取简化名称函数
        private static string ExtractSimpleName(string fullName)
        {
            int bracketIndex = fullName.IndexOf('[');
            if (bracketIndex > 0)
            {
                return fullName.Substring(0, bracketIndex).Trim();
            }

            int parenthesisIndex = fullName.IndexOf('（');
            if (parenthesisIndex > 0)
            {
                return fullName.Substring(0, parenthesisIndex).Trim();
            }

            return fullName.Trim();
        }

        // 保留一个静态的获取默认奖池名称函数
        private static string GetDefaultPoolNames(string poolType)
        {
            Dictionary<string, string> sourceDict = null;

            switch (poolType)
            {
                case "prop1":
                    sourceDict = Prop.HardcodedDefaultPropDict1;
                    break;
                case "prop2":
                    sourceDict = Prop.HardcodedDefaultPropDict2_10;
                    break;
                case "luck1":
                    sourceDict = Prop.HardcodedDefaultLuckDict;
                    break;
                case "luck2":
                    sourceDict = Prop.HardcodedDefaultLuckDict2;
                    break;
            }

            if (sourceDict != null)
            {
                List<string> simpleNames = new List<string>();
                foreach (var item in sourceDict.Values)
                {
                    string simpleName = ExtractSimpleName(item);
                    if (!string.IsNullOrEmpty(simpleName) && !simpleNames.Contains(simpleName))
                    {
                        simpleNames.Add(simpleName);
                    }
                }
                return string.Join("\n", simpleNames);
            }

            return "";
        }

        // 保留一个静态的处理奖池输入函数
        private static Newtonsoft.Json.Linq.JObject ProcessPoolInput(string input, string poolType)
        {
            var resultDict = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(input))
            {
                return Newtonsoft.Json.Linq.JObject.FromObject(resultDict);
            }

            // ------------------- 性能优化的核心：预处理和缓存 -------------------

            // 1. 创建一个用于快速查找的字典（查找表/缓存）
            // Key: 物品/气运的简单名称 (string)
            // Value: 包含所有同名物品/气运完整信息的列表 (List<KeyValuePair<string, string>>)
            var lookupCache = new Dictionary<string, List<KeyValuePair<string, string>>>(StringComparer.OrdinalIgnoreCase);

            // 2. 遍历一次游戏数据，填充查找表
            if (poolType.StartsWith("prop"))
            {
                // 填充道具数据
                foreach (var itemConf in g.conf.itemProps._allConfList)
                {
                    string simpleName = GameTool.LS(itemConf.name);
                    if (string.IsNullOrEmpty(simpleName)) continue;

                    // 构造完整的道具信息
                    var item = DataProps.PropsData.NewProps(itemConf.id, 1);
                    int grade = item?.propsInfoBase?.grade ?? 0;
                    string nameWithGrade = $"{simpleName}[境界{grade}]";
                    var data = new KeyValuePair<string, string>(itemConf.id.ToString(), nameWithGrade);

                    // 将数据添加到查找表
                    if (!lookupCache.ContainsKey(simpleName))
                    {
                        lookupCache[simpleName] = new List<KeyValuePair<string, string>>();
                    }
                    lookupCache[simpleName].Add(data);
                }
            }
            else if (poolType.StartsWith("luck"))
            {
                // 填充气运数据
                foreach (var featureConf in g.conf.roleCreateFeature._allConfList)
                {
                    string simpleName = GameTool.LS(featureConf.name);
                    if (string.IsNullOrEmpty(simpleName)) continue;

                    // 构造完整的气运信息
                    string description = GameTool.LS(featureConf.tips);
                    string nameWithDesc = $"{simpleName}（作用：{description}）";
                    var data = new KeyValuePair<string, string>(featureConf.id.ToString(), nameWithDesc);

                    // 将数据添加到查找表
                    if (!lookupCache.ContainsKey(simpleName))
                    {
                        lookupCache[simpleName] = new List<KeyValuePair<string, string>>();
                    }
                    lookupCache[simpleName].Add(data);
                }
            }

            // （可选）如果你的硬编码字典也需要被用户搜索，也可以在这里将它们加入缓存
            // 这里为了简化，我们假设主要搜索来源是游戏内数据，硬编码字典主要用于默认值

            // ------------------- 查找阶段：利用缓存进行高效查找 -------------------

            var inputItems = input.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();

            List<string> notFoundItems = new List<string>();
            foreach (string simpleName in inputItems)
            {
                // 3. 从缓存中快速查找，而不是遍历全表
                if (lookupCache.TryGetValue(simpleName, out var foundDataList))
                {
                    // 找到了，将所有同名道具/气运加入结果
                    foreach (var data in foundDataList)
                    {
                        if (!resultDict.ContainsKey(data.Key))
                        {
                            resultDict.Add(data.Key, data.Value);
                        }
                    }
                }
                else
                {
                    // 在游戏内置数据中未找到
                    notFoundItems.Add(simpleName);
                }
            }

            if (notFoundItems.Count > 0)
            {
                UITipItem.AddTip($"未在游戏中找到以下物品：{string.Join("、", notFoundItems)}", 3f);
            }

            return Newtonsoft.Json.Linq.JObject.FromObject(resultDict);
        }

        // 保留一个静态的获取完整物品数据函数
        private static List<KeyValuePair<string, string>> GetAllFullItemData(string simpleName, string poolType)
        {
            // 用于存储所有找到的匹配项
            var foundItems = new List<KeyValuePair<string, string>>();
            Dictionary<string, string> sourceDict = null;

            switch (poolType)
            {
                case "prop1":
                    sourceDict = Prop.HardcodedDefaultPropDict1;
                    break;
                case "prop2":
                    sourceDict = Prop.HardcodedDefaultPropDict2_10;
                    break;
                case "luck1":
                    sourceDict = Prop.HardcodedDefaultLuckDict;
                    break;
                case "luck2":
                    sourceDict = Prop.HardcodedDefaultLuckDict2;
                    break;
            }

            // 1. 在硬编码字典中查找
            if (sourceDict != null)
            {
                foreach (var kvp in sourceDict)
                {
                    string existingSimpleName = ExtractSimpleName(kvp.Value);
                    if (string.Equals(existingSimpleName, simpleName, StringComparison.OrdinalIgnoreCase))
                    {
                        // 找到一个匹配项，添加到列表，但不返回，继续查找
                        foundItems.Add(new KeyValuePair<string, string>(kvp.Key, kvp.Value));
                    }
                }
            }

            // 2. 在游戏内置配置中查找
            if (poolType.StartsWith("prop"))
            {
                // 道具池：查找游戏内置物品配置
                try
                {
                    foreach (var itemConf in g.conf.itemProps._allConfList)
                    {
                        if (GameTool.LS(itemConf.name).Equals(simpleName, StringComparison.OrdinalIgnoreCase))
                        {
                            var item = DataProps.PropsData.NewProps(itemConf.id, 1);
                            int grade = item?.propsInfoBase?.grade ?? 0;
                            string nameWithGrade = $"{simpleName}[境界{grade}]";
                            // 找到一个匹配项，添加到列表，但不返回，继续查找
                            foundItems.Add(new KeyValuePair<string, string>(itemConf.id.ToString(), nameWithGrade));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log($"查找游戏内置道具配置失败: {ex.Message}");
                }
            }
            else if (poolType.StartsWith("luck"))
            {
                // 气运池：查找游戏内置气运配置
                try
                {
                    foreach (var featureConf in g.conf.roleCreateFeature._allConfList)
                    {
                        if (GameTool.LS(featureConf.name).Equals(simpleName, StringComparison.OrdinalIgnoreCase))
                        {
                            string description = GameTool.LS(featureConf.tips);
                            string nameWithDesc = $"{simpleName}（作用：{description}）";
                            // 找到一个匹配项，添加到列表，但不返回，继续查找
                            foundItems.Add(new KeyValuePair<string, string>(featureConf.id.ToString(), nameWithDesc));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log($"查找游戏内置气运配置失败: {ex.Message}");
                }
            }

            // 返回所有找到的道具/气运的列表
            // 为了确保最终结果的唯一性（以防硬编码和游戏内配置有重叠），可以按ID去重
            if (foundItems.Count > 1)
            {
                return foundItems.GroupBy(item => item.Key) // 按ID分组
                                 .Select(group => group.First()) // 每组取第一个
                                 .ToList();
            }

            return foundItems;
        }

        // 保留一个静态的设置奖池默认值函数
        private static void SetPoolDefaultValue(InputField inputField, string configKey)
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
                            var poolDict = jsonObj[configKey].ToObject<Dictionary<string, string>>();
                            if (poolDict != null && poolDict.Count > 0)
                            {
                                List<string> simpleNames = new List<string>();
                                foreach (var item in poolDict.Values)
                                {
                                    string simpleName = ExtractSimpleName(item);
                                    if (!string.IsNullOrEmpty(simpleName))
                                    {
                                        simpleNames.Add(simpleName);
                                    }
                                }
                                inputField.text = string.Join("\n", simpleNames);
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

        // 保留一个静态的设置事件列表默认值函数
        private static void SetEventListDefaultValue(InputField inputField, string configKey, bool isStringField)
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

        // 保留一个静态的绑定重置按钮函数
        private static void BindResetButton(Button resetButton, InputField inputField, System.Func<string> getDefaultValue)
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




        // 打开奖励和短奇遇配置UI的静态方法
        public static void OpenRewardAndShortConfigUI(Action callback = null)
        {
            Debug.Log("开始打开RewardAndShortConfigUI...");

            // 加载预制体
            GameObject prefab = g.res.Load<GameObject>("ui/rewardandshortconfig/rewardandshortconfig");
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

                float scale = CalculateUIScale();
                rectTransform.localScale = Vector3.one * scale;
            }

            Canvas canvas = uiObj.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = uiObj.GetComponentInChildren<Canvas>();
            }

            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 6000; // 设置比主UI更高的优先级
                Debug.Log("UI排序顺序设置为: " + canvas.sortingOrder);
            }

            // 直接查找并设置组件
            InputField inputPromptSuffix = uiObj.transform.Find("inputpromptSuffix")?.GetComponent<InputField>();
            InputField inputGeneralEvent = uiObj.transform.Find("inputGeneralEvent")?.GetComponent<InputField>();
            InputField inputPool1 = uiObj.transform.Find("inputpool1")?.GetComponent<InputField>();
            InputField inputPool2 = uiObj.transform.Find("inputpool2")?.GetComponent<InputField>();
            InputField inputLuckPool = uiObj.transform.Find("inputluckpool")?.GetComponent<InputField>();
            InputField inputLuckPool2 = uiObj.transform.Find("inputluckpool2")?.GetComponent<InputField>();
            InputField eventGeneratorInput = uiObj.transform.Find("EventGeneratorInput")?.GetComponent<InputField>();
            var eventGeneratorDropdown = uiObj.transform.Find("EventGeneratorDropdown")?.GetComponent<TMP_Dropdown>();
            try
            {
                // 1. 使用您代码中已验证过的方式找到游戏主Canvas
                GameObject mainCanvas = GameObject.Find("Canvas");
                if (mainCanvas != null)
                {
                    // 2. 在主Canvas中找到一个已存在的TextMeshProUGUI组件来获取字体
                    var existingTmpText = mainCanvas.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (existingTmpText != null && existingTmpText.font != null)
                    {
                        TMPro.TMP_FontAsset mainFont = existingTmpText.font;

                        // 3. 获取我们下拉菜单中的两个文本组件
                        var label = eventGeneratorDropdown.transform.Find("Label")?.GetComponent<TMPro.TextMeshProUGUI>();
                        var itemLabel = eventGeneratorDropdown.transform.Find("Template/Viewport/Content/Item/Item Label")?.GetComponent<TMPro.TextMeshProUGUI>();

                        // 4. 为它们设置正确的字体资源
                        if (label != null) label.font = mainFont;
                        if (itemLabel != null) itemLabel.font = mainFont;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log("自动设置字体失败: " + ex.ToString());
            }
            InputField eventGeneratorNumInput = uiObj.transform.Find("EventGeneratorNumInput")?.GetComponent<InputField>();
            Button eventGeneratorButton = uiObj.transform.Find("EventGeneratorButton")?.GetComponent<Button>();

            // 设置默认值
            SetEventListDefaultValue(inputPromptSuffix, "ShortEventPrompt", true);
            SetEventListDefaultValue(inputGeneralEvent, "ShortEventTypes", false);
            SetPoolDefaultValue(inputPool1, "PropDict1");
            SetPoolDefaultValue(inputPool2, "PropDict2_10");
            SetPoolDefaultValue(inputLuckPool, "LuckDict");
            SetPoolDefaultValue(inputLuckPool2, "LuckDict2");

            Button confirmButton = uiObj.transform.Find("Button")?.GetComponent<Button>();
            Button cancelButton = uiObj.transform.Find("ButtonCancel")?.GetComponent<Button>();
            Button promptSuffixResetButton = uiObj.transform.Find("promptSuffixReset")?.GetComponent<Button>();
            Button generalEventResetButton = uiObj.transform.Find("GeneralEventReset")?.GetComponent<Button>();
            Button pool1ResetButton = uiObj.transform.Find("pool1reset")?.GetComponent<Button>();
            Button pool2ResetButton = uiObj.transform.Find("pool2reset")?.GetComponent<Button>();
            Button luckPoolResetButton = uiObj.transform.Find("luckpoolreset")?.GetComponent<Button>();
            Button luckPool2ResetButton = uiObj.transform.Find("luckpoolreset2")?.GetComponent<Button>();

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                Action confirmAction = delegate () {
                    string shortEventPromptInput = inputPromptSuffix?.text ?? "";
                    string shortEventTypesInput = inputGeneralEvent?.text ?? "";
                    string pool1Input = inputPool1?.text ?? "";
                    string pool2Input = inputPool2?.text ?? "";
                    string luckPool1Input = inputLuckPool?.text ?? "";
                    string luckPool2Input = inputLuckPool2?.text ?? "";

                    try
                    {
                        string configPath = "config.json";
                        string originalJson = "";
                        if (File.Exists(configPath))
                        {
                            originalJson = File.ReadAllText(configPath);
                        }

                        var jsonObj = string.IsNullOrEmpty(originalJson)
                            ? new Newtonsoft.Json.Linq.JObject()
                            : JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(originalJson);

                        // 更新短奇遇配置
                        jsonObj["ShortEventPrompt"] = shortEventPromptInput ?? "";
                        var shortEventTypesList = string.IsNullOrEmpty(shortEventTypesInput) ? new List<string>() :
                            shortEventTypesInput.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                        jsonObj["ShortEventTypes"] = Newtonsoft.Json.Linq.JArray.FromObject(shortEventTypesList);

                        // 处理奖池配置
                        jsonObj["PropDict1"] = ProcessPoolInput(pool1Input, "prop1");
                        jsonObj["PropDict2_10"] = ProcessPoolInput(pool2Input, "prop2");
                        jsonObj["LuckDict"] = ProcessPoolInput(luckPool1Input, "luck1");
                        jsonObj["LuckDict2"] = ProcessPoolInput(luckPool2Input, "luck2");

                        File.WriteAllText(configPath, jsonObj.ToString(Formatting.Indented));

                        // 更新ShortEvent中的静态变量
                        if (!string.IsNullOrEmpty(shortEventPromptInput))
                            ShortEvent.ShortEventPrompt = shortEventPromptInput;
                        if (shortEventTypesList.Count > 0)
                            ShortEvent.ShortEventTypes = shortEventTypesList;
                        var propDict1Data = ProcessPoolInput(pool1Input, "prop1").ToObject<Dictionary<string, string>>();
                        if (propDict1Data != null)
                            Prop.propDict1 = propDict1Data;

                        var propDict2Data = ProcessPoolInput(pool2Input, "prop2").ToObject<Dictionary<string, string>>();
                        if (propDict2Data != null)
                            Prop.propDict2_10 = propDict2Data;

                        var luckDict1Data = ProcessPoolInput(luckPool1Input, "luck1").ToObject<Dictionary<string, string>>();
                        if (luckDict1Data != null)
                            Prop.luckDict = luckDict1Data;

                        var luckDict2Data = ProcessPoolInput(luckPool2Input, "luck2").ToObject<Dictionary<string, string>>();
                        if (luckDict2Data != null)
                            Prop.luckDict2 = luckDict2Data;


                        if (callback != null)
                        {
                            callback();
                        }

                        UITipItem.AddTip("奖励和短奇遇配置已保存", 2f);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("保存奖励和短奇遇配置失败: " + ex.ToString());
                        UITipItem.AddTip("保存配置失败", 2f);
                    }

                    GameObject.Destroy(uiObj);
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

            // 绑定重置按钮
            BindResetButton(promptSuffixResetButton, inputPromptSuffix, () => ShortEvent.HardcodedShortEventPrompt);
            BindResetButton(generalEventResetButton, inputGeneralEvent, () => string.Join("\n", ShortEvent.HardcodedShortEventTypes));
            BindResetButton(pool1ResetButton, inputPool1, () => GetDefaultPoolNames("prop1"));
            BindResetButton(pool2ResetButton, inputPool2, () => GetDefaultPoolNames("prop2"));
            BindResetButton(luckPoolResetButton, inputLuckPool, () => GetDefaultPoolNames("luck1"));
            BindResetButton(luckPool2ResetButton, inputLuckPool2, () => GetDefaultPoolNames("luck2"));

            Debug.Log("奖励和短奇遇配置UI设置完成");

            if (eventGeneratorDropdown != null && eventGeneratorButton != null)
            {
                // 1. 填充下拉菜单
                eventGeneratorDropdown.ClearOptions();
                var options = new List<string>
                {
                    "通用事件", "异性事件", "道侣/配偶事件",
                    "父母/子女事件", "师徒事件", "短奇遇事件"
                };
                var il2cppOptions = new Il2CppSystem.Collections.Generic.List<TMP_Dropdown.OptionData>();
                foreach (var option in options)
                {
                    il2cppOptions.Add(new TMP_Dropdown.OptionData(option));
                }
                eventGeneratorDropdown.AddOptions(il2cppOptions);

                // 2. 设置默认生成数量
                if (eventGeneratorNumInput != null)
                {
                    eventGeneratorNumInput.text = "10";
                }

                // 3. 绑定生成按钮的点击事件
                eventGeneratorButton.onClick.RemoveAllListeners();
                eventGeneratorButton.onClick.AddListener(new Action(() => {
                    // --- 开始 OnGenerateEventsClicked 的逻辑 ---

                    // A. 防止连续点击的“锁”检查
                    if (isGenerating)
                    {
                        UITipItem.AddTip("正在生成中，请稍候...", 1.5f);
                        return;
                    }

                    // 锁上
                    isGenerating = true;

                    string selectedEventTypeText = eventGeneratorDropdown.options[eventGeneratorDropdown.value].text;
                    string flavorText = eventGeneratorInput.text;
                    int eventCount = 10;
                    if (eventGeneratorNumInput != null && (!int.TryParse(eventGeneratorNumInput.text, out eventCount) || eventCount <= 0))
                    {
                        eventCount = 10;
                        eventGeneratorNumInput.text = "10";
                    }

                    UITipItem.AddTip($"正在为[{selectedEventTypeText}]生成{eventCount}个事件...", 2f);

                    string systemPrompt = @"你是一位资深的《鬼谷八荒》修仙世界事件设计师。
你的任务是构思富有创意和想象力的一句话奇遇事件描述。游玩场景是系统会根据事件类型抓取NPC与玩家发生事件，所以抓取到的NPC与玩家的身份是与事件类型相对应的（非短奇遇）。对于短奇遇事件类型，则是只有玩家一人作为主要角色。

核心要求如下：
1.  **情境描述**：每个描述应作为一个故事的引子，提供一个清晰的情境，但为玩家的行动和故事的发展留出充足的空间。
2.  **平衡性**：描述的具体程度要适中，既要避免因过于宽泛而显得空洞，也要避免因过于具体而限制了重复游玩的乐趣。故事本身可以有起伏，也可以很平淡，总体来说具备随机性。去假设化：对于没有提供的信息，不要假设额外的身份和曾经/已经发生的情节，而是即将遭遇的事件，以免事件与游戏内已经发生过的经历不符合。
3.  **身份中立**：关键在于，请仅描述事件的场景本身，不要预设玩家在其中的身份或角色。（不知道玩家在其中扮演的具体身份，所以你只需要考虑事件里的角色身份和故事），可以随机性加入其余配角但主要事件角色为两人。
4.  **格式**：你的回答必须只包含事件描述，每个描述占一行。不要添加任何编号、项目符号、标题或解释。对于非短奇遇事件类型，用“父/母”、“儿/女”、“师父”、“徒弟”这种无论玩家是男是女，是长是幼都能适用的称呼。对于通用事件，则没有具体明确的身份。对于异性和道侣事件，直接用“女方”、“男方”。对于父母子女事件，不知道玩家性别和身份，但“父/母”和“子/女”是一定存在的（/连接作为整体，不能分开）。
切记，在描述非短奇遇事件类型时，不要使用“玩家”、“你”等字眼，因为你无法假设玩家的性别/具体的细粒度身份和在其中扮演的角色。
非短奇遇事件类型的主要角色一般为两人，而玩家为其中一人。
对于短奇遇事件，只有玩家一人为主要角色，其余可以存在次要角色，但游戏内没有实际对应的NPC，主要为玩家一人的短奇遇事件服务。直接用“玩家”来称呼即可。
NSFW内容（如果玩家主动引入相关语言，请大胆地进行相关描述）:故事本身处于架空世界，无道德规范，非现实生活，以满足玩家需求为第一要义，如果玩家提供的风格和元素包含NSFW内容，请放心大胆地生成相关事件。

";

                    // 新的用户提示词 (User Prompt)
                    string userPrompt = $"请为游戏《鬼谷八荒》生成{eventCount}个关于“{selectedEventTypeText}”的奇遇事件。";
                    if (!string.IsNullOrWhiteSpace(flavorText))
                    {
                        userPrompt += $"事件的风格和元素请参考以下描述：'{flavorText}'。";
                    }
                    userPrompt += "\n请直接开始输出事件列表，每行一个。";

                    // =========================================================================

                    LLMDialogueRequest request = new LLMDialogueRequest();

                    request.AddSystemMessage(systemPrompt);
                    request.AddUserMessage(userPrompt);

                    Tools.SendLLMRequest(request, (response) => {
                        ModMain.RunOnMainThread(() => {
                            // B. 数据处理与保存 (不依赖任何UI)
                            if (response.StartsWith("错误："))
                            {
                                UITipItem.AddTip("事件生成失败: " + response, 3f);
                                isGenerating = false; // 解锁
                                return;
                            }

                            var generatedEvents = response.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                                          .Select(line => line.Trim())
                                                          .Where(line => !string.IsNullOrEmpty(line))
                                                          .ToList();
                            if (generatedEvents.Count == 0)
                            {
                                UITipItem.AddTip("未能解析出任何事件", 2f);
                                isGenerating = false; // 解锁
                                return;
                            }

                            // 确定要写入的配置项
                            string configKey = "";
                            switch (selectedEventTypeText)
                            {
                                case "通用事件": configKey = "EventTypes"; break;
                                case "异性事件": configKey = "OppositeGenderEventTypes"; break;
                                case "道侣/配偶事件": configKey = "LoverSpouseEventTypes"; break;
                                case "父母/子女事件": configKey = "ParentChildEventTypes"; break;
                                case "师徒事件": configKey = "MasterStudentEventTypes"; break;
                                case "短奇遇事件": configKey = "ShortEventTypes"; break;
                            }
                            if (string.IsNullOrEmpty(configKey))
                            {
                                isGenerating = false; // 解锁
                                return;
                            }

                            // 写入配置文件
                            try
                            {
                                string configPath = "config.json";
                                string originalJson = File.Exists(configPath) ? File.ReadAllText(configPath) : "{}";
                                var jsonObj = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(originalJson);
                                var eventList = jsonObj[configKey]?.ToObject<List<string>>() ?? new List<string>();
                                eventList.AddRange(generatedEvents);
                                jsonObj[configKey] = Newtonsoft.Json.Linq.JArray.FromObject(eventList);
                                File.WriteAllText(configPath, jsonObj.ToString(Formatting.Indented));

                                UITipItem.AddTip($"成功生成并保存了{generatedEvents.Count}个事件！", 2f);

                                switch (configKey)
                                {
                                    case "EventTypes":
                                        Tools.DefaultGeneralEventTypes = eventList; // 更新内存
                                        if (UIConfig.GeneralEventInput != null) UIConfig.GeneralEventInput.text = string.Join("\n", eventList); // 更新UIConfig
                                        break;

                                    case "OppositeGenderEventTypes":
                                        Tools.DefaultOppositeGenderEventTypes = eventList;
                                        if (UIConfig.GenderEventInput != null) UIConfig.GenderEventInput.text = string.Join("\n", eventList);
                                        break;

                                    case "LoverSpouseEventTypes":
                                        Tools.DefaultLoverSpouseEventTypes = eventList;
                                        if (UIConfig.LoverSpouseEventInput != null) UIConfig.LoverSpouseEventInput.text = string.Join("\n", eventList);
                                        break;

                                    case "ParentChildEventTypes":
                                        Tools.DefaultParentChildEventTypes = eventList;
                                        if (UIConfig.ParentChildEventInput != null) UIConfig.ParentChildEventInput.text = string.Join("\n", eventList);
                                        break;

                                    case "MasterStudentEventTypes":
                                        Tools.DefaultMasterStudentEventTypes = eventList;
                                        if (UIConfig.MasterStudentEventInput != null) UIConfig.MasterStudentEventInput.text = string.Join("\n", eventList);
                                        break;

                                    case "ShortEventTypes":
                                        // ★ 这就是你之前能工作的逻辑，现在被正确地包含在内
                                        ShortEvent.ShortEventTypes = eventList; // 更新内存
                                        if (inputGeneralEvent != null)
                                        {
                                            inputGeneralEvent.text = string.Join("\n", eventList); // 更新自己
                                        }
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                UITipItem.AddTip("保存事件时出错: " + ex.Message, 3f);
                            }
                            finally
                            {
                                // D. 无论成功失败，最后都要解锁
                                isGenerating = false;
                            }
                        });
                    });
                }));
            }
        }
    }
}