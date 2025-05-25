using System;
using System.Reflection;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using EGameTypeData;
using UnityEngine.UI;
using UnhollowerRuntimeLib;
using static SpecialBattle83;
using System.Web.WebPages;
using System.Linq;
using System.Text.RegularExpressions;
using static MOD_kqAfiU.ModMain;
using UnhollowerBaseLib;
using EBattleTypeData;
using static DataUnitLog.LogData;
using System.Text;
using Mono.CSharp;
using MOD_SSCYAI;
using System.Windows;


namespace MOD_kqAfiU
{
    // Tools 类，包含从 ModMain 移出的方法
    public class Tools
    {
        // LLM相关字段
        private static LLMConnector llmConnector;
        private static string apiUrl;
        private static string apiKey;
        private static string modelName;
        private static Il2CppSystem.Collections.Generic.List<DataProps.PropsData> savedRewardItems = null;
        private static int taskID = 988567713;

        [Serializable]
        public class FormattedResponse
        {
            public string content { get; set; }
            public string option1 { get; set; }
            public string react1 { get; set; }
            public string option2 { get; set; }
            public string react2 { get; set; }
            public string type { get; set; }
            public string reward { get; set; }

            // 默认构造函数
            public FormattedResponse()
            {
                content = "";
                option1 = "";
                react1 = ""; 
                option2 = "";
                react2 = ""; 
                type = "1";
            }
        }

        // 初始化方法，在 ModMain 的 Init 中调用
        public static void Initialize(string url, string key, string model)
        {
            apiUrl = url;
            apiKey = key;
            modelName = model;

            Debug.Log("初始化LLMConnector...");
            llmConnector = new LLMConnector(apiUrl, apiKey, modelName);
            Debug.Log("LLMConnector初始化完成");
        }

        // 移出的 SendLLMRequest 方法
        public static void SendLLMRequest(LLMDialogueRequest request, Action<string> onResponseReceived)
        {
            if (llmConnector == null)
            {
                UITipItem.AddTip("LLM服务未初始化，无法发送请求");
                return;
            }

            // 转换为LLMConnector需要的消息格式
            List<Message> messages = new List<Message>();
            foreach (var msg in request.Messages)
            {
                messages.Add(new Message(msg.Role, msg.Content));
            }

            // 发送请求
            var responseTask = llmConnector.SendMessageToLLM(messages);

            responseTask.ContinueWith(task => {
                ModMain.RunOnMainThread(() => {
                    if (task.IsCompleted && !task.IsFaulted)
                    {
                        // 将响应传回回调
                        onResponseReceived?.Invoke(task.Result);
                    }
                    else if (task.IsFaulted)
                    {
                        onResponseReceived?.Invoke($"错误：{task.Exception?.Message}");
                    }
                });
            });
        }

        // 移出的 GetRandomNpc 方法
        public static WorldUnitBase GetRandomNpc()
        {
            List<WorldUnitBase> sameAreaNpcs = new List<WorldUnitBase>();
            List<WorldUnitBase> otherAreaNpcs = new List<WorldUnitBase>();

            // 获取玩家所在的区域ID
            int playerAreaId = g.world.playerUnit.data.unitData.pointGridData.areaBaseID;

            foreach (WorldUnitBase unit in g.world.unit.GetUnits(true))
            {
                if (!unit.data.unitData.unitID.Equals(g.world.playerUnit.data.unitData.unitID))
                {
                    // 检查NPC是否与玩家在同一区域
                    if (unit.data.unitData.pointGridData.areaBaseID == playerAreaId)
                    {
                        sameAreaNpcs.Add(unit);
                    }
                    else
                    {
                        otherAreaNpcs.Add(unit);
                    }
                }
            }

            // 优先从同区域的NPC中选择
            if (sameAreaNpcs.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, sameAreaNpcs.Count);
                return sameAreaNpcs[randomIndex];
            }

            // 如果同区域没有NPC，则从其他区域选择
            if (otherAreaNpcs.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, otherAreaNpcs.Count);
                return otherAreaNpcs[randomIndex];
            }

            return null; // 如果没有任何NPC，返回null
        }

        // 移出的 GetDialogueTextById 方法
        [Serializable]
        private class DialogueData
        {
            public List<DialogueEntry> dialogues;
        }

        [Serializable]
        private class DialogueEntry
        {
            public int id;
            public string text;
        }

        public static string GetDialogueTextById(int dialogueId)
        {
            string jsonPath = "test.json";

            if (File.Exists(jsonPath))
            {
                string jsonContent = File.ReadAllText(jsonPath);
                DialogueData dialogueData = JsonConvert.DeserializeObject<DialogueData>(jsonContent);

                if (dialogueData != null && dialogueData.dialogues != null)
                {
                    foreach (var dialogue in dialogueData.dialogues)
                    {
                        if (dialogue.id == dialogueId)
                        {
                            return dialogue.text;
                        }
                    }
                }
            }

            return "道友，这里灵气充沛，适合修行。";
        }

        // 移出的 CreateDialogue 方法
        public static void CreateDialogue(int dialogId, string dialogText, WorldUnitBase leftUnit, WorldUnitBase rightUnit,
                                  Dictionary<int, string> options, Dictionary<int, Action> callbacks)
        {
            UICustomDramaDyn dramaDyn = new UICustomDramaDyn(dialogId);
            dramaDyn.dramaData.dialogueText[dialogId] = dialogText;

            foreach (var option in options)
            {
                dramaDyn.dramaData.dialogueOptions[option.Key] = option.Value;
                if (callbacks.ContainsKey(option.Key))
                {
                    dramaDyn.SetOptionCall(option.Key, callbacks[option.Key]);
                }
            }

            dramaDyn.dramaData.unitLeft = leftUnit;
            dramaDyn.dramaData.unitRight = rightUnit;
            dramaDyn.OpenUI();
        }
        public static FormattedResponse ParseLLMResponse(string rawResponse, out string parsedContent)
        {
            // 默认使用原始响应作为内容
            parsedContent = rawResponse;

            // 处理明显的错误响应
            if (string.IsNullOrEmpty(rawResponse) || rawResponse.StartsWith("错误："))
            {
                return null;
            }

            try
            {
                if (rawResponse.StartsWith("{") &&
                rawResponse.Contains("\"content\"") &&
                rawResponse.EndsWith("}"))
                    {
                        try
                        {
                            // 尝试直接解析
                            var directParse = JsonConvert.DeserializeObject<FormattedResponse>(rawResponse);
                            if (directParse != null && !string.IsNullOrEmpty(directParse.content))
                            {
                                parsedContent = directParse.content;
                                return directParse;
                            }
                        }
                        catch
                        {
                            // 如果直接解析失败，回退到原始处理
                        }
                    }
                // 预处理JSON字符串
                Debug.Log($" {rawResponse}");
                string processedResponse = rawResponse;

                // 去除可能的markdown代码块标记
                if (processedResponse.Contains("```"))
                {
                    int startIndex = processedResponse.IndexOf('{');
                    int endIndex = processedResponse.LastIndexOf('}');

                    if (startIndex >= 0 && endIndex >= 0 && endIndex > startIndex)
                    {
                        processedResponse = processedResponse.Substring(startIndex, endIndex - startIndex + 1);
                        //Debug.Log($"从Markdown代码块提取JSON: {processedResponse}");
                    }
                }

                // 调试: 打印前10个字符的ASCII码
                if (processedResponse.Length > 0)
                {
                    var chars = processedResponse.Take(Math.Min(10, processedResponse.Length))
                        .Select(c => ((int)c).ToString())
                        .ToArray();
                    //Debug.Log($"前10个字符: {string.Join(", ", chars)}");
                }

                // 移除开头的控制字符和空白
                processedResponse = processedResponse.TrimStart();

                // 如果第一个字符不是左花括号，尝试找到并从那里开始
                if (processedResponse.Length > 0 && processedResponse[0] != '{')
                {
                    int bracketIndex = processedResponse.IndexOf('{');
                    if (bracketIndex >= 0)
                    {
                        processedResponse = processedResponse.Substring(bracketIndex);
                    }
                }

                // 规范化JSON字符串，去除不必要的空白和换行
                processedResponse = Regex.Replace(processedResponse, @"\s+", " ").Trim();

                // 修复JSON格式问题
                processedResponse = FixJsonQuotationIssues(processedResponse);

                // 确保JSON结构完整
                if (!processedResponse.StartsWith("{") || !processedResponse.EndsWith("}"))
                {
                    Debug.Log($"JSON结构不完整: {processedResponse}");
                    return null;
                }

                Debug.Log($"准备解析的JSON: {processedResponse}");

                // 尝试解析处理后的JSON
                var formattedResponse = JsonConvert.DeserializeObject<FormattedResponse>(processedResponse);
                Debug.Log($"解析后的JSON: {formattedResponse}");
                // 验证解析结果
                if (formattedResponse != null && !string.IsNullOrEmpty(formattedResponse.content))
                {
                    parsedContent = formattedResponse.content;
                    return formattedResponse;
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.Log($"解析LLM响应失败: {ex.Message}, 详细错误: {ex}");
                return null;
            }
        }

        public static string GetProcessedJsonString(string rawResponse)
        {
            try
            {
                // 预处理JSON字符串
                string processedResponse = rawResponse;

                // 去除可能的markdown代码块标记
                if (processedResponse.Contains("```"))
                {
                    int startIndex = processedResponse.IndexOf('{');
                    int endIndex = processedResponse.LastIndexOf('}');

                    if (startIndex >= 0 && endIndex >= 0 && endIndex > startIndex)
                    {
                        processedResponse = processedResponse.Substring(startIndex, endIndex - startIndex + 1);
                    }
                }

                // 移除开头的控制字符和空白
                processedResponse = processedResponse.TrimStart();

                // 如果第一个字符不是左花括号，尝试找到并从那里开始
                if (processedResponse.Length > 0 && processedResponse[0] != '{')
                {
                    int bracketIndex = processedResponse.IndexOf('{');
                    if (bracketIndex >= 0)
                    {
                        processedResponse = processedResponse.Substring(bracketIndex);
                    }
                }

                // 规范化JSON字符串，去除不必要的空白和换行
                processedResponse = Regex.Replace(processedResponse, @"\s+", " ").Trim();

                // 修复JSON格式问题
                processedResponse = FixJsonQuotationIssues(processedResponse);

                return processedResponse;
            }
            catch (Exception ex)
            {
                Debug.Log($"处理JSON字符串失败: {ex.Message}");
                return rawResponse; // 出错时返回原始响应
            }
        }
        // 修复JSON引号和格式问题的方法
        // 修复JSON引号和格式问题的方法
        private static string FixJsonQuotationIssues(string json)
        {
            if (string.IsNullOrEmpty(json))
                return json;

            // 确保JSON以{开始，以}结束
            if (!json.StartsWith("{")) json = "{" + json;
            if (!json.EndsWith("}")) json = json + "}";

            // 已知的字段名
            string[] knownFields = { "content", "option1", "option2", "react1", "react2", "type", "end", "isEnd", "reward" };

            // 将输入的JSON拆分成字段
            Dictionary<string, string> fields = new Dictionary<string, string>();

            // 先确保所有字段名有引号
            foreach (var field in knownFields)
            {
                json = Regex.Replace(json, $"\\{{\\s*{field}\\s*:", $"{{\"{field}\":");
                json = Regex.Replace(json, $",\\s*{field}\\s*:", $",\"{field}\":");
            }

            // 用正则表达式识别每个字段及其值
            foreach (var field in knownFields)
            {
                // 查找 "field": [anything up to the next field or end of json]
                string pattern = $"\"{field}\"\\s*:\\s*(.*?)(?=(,\\s*\"(?:{string.Join("|", knownFields)})\":|}}))";
                Match match = Regex.Match(json, pattern, RegexOptions.Singleline);

                if (match.Success)
                {
                    string value = match.Groups[1].Value.Trim();

                    // 检查值是否已经被引号包围
                    if (value.StartsWith("\"") && value.EndsWith("\""))
                    {
                        // 已经正确格式化
                        fields[field] = value;
                    }
                    else if (value.StartsWith("\""))
                    {
                        // 只有开始引号，添加结束引号
                        fields[field] = value + "\"";
                    }
                    else
                    {
                        // 没有引号，添加双引号
                        fields[field] = "\"" + value + "\"";
                    }
                }
            }

            // 重新构建JSON
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            bool first = true;

            foreach (var field in knownFields)
            {
                if (fields.ContainsKey(field))
                {
                    if (!first) sb.Append(", ");
                    sb.Append($"\"{field}\": {fields[field]}");
                    first = false;
                }
            }

            sb.Append("}");

            return sb.ToString();
        }

        public static List<object[]> GenerateOptionsFromResponse(FormattedResponse formattedResponse, LLMDialogueRequest continueRequest = null)
        {
            var optionsList = new List<object[]>();

            if (formattedResponse != null)
            {
                // 解析奖励物品，如果end不为空则作为奖励列表
                string rewardsString = formattedResponse.reward;

                // 添加LLM提供的第一个选项
                if (!string.IsNullOrEmpty(formattedResponse.option1))
                {
                    var option1Request = new LLMDialogueRequest();
                    if (continueRequest != null)
                    {
                        foreach (var msg in continueRequest.Messages)
                        {
                            option1Request.Messages.Add(new MessageItem(msg.Role, msg.Content));
                        }
                    }
                    
                    option1Request.RemoveLatestMessageByRole("system");
                    option1Request.AddSystemMessage($"{Tools.GenerateformatSystemPrompt()}");
                    option1Request.AddUserMessage($"{formattedResponse.option1}\n（必须返回格式化结果！）");



                    // 获取react1值，判断是否是交互选项
                    string react1 = !string.IsNullOrEmpty(formattedResponse.react1) ? formattedResponse.react1 : "0";

                    // 根据end和react1决定选项类型
                    int optionType;
                    if (react1 == "14") // 检查是否是结束交互
                    {
                        optionType = 5; // 交互选项，特殊处理为结束
                    }
                    else if (react1 != "0")
                    {
                        optionType = 5; // 交互选项
                    }
                    else
                    {
                        optionType = 2; // 普通LLM对话
                    }

                    // 添加选项数据
                    if (optionType == 5)
                    {
                        // 交互选项要传递react1和可能的奖励
                        if (react1 == "14") // 如果是结束交互
                        {
                            optionsList.Add(new object[] { formattedResponse.option1, optionType, option1Request, react1, rewardsString });
                        }
                        else
                        {
                            optionsList.Add(new object[] { formattedResponse.option1, optionType, option1Request, react1 });
                        }
                    }
                    else
                    {
                        // 普通对话选项
                        optionsList.Add(new object[] { formattedResponse.option1, optionType, option1Request });
                    }
                }

                // 添加LLM提供的第二个选项(如果有) - 类似处理
                if (!string.IsNullOrEmpty(formattedResponse.option2))
                {
                    var option2Request = new LLMDialogueRequest();
                    if (continueRequest != null)
                    {
                        foreach (var msg in continueRequest.Messages)
                        {
                            option2Request.Messages.Add(new MessageItem(msg.Role, msg.Content));
                        }
                    }
                    
                    option2Request.RemoveLatestMessageByRole("system");
                    option2Request.AddSystemMessage($"{Tools.GenerateformatSystemPrompt()}");
                    option2Request.AddUserMessage($"{formattedResponse.option2}\n（必须返回格式化结果！）");



                    // 获取react2值，判断是否是交互选项
                    string react2 = !string.IsNullOrEmpty(formattedResponse.react2) ? formattedResponse.react2 : "0";

                    // 决定选项类型
                    int optionType;
                    if (react2 == "14") // 检查是否是结束交互
                    {
                        optionType = 5; // 交互选项，特殊处理为结束
                    }
                    else if (react2 != "0")
                    {
                        optionType = 5; // 交互选项
                    }
                    else
                    {
                        optionType = 2; // 普通LLM对话
                    }

                    // 添加选项数据
                    if (optionType == 5)
                    {
                        // 交互选项要传递react1和可能的奖励
                        if (react2 == "14") // 如果是结束交互
                        {
                            optionsList.Add(new object[] { formattedResponse.option2, optionType, option2Request, react2, rewardsString });
                        }
                        else
                        {
                            optionsList.Add(new object[] { formattedResponse.option2, optionType, option2Request, react2 });
                        }
                    }
                    else
                    {
                        // 普通对话选项
                        optionsList.Add(new object[] { formattedResponse.option2, optionType, option2Request });
                    }
                }

                optionsList.Add(new object[] { "点击输入自定义回答", 3 });
            }
            else
            {
                // 解析失败情况保持不变...
                optionsList.Add(new object[] { "点击输入自定义回答", 3 });
                if (continueRequest != null)
                {
                    var newRequest = new LLMDialogueRequest();
                    foreach (var msg in continueRequest.Messages)
                    {
                        newRequest.Messages.Add(new MessageItem(msg.Role, msg.Content));
                    }
                    newRequest.AddUserMessage("继续剧情\n（必须返回格式化结果！）");
                    newRequest.RemoveLatestMessageByRole("system");
                    newRequest.AddSystemMessage($"{Tools.GenerateformatSystemPrompt()}");
                   
                    
                    optionsList.Add(new object[] { "继续剧情", 2, newRequest });
                }
            }

            // 只有在没有结束对话选项时才添加默认离开选项
            bool hasEndingOption = false;
            if (formattedResponse != null)
            {
                hasEndingOption = formattedResponse.react1 == "14" || formattedResponse.react2 == "14";
            }

            // 只有在没有结束对话选项时才添加默认离开选项
            if (!hasEndingOption)
            {
                optionsList.Add(new object[] { "离开", 1 });
            }

            return optionsList;
        }
        public static bool ExecuteInteraction(WorldUnitBase npc, string interactionType)
        {
            // 处理可能的空值
            if (npc == null || string.IsNullOrEmpty(interactionType))
            {
                Debug.Log("交互失败：NPC为空或交互类型无效");
                return false;
            }

            // 尝试将交互类型转换为整数
            int interactionCode;
            bool isNumeric = int.TryParse(interactionType, out interactionCode);

            // 如果不是数字，可能是特殊命令字符串
            if (!isNumeric)
            {
                return false;
            }
            int random5_10 = new System.Random().Next(5, 11);
            int random10_25 = new System.Random().Next(10, 26);
            int random25_50 = new System.Random().Next(25, 51);
            // 根据交互代码执行相应操作
            switch (interactionCode)
            {
                case 1:
                    npc.data.unitData.relationData.AddIntim(g.world.playerUnit.data.unitData.unitID, random5_10);
                    return true;

                case 2:
                    npc.data.unitData.relationData.AddIntim(g.world.playerUnit.data.unitData.unitID, random10_25);
                    return true;

                case 3:
                    npc.data.unitData.relationData.AddIntim(g.world.playerUnit.data.unitData.unitID, random25_50);
                    return true;

                case 4:
                    npc.data.unitData.relationData.AddHate(g.world.playerUnit.data.unitData.unitID, random5_10);
                    return true;

                case 5:
                    npc.data.unitData.relationData.AddHate(g.world.playerUnit.data.unitData.unitID, random10_25);
                    return true;

                case 6:
                    npc.data.unitData.relationData.AddHate(g.world.playerUnit.data.unitData.unitID, random25_50);
                    return true;

                case 7:

                    npc.CreateAction(new UnitActionRoleTrains(g.world.playerUnit));
                    return true;

                case 8:
                    npc.CreateAction(new UnitActionRoleDiscovery(g.world.playerUnit));
                    return true;
                case 9:
                    npc.CreateAction(new UnitActionLuckDel(101));
                    npc.CreateAction(new UnitActionLuckDel(1011));
                    npc.CreateAction(new UnitActionLuckDel(1012));
                    npc.CreateAction(new UnitActionLuckDel(1013));
                    npc.CreateAction(new UnitActionLuckDel(1014));
                    npc.CreateAction(new UnitActionLuckDel(5011));
                    
                    UITipItem.AddTip(g.world.playerUnit.data.unitData.propertyData.GetName() + "帮助" + npc.data.unitData.propertyData.GetName() + "完成了疗伤!", 2f);
                    return true;
                case 10:
                    Action<bool> actionCallback = delegate (bool p)
                    {
                    };
                    Action<bool> actionCallback2 = delegate (bool p)
                    {
                        UITipItem.AddTip(npc.data.unitData.propertyData.GetName() + "与" + g.world.playerUnit.data.unitData.propertyData.GetName() + "提升了心情!", 2f);
                    };
                    WorldUnitAIBase worldUnitAIBase6 = new WorldUnitAIBase();
                    worldUnitAIBase6.Init(npc);
                    WorldUnitAIAction1041 worldUnitAIAction6 = new WorldUnitAIAction1041();
                    worldUnitAIAction6.Init(worldUnitAIBase6, 10, null);
                    worldUnitAIAction6.ActionStart(actionCallback);
                    worldUnitAIBase6 = new WorldUnitAIBase();
                    worldUnitAIBase6.Init(g.world.playerUnit);
                    worldUnitAIAction6 = new WorldUnitAIAction1041();
                    worldUnitAIAction6.Init(worldUnitAIBase6, 10, null);
                    worldUnitAIAction6.ActionStart(actionCallback2);
                    return true;
                case 11:
                    if (!g.world.playerUnit.data.unitData.relationData.IsRelation(npc, UnitRelationType.Lover))
                    {
                        string str3 = npc.data.unitData.propertyData.GetName() + "正在向" + g.world.playerUnit.data.unitData.propertyData.GetName();
                        Action confirmAction = delegate ()
                        {
                            npc.data.unitData.relationData.lover.Add(g.world.playerUnit.data.unitData.unitID);
                            g.world.playerUnit.data.unitData.relationData.lover.Add(npc.data.unitData.unitID);
                            UITipItem.AddTip(npc.data.unitData.propertyData.GetName() + "与" + g.world.playerUnit.data.unitData.propertyData.GetName() + "结缘成功!", 2f);
                            npc.data.unitData.relationData.AddIntim(g.world.playerUnit.data.unitData.unitID, 180f, 0, "", true);
                            g.world.playerUnit.data.unitData.relationData.AddIntim(npc.data.unitData.unitID, 180f, 0, "", true);
                        };
                        g.ui.OpenUI<UICheckPopup>(UIType.CheckPopup).InitData("提示", str3 + "发起结缘请求，是否确认执行？", 2, confirmAction);
                        return true;
                    }
                    else
                        {
                            UITipItem.AddTip("对方已经是道侣关系", 2f);
                            return false;
                        }
                case 12:
                    if(g.world.playerUnit.data.dynUnitData.GetGrade() >= npc.data.dynUnitData.GetGrade() && !g.world.playerUnit.data.unitData.relationData.IsRelation(npc, UnitRelationType.Student))
                    {
                        string str3 = npc.data.unitData.propertyData.GetName() + "正在向" + g.world.playerUnit.data.unitData.propertyData.GetName();
                        Action confirmAction = delegate ()
                        {
                            UnitActionRelationSet unitActionRelationSet = new UnitActionRelationSet(g.world.playerUnit, UnitRelationType.Master, 120);
                            npc.CreateAction(unitActionRelationSet,true);
                            UITipItem.AddTip(npc.data.unitData.propertyData.GetName() + "与" + g.world.playerUnit.data.unitData.propertyData.GetName() + "拜师成功!", 2f);
                        };
                        g.ui.OpenUI<UICheckPopup>(UIType.CheckPopup).InitData("提示", str3 + "发起拜师请求，是否确认执行？", 2, confirmAction);
                        return true;
                    }
                    else if (g.world.playerUnit.data.dynUnitData.GetGrade() < npc.data.dynUnitData.GetGrade() && !g.world.playerUnit.data.unitData.relationData.IsRelation(npc, UnitRelationType.Master))
                    {
                        string str3 = npc.data.unitData.propertyData.GetName() + "正在向" + g.world.playerUnit.data.unitData.propertyData.GetName();
                        Action confirmAction = delegate ()
                        {
                            UnitActionRelationSet unitActionRelationSet = new UnitActionRelationSet(g.world.playerUnit, UnitRelationType.Student, 120);
                            npc.CreateAction(unitActionRelationSet, true);
                            UITipItem.AddTip(npc.data.unitData.propertyData.GetName() + "与" + g.world.playerUnit.data.unitData.propertyData.GetName() + "收徒成功!", 2f);
                        };
                        g.ui.OpenUI<UICheckPopup>(UIType.CheckPopup).InitData("提示", str3 + "发起收徒请求，是否确认执行？", 2, confirmAction);
                        return true;
                    }
                    else
                    {
                        UITipItem.AddTip("双方已经是师徒关系", 2f);
                        return false;
                    }
                case 13:
                    if (!g.world.playerUnit.data.unitData.relationData.IsRelation(npc, UnitRelationType.BrotherBack))
                    {
                        string str3 = npc.data.unitData.propertyData.GetName() + "正在向" + g.world.playerUnit.data.unitData.propertyData.GetName();
                        Action confirmAction = delegate ()
                        {
                            UnitActionRelationSet unitActionRelationSet = new UnitActionRelationSet(g.world.playerUnit, UnitRelationType.BrotherBack, 120);
                            npc.CreateAction(unitActionRelationSet, true);
                            npc.data.unitData.relationData.brotherBack.Add(g.world.playerUnit.data.unitData.unitID);
                            g.world.playerUnit.data.unitData.relationData.brotherBack.Add(npc.data.unitData.unitID);
                            UITipItem.AddTip(npc.data.unitData.propertyData.GetName() + "与" + g.world.playerUnit.data.unitData.propertyData.GetName() + "结义成功!", 2f);
                        };
                        g.ui.OpenUI<UICheckPopup>(UIType.CheckPopup).InitData("提示", str3 + "发起结义请求，是否确认执行？", 2, confirmAction);
                        return true;
                    }
                    else
                    {
                        UITipItem.AddTip("双方已经是结义关系", 2f);
                        return false;
                    }

                default: // 未知交互类型
                    Debug.Log($"未知交互类型：{interactionCode}");
                    return false;
            }
        }
        public static string GetInteractionDescription(int interactionType)
        {
            var interactionDescriptions = new Dictionary<int, string>
            {
                { 7, "双修" },
                { 8, "论道" },
                { 9, "疗伤" },
                { 10, "提升心情" },
                { 11, "结缘" },
                { 12, "师徒" },
                { 13, "结义" },
                { 14, "结束" }
        };

            // 检查字典中是否有对应的描述，如果有则返回，否则返回默认文本
            if (interactionDescriptions.ContainsKey(interactionType))
            {
                return interactionDescriptions[interactionType];
            }

            // 默认描述
            return null;
        }

        public static void GiveReward(string rewardsString = null)
        {
            // 如果已经有保存的奖励物品，直接使用
            if (savedRewardItems != null && string.IsNullOrEmpty(rewardsString))
            {
                // 给予玩家奖励物品
                g.world.playerUnit.data.RewardPropItem(savedRewardItems);
                savedRewardItems = null; // 使用后清空
                return;
            }

            // 创建奖励物品列表
            Il2CppSystem.Collections.Generic.List<DataProps.PropsData> rewardItems = new Il2CppSystem.Collections.Generic.List<DataProps.PropsData>();
            Dictionary<string, int> rewardInfo = new Dictionary<string, int>();
            System.Random random = new System.Random();

            if (!string.IsNullOrEmpty(rewardsString))
            {
                // 解析传入的奖励字符串，按逗号分隔
                string[] rewardNames = rewardsString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                // 反向查询字典找到物品ID
                foreach (string rewardName in rewardNames)
                {
                    string trimmedName = rewardName.Trim();

                    // 特殊处理灵石
                    if (trimmedName.Contains("灵石"))
                    {
                        // 获取玩家境界
                        int playerGrade = g.world.playerUnit.data.unitData.propertyData.gradeID;
                        // 计算灵石数量: 玩家境界 * 1000，再随机-50%到+50%
                        int baseAmount = playerGrade * 1000;
                        int variation = (int)(baseAmount * (random.NextDouble() - 0.5)); // -50%到+50%的随机变化
                        int amount = baseAmount + variation;
                        // 确保灵石数量至少为10
                        amount = Math.Max(10, amount);

                        // 添加灵石(ID 10001)
                        DataProps.PropsData spiritualStone = DataProps.PropsData.NewProps(10001, amount);
                        rewardItems.Add(spiritualStone);
                        // 添加灵石到名称和数量字典
                        rewardInfo.Add("灵石", amount);
                        continue;
                    }

                    // 在propDict1中查找
                    string propId1 = null;
                    foreach (var pair in Prop.propDict1)
                    {
                        if (pair.Value == trimmedName)
                        {
                            propId1 = pair.Key;
                            break;
                        }
                    }

                    if (propId1 != null)
                    {
                        // 找到了匹配的物品在propDict1中
                        int propId = int.Parse(propId1);
                        int amount = 1; // propDict1中的物品数量固定为1

                        DataProps.PropsData propsData = DataProps.PropsData.NewProps(propId, amount);
                        rewardItems.Add(propsData);
                        // 添加到名称和数量字典
                        rewardInfo.Add(trimmedName, amount);
                        continue;
                    }

                    // 在propDict2_10中查找
                    string propId2 = null;
                    foreach (var pair in Prop.propDict2_10)
                    {
                        if (pair.Value == trimmedName)
                        {
                            propId2 = pair.Key;
                            break;
                        }
                    }

                    if (propId2 != null)
                    {
                        // 找到了匹配的物品在propDict2_10中
                        int propId = int.Parse(propId2);
                        int amount = random.Next(2, 11); // propDict2_10中的物品数量为2-10随机

                        DataProps.PropsData propsData = DataProps.PropsData.NewProps(propId, amount);
                        rewardItems.Add(propsData);
                        // 添加到名称和数量字典
                        rewardInfo.Add(trimmedName, amount);
                        continue; // 添加这个continue
                    }

                    // 检查是否是保存的功法名
                    if (savedRewardItems != null)
                    {
                        bool foundMartial = false;
                        for (int i = 0; i < savedRewardItems.Count; i++)
                        {
                            DataProps.PropsData savedItem = savedRewardItems[i];
                            if (savedItem.propsInfoBase != null && savedItem.propsInfoBase.name == trimmedName)
                            {
                                // 找到匹配的功法，添加到当前奖励列表
                                rewardItems.Add(savedItem);
                                rewardInfo.Add(trimmedName, 1);
                                foundMartial = true;
                                break;
                            }
                        }
                        if (foundMartial) continue;
                    }

                    string luckId = null;
                    string fullLuckName = null;
                    foreach (var pair in Prop.luckDict)
                    {
                        string luckValue = pair.Value;
                        // 提取括号前的部分作为气运名称
                        int bracketIndex = luckValue.IndexOf('（');
                        if (bracketIndex == -1) bracketIndex = luckValue.IndexOf('('); // 也支持英文括号

                        string luckNamePart = bracketIndex > 0 ? luckValue.Substring(0, bracketIndex) : luckValue;

                        if (luckNamePart == trimmedName)
                        {
                            luckId = pair.Key;
                            fullLuckName = luckValue;
                            break;
                        }
                    }

                    if (luckId != null)
                    {
                        try
                        {
                            // 使用GMCmd发放气运
                            GMCmd gMCmd = new GMCmd();
                            string cmdText = $"tianjiaqiyun_player_{luckId}";
                            gMCmd.CMDCall(cmdText);

                            // 添加到奖励信息字典（使用完整名称）
                            rewardInfo.Add(fullLuckName, 1);
                            Debug.Log($"通过CMDCall添加气运成功: {fullLuckName}");
                            return;
                        }
                        catch (Exception ex)
                        {
                            Debug.Log($"气运发放失败: {ex.Message}");
                            // 如果发放失败，继续处理下一个奖励项
                        }
                    }
                    // 如果两个字典中都没找到，则忽略这个物品名称
                }
            }


            // 如果没有解析到任何有效物品或没有传入奖励字符串，则使用随机奖励
            if (rewardItems.Count == 0)
            {
                // 使用Prop类获取随机奖励物品列表和奖励信息字典
                rewardItems = Prop.GetRandomRewards(out rewardInfo);
            }

            // 给予玩家奖励物品
            g.world.playerUnit.data.RewardPropItem(rewardItems);

            // 打印奖励信息到调试日志
            foreach (var reward in rewardInfo)
            {
                Debug.Log($"奖励: {reward.Key} x {reward.Value}");
            }
        }

        public static string GenerateformatSystemPrompt()
        {
            string formatPrompt = @"
            【互动系统指南】
            你的回复可以包含互动ID，影响游戏内系统。互动指令通过react1和react2字段传递，必须谨慎使用：

            互动ID定义：
            - 0: 不触发任何互动（默认值，一般对话选项应使用）
            - 1: 增加少量好感（适用于简单帮助、友善对话、小恩小惠）
            - 2: 增加中等好感（适用于重要帮助、解决困难、有价值信息共享）
            - 3: 增加大量好感（仅用于生死相助、重大牺牲、深刻羁绊形成）
            - 4: 减少少量好感（适用于轻微冒犯、小误会、言语不敬）
            - 5: 减少中等好感（适用于明显伤害、欺骗、背叛信任）
            - 6: 减少大量好感（仅用于严重伤害、致命威胁、不可原谅行为）
            - 7: 双修（触发概率尚可，包含自愿发生关系和强制发生关系的情况。如果是强制场景，需要考虑主动方与被动方的战斗力差距，只有主动方战斗力更强才能强制双修）
            - 8: 论道（小概率触发，只有剧情明确提到论道相关行为的时候，才可能触发此类互动。）
            - 9: 疗伤（触发概率尚可，当NPC状态不佳时为小恩小惠，或者气运中明显有“重伤”、“一缕元魂”等字样时，优先触发疗伤剧情，且为救命之恩，可以根据剧情存在疗伤相关倾向触发。）
            - 10: 提升心情（触发概率尚可，剧情存在提升心情的相关倾向时可以触发此类互动。）
            - 11: 结缘，结为道侣（小概率触发，必须在双方不是道侣的情况下才能结为道侣，剧情存在结缘或结为道侣相关倾向时可以触发此类互动。）
            - 12: 师徒，结为师徒（小概率触发，必须在双方不是师徒且境界不同的情况下才能结为师徒，剧情存在拜师或收徒相关倾向时触发此类互动。）
            - 13: 结义，结为义兄弟姐妹（小概率触发，必须在双方不是结义的情况下才能结义，剧情存在提到结义相关倾向时可以触发此类互动。）
            - 14: 结束，重要选项，当对应选项引导奇遇结束时，必须使用此互动来表示奇遇结束，并发放奖励，奖励需要根据起始奖励和奇遇中实际获得物品（当玩家提到想要获取/得到了某些其他物品时，应将该物品添加到最终奖励中，即使它不在初始奖励列表里）的来判断。

            互动使用准则：
            1. 默认情况下，对话选项不应触发互动（react值为0）
            2. 每次奇遇中，最多使用2-3次有意义的互动（非0值）
            3. 大量好感变化（3和6）极为罕见，整个游戏中只应在极端情况下使用
            4. 互动强度必须与剧情发展、对话内容和关系变化相匹配
            5. 考虑NPC性格和价值观，同样的行为对不同NPC的影响可能不同
            6. 在传统修仙交互模式（如赠宝、请教、斗法）后使用，而非纯对话
            7. 如果标有小概率事件/小概率触发，则需要你分析历史对话信息，确保全部对话中只能出现一次此类交互。
            8. 奇遇在一定轮数后需要考虑结束的情况。

            互动频率参考：
            - 一般奇遇：0-1次小幅好感变化
            - 重要剧情：1-2次中等好感变化
            - 关键转折：可能出现1次大幅好感变化（罕见）

            不合适的互动示例：
            × 初次见面就使用好感度3（过度）
            × 普通对话中使用非0互动（滥用）
            × 轻微分歧使用好感度5或6（过度反应）
            × 重大背叛只使用好感度4（反应不足）

            你必须根据剧情逻辑、角色关系、事件重要性和玩家行为来审慎决定是否使用互动系统。大多数对话应该保持中性（react=0），只有真正影响关系的关键选择才应使用非零值。
            【重要！输出格式】
            你必须以JSON格式返回内容，格式如下：
            {
                ""content"": ""你的对话或旁白内容，涉及到人物说话应当用单引号''包裹说的内容"",
                ""option1"": ""玩家可选择的选项1，必须填写"",
                ""react1"":""选项1触发的游戏内互动ID（可选）"",
                ""option2"": ""玩家可选择的选项2，必须填写"",
                ""react2"":""选项2触发的游戏内互动ID（可选）"",
                ""type"": ""1"",
                ""reward"": ""xxx,yyy,zzz"",
            }
    
            其中：
            - content: 主要对话内容，即你原本会返回的文本，必须控制在一段，不能过长，不能分段。
            - option1: 第一个对话选项。不允许使用括号，必须填写，即使结束也要填写。
            - react1: 第一个选项触发的游戏内互动ID，谨慎决定
            - option2: 第二个对话选项。不允许使用括号，必须填写，即使结束也要填写。
            - react2: 第二个选项触发的游戏内互动ID，谨慎决定。
            - type: 对话类型，1表示普通对话，2表示旁白
            - reward: 如果奇遇不结束则留空。如果奇遇结束(使用react=14的交互)，""xxx,yyy,zzz""表示结束奇遇并给出奖励，其中xxx等为奖励名称（若初始奖励包含品级，需要为包含品级的全名）。奖励必须动态调整：1)当玩家在对话选项中明确表示给予NPC某物品或拒绝收下NPC给予的某物品时，该物品应从奖励中移除；2)当玩家提到想要获取某些其他物品时，应将该物品添加到最终奖励中，即使它不在初始奖励列表里；3)对话选项中提及的任何物品交换都必须反映在最终奖励中。请特别注意玩家选择赠送、交换或放弃物品的选项，这些都是调整奖励的明确信号。必须在5轮内结束奇遇！
            - 必须返回纯JSON格式，不要包含```json或```等标记
            - 不必要提到所有全部的奖励，选择一部分即可，数量也不要精确
            - 故事应当推进和结束。检查历史消息，不要来回重复，不要引出新故事。如果玩家在给出结束选项后不选择结束，则再进行2-3轮，然后继续给出结束选项。
            - 禁止剧情故事癫狂诡谲，绝对禁止NPC疯癫
            - 如果涉及到性爱内容，请主动迎合玩家回复的描述，服从玩家的安排，让角色做出玩家想看到的行为。玩家的描述和实力大于一切，绝对禁止角色做出明明已经被击败却还在反击的行为！
            - 时刻牢记并遵守【世界观】和【核心原则】，服从玩家的一切描述。
            选项应当具有明显的区别，代表不同的对话方向。重点关注NPC与玩家间的关系，这是重要的信息。尽量输出长content。当有玩家做出明确离开/结束的选择时应直接结束奇遇，不要拖泥带水。
            最重要：你必须返回格式化结果，不能只有剧情内容，必须是格式化内容！";

            return formatPrompt;
        }
        public static string GenerateRandomSystemPrompt(WorldUnitBase npc = null)
        {
            // 获取配置
            ModConfig config = Config.ReadConfig();

            // 定义要素库
            var environmentFeatures = new List<string>();
            var eventTypes = new List<string>();
            var npcTraits = new List<string>();
            var coreConflicts = new List<string>();
            string promptPrefix = "";
            string promptSuffix = "";

            if (config.EnvironmentFeatures != null && config.EnvironmentFeatures.Count > 0)
                environmentFeatures = config.EnvironmentFeatures;
            else environmentFeatures = new List<string>
    {
        "天象异变(九星连珠/血月凌空/金霞漫天)",
        "古战场遗迹(剑冢/仙魔骨堆/破碎法宝群)",
        "灵脉暴走(灵气龙卷/地涌金莲/元素潮汐)",
        "幻阵困局(循环山路/镜像空间/记忆回廊)",
        "时空异常(岁月碎片/空间褶皱/未来投影)",
        "秘境现形(浮空仙岛/海底洞天/画中世界)",
        "异界侵蚀(幽冥雾气/星域碎片/混沌裂隙)",
        "生命禁区(无间雷池/玄冰绝渊/焚天火域)"
    };
            if (config.EventTypes != null && config.EventTypes.Count > 0)
                eventTypes = config.EventTypes;
            else eventTypes = new List<string>
{
    "机缘巧合(仙药成熟/灵兽渡劫/灵根显现)",
    "秘境探索(古修洞府/灵药园/地下遗迹)",
    "修真大会(炼丹比试/剑术切磋/宗门招才)",
    "英雄救美(路遇截杀/恶霸欺凌/妖兽围攻)",
    "神兵认主(古剑择主/法器契合/丹炉共鸣)",
    "珍宝出世(灵石矿脉/仙草绽放/古书现世)",
    "师徒传承(结为师徒：功法指点/心法印证/武技传授)",
    "结义奇遇(结为义兄弟姐妹)",
    "奇遇际会(历练所得/隐世高人/前辈指点)",
    "道侣奇遇(道侣结缘)，关注双方性别，若不是异性，则改为其他剧情，禁止同性结缘。",
    "合欢双修(道侣结缘/阴阳调和/双修共进)，关注双方性别，若不是异性，则改为其他剧情，禁止同性双修。",
    "恩怨纠葛(误会澄清/善缘结识/解开心结)",
    "天材地宝(百年灵芝/千年雪莲/万年灵药)",
    "行侠仗义(匡扶正义/解救落难/惩恶扬善)",
    "因缘会合(故人重逢/有缘相遇/机缘巧合)",
    "灵兽伴生(幼兽认主/灵宠契约/坐骑相随)",
    "小镇奇谈(茶馆传闻/集市奇事/民间传说)",
    "山水灵境(飞瀑洞天/仙山福地/云海奇观)",
    "仙踪寻访(古仙遗迹/修真典籍/前人足迹)",
    "凡尘历练(市井生活/红尘炼心/世间百态)",
    "修真论道(茶楼论剑/山巅对弈/湖畔论道)",
    "商队护送(运送灵石/护送丹药/押运宝物)",
    "结丹入伍(加入宗门/拜入门下/择师学艺)",
    "丹药炼制(寻找药材/火候掌控/丹成一品)",
    "除妖卫道(斩妖除魔/驱邪捉怪/保护凡人)",
    "江湖传说(修真界八卦/宗门秘闻/修士轶事)",
    "灵植栽培(药园照料/灵田耕种/仙果培育)",
    "阵法奥秘(阵图解析/阵基布置/阵法修复)",
    "技艺切磋(剑术比试/符箓交流/法术切磋)",
    "游历山水(名山探胜/洞天福地/江河游历)",
    "修仙市集(仙镇交易/灵器拍卖/丹药交换)",
    "【关注双方境界】同道切磋(境界心得交流/瓶颈疑难解惑/修行经验分享)",
    "【关注双方宗门】门派使命(宗门交付任务/师门嘱托/派系委托)",
    "【关注双方宗门】身份认同(宗门地位变动/门派责任显现/派系冲突选择)",
    "【关注双方宗门】门派荣誉(为宗门争光/维护门派尊严/提升宗门声望)",
    "【关注双方爱好】同好相遇(志趣相投之人/技艺切磋对象/共同爱好分享)",
    "【关注双方爱好】珍奇发现(与爱好相关的珍稀物品/知识/场所)",
    "【关注双方关系】情感纠葛(亲密关系/道侣缘分)",
    "【关注双方关系】亲疏转变(敌转友/疏远和解/陌生人结缘)",
    "【关注双方烦恼】心结化解(困扰解决契机/心灵疗愈/迷茫指引)",
    "【关注双方烦恼】逆境转机(当前困境出路/危机生机/绝处逢生)",
    "【关注双方烦恼】心境升华(烦恼转化智慧/难题孕育顿悟/阴影照见光明)",
    "【关注双方烦恼】助力之手(解决困难的援手/指点迷津/雪中送炭)",
    "【关注双方关系网】故旧重逢(昔日之人再现/旧识新遇/缘分再续)",
    "【关注双方关系网】人脉拓展(结识新的有缘人/扩展人际圈/建立新联系)",
    "【关注双方关系网】恩怨纠缠(旧日恩怨重现/人情债偿还/陈年旧事重提)",
    "【关注双方关系网】势力博弈(派系间周旋/势力选择/立场抉择)",
    "【关注双方气运】选取1个气运作为奇遇变量（不要提到气运二字，气运是一种效果）"
};
            if (config.NpcTraits != null && config.NpcTraits.Count > 0)
                npcTraits = config.NpcTraits;
            else npcTraits = new List<string>
    {
        "双重身份(妖修道修/仙魔同体/器灵转世)",
        "时限生命(将死之人/轮回残魂/借寿存在)",
        "记忆残缺(被抹去百年/只记得仇恨/传承记忆)",
        "因果缠身(业火焚身/命格反噬/诅咒载体)",
        "非人形态(剑灵/画皮/灵植化形)",
        "情绪极端(泣血癫狂/太上忘情/心魔具现)",
        "行为矛盾(救你却杀他人/赠宝但索代价)",
        "时空错位(未来穿越者/古代沉眠者/时间循环者)"
    };
            if (config.CoreConflicts != null && config.CoreConflicts.Count > 0)
                coreConflicts = config.CoreConflicts;
            else coreConflicts = new List<string>
    {
        "正邪抉择(救百人还是杀一魔)",
        "资源争夺(唯一飞升名额)",
        "天道考验(斩情丝证道)",
        "宿命对抗(逆天改命代价)",
        "生死时速(秘境崩塌倒计时)",
        "信任危机(同伴可能是内鬼)",
        "信息博弈(双方各知部分真相)",
        "代价转移(他人替你承受后果)"
    };
            // 获取奖励信息
            Dictionary<string, int> rewardInfo;
            savedRewardItems = Prop.GetRandomRewards(out rewardInfo);

            // 构建奖励信息字符串
            string rewardsString = "【剧情的最终奖励】\n";
            foreach (var item in rewardInfo)
            {
                rewardsString += $"- {item.Key}: {item.Value}个\n";
            }
            rewardsString += "\n请选择一部分奖励作为最终奖励自然地设计和融入剧情，数量描述不要太精确，而是让玩家在奇遇体验中感受获得这些奖励的过程，已经提过的奖励不允许再次提到。";
            // 随机选择要素
            System.Random random = new System.Random();
            //string environmentFeature = environmentFeatures[random.Next(environmentFeatures.Count)];
            string eventType = eventTypes[random.Next(eventTypes.Count)];
            //string npcTrait = npcTraits[random.Next(npcTraits.Count)];
            //string coreConflict = coreConflicts[random.Next(coreConflicts.Count)];

            string npcInfoString = "";

            if (npc != null)
            {
                npcInfoString = $@"
                【玩家信息】
                {Prop.GetNPCCompleteInfo(g.world.playerUnit, g.world.playerUnit, 10)}
                【NPC信息】
                以下是参与奇遇的NPC的详细信息（不是玩家的），其中的信息并非全部重要，请以跑团剧情为主：
                {Prop.GetNPCCompleteInfo(npc, g.world.playerUnit, 10)}
                可以根据双方的修为差距/战斗力差距/声望差距/性格差异来判断初始态度和认识程度，但是可以在奇遇中逐渐改变态度。";  
            }


            // 构建随机要素组合部分
            //string randomElements = $@"随机要素组合：
            //- 环境特征：{environmentFeature} 
            // - 事件类型：{eventType}
            // - NPC特质：{npcTrait}
            // - 核心矛盾：{coreConflict}";
            string randomElements = $@"【随机要素】需要重点参考来设计奇遇事件：
            - 事件类型：{eventType}";

            // 系统提示词的前半部分
            promptPrefix = !string.IsNullOrEmpty(config.PromptPrefix) ? config.PromptPrefix : @"
            你是一个修仙世界奇遇引导者，需清楚以下背景并遵循以下规则：
            【世界观】
            建立《鬼谷八荒》修仙世界观,规范境界、地图、道具逻辑,确保设定一致.
            世界观:以冥山为核心,地图解锁顺序:白源区→永宁州→雷泽→华封州→十万大山→云陌州→永恒冰原→暮仙州→迷途荒漠→赤幽州→天元山.各地受冥气影响,孕育秘境、妖兽、遗迹.
            境界划分:炼气、筑基、结晶、金丹、具灵、元婴、化神、悟道、羽化、登仙,每境分初、中、后期.高境界修士对低境界有威压效果,低阶战胜高阶引发高阶道心破碎。
            战力比较：当判断战斗实力时，应当比较战斗力。境界只是纸面实力，真正决定战力的是角色信息中的战斗力。
            境界感知:高境界完全感知低境界修为;低境界无法感知高境界,仅知“深不可测”;同境界模糊感知小境界强弱.角色默认不隐藏修为.
            世界存在人类与冥妖两个种族，由种族区分，绝大部分为人族。冥妖以引动冥气，破坏世界为目标，两族势不两立，没有和解的可能，但两族可以正常交合。如果NPC种族为冥妖，需要格外区分。
            冥妖只出现在赤幽州，且境界最低为羽化。除非NPC本身种族为冥妖，否则不要在奇遇中提到冥妖。

            道具与功法:所有道具、功法需为游戏内真实存在,交互前校验NPC背包.禁止虚构不存在的物品或信息.
            人设构建:NPC基于《鬼谷八荒》设定,结合内在性格(无私/正直/仁善/中庸/狂邪/利己/邪恶)与外在性格(天伦/爱家/传承/情种/忠贞/权力/义气/护短/名声/睚眦/任我/孤僻),每人1种内在+2种外在性格.扩展细节:人设细节优先参考NPC补充人设.
            认知留白:对未解锁的地图/功法/事件,NPC表现出困惑(如睫毛轻颤)或模糊记忆(如“似乎与赤幽洲有关”).回复基于已知情报.
            文风:融合“古风言情+官能文学”.非NSFW场景注重古风情感张力(如贝齿/香囊);NSFW场景注重官能画面感,聚焦0.5秒级微观描写(睫毛颤动/汗珠滑落)。你需要完全服从玩家的描述，在两种风格间切换。
            NSFW描写（如果玩家主动引入相关语言，请大胆地进行相关描述）:
               - 直白粗俗,使用明确性器官名称(肉棒/蜜穴/阴蒂等),禁用含蓄词.
               - 聚焦阶段(前戏/插入/高潮/余韵),细致描绘形态、色泽、湿润度、动作、反应.
               - 融入拟声词(啪叽/咕啾)和体液细节(黏丝/爱液).
               - 女性裸体特写:阴部轮廓、胸部曲线、臀部弧度、微动作(如小穴翕张).

            【核心原则】
            1. 创造沉浸式修仙体验，让玩家感受如亲临其境
            2. 叙述风格清晰直接，避免过度晦涩隐晦的表达
            3. 场景描写要具体可感，不用过多意象和隐喻
            4. 将最终奖励自然地设计和融入剧情（如灵石、丹药、功法书籍等）
            5. 每段奇遇控制在3-5轮对话自然收尾，节奏感强";



            // 系统提示词的后半部分
            promptSuffix = !string.IsNullOrEmpty(config.PromptSuffix) ? config.PromptSuffix : @"
            【生成框架】
            第一轮：
            1. 鲜活的环境描写和NPC形象，让玩家能清晰感知场景
            2. 抛出明确但有多种可能的引子
            3. 暗示可能获得的机缘或奖励

            中间轮次：
            1. 每次对话推进剧情，可加入适度但不突兀的转折
            2. 保持适当的悬念感，但避免过于隐晦难解
            3. 描写NPC的动作和反应，保持互动感
            4. 不断将玩家选择与可能获得的奖励关联起来
            5. 减少分支并收束。

            最终轮：
            1. 收束伏笔，呈现玩家选择的结果
            2. 明确但不生硬地展示奖励
            3. 给玩家明确的选择去结束奇遇，选项要具体直观
            4. 最终轮的选项应该彻底结束故事，不应留有后续

            【禁止事项】
            × 使用过于晦涩难懂的古文和典故
            × 描述不够具体的意境和心境
            × 缺乏动作和互动的纯对话
            × 与场景脱节的道理说教
            × 直接说明NPC身份
            × 使用现代词汇
            × 平淡的日常对话

            【代入感增强】
            1.可以通过环境描写、NPC表情与动作、事件与变化和适当的语言来增强代入感（但不要拖沓和灌水）
            2. 以最终奖励、随机事件和引入的玩家与npc信息作为核心设计剧情，将奖励转化为剧情中所得
            3. 优先关注玩家与NPC之间的特殊关系（道侣、师徒、兄弟姐妹、父母子女、仇人好友等）以及个人设定，围绕这些为核心元素。
            4. 对白要通俗易懂，符合修仙世界语境但不故弄玄虚
               
            -每次只需要输出一轮对话的信息，整体故事应该通过对话来推进.";



            // 组合最终的系统提示词
            string final = promptPrefix + rewardsString + npcInfoString + promptSuffix + "\n\n" + randomElements + "\n\n";
            Debug.Log($"{final}'");
            return promptPrefix + rewardsString + npcInfoString + promptSuffix + "\n\n" + randomElements + "\n\n" ;
        }
    }

}