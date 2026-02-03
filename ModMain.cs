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
using System.Text;
using static MOD_kqAfiU.Tools;
using static UIIconTool;
using UnhollowerBaseLib;

namespace MOD_kqAfiU
{
    // 消息项类，表示对话中的一条消息
    [Serializable]
    public class MessageItem
    {
        public string Role { get; set; }
        public string Content { get; set; }

        public MessageItem(string role, string content)
        {
            Role = role;
            Content = content;
        }
    }

    // 改进的LLM对话请求数据结构
    [Serializable]
    public class LLMDialogueRequest
    {
        public List<MessageItem> Messages { get; set; } = new List<MessageItem>();

        public LLMDialogueRequest()
        {
        }
        public void DebugPrintMessages()
        {
            Debug.Log($"===== LLMDialogueRequest Messages (Total: {Messages.Count}) =====");
            for (int i = 0; i < Messages.Count; i++)
            {
                Debug.Log($"[{i}] Role: {Messages[i].Role}, Content前20字符: {Messages[i].Content.Substring(0, Math.Min(20, Messages[i].Content.Length))}");
            }
            Debug.Log("===== End of LLMDialogueRequest Messages =====");
        }


        public bool RemoveLatestMessageByRole(string role)
        {
            // 从后向前遍历查找匹配角色的消息
            for (int i = Messages.Count - 1; i >= 0; i--)
            {
                if (Messages[i].Role.Equals(role, StringComparison.OrdinalIgnoreCase))
                {
                    // 找到匹配角色的最新消息，删除它
                    Messages.RemoveAt(i);
                    return true; // 返回true表示成功删除
                }
            }

            // 未找到匹配角色的消息，返回false
            return false;
        }

        // 添加一条系统消息
        public void AddSystemMessage(string content)
        {
            Messages.Add(new MessageItem("system", content));
        }

        // 添加一条用户消息
        public void AddUserMessage(string content)
        {
            Messages.Add(new MessageItem("user", content));
        }

        // 添加一条助手消息
        public void AddAssistantMessage(string content)
        {
            Messages.Add(new MessageItem("assistant", content));
        }

        // 创建一个简单的系统+用户消息请求
        public static LLMDialogueRequest Create(string userMessage, string systemPrompt = null)
        {
            var request = new LLMDialogueRequest();

            if (!string.IsNullOrEmpty(systemPrompt))
            {
                request.AddSystemMessage(systemPrompt);
            }

            request.AddUserMessage(userMessage);
            return request;
        }
    }

    public class ModMain
    {
        private TimerCoroutine corUpdate;
        private static HarmonyLib.Harmony harmony;
        private Example example;
        private static ModMain _instance;
        public static int playerTalk = 437762042;
        private static int pictalk = 705106572;
        private static int spliceDialogId = 1100709579;
        public static int npcTalk = 1461570040;
        private static int onlyTalk = -250527135;
        private static int taskID = 988567713;
        private readonly Queue<Action> _mainThreadQueue = new Queue<Action>();
        private readonly object _queueLock = new object();
        private bool waitingForLLMResponse = false;
        private bool isFirstDialog = true;
        private bool inOngoingDialog = false;
        public static List<WorldUnitBase> dialogueNpcs = new List<WorldUnitBase>();
        private static Dictionary<string, string> pendingCachedEffects = new Dictionary<string, string>();

        // LLM相关参数和状态
        private LLMConnector llmConnector;
        public static string apiUrl = ""; // 默认为空，将从配置中读取
        public static string apiKey = ""; // 默认为空，将从配置中读取
        public static string modelName = ""; // 默认为空，将从配置中读取

        // 模型参数变量
        public static int max_tokens = 0; // 默认为0，将从配置中读取
        public static float temperature = 0; // 默认为0，将从配置中读取
        public static int top_k = 0; // 默认为0，将从配置中读取
        public static float top_p = 0; // 默认为0，将从配置中读取
        public static float frequency_penalty = 0; // 默认为0，将从配置中读取
        private float lastMoveTime = 0f;
        public static string pendingLLMResponse = null;
        private GameObject npcButton1;
        private GameObject configButton1;
        public static bool hasTriggeredBattleInCurrentAdventure = false;
        public static bool needReview = false;
        public static LLMDialogueRequest currentRequest = null; // 存储当前对话的请求，用于继续对话
        public static bool hasTriggeredDiscussionInCurrentAdventure = false; // 当前奇遇是否已触发论道
        public static List<string> givenRewardsInCurrentAdventure = new List<string>(); // 当前奇遇已发放的奖励
        public static bool waitingForShortEventResponse = false;
        public static string pendingShortEventResponse = null;

        public static float encounterProbability = 0.015f; // 默认值为0.015
        public static float shortEventProbability = 0.015f;

        public static float llmRequestStartTime = 0f;

        public static bool isInBattle = false;
        public static bool autoColoringEnabled = true;
        public static bool isCreatingItems = false;


        private GameObject _currentUIInstance = null;
        private WorldUnitBase _currentNpcForInput = null;
        private bool _inputUIOpened = false;
        private Action<ETypeData> battleStartCall;


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

        // 添加处理战斗结束响应的静态方法

        public void Init()
        {
            _instance = this;
            if (harmony != null)
            {
                harmony.UnpatchSelf();
                harmony = null;
            }


            harmony = new HarmonyLib.Harmony("MOD_kqAfiU");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            g.events.On(EGameType.IntoWorld, (Il2CppSystem.Action<ETypeData>)OnIntoWorld);
            g.events.On(EGameType.OpenUIEnd, (Il2CppSystem.Action<ETypeData>)OpenUIEnd);
            //g.events.On(EGameType.CloseUIEnd, (Il2CppSystem.Action<ETypeData>)CloseUIEnd);
        }

        public void OnIntoWorld(ETypeData data)
        {
            Config.InitConfig();
            ModConfig config = Config.ReadConfig();
            if (config != null)
            {
                // 将配置写入MOD全局变量
                ModMain.apiUrl = config.ApiUrl;
                ModMain.apiKey = config.ApiKey;
                ModMain.modelName = config.ModelName;
                ModMain.max_tokens = config.MaxTokens;
                ModMain.temperature = config.Temperature;
                ModMain.top_k = config.TopK;
                ModMain.top_p = config.TopP;
                ModMain.frequency_penalty = config.FrequencyPenalty;
                ModMain.encounterProbability = config.EncounterProbability;
                ModMain.shortEventProbability = config.ShortEventProbability;
                ModMain.autoColoringEnabled = config.AutoColoringEnabled;
            }


            Tools.Initialize(apiUrl, apiKey, modelName);
            UIRewardAndShortConfig.isGenerating = false;
            //DramaPolish.Initialize();

            hasTriggeredDiscussionInCurrentAdventure = false;
            givenRewardsInCurrentAdventure.Clear();
            g.data.dataObj.data.SetString("LLMcontent", "");
            g.data.dataObj.data.SetString("ShortEventContent", "");
            dialogueNpcs.Clear();
            pendingLLMResponse = null;
            currentRequest = null;
            waitingForLLMResponse = false;
            isInBattle = false;
            inOngoingDialog = false;
            var moveAction = new UnitActionMovePlayer(Vector2Int.right);
            g.events.On(EGameType.OneUnitCreateOneActionBack(g.world.playerUnit, moveAction.GetIl2CppType()), (Il2CppSystem.Action<ETypeData>)OnPlayerMove);
            this.battleStartCall = new Action<ETypeData>(this.OnBattleStart);
            g.events.On(EBattleType.BattleStart, this.battleStartCall, 0, false);
            
            corUpdate = g.timer.Frame(new Action(OnUpdate), 60, true);
        }

        public void OnBattleStart(ETypeData e)
        {
            isInBattle = true;
        }

        public void CloseUIEnd(ETypeData e)
        {
            // 销毁按钮
            CloseUIEnd closeUIEnd = e.Cast<CloseUIEnd>();
            if (closeUIEnd.uiType.uiName == UIType.NPCInfo.uiName)
            {
                if (configButton1 != null)
                {
                    GameObject.Destroy(configButton1);
                    configButton1 = null;
                }
                if (npcButton1 != null)
                {
                    GameObject.Destroy(npcButton1);
                    npcButton1 = null;
                }
            }


        }

        public void OpenUIEnd(ETypeData e)
        {
            OpenUIEnd openUIEnd = e.Cast<OpenUIEnd>();
            /*
            if (openUIEnd.uiType.uiName == UIType.DramaDialogue.uiName)
            {
                UIDramaDialogue dramaUI = g.ui.GetUI<UIDramaDialogue>(UIType.DramaDialogue);
                if (dramaUI != null)
                {
                    if (dramaUI.dramaData != null && dramaUI.dramaData.dialogueText.ContainsKey(1461570040))
                    {
                        GameObject regenPrefab = g.res.Load<GameObject>("ui/regenbutton/regenbutton");
                        if (regenPrefab != null)
                        {
                            GameObject regenButtonObj = UnityEngine.Object.Instantiate(regenPrefab);
                            regenButtonObj.name = "RegenButton";

                            // 获取按钮内部Canvas并设置位置
                            Transform regenCanvasTransform = regenButtonObj.transform.Find("Canvas");
                            if (regenCanvasTransform != null)
                            {
                                Canvas regenCanvas = regenCanvasTransform.GetComponent<Canvas>();
                                if (regenCanvas != null)
                                {
                                    regenCanvasTransform.SetParent(dramaUI.transform, false);
                                    regenCanvas.overrideSorting = false;
                                    //regenCanvas.sortingOrder = 100;

                                    // 设置内部Canvas的位置
                                    RectTransform canvasRectTrans = regenCanvasTransform.GetComponent<RectTransform>();
                                    canvasRectTrans.anchorMin = new Vector2(0.415f, 0.71f);
                                    canvasRectTrans.anchorMax = new Vector2(0.415f, 0.71f);
                                    canvasRectTrans.pivot = new Vector2(1, 1);
                                    //canvasRectTrans.anchoredPosition = new Vector2(-70, -20);
                                }

                                UnityEngine.UI.Button regenButton = regenCanvasTransform.GetComponentInChildren<UnityEngine.UI.Button>();
                                if (regenButton != null)
                                {
                                    // 修改按钮文本
                                    Text buttonText = regenButton.GetComponentInChildren<Text>();
                                    if (buttonText != null)
                                    {
                                        buttonText.text = "重新生成";
                                    }

                                    // 设置按钮事件
                                    regenButton.onClick.RemoveAllListeners();
                                    Action clickAction = delegate () {
                                        UITipItem.AddTip("重新生成回应中...", 1f);

                                        // 关闭对话UI
                                        dramaUI.CloseUI();


                                        // 获取当前存储的对话内容 - 仅用于参考，不会修改
                                        string currentContent = g.data.dataObj.data.GetString("LLMcontent");

                                        // 原来的重新生成逻辑
                                        if (currentRequest != null)
                                        {
                                            currentRequest.RemoveLatestMessageByRole("assistant");
                                            // Debug.Log("移除assistant后的消息列表:");
                                            //currentRequest.DebugPrintMessages();

                                            // 设置等待响应状态
                                            pendingLLMResponse = null; // 清空之前的响应

                                            // 发送请求
                                            llmRequestStartTime = Time.time;
                                            needReview = false;
                                            Tools.SendLLMRequest(currentRequest, (response) => {
                                                pendingLLMResponse = response;
                                            });
                                        }
                                    };
                                    regenButton.onClick.AddListener(clickAction);
                                }
                            }
                        }
                    }
                }
            }
            */
            if (openUIEnd.uiType.uiName == UIType.NPCInfo.uiName)
            {
                UINPCInfo ui = g.ui.GetUI<UINPCInfo>(UIType.NPCInfo);
                if (ui != null)
                {
                    bool isPlayer = ui.unit.data.unitData.unitID.Equals(g.world.playerUnit.data.unitData.unitID);
                    if (isPlayer)
                    {
                        return; // 如果是玩家自己，直接返回
                    }

                    // 创建奇遇按钮
                    GameObject npcPrefab = g.res.Load<GameObject>("ui/npcbutton/npcbutton");
                    if (npcPrefab != null)
                    {
                        GameObject npcButtonObj = UnityEngine.Object.Instantiate(npcPrefab);
                       
                        npcButtonObj.name = "RandomEventButton";

                        // 获取按钮内部Canvas并设置位置
                        Transform npcCanvasTransform = npcButtonObj.transform.Find("Canvas");
                        if (npcCanvasTransform != null)
                        {
                            Canvas npcCanvas = npcCanvasTransform.GetComponent<Canvas>();
                            if (npcCanvas != null)
                            {
                                npcCanvasTransform.SetParent(ui.transform, false);
                                npcCanvas.overrideSorting = false;
                                //npcCanvas.sortingOrder = 100;

                                // 设置内部Canvas的位置 - 这是关键
                                RectTransform canvasRectTrans = npcCanvasTransform.GetComponent<RectTransform>();
                                canvasRectTrans.anchorMin = new Vector2(0.53f, 0.5f);
                                canvasRectTrans.anchorMax = new Vector2(0.53f, 0.5f);
                                canvasRectTrans.pivot = new Vector2(1, 1);
                                //canvasRectTrans.anchoredPosition = new Vector2(-70, -20);
                            }

                            UnityEngine.UI.Button npcButton = npcCanvasTransform.GetComponentInChildren<UnityEngine.UI.Button>();
                            if (npcButton != null)
                            {
                                // 修改按钮文本
                                Text buttonText = npcButton.GetComponentInChildren<Text>();
                                if (buttonText != null)
                                {
                                    buttonText.text = "奇遇";
                                }

                                // 设置按钮事件
                                npcButton.onClick.RemoveAllListeners();
                                npcButton.onClick.RemoveAllListeners();
                                Action clickAction = delegate () {
                                    string content = g.data.dataObj.data.GetString("LLMcontent");
                                    if (waitingForLLMResponse || inOngoingDialog || !string.IsNullOrEmpty(content))
                                    {
                                        UITipItem.AddTip("已有正在进行的奇遇！", 1f);
                                        return;
                                    }

                                    if (string.IsNullOrEmpty(content))
                                    {
                                        WorldUnitBase randomNpc = ui.unit;
                                        if (randomNpc == null)
                                        {
                                            UITipItem.AddTip("附近没有发现可对话的NPC", 1f);
                                            return;
                                        }

                                        // 复用OnPlayerMove的逻辑来抽取事件
                                        var npcUnitID = randomNpc.data.unitData.unitID;
                                        var playerRelation = g.world.playerUnit.data.unitData.relationData;

                                        List<string> allAvailableEvents = new List<string>();
                                        bool hasSpecialRelation = false;

                                        // 先检查特殊关系
                                        // 检查道侣/配偶关系
                                        bool isLoverSpouse = playerRelation.lover.Contains(npcUnitID) || playerRelation.married == npcUnitID.ToString();
                                        if (isLoverSpouse)
                                        {
                                            var loverEvents = Tools.GetLoverSpouseEventTypes();
                                            allAvailableEvents.AddRange(loverEvents);
                                            hasSpecialRelation = true;
                                        }

                                        // 检查父母子女关系
                                        bool isParentChild = playerRelation.parent.Contains(npcUnitID) || playerRelation.children.Contains(npcUnitID) || playerRelation.parentBack.Contains(npcUnitID) || playerRelation.childrenBack.Contains(npcUnitID) || playerRelation.childrenPrivate.Contains(npcUnitID);
                                        if (isParentChild)
                                        {
                                            var parentEvents = Tools.GetParentChildEventTypes();
                                            allAvailableEvents.AddRange(parentEvents);
                                            hasSpecialRelation = true;
                                        }

                                        // 检查师徒关系
                                        bool isMasterStudent = playerRelation.master.Contains(npcUnitID) || playerRelation.student.Contains(npcUnitID);
                                        if (isMasterStudent)
                                        {
                                            var masterEvents = Tools.GetMasterStudentEventTypes();
                                            allAvailableEvents.AddRange(masterEvents);
                                            hasSpecialRelation = true;
                                        }

                                        // 如果没有特殊关系，才使用通用和异性关系
                                        if (!hasSpecialRelation)
                                        {
                                            // 通用事件
                                            var generalEvents = Tools.GetGeneralEventTypes();
                                            allAvailableEvents.AddRange(generalEvents);

                                            // 检查异性关系
                                            string playerGender = ((int)g.world.playerUnit.data.unitData.propertyData.sex == 1) ? "男" : "女";
                                            string npcGender = ((int)randomNpc.data.unitData.propertyData.sex == 1) ? "男" : "女";
                                            bool isOppositeGender = playerGender != npcGender;
                                            if (isOppositeGender)
                                            {
                                                var oppositeEvents = Tools.GetOppositeGenderEventTypes();
                                                allAvailableEvents.AddRange(oppositeEvents);
                                            }
                                        }

                                        // 随机选择一个事件
                                        System.Random random = new System.Random();
                                        string selectedEvent = allAvailableEvents[random.Next(allAvailableEvents.Count)];

                                        string npcName = randomNpc.data.unitData.propertyData.GetName();
                                        dialogueNpcs.Add(randomNpc);
                                        waitingForLLMResponse = true;
                                        hasTriggeredBattleInCurrentAdventure = false;
                                        isFirstDialog = true;
                                        currentRequest = new LLMDialogueRequest();
                                        currentRequest.AddSystemMessage($"{Tools.GenerateRandomSystemPrompt(randomNpc, selectedEvent)}你是{npcName}。");
                                        currentRequest.AddSystemMessage($"{Tools.GenerateformatSystemPrompt()}");
                                        currentRequest.AddUserMessage("请给我第一段随机奇遇剧情，必须使用格式化返回。");
                                        UITipItem.AddTip("开始准备奇遇~", 1f);
                                        llmRequestStartTime = Time.time;
                                        Tools.SendLLMRequest(currentRequest, (response) => {
                                            pendingLLMResponse = response;
                                        });
                                    }
                                };
                                npcButton.onClick.AddListener(clickAction);
                                npcButton1 = npcButtonObj;
                            }
                        }
                    }

                    // 创建配置按钮
                    GameObject configPrefab = g.res.Load<GameObject>("ui/configbutton/configbutton");
                    if (configPrefab != null)
                    {
                        GameObject configButtonObj = UnityEngine.Object.Instantiate(configPrefab);
                        configButtonObj.name = "ConfigButton";

                        // 获取按钮内部Canvas并设置位置
                        Transform configCanvasTransform = configButtonObj.transform.Find("Canvas");
                        if (configCanvasTransform != null)
                        {
                            Canvas configCanvas = configCanvasTransform.GetComponent<Canvas>();
                            if (configCanvas != null)
                            {
                                configCanvasTransform.SetParent(ui.transform, false);
                                configCanvas.overrideSorting = false;
                                //configCanvas.sortingOrder = 100;

                                // 设置内部Canvas的位置 - 这是关键
                                RectTransform canvasRectTrans = configCanvasTransform.GetComponent<RectTransform>();
                                canvasRectTrans.anchorMin = new Vector2(0.48f, 0.5f);
                                canvasRectTrans.anchorMax = new Vector2(0.48f, 0.5f);
                                canvasRectTrans.pivot = new Vector2(1, 1);
                                //canvasRectTrans.anchoredPosition = new Vector2(-170, -20);
                            }

                            UnityEngine.UI.Button configButton = configCanvasTransform.GetComponentInChildren<UnityEngine.UI.Button>();
                            if (configButton != null)
                            {
                                // 修改按钮文本
                                Text buttonText = configButton.GetComponentInChildren<Text>();
                                if (buttonText != null)
                                {
                                    buttonText.text = "配置";
                                }

                                // 设置按钮事件
                                configButton.onClick.RemoveAllListeners();
                                Action configAction = delegate () {
                                    UIConfig.OpenConfigUI(
                                        ModMain.apiUrl,
                                        ModMain.apiKey,
                                        ModMain.modelName,
                                        (url, key, model) => {
                                            UITipItem.AddTip("配置已更新", 1f);
                                        }
                                    );
                                };
                                configButton.onClick.AddListener(configAction);
                                configButton1 = configButtonObj;
                            }
                        }
                    }
                }
            }
        }

        public void OnPlayerMove(ETypeData data)
        {
            int testRingId = 8888889;
            if (g.world.playerUnit.data.unitData.propData.GetPropsNum(testRingId) == 0)
            {
                // 1. 定义：白板戒指 + 背包扩容词条
                var baseInfo = new CreationBaseInfo
                {
                    Name = "虚空之戒(修复版)",
                    Grade = 4,
                    Description = "修复了注册逻辑，应该能看到名字和属性了。",
                    IconCategory = "Item_Ring"
                };

                // 2. 词条：增加50格背包
                string effects = "atk_1_10|storage_1_500";

                // 3. 生成
                CreationSystem.CreateRing(baseInfo, effects, new CreationExtraInfo { Worth = 1, RealmReq = 1 }, false, testRingId);

                // 4. 发放
                var rewardList = new Il2CppSystem.Collections.Generic.List<DataProps.PropsData>();
                rewardList.Add(DataProps.PropsData.NewProps(testRingId, 1));
                g.world.playerUnit.data.RewardPropItem(rewardList);

                UITipItem.AddTip("测试：已发放虚空之戒，请检查属性", 4f);
            }

            if (Time.time - lastMoveTime < 1f)
            {
                return; // 静默忽略，不显示提示以避免频繁弹窗
            }
            lastMoveTime = Time.time;

            if (isInBattle)
            {
                isInBattle = false;
            }

            UIDramaDialogue dramaUI = g.ui.GetUI<UIDramaDialogue>(UIType.DramaDialogue);

            if (needReview && dialogueNpcs.Count > 0 && dramaUI == null)
            {
                // 需要重新展示对话，与已有对话继续
                inOngoingDialog = true;
                WorldUnitBase randomNpc = dialogueNpcs[0];

                // 获取当前存储的对话内容
                string content1 = g.data.dataObj.data.GetString("LLMcontent");
                if (!string.IsNullOrEmpty(content1))
                {
                    // 解析存储的内容
                    string dialogText;
                    FormattedResponse formattedResponse = Tools.ParseLLMResponse(content1, out dialogText);

                    // 使用 Tools 中的函数生成选项
                    var optionsList = Tools.GenerateOptionsFromResponse(formattedResponse, currentRequest);
                    var (options, callbacks) = GenerateDialogueOptions(optionsList, randomNpc);
                    //Tools.CreateDialogue(npcTalk, dialogText, g.world.playerUnit, randomNpc, options, callbacks);
                    CreateMultiPartDialogue(npcTalk, dialogText, g.world.playerUnit, randomNpc, options, callbacks);

                    needReview = true;
                }
                return;
            }

            if (waitingForLLMResponse || inOngoingDialog || isCreatingItems)
            {
                return;
            }

            string shortEventContent = g.data.dataObj.data.GetString("ShortEventContent");
            if (!string.IsNullOrEmpty(shortEventContent))
            {
                // 有短奇遇内容，直接触发
                ShortEvent.TriggerShortEvent();
                return;
            }


            if (string.IsNullOrEmpty(g.data.dataObj.data.GetString("LLMcontent")))
            {
                float randomValue = UnityEngine.Random.Range(0f, 1f);
                float shortEventProb = shortEventProbability;
                float totalProb = encounterProbability + shortEventProb;

                if (randomValue > totalProb)
                {
                    Debug.Log("skip");
                    return; // 都不触发
                }

                if (randomValue <= shortEventProb)
                {
                    // 触发短奇遇
                    if (!waitingForShortEventResponse)
                    {
                        ShortEvent.RequestShortEvent();
                    }
                    return;
                }
            }
            // 检查是否已有存储的LLM内容
            string content = g.data.dataObj.data.GetString("LLMcontent");
            if (string.IsNullOrEmpty(content))
            {
                // 使用新的整合函数，一次性完成事件抽取和NPC选择
                var (randomNpc, selectedEvent) = Tools.GetRandomNpcWithEvent();
                if (randomNpc == null)
                {
                    UITipItem.AddTip("附近没有发现可对话的NPC", 1f);
                    return;
                }

                // 后续逻辑保持不变
                string npcName = randomNpc.data.unitData.propertyData.GetName();
                dialogueNpcs.Add(randomNpc);
                waitingForLLMResponse = true;
                hasTriggeredBattleInCurrentAdventure = false;
                isFirstDialog = true;
                currentRequest = new LLMDialogueRequest();
                currentRequest.AddSystemMessage($"{Tools.GenerateRandomSystemPrompt(randomNpc, selectedEvent)}你是{npcName}。");
                currentRequest.AddSystemMessage($"{Tools.GenerateformatSystemPrompt()}");
                currentRequest.AddUserMessage("请给我第一段随机奇遇剧情，必须使用格式化返回。");

                llmRequestStartTime = Time.time;
                Tools.SendLLMRequest(currentRequest, (response) => {
                    pendingLLMResponse = response;
                });
            }
            else
            {
                // 如果已有内容，直接创建对话
                isFirstDialog = false;
                inOngoingDialog = true;
                WorldUnitBase randomNpc = dialogueNpcs[0];

                // 尝试解析存储的内容，看是否为JSON格式
                string dialogText;
                FormattedResponse formattedResponse = Tools.ParseLLMResponse(content, out dialogText);

                // 防御性编程：检查currentRequest是否为null
                if (currentRequest == null)
                {
                    // 如果为null，创建新的请求对象
                    currentRequest = new LLMDialogueRequest();
                    string npcName = randomNpc.data.unitData.propertyData.GetName();
                    currentRequest.AddSystemMessage($"{Tools.GenerateRandomSystemPrompt(randomNpc)}你是{npcName}。");
                    currentRequest.AddSystemMessage($"{Tools.GenerateformatSystemPrompt()}");
                }

                // 添加assistant消息

                currentRequest.AddAssistantMessage(dialogText);

                // 使用Tools中的函数生成选项
                var optionsList = Tools.GenerateOptionsFromResponse(formattedResponse, currentRequest);
                var (options, callbacks) = GenerateDialogueOptions(optionsList, randomNpc);
                //Tools.CreateDialogue(npcTalk, dialogText, g.world.playerUnit, randomNpc, options, callbacks);
                CreateMultiPartDialogue(npcTalk, dialogText, g.world.playerUnit, randomNpc, options, callbacks);
                needReview = true;
            }
        }

        

        public void Destroy()
        {
            g.events.Off(EGameType.IntoWorld, (Il2CppSystem.Action<ETypeData>)OnIntoWorld);
            g.events.Off(EGameType.OpenUIEnd, (Il2CppSystem.Action<ETypeData>)OpenUIEnd);
            var moveAction = new UnitActionMovePlayer(Vector2Int.right);
            g.events.Off(EGameType.OneUnitCreateOneActionBack(g.world.playerUnit, moveAction.GetIl2CppType()),
                (Il2CppSystem.Action<ETypeData>)OnPlayerMove);

            if (corUpdate != null)
            {
                corUpdate.Stop();
                corUpdate = null;
            }

            dialogueNpcs.Clear();
            pendingLLMResponse = null;
            currentRequest = null;
        }

        public static void RunOnMainThread(Action action)
        {
            if (_instance == null) return;

            lock (_instance._queueLock)
            {
                _instance._mainThreadQueue.Enqueue(action);
            }
        }
        public static void AddMessageToChat(WorldUnitBase npc, string role, string content)
        {
            if (npc == null) return;

            try
            {
                // 获取unitID并转换为字符串

                // 创建消息对象
                MOD_SSCYAI.Message message = new MOD_SSCYAI.Message
                {
                    role = role,
                    content = content
                };

                // 直接添加到MOD_SSCYAI.ModMain.Messages字典
                if (!MOD_SSCYAI.ModMain.Messages.ContainsKey(npc.data.unitData.unitID))
                {
                    MOD_SSCYAI.ModMain.Messages[npc.data.unitData.unitID] = new List<MOD_SSCYAI.Message>();
                }
                MOD_SSCYAI.ModMain.Messages[npc.data.unitData.unitID].Add(message);

                string unitID = npc.data.unitData.unitID.ToString();

                // 添加到ActionMessages字典 (使用字符串类型的unitID作为键)
                if (!MOD_SSCYAI.ModMain.ActionMessages.ContainsKey(unitID))
                {
                    MOD_SSCYAI.ModMain.ActionMessages[unitID] = new List<MOD_SSCYAI.Message>();
                    
                }
                MOD_SSCYAI.ModMain.ActionMessages[unitID].Add(message);

            }
            catch (Exception ex)
            {
                Debug.Log($"添加消息到SSCYAI.Messages失败: {ex}");
            }
        }
        private (Dictionary<int, string>, Dictionary<int, Action>) GenerateDialogueOptions(
    List<object[]> optionsList, WorldUnitBase npc = null, int startId = 11451)
        {
            Dictionary<int, string> options = new Dictionary<int, string>();
            Dictionary<int, Action> callbacks = new Dictionary<int, Action>();

            foreach (var option in optionsList)
            {
                if (option.Length >= 2 && option[0] is string text && option[1] is int type)
                {
                    int optionId = startId++;
                    string optionText = text;

                    if (type == 5 && option.Length > 3 && option[3] is string reactValue)
                    {
                        int interactionType;
                        if (int.TryParse(reactValue, out interactionType))
                        {
                            string interactionDescription = Tools.GetInteractionDescription(interactionType);
                            if (interactionDescription != null)
                            {
                                // 有交互描述，检查是否有奖励来决定颜色
                                if (option.Length > 4 && option[4] is string rewards && !string.IsNullOrEmpty(rewards))
                                {
                                    optionText = $"{text}<color=#FFA500>（{interactionDescription}）</color>";
                                }
                                else
                                {
                                    optionText = $"{text}（{interactionDescription}）";
                                }
                            }
                            else
                            {
                                // 没有交互描述，但有奖励时添加（收下）
                                if (option.Length > 4 && option[4] is string rewards && !string.IsNullOrEmpty(rewards))
                                {
                                    optionText = $"{text}<color=#FFA500>（收下）</color>";
                                }
                            }
                        }
                    }

                    if (type == 4)
                    {
                        optionText = $"{text}（结束）";
                    }

                    // 使用可能修改后的选项文本
                    options[optionId] = optionText;
                    switch (type)
                    {
                        case 1: // 关闭对话
                            callbacks[optionId] = () => {
                                string content1 = g.data.dataObj.data.GetString("LLMcontent");
                                if (dialogueNpcs.Count > 0 && content1 != "")
                                {
                                    string dialogText;
                                    Tools.ParseLLMResponse(content1, out dialogText);
                                    AddMessageToChat(dialogueNpcs[0], "assistant", dialogText);
                                    dialogueNpcs[0].CreateAction(new UnitActionMoveNPC(g.world.playerUnit.data.unitData.GetPoint()));
                                }

                                hasTriggeredDiscussionInCurrentAdventure = false;
                                givenRewardsInCurrentAdventure.Clear();
                                hasTriggeredBattleInCurrentAdventure = false;
                                currentRequest = null;
                                dialogueNpcs.Clear();
                                inOngoingDialog = false;
                                pendingLLMResponse = null;
                                needReview = false;
                                g.data.dataObj.data.SetString("LLMcontent", "");

                            };
                            break;
                        case 2: // LLM对话
                            callbacks[optionId] = () => {
                                string content1 = g.data.dataObj.data.GetString("LLMcontent");
                                needReview = false;
                                if (option.Length > 2 && option[2] is LLMDialogueRequest request)
                                {
                                    if (dialogueNpcs.Count > 0 && content1 != "")
                                    {
                                        string dialogText;
                                        Tools.ParseLLMResponse(content1, out dialogText);
                                        AddMessageToChat(dialogueNpcs[0], "assistant", dialogText);

                                        // 再添加用户选择的选项作为user消息
                                        if (option[0] is string selectedOption)
                                        {
                                            // 处理选项文本，移除格式化指令
                                            string cleanOption = selectedOption;
                                            if (cleanOption.Contains("\n（必须返回格式化结果！）"))
                                            {
                                                cleanOption = cleanOption.Replace("\n（必须返回格式化结果！）", "");
                                            }

                                            // 使用清理后的文本添加到聊天记录
                                            AddMessageToChat(dialogueNpcs[0], "user", cleanOption);
                                        }
                                        dialogueNpcs[0].CreateAction(new UnitActionMoveNPC(g.world.playerUnit.data.unitData.GetPoint()));
                                    }

                                    currentRequest = request;
                                    UITipItem.AddTip("对方思考如何反应中~", 1f);
                                    needReview = false;
                                    llmRequestStartTime = Time.time;
                                    Tools.SendLLMRequest(request, (response) => {
                                        pendingLLMResponse = response;
                                    });
                                }
                            };
                            break;
                        case 3: // 输入框UI，并将输入内容发送给LLM
                            callbacks[optionId] = () => {
                                string content1 = g.data.dataObj.data.GetString("LLMcontent");
                                // 定义输入框回调
                                System.Action<string> callback = (input) => {
                                    //Debug.Log($"玩家输入: {input}");
                                    needReview = false; // 重置状态，表示已正常提交输入
                                    if (dialogueNpcs.Count > 0 && content1 != "")
                                    {
                                        string dialogText;
                                        Tools.ParseLLMResponse(content1, out dialogText);
                                        AddMessageToChat(dialogueNpcs[0], "assistant", dialogText);
                                        AddMessageToChat(dialogueNpcs[0], "user", input);
                                        dialogueNpcs[0].CreateAction(new UnitActionMoveNPC(g.world.playerUnit.data.unitData.GetPoint()));
                                    }
                                    // 如果 currentRequest 不存在（初次对话），初始化一个
                                    if (currentRequest == null)
                                    {
                                        currentRequest = new LLMDialogueRequest();
                                        currentRequest.AddSystemMessage(Tools.GenerateRandomSystemPrompt()); // 默认系统提示
                                        currentRequest.AddSystemMessage($"{Tools.GenerateformatSystemPrompt()}");
                                    }

                                    // 将玩家输入作为 user 消息添加到请求
                                    
                                    currentRequest.RemoveLatestMessageByRole("system");
                                    currentRequest.AddSystemMessage($"{Tools.GenerateformatSystemPrompt()}");
                                    currentRequest.AddUserMessage($"{input}\n（必须返回格式化结果！）");


                                    UITipItem.AddTip("对方思考如何反应中~", 1f);
                                    // 发送给LLM
                                    llmRequestStartTime = Time.time;
                                    Tools.SendLLMRequest(currentRequest, (response) => {
                                        pendingLLMResponse = response;
                                        // 其他处理逻辑
                                    });
                                };

                                // 打开输入框
                                TextInputUICtrl.OpenTextInputUI(
                                     "请输入你的自定义回答",  // 占位符文本
                                     "",                    // 默认为空文本
                                     callback               // 回调函数保持不变
                                 );
                                needReview = false; // 设置状态，表示需要重新查看对话
                            };
                            break;
                        case 5: // 交互选项
                            callbacks[optionId] = () => {
                                string content1 = g.data.dataObj.data.GetString("LLMcontent");
                                needReview = false;

                                // 声明变量
                                string reactionValue = "";
                                string rewardsString = null;
                                if (option.Length > 3 && option[3] is string react)
                                {
                                    reactionValue = react;
                                }

                                // 检查是否有奖励并发放（无论reactionValue是什么）
                                if (option.Length > 4 && option[4] is string rewards && !string.IsNullOrEmpty(rewards))
                                {
                                    rewardsString = rewards;
                                    Tools.GiveRewardSingle(rewardsString);
                                }

                                if (dialogueNpcs.Count > 0 && content1 != "")
                                {
                                    string dialogText;
                                    Tools.ParseLLMResponse(content1, out dialogText);
                                    AddMessageToChat(dialogueNpcs[0], "assistant", dialogText);

                                    // 再添加用户选择的选项作为user消息
                                    if (option[0] is string selectedOption)
                                    {
                                        string cleanOption = selectedOption;
                                        if (cleanOption.Contains("\n（必须返回格式化结果！）"))
                                        {
                                            cleanOption = cleanOption.Replace("\n（必须返回格式化结果！）", "");
                                        }
                                        AddMessageToChat(dialogueNpcs[0], "user", cleanOption);
                                    }
                                    dialogueNpcs[0].CreateAction(new UnitActionMoveNPC(g.world.playerUnit.data.unitData.GetPoint()));
                                }

                                // 判断是否是结束交互(ID=14)
                                if (reactionValue == "14")
                                {
                                    hasTriggeredDiscussionInCurrentAdventure = false;
                                    givenRewardsInCurrentAdventure.Clear();
                                    currentRequest = null;
                                    dialogueNpcs.Clear();
                                    inOngoingDialog = false;
                                    pendingLLMResponse = null;
                                    needReview = false;
                                    g.data.dataObj.data.SetString("LLMcontent", "");
                                    hasTriggeredBattleInCurrentAdventure = false;
                                    return; // 退出回调
                                }

                                // 如果已经发放了奖励，且不是其他特殊交互，则继续对话流程
                                if (!string.IsNullOrEmpty(rewardsString) && reactionValue != "15" && reactionValue != "16")
                                {
                                    if (option.Length > 2 && option[2] is LLMDialogueRequest)
                                    {
                                        var rewardRequest = (LLMDialogueRequest)option[2];
                                        currentRequest = rewardRequest;
                                        UITipItem.AddTip("对方思考如何反应中~", 1f);
                                        llmRequestStartTime = Time.time;
                                        Tools.SendLLMRequest(rewardRequest, (response) => {
                                            pendingLLMResponse = response;
                                        });
                                    }
                                    return;
                                }

                                // 原有的特殊交互逻辑继续...
                                if (reactionValue == "15" || reactionValue == "16")
                                {
                                    // 执行战斗交互，但不发送LLM请求
                                    if (dialogueNpcs.Count > 0)
                                    {
                                        Tools.ExecuteInteraction(dialogueNpcs[0], reactionValue);
                                    }
                                    // 保存当前请求对象，战斗结束后使用
                                    if (option.Length > 2 && option[2] is LLMDialogueRequest battleRequest)
                                    {
                                        currentRequest = battleRequest; // 保存到现有变量
                                    }
                                    return; // 重要：直接返回，不发送LLM请求
                                }

                                // 获取对话请求对象
                                if (option.Length > 2 && option[2] is LLMDialogueRequest request)
                                {
                                    // 原有的交互逻辑保持不变...

                                    // 执行交互功能
                                    if (dialogueNpcs.Count > 0 && !string.IsNullOrEmpty(reactionValue))
                                    {
                                        Tools.ExecuteInteraction(dialogueNpcs[0], reactionValue);
                                    }
                                    currentRequest = request;
                                    UITipItem.AddTip("对方思考如何反应中~", 1f);
                                    // 发送请求给LLM
                                    llmRequestStartTime = Time.time;
                                    Tools.SendLLMRequest(request, (response) => {
                                        pendingLLMResponse = response;
                                    });
                                }
                            };
                            break;
                    }
                }
            }

            int regenId = startId + 1000; // 使用一个不冲突的ID
            options[regenId] = "重新生成";
            callbacks[regenId] = () => {
                UITipItem.AddTip("重新生成回应中...", 1f);

                // 不需要手动关闭UI，不需要AddMessageToChat

                if (currentRequest != null)
                {
                    // 移除最后的assistant回应，准备重新生成
                    currentRequest.RemoveLatestMessageByRole("assistant");

                    // 清空之前的响应，重新请求
                    pendingLLMResponse = null;
                    llmRequestStartTime = Time.time;
                    needReview = false;

                    Tools.SendLLMRequest(currentRequest, (response) => {
                        pendingLLMResponse = response;
                    });
                }
                else
                {
                    UITipItem.AddTip("无法重新生成，对话上下文丢失", 1f);
                }
            };

            return (options, callbacks);
        }

        // 添加到ModMain类中的文本划分方法
        private static List<string> SplitDialogText(string dialogText, int maxLength = 100)
        {
            List<string> textBlocks = new List<string>();

            if (string.IsNullOrEmpty(dialogText))
            {
                textBlocks.Add("");
                return textBlocks;
            }

            // 如果文本长度小于等于最大长度，直接返回
            if (dialogText.Length <= maxLength)
            {
                textBlocks.Add(dialogText);
                return textBlocks;
            }

            // 强断点：句号、感叹号、问号、省略号、后双引号
            char[] strongBreakers = { '。', '！', '？', '…', '"', '.', '!', '?' };

            string remainingText = dialogText;

            // 检查换行符数量，如果超过5个则按换行符切分
            int newlineCount = 0;
            for (int i = 0; i < dialogText.Length; i++)
            {
                if (dialogText[i] == '\n')
                    newlineCount++;
                else if (dialogText[i] == '\r' && (i + 1 >= dialogText.Length || dialogText[i + 1] != '\n'))
                    newlineCount++;
                
            }

            if (newlineCount > 5)
            {
                int currentNewlines = 0;
                int lastSplitIndex = 0;

                for (int i = 0; i < dialogText.Length; i++)
                {
                    bool isNewline = false;
                    if (dialogText[i] == '\n')
                        isNewline = true;
                    else if (dialogText[i] == '\r' && (i + 1 >= dialogText.Length || dialogText[i + 1] != '\n'))
                        isNewline = true;

                    if (isNewline)
                    {
                        currentNewlines++;
                        if (currentNewlines >= 5)
                        {
                            string block = dialogText.Substring(lastSplitIndex, i - lastSplitIndex + 1).Trim();
                            if (!string.IsNullOrEmpty(block))
                                textBlocks.Add(block);
                            lastSplitIndex = i + 1;
                            currentNewlines = 0;
                        }
                    }
                }

                // 添加剩余部分
                if (lastSplitIndex < dialogText.Length)
                {
                    string remainingBlock = dialogText.Substring(lastSplitIndex).Trim();
                    if (!string.IsNullOrEmpty(remainingBlock))
                        textBlocks.Add(remainingBlock);
                }
                return textBlocks;
            }

            while (remainingText.Length > 0)
            {
                if (remainingText.Length <= maxLength)
                {
                    // 剩余文本长度不超过最大长度，直接添加
                    textBlocks.Add(remainingText);
                    break;
                }

                int bestSplitPoint = -1;

                // 第一优先级：在maxLength范围内寻找强断点
                for (int i = Math.Min(maxLength, remainingText.Length) - 1; i >= 0; i--)
                {
                    if (Array.IndexOf(strongBreakers, remainingText[i]) >= 0 && !IsInsideQuotes(remainingText, i))
                    {
                        bestSplitPoint = i + 1; // 包含标点符号
                        break;
                    }
                }

                // 第二优先级：如果maxLength范围内没有强断点，扩展搜索范围
                if (bestSplitPoint == -1)
                {
                    // 向后搜索，最多搜索到maxLength*1.5的位置，寻找强断点
                    int extendedSearchLimit = Math.Min((int)(maxLength * 1.5), remainingText.Length);
                    for (int i = maxLength; i < extendedSearchLimit; i++)
                    {
                        if (Array.IndexOf(strongBreakers, remainingText[i]) >= 0 && !IsInsideQuotes(remainingText, i))
                        {
                            bestSplitPoint = i + 1;
                            break;
                        }
                    }
                }

                // 第三优先级：如果还是没找到，在maxLength处寻找空格
                if (bestSplitPoint == -1)
                {
                    for (int i = Math.Min(maxLength, remainingText.Length) - 1; i >= maxLength - 20; i--)
                    {
                        if (remainingText[i] == ' ' && !IsInsideQuotes(remainingText, i))
                        {
                            bestSplitPoint = i + 1;
                            break;
                        }
                    }
                }

                // 最后选择：如果实在没有合适断点，才在maxLength处强制断开
                if (bestSplitPoint == -1)
                {
                    bestSplitPoint = Math.Min(maxLength, remainingText.Length);
                }

                // 提取当前块并加入列表
                string currentBlock = remainingText.Substring(0, bestSplitPoint).Trim();
                if (!string.IsNullOrEmpty(currentBlock))
                {
                    textBlocks.Add(currentBlock);
                }

                // 更新剩余文本
                remainingText = remainingText.Substring(bestSplitPoint).Trim();
            }

            return textBlocks;
        }

        // 辅助方法：检查指定位置是否在引号或配对符号内部
        private static bool IsInsideQuotes(string text, int position)
        {
            // 只检查最常用的引号，避免特殊字符问题
            bool insideDoubleQuote = false;
            bool insideSingleQuote = false;
            bool insideParentheses = false;
            bool insideBrackets = false;

            for (int j = 0; j < position && j < text.Length; j++)
            {
                char c = text[j];

                // 检查双引号
                if (c == '"' || c == '\u201C' || c == '\u201D') // " " "
                {
                    insideDoubleQuote = !insideDoubleQuote;
                }
                // 检查单引号  
                else if (c == '\'' || c == '\u2018' || c == '\u2019') // ' ' '
                {
                    insideSingleQuote = !insideSingleQuote;
                }
                // 检查括号
                else if (c == '(')
                {
                    insideParentheses = true;
                }
                else if (c == ')')
                {
                    insideParentheses = false;
                }
                // 检查方括号
                else if (c == '[')
                {
                    insideBrackets = true;
                }
                else if (c == ']')
                {
                    insideBrackets = false;
                }
            }

            return insideDoubleQuote || insideSingleQuote || insideParentheses || insideBrackets;
        }


        // 辅助方法：创建多段对话
        public static void CreateMultiPartDialogue(int dialogType, string dialogText, WorldUnitBase leftUnit, WorldUnitBase rightUnit, Dictionary<int, string> options, Dictionary<int, Action> callbacks)
        {
            List<string> textBlocks = SplitDialogText(dialogText, 250);


            // 如果只有一个文本块，直接使用原来的逻辑
            if (textBlocks.Count <= 1)
            {
                Debug.Log("只有一个文本块，使用原始逻辑");
                Tools.CreateDialogue(dialogType, dialogText, leftUnit, rightUnit, options, callbacks);
                return;
            }
            Debug.Log($"准备进入for循环，textBlocks.Count = {textBlocks.Count}");
            Debug.Log($"for循环条件：i < {textBlocks.Count - 1}");

            // 创建前面的对话（无按钮）
            for (int i = 0; i < textBlocks.Count - 1; i++)
            {
                Debug.Log($"进入for循环，i = {i}");
                // 为前面的对话块创建"继续"按钮
                var continueOptions = new Dictionary<int, string> { { 12999, "继续" } };
                var continueCallbacks = new Dictionary<int, Action> {
            { 12999, () => { } }
        };
                Tools.CreateDialogue(dialogType, textBlocks[i], leftUnit, rightUnit, continueOptions, continueCallbacks);
            }

            // 创建最后一个对话（有按钮）
            Debug.Log($"for循环结束，准备创建最后一个对话框");
            Debug.Log($"创建最后一个对话框，选项数量: {options?.Count ?? 0}");
            Debug.Log($"创建最后一个对话框，文本: {textBlocks[textBlocks.Count - 1]}");
            Tools.CreateDialogue(dialogType, textBlocks[textBlocks.Count - 1], leftUnit, rightUnit, options, callbacks);
        }

        private void OnUpdate()
        {
            lock (_queueLock)
            {
                while (_mainThreadQueue.Count > 0)
                {
                    var action = _mainThreadQueue.Dequeue();
                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"主线程操作失败: {ex}");
                    }
                }
            }

            if (isInBattle)
            {
                return;
            }

            if (Battle.HasPendingBattleStart())
            {
                Battle.ProcessPendingBattleStart();
            }

            if (Battle.HasPendingBattleDescription())
            {
                Battle.ProcessPendingDescription();
            }

            if (BattlePVE.HasPendingBattleStart())
            {
                BattlePVE.ProcessPendingBattleStart();
            }

            if (BattlePVE.HasPendingBattleDescription())
            {
                BattlePVE.ProcessPendingDescription();
            }

            if (llmRequestStartTime > 0f && Time.time - llmRequestStartTime > 300f)
            {
                // 超时处理 - 重置状态
                UITipItem.AddTip("请求超时，将重置奇遇状态", 1f);
                waitingForLLMResponse = false;
                inOngoingDialog = false;
                needReview = false;
                llmRequestStartTime = 0f;
                pendingLLMResponse = null;
                currentRequest = null;
                dialogueNpcs.Clear();
                waitingForShortEventResponse = false;
                pendingShortEventResponse = null;
                g.data.dataObj.data.SetString("LLMcontent", "");
                g.data.dataObj.data.SetString("ShortEventContent", "");
                return;
            }

            if (pendingShortEventResponse != null)
            {
                llmRequestStartTime = 0f;

                if (pendingShortEventResponse.StartsWith("错误："))
                {
                    UITipItem.AddTip($"短奇遇生成失败：{pendingShortEventResponse.Substring(3)}", 2f);
                    waitingForShortEventResponse = false;
                    pendingShortEventResponse = null;
                }
                else
                {
                    // 存储短奇遇内容，等待下次移动触发
                    g.data.dataObj.data.SetString("ShortEventContent", pendingShortEventResponse);
                    waitingForShortEventResponse = false;
                    pendingShortEventResponse = null;
                }
            }

            // 处理LLM响应
            if (pendingLLMResponse != null)
            {
                llmRequestStartTime = 0f;
                string dialogText;
                FormattedResponse formattedResponse = null;

                if (pendingLLMResponse.StartsWith("错误："))
                {
                    dialogText = $"无法获取回应：{pendingLLMResponse.Substring(3)}";
                }
                else
                {
                    // 使用工具方法解析响应
                    formattedResponse = Tools.ParseLLMResponse(pendingLLMResponse, out dialogText);
                }
                g.data.dataObj.data.SetString("LLMcontent", pendingLLMResponse); // 存储原始响应
                if (isFirstDialog)
                {
                    // 第一次对话，存储LLM响应
                    waitingForLLMResponse = false;
                    isFirstDialog = false;
                    UITipItem.AddTip("收到仙人指点，下次移动将触发对话", 1f);

                    pendingLLMResponse = null;
                    return;
                }

                // 非第一次对话，继续原有逻辑
                var continueRequest = new LLMDialogueRequest();
                if (currentRequest != null)
                {
                    foreach (var msg in currentRequest.Messages)
                    {
                        continueRequest.Messages.Add(new MessageItem(msg.Role, msg.Content));
                    }

                    // 添加当前回答到对话历史
                    string processedJsonResponse;
                    Tools.ParseLLMResponse(pendingLLMResponse, out _); 
                    processedJsonResponse = Tools.GetProcessedJsonString(pendingLLMResponse);
                    continueRequest.AddAssistantMessage(processedJsonResponse);
                    Debug.Log($"{processedJsonResponse}");
                }
                else
                {
                    // 当currentRequest为null时，创建一个新的请求
                    continueRequest.AddSystemMessage(Tools.GenerateRandomSystemPrompt());
                    currentRequest.AddSystemMessage($"{Tools.GenerateformatSystemPrompt()}");
                    string processedJsonResponse;
                    Tools.ParseLLMResponse(pendingLLMResponse, out _);
                    processedJsonResponse = Tools.GetProcessedJsonString(pendingLLMResponse);
                    continueRequest.AddAssistantMessage(processedJsonResponse);
                }

                // 保存当前请求，用于继续对话
                currentRequest = continueRequest;

                // 使用工具方法生成选项列表
                var optionsList = Tools.GenerateOptionsFromResponse(formattedResponse, continueRequest);

                if (isCreatingItems) return;

                var (options, callbacks) = GenerateDialogueOptions(optionsList);
                WorldUnitBase rightUnit = dialogueNpcs.Count > 0 ? dialogueNpcs[0] : null;

                if (rightUnit != null)
                    //Tools.CreateDialogue(npcTalk, dialogText, g.world.playerUnit, rightUnit, options, callbacks);
                    CreateMultiPartDialogue(npcTalk, dialogText, g.world.playerUnit, rightUnit, options, callbacks);
                else
                    //Tools.CreateDialogue(onlyTalk, dialogText, g.world.playerUnit, null, options, callbacks);
                    CreateMultiPartDialogue(onlyTalk, dialogText, g.world.playerUnit, null, options, callbacks);
                needReview = true;
                pendingLLMResponse = null;
            }
        }
    }
}