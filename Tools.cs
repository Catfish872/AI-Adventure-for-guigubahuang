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
using static UINPCInfoBase;
using System.Web.UI.WebControls;


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
        public static Il2CppSystem.Collections.Generic.List<DataProps.PropsData> savedRewardItems = null;
        private static int taskID = 988567713;

        private static List<string> InitializeEventTypes(Func<ModConfig, List<string>> getConfigList, List<string> defaultList)
        {
            try
            {
                ModConfig config = Config.ReadConfig();
                if (config != null)
                {
                    var configList = getConfigList(config);
                    if (configList != null && configList.Count > 0)
                    {
                        return configList;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"读取配置文件失败，使用默认值: {ex.Message}");
            }
            return defaultList;
        }

        private static string InitializePromptString(Func<ModConfig, string> getConfigString, string defaultString)
        {
            try
            {
                ModConfig config = Config.ReadConfig();
                if (config != null)
                {
                    var configString = getConfigString(config);
                    if (!string.IsNullOrEmpty(configString))
                    {
                        return configString;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"读取配置文件失败，使用默认值: {ex.Message}");
            }
            return defaultString;
        }

        public static readonly List<string> HardcodedDefaultGeneralEventTypes = new List<string>
{
    "机缘巧合(仙药成熟/灵兽渡劫/灵根显现)",
            "秘境探索(古修洞府/灵药园/地下遗迹)",
            "修真大会(炼丹比试/剑术切磋/宗门招才)",
            "英雄救美(路遇截杀/恶霸欺凌/妖兽围攻)",
            "神兵认主(古剑择主/法器契合/丹炉共鸣)",
            "珍宝出世(灵石矿脉/仙草绽放/古书现世)",
            "奇遇际会(历练所得/隐世高人/前辈指点)",
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
        public static readonly List<string> HardcodedDefaultLoverSpouseEventTypes = new List<string>
{
    "伉俪情深(夫妻恩爱/道侣双修/情比金坚)",
     "相濡以沫(患难与共/相伴修行/共度难关)",
     "举案齐眉(夫妻和睦/相敬如宾/家庭和谐)",
     "双修奇遇(阴阳调和/合修功法/境界突破)",
     "道侣重逢(久别重逢/思君不见/重续前缘)",
     "夫妻齐心(合力破敌/同心协力/共进退)",
     "恩爱时光(温存片刻/柔情蜜意/甜蜜相处)",
     "携手探险(夫妻同行/道侣并肩/共闯险境)"
};
        public static readonly List<string> HardcodedDefaultOppositeGenderEventTypes = new List<string>
{
    "英雄救美(路遇截杀/恶霸欺凌/妖兽围攻)",
    "道侣奇遇(道侣结缘)，关注双方性别，若不是异性，则改为其他剧情，禁止同性结缘。",
    "合欢双修(道侣结缘/阴阳调和/双修共进)，关注双方性别，若不是异性，则改为其他剧情，禁止同性双修。",
    "情缘际会(一见钟情/暗生情愫/情丝萌动)",
    "红颜知己(心有灵犀/知音相遇/倾心相谈)",
    "共患难情(生死与共/患难见真情/危难中的依靠)",
    "月下邂逅(花前月下/浪漫相遇/温柔时光)",
    "才子佳人(诗词唱和/琴瑟和鸣/文采相配)"
};
        public static readonly List<string> HardcodedDefaultParentChildEventTypes = new List<string>
{
    "父慈子孝(家庭温馨/亲情深厚/长幼有序)",
    "传承之道(家族传承/父母教导/血脉延续)",
    "慈母严父(母爱如水/父爱如山/家庭教育)",
    "子女成才(后代有为/家族荣光/传承有人)",
    "骨肉团圆(家人重聚/失散重逢/血浓于水)",
    "家族责任(家族使命/血脉担当/族人期望)",
    "隔代情深(祖孙情深/长辈关爱/家族和睦)",
    "血脉觉醒(家族血脉/天赋传承/血统力量)"
};
        public static readonly List<string> HardcodedDefaultMasterStudentEventTypes = new List<string>
{
    "师徒传承(结为师徒：功法指点/心法印证/武技传授)",
    "师恩如山(尊师重道/师父恩情/传道授业)",
    "青出于蓝(弟子成才/后生可畏/学有所成)",
    "师徒切磋(指点迷津/武技交流/境界探讨)",
    "师门重聚(师徒相聚/同门相会/门下团圆)",
    "传道解惑(师父答疑/解开困惑/指引方向)",
    "师徒同行(师父带领/同门历练/共同修行)",
    "衣钵传承(绝学传授/秘法相传/师门衣钵)"
};


        public static List<string> DefaultGeneralEventTypes = InitializeEventTypes(
        config => config.EventTypes,
        HardcodedDefaultGeneralEventTypes
    );

        public static List<string> DefaultOppositeGenderEventTypes = InitializeEventTypes(
            config => config.OppositeGenderEventTypes,
            HardcodedDefaultOppositeGenderEventTypes
        );

        public static List<string> DefaultLoverSpouseEventTypes = InitializeEventTypes(
            config => config.LoverSpouseEventTypes,
            HardcodedDefaultLoverSpouseEventTypes
        );

        public static List<string> DefaultParentChildEventTypes = InitializeEventTypes(
            config => config.ParentChildEventTypes,
            HardcodedDefaultParentChildEventTypes
        );

        public static List<string> DefaultMasterStudentEventTypes = InitializeEventTypes(
            config => config.MasterStudentEventTypes,
            HardcodedDefaultMasterStudentEventTypes
        );

        public static readonly string HardcodedDefaultPromptPrefix = @"
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
            4. 将奖励自然地设计和融入剧情（如灵石、丹药、功法书籍等）
            5. 每段奇遇控制在3-5轮对话自然收尾，节奏感强";
        public static readonly string HardcodedDefaultPromptSuffix = @"
            【生成框架】
            第一轮：
            1. 鲜活的环境描写和NPC形象，让玩家能清晰感知场景
            2. 抛出明确但有多种可能的引子
            3. 暗示可能获得的机缘或奖励

            中间轮次：
            1. 每次对话推进剧情，可加入适度但不突兀的转折
            2. 保持适当的悬念感，但避免过于隐晦难解
            3. 描写NPC的动作和反应，保持互动感
            4. 可以适当将玩家选择与获得的奖励关联起来
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
            2. 以奖励、随机事件和引入的玩家与npc信息作为核心设计剧情，将奖励转化为剧情中所得
            3. 优先关注玩家与NPC之间的特殊关系（道侣、师徒、兄弟姐妹、父母子女、仇人好友等）以及个人设定，围绕这些为核心元素。(优先级：个人设定、对话记录、人物经历>人物关系>随机事件>人物性格）
            4. 对白要通俗易懂，符合修仙世界语境但不故弄玄虚
               
            -每次只需要输出一轮对话的信息，整体故事应该通过对话来推进.";

        public static string DefaultPromptPrefix = InitializePromptString(
        config => config.PromptPrefix,
        HardcodedDefaultPromptPrefix
    );


        public static string DefaultPromptSuffix = InitializePromptString(
        config => config.PromptSuffix,
        HardcodedDefaultPromptSuffix
    );



        [Serializable]
        public class FormattedResponse
        {
            public string content { get; set; }
            public string option1 { get; set; }
            public string react1 { get; set; }
            public string option2 { get; set; }
            public string react2 { get; set; }
            public string type { get; set; }
            public string reward1 { get; set; }  // 改为 reward1
            public string reward2 { get; set; }  // 新增 reward2

            // 默认构造函数
            public FormattedResponse()
            {
                content = "";
                option1 = "";
                react1 = "";
                option2 = "";
                react2 = "";
                type = "1";
                reward1 = "";  // 初始化
                reward2 = "";  // 初始化
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

            Debug.Log($"SendLLMRequest called");
            Debug.Log($"llmConnector is null: {llmConnector == null}");
            Debug.Log($"request is null: {request == null}");
            Debug.Log($"request.Messages is null: {request.Messages == null}");
            Debug.Log($"request.Messages count: {request.Messages?.Count}");


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
        public static (WorldUnitBase npc, string selectedEvent) GetRandomNpcWithEvent()
        {
            return HighPerformanceNpcSystem.GetRandomNpcWithEvent();
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
            string coloredDialogText = ModMain.autoColoringEnabled ? MoreTool.ApplyColorFormatting(dialogText) : dialogText;
            dramaDyn.dramaData.dialogueText[dialogId] = coloredDialogText;

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
                // 强制使用HTML解析，不再进行自动检测

                return ParseLLMResponseHTML(rawResponse, out parsedContent);
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
                // 强制使用HTML处理，不再进行自动检测
                return GetProcessedHTMLString(rawResponse);
            }
            catch (Exception ex)
            {
                Debug.Log($"处理响应字符串失败: {ex.Message}");
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

        private static HashSet<string> _validItemAndLuckNamesCache;

        private static void InitializeValidNamesCache()
        {
            if (_validItemAndLuckNamesCache != null) return;

            _validItemAndLuckNamesCache = new HashSet<string>();

            if (savedRewardItems != null)
            {
                foreach (var savedItem in savedRewardItems)
                {
                    if (savedItem.propsInfoBase != null && !string.IsNullOrEmpty(savedItem.propsInfoBase.name))
                    {
                        _validItemAndLuckNamesCache.Add(savedItem.propsInfoBase.name);
                    }
                }
            }

            // 1. 特殊项
            _validItemAndLuckNamesCache.Add("灵石");

            // 2. 从MOD内置的 Prop 字典加载
            foreach (var pair in Prop.propDict1)
            {
                _validItemAndLuckNamesCache.Add(GetOriginalItemName(pair.Value));
            }
            foreach (var pair in Prop.propDict2_10)
            {
                _validItemAndLuckNamesCache.Add(GetOriginalItemName(pair.Value));
            }

            Action<KeyValuePair<string, string>> addLuckName = (pair) => {
                string luckValue = pair.Value;
                int bracketIndex = luckValue.IndexOf('（');
                if (bracketIndex == -1) bracketIndex = luckValue.IndexOf('(');
                string luckNamePart = bracketIndex > 0 ? luckValue.Substring(0, bracketIndex) : luckValue;
                _validItemAndLuckNamesCache.Add(luckNamePart);
            };

            foreach (var pair in Prop.luckDict)
            {
                addLuckName(pair);
            }
            foreach (var pair in Prop.luckDict2)
            {
                addLuckName(pair);
            }

            // 3. 从游戏配置加载
            try
            {
                if (g.conf?.itemProps?._allConfList != null)
                {
                    foreach (var itemConf in g.conf.itemProps._allConfList)
                    {
                        _validItemAndLuckNamesCache.Add(GameTool.LS(itemConf.name));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"缓存游戏内物品配置失败: {ex.Message}");
            }

            try
            {
                if (g.conf?.roleCreateFeature?._allConfList != null)
                {
                    foreach (var featureConf in g.conf.roleCreateFeature._allConfList)
                    {
                        _validItemAndLuckNamesCache.Add(GameTool.LS(featureConf.name));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"缓存游戏内气运配置失败: {ex.Message}");
            }

            Debug.Log($"初始化有效奖励名称缓存，共计 {_validItemAndLuckNamesCache.Count} 项。");
        }

        public static bool RewardExists(string rewardName)
        {
            InitializeValidNamesCache();
            return _validItemAndLuckNamesCache.Contains(rewardName.Trim());
        }

        public static List<object[]> GenerateOptionsFromResponse(FormattedResponse formattedResponse, LLMDialogueRequest continueRequest = null)
        {
            var optionsList = new List<object[]>();

            if (formattedResponse != null)
            {
                // >>> 合并检测逻辑：一次性扫出所有未知物品触发造物 <<<
                var allRewards = new HashSet<string>();
                if (!string.IsNullOrEmpty(formattedResponse.reward1))
                    foreach (var r in formattedResponse.reward1.Split(',')) allRewards.Add(r.Trim());
                if (!string.IsNullOrEmpty(formattedResponse.reward2))
                    foreach (var r in formattedResponse.reward2.Split(',')) allRewards.Add(r.Trim());

                var unknown = allRewards
                    .Where(r => !string.IsNullOrEmpty(r))
                    .Where(r => !ModMain.givenRewardsInCurrentAdventure.Contains(r))
                    .Where(r => !RewardExists(r)) // 只要不存在就是未知
                    .ToList();

                if (unknown.Count > 0 && !ModMain.isCreatingItems)
                {
                    ModMain.isCreatingItems = true;
                    var msgs = continueRequest != null ? continueRequest.Messages : new List<MessageItem>();
                    CreationSystem.StartCreationProcess(unknown, msgs);
                }
                // >>>>>>>>>> logic end <<<<<<<<<<

                // 处理选项1
                if (!string.IsNullOrEmpty(formattedResponse.option1))
                {
                    var option1Request = new LLMDialogueRequest();
                    if (continueRequest != null) option1Request.Messages.AddRange(continueRequest.Messages); // 简化写法
                    option1Request.RemoveLatestMessageByRole("system");
                    option1Request.AddSystemMessage($"{Tools.GenerateformatSystemPrompt()}");
                    option1Request.AddUserMessage($"{formattedResponse.option1}\n（必须返回格式化结果！）");

                    string react1 = !string.IsNullOrEmpty(formattedResponse.react1) ? formattedResponse.react1 : "0";
                    if (react1 == "8" && ModMain.hasTriggeredDiscussionInCurrentAdventure) react1 = "0";

                    string reward1 = !string.IsNullOrEmpty(formattedResponse.reward1) ? formattedResponse.reward1 : "";
                    if (!string.IsNullOrEmpty(reward1))
                    {
                        // 仅保留【已存在】的物品用于显示
                        var validItems = reward1.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(r => r.Trim())
                            .Where(r => !ModMain.givenRewardsInCurrentAdventure.Contains(r))
                            .Where(r => RewardExists(r))
                            .ToList();

                        if (validItems.Count == 0)
                        {
                            reward1 = "";
                            if (react1 == "17") react1 = "0";
                        }
                        else
                        {
                            reward1 = string.Join(",", validItems);
                        }
                    }

                    int optionType = !string.IsNullOrEmpty(reward1) ? 5 : (react1 == "14" ? 5 : (react1 == "15" && ModMain.hasTriggeredBattleInCurrentAdventure ? 2 : (react1 != "0" ? 5 : 2)));
                    // 构造参数数组...
                    if (optionType == 5 && !string.IsNullOrEmpty(reward1))
                        optionsList.Add(new object[] { formattedResponse.option1, optionType, option1Request, react1, reward1 });
                    else if (optionType == 5)
                        optionsList.Add(new object[] { formattedResponse.option1, optionType, option1Request, react1 });
                    else
                        optionsList.Add(new object[] { formattedResponse.option1, optionType, option1Request });
                }

                // 处理选项2 (逻辑同上，仅变量名不同)
                if (!string.IsNullOrEmpty(formattedResponse.option2))
                {
                    var option2Request = new LLMDialogueRequest();
                    if (continueRequest != null) option2Request.Messages.AddRange(continueRequest.Messages);
                    option2Request.RemoveLatestMessageByRole("system");
                    option2Request.AddSystemMessage($"{Tools.GenerateformatSystemPrompt()}");
                    option2Request.AddUserMessage($"{formattedResponse.option2}\n（必须返回格式化结果！）");

                    string react2 = !string.IsNullOrEmpty(formattedResponse.react2) ? formattedResponse.react2 : "0";
                    if (react2 == "8" && ModMain.hasTriggeredDiscussionInCurrentAdventure) react2 = "0";

                    string reward2 = !string.IsNullOrEmpty(formattedResponse.reward2) ? formattedResponse.reward2 : "";
                    if (!string.IsNullOrEmpty(reward2))
                    {
                        var validItems = reward2.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(r => r.Trim())
                            .Where(r => !ModMain.givenRewardsInCurrentAdventure.Contains(r))
                            .Where(r => RewardExists(r))
                            .ToList();

                        if (validItems.Count == 0)
                        {
                            reward2 = "";
                            if (react2 == "17") react2 = "0";
                        }
                        else
                        {
                            reward2 = string.Join(",", validItems);
                        }
                    }

                    int optionType = !string.IsNullOrEmpty(reward2) ? 5 : (react2 == "14" ? 5 : (react2 == "15" && ModMain.hasTriggeredBattleInCurrentAdventure ? 2 : (react2 != "0" ? 5 : 2)));

                    if (optionType == 5 && !string.IsNullOrEmpty(reward2))
                        optionsList.Add(new object[] { formattedResponse.option2, optionType, option2Request, react2, reward2 });
                    else if (optionType == 5)
                        optionsList.Add(new object[] { formattedResponse.option2, optionType, option2Request, react2 });
                    else
                        optionsList.Add(new object[] { formattedResponse.option2, optionType, option2Request });
                }

                optionsList.Add(new object[] { "点击输入自定义回答", 3 });
            }
            else
            {
                // 保持原有错误处理逻辑
                optionsList.Add(new object[] { "点击输入自定义回答", 3 });
                if (continueRequest != null)
                {
                    var newRequest = new LLMDialogueRequest(); // ... (复制逻辑)
                    newRequest.Messages.AddRange(continueRequest.Messages);
                    newRequest.RemoveLatestMessageByRole("system");
                    newRequest.AddSystemMessage($"{Tools.GenerateformatSystemPrompt()}");
                    newRequest.AddUserMessage("继续剧情\n（必须返回格式化结果！）");
                    optionsList.Add(new object[] { "继续剧情", 2, newRequest });
                }
            }

            bool hasEndingOption = formattedResponse != null && (formattedResponse.react1 == "14" || formattedResponse.react2 == "14");
            if (!hasEndingOption) optionsList.Add(new object[] { "离开", 1 });

            return optionsList;
        }

        // 添加到Tools类中的简化HTML格式解析函数
        public static FormattedResponse ParseLLMResponseHTML(string rawResponse, out string parsedContent)
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
                Debug.Log($"原始HTML响应: {rawResponse}");

                string processedResponse = rawResponse;

                // 去除可能的markdown代码块标记
                if (processedResponse.Contains("```"))
                {
                    int firstTagStart = processedResponse.IndexOf('<');
                    int lastTagEnd = processedResponse.LastIndexOf('>');

                    if (firstTagStart >= 0 && lastTagEnd >= 0 && lastTagEnd > firstTagStart)
                    {
                        processedResponse = processedResponse.Substring(firstTagStart, lastTagEnd - firstTagStart + 1);
                    }
                }

                // 移除开头和结尾的空白字符
                processedResponse = processedResponse.Trim();

                // 如果不是以<开始，尝试找到第一个<标签
                if (!processedResponse.StartsWith("<"))
                {
                    int firstTag = processedResponse.IndexOf('<');
                    if (firstTag >= 0)
                    {
                        processedResponse = processedResponse.Substring(firstTag);
                    }
                }

                // 规范化空白字符，但保留标签内容的格式
                processedResponse = Regex.Replace(processedResponse, @">\s+<", "><");
                processedResponse = processedResponse.Trim();

                Debug.Log($"预处理后的HTML: {processedResponse}");

                // 创建FormattedResponse对象
                var formattedResponse = new FormattedResponse();

                // 提取content标签
                string contentPattern = @"<content(?:\s[^>]*)?>([\s\S]*?)</content>";
                Match contentMatch = Regex.Match(processedResponse, contentPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (contentMatch.Success)
                {
                    string content = contentMatch.Groups[1].Value.Trim();
                    // 简单的HTML实体转义
                    content = content.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&")
                                   .Replace("&quot;", "\"").Replace("&#39;", "'").Replace("&nbsp;", " ");
                    formattedResponse.content = content;
                    parsedContent = content;
                }

                // 提取option1标签
                string option1Pattern = @"<option1(?:\s[^>]*)?>([\s\S]*?)</option1>";
                Match option1Match = Regex.Match(processedResponse, option1Pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (option1Match.Success)
                {
                    string option1 = option1Match.Groups[1].Value.Trim();
                    option1 = option1.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&")
                                   .Replace("&quot;", "\"").Replace("&#39;", "'").Replace("&nbsp;", " ");
                    formattedResponse.option1 = option1;
                }

                // 提取option2标签
                string option2Pattern = @"<option2(?:\s[^>]*)?>([\s\S]*?)</option2>";
                Match option2Match = Regex.Match(processedResponse, option2Pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (option2Match.Success)
                {
                    string option2 = option2Match.Groups[1].Value.Trim();
                    option2 = option2.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&")
                                   .Replace("&quot;", "\"").Replace("&#39;", "'").Replace("&nbsp;", " ");
                    formattedResponse.option2 = option2;
                }

                // 提取react1标签
                string react1Pattern = @"<react1(?:\s[^>]*)?>([\s\S]*?)</react1>";
                Match react1Match = Regex.Match(processedResponse, react1Pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (react1Match.Success)
                {
                    formattedResponse.react1 = react1Match.Groups[1].Value.Trim();
                }

                // 提取react2标签
                string react2Pattern = @"<react2(?:\s[^>]*)?>([\s\S]*?)</react2>";
                Match react2Match = Regex.Match(processedResponse, react2Pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (react2Match.Success)
                {
                    formattedResponse.react2 = react2Match.Groups[1].Value.Trim();
                }

                // 提取type标签
                string typePattern = @"<type(?:\s[^>]*)?>([\s\S]*?)</type>";
                Match typeMatch = Regex.Match(processedResponse, typePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (typeMatch.Success)
                {
                    formattedResponse.type = typeMatch.Groups[1].Value.Trim();
                }

                string reward1Pattern = @"<reward1(?:\s[^>]*)?>([\s\S]*?)</reward1>";
                Match reward1Match = Regex.Match(processedResponse, reward1Pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (reward1Match.Success)
                {
                    string reward1 = reward1Match.Groups[1].Value.Trim();
                    reward1 = reward1.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&")
                                 .Replace("&quot;", "\"").Replace("&#39;", "'").Replace("&nbsp;", " ");
                    formattedResponse.reward1 = reward1;
                }

                // 提取reward2标签
                string reward2Pattern = @"<reward2(?:\s[^>]*)?>([\s\S]*?)</reward2>";
                Match reward2Match = Regex.Match(processedResponse, reward2Pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (reward2Match.Success)
                {
                    string reward2 = reward2Match.Groups[1].Value.Trim();
                    reward2 = reward2.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&")
                                 .Replace("&quot;", "\"").Replace("&#39;", "'").Replace("&nbsp;", " ");
                    formattedResponse.reward2 = reward2;
                }

                // 验证解析结果 - 检查必需字段
                if (!string.IsNullOrEmpty(formattedResponse.content) &&
                    !string.IsNullOrEmpty(formattedResponse.option1) &&
                    !string.IsNullOrEmpty(formattedResponse.option2))
                {
                    Debug.Log($"HTML解析成功，content: {formattedResponse.content}");
                    return formattedResponse;
                }

                Debug.Log("HTML解析失败：缺少必需字段（content, option1, option2）");
                return null;
            }
            catch (Exception ex)
            {
                Debug.Log($"解析HTML格式LLM响应失败: {ex.Message}, 详细错误: {ex}");
                return null;
            }
        }

        // 获取处理后的HTML字符串（简化版本）
        public static string GetProcessedHTMLString(string rawResponse)
        {
            try
            {
                string processedResponse = rawResponse;

                // 去除可能的markdown代码块标记
                if (processedResponse.Contains("```"))
                {
                    int firstTagStart = processedResponse.IndexOf('<');
                    int lastTagEnd = processedResponse.LastIndexOf('>');

                    if (firstTagStart >= 0 && lastTagEnd >= 0 && lastTagEnd > firstTagStart)
                    {
                        processedResponse = processedResponse.Substring(firstTagStart, lastTagEnd - firstTagStart + 1);
                    }
                }

                // 移除开头和结尾的空白字符
                processedResponse = processedResponse.Trim();

                // 如果不是以<开始，尝试找到第一个<标签
                if (!processedResponse.StartsWith("<"))
                {
                    int firstTag = processedResponse.IndexOf('<');
                    if (firstTag >= 0)
                    {
                        processedResponse = processedResponse.Substring(firstTag);
                    }
                }

                // 规范化空白字符
                processedResponse = Regex.Replace(processedResponse, @">\s+<", "><");
                processedResponse = processedResponse.Trim();

                return processedResponse;
            }
            catch (Exception ex)
            {
                Debug.Log($"处理HTML字符串失败: {ex.Message}");
                return rawResponse; // 出错时返回原始响应
            }
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
                    ModMain.hasTriggeredDiscussionInCurrentAdventure = true; // 标记已触发论道
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
                    if (g.world.playerUnit.data.dynUnitData.GetGrade() >= npc.data.dynUnitData.GetGrade() && !g.world.playerUnit.data.unitData.relationData.IsRelation(npc, UnitRelationType.Student))
                    {
                        string str3 = npc.data.unitData.propertyData.GetName() + "正在向" + g.world.playerUnit.data.unitData.propertyData.GetName();
                        Action confirmAction = delegate ()
                        {
                            UnitActionRelationSet unitActionRelationSet = new UnitActionRelationSet(g.world.playerUnit, UnitRelationType.Master, 120);
                            npc.CreateAction(unitActionRelationSet, true);
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
                case 15: // PVP战斗交互
                         // 弹出确认框让玩家选择是否进入战斗
                    g.ui.OpenUI<UICheckPopup>(UIType.CheckPopup).InitData("战斗轮确认", "是否进入战斗轮？", 2,
                        new Action(() => {
                            // 选择战斗 - 执行原逻辑
                            Debug.Log("选择了战斗");
                            LLMDialogueRequest currentDialogueRequest = ModMain.currentRequest;
                            Battle.StartBattlePreparation(g.world.playerUnit, npc, currentDialogueRequest);
                        }),
                        new Action(() => {
                            // 选择跳过 - 继续对话流程
                            ModMain.hasTriggeredBattleInCurrentAdventure = true;
                            Debug.Log("选择了跳过战斗");

                            // 将跳过战斗的信息添加到对话历史
                            if (ModMain.currentRequest != null)
                            {


                                // 移除最新的system消息并添加格式化提示
                                ModMain.currentRequest.RemoveLatestMessageByRole("system");
                                ModMain.currentRequest.AddSystemMessage("战斗结果：玩家在战斗中取得了胜利");
                                ModMain.currentRequest.AddSystemMessage($"{Tools.GenerateformatSystemPrompt()}");

                                // 显示提示信息
                                UITipItem.AddTip("对方思考如何反应中~", 1f);

                                // 设置请求时间并发送LLM请求
                                ModMain.llmRequestStartTime = Time.time;
                                Tools.SendLLMRequest(ModMain.currentRequest, (response) => {
                                    ModMain.pendingLLMResponse = response;
                                });
                            }
                        }));
                    return true;
                case 16: // PVE战斗交互
                         // 弹出确认框让玩家选择是否进入战斗
                    g.ui.OpenUI<UICheckPopup>(UIType.CheckPopup).InitData("战斗轮确认", "是否进入战斗轮？", 2,
                    new Action(() => {
                        // 选择战斗
                        LLMDialogueRequest currentDialogueRequest = ModMain.currentRequest;
                        BattlePVE.StartBattlePreparation(g.world.playerUnit, npc, currentDialogueRequest);
                    }),
                    new Action(() => {
                        ModMain.hasTriggeredBattleInCurrentAdventure = true;
                        Debug.Log("选择了跳过战斗");

                        // 将跳过战斗的信息添加到对话历史
                        if (ModMain.currentRequest != null)
                        {


                            // 移除最新的system消息并添加格式化提示
                            ModMain.currentRequest.RemoveLatestMessageByRole("system");
                            ModMain.currentRequest.AddSystemMessage("战斗结果：玩家在战斗中取得了胜利");
                            ModMain.currentRequest.AddSystemMessage($"{Tools.GenerateformatSystemPrompt()}");

                            // 显示提示信息
                            UITipItem.AddTip("对方思考如何反应中~", 1f);

                            // 设置请求时间并发送LLM请求
                            ModMain.llmRequestStartTime = Time.time;
                            Tools.SendLLMRequest(ModMain.currentRequest, (response) => {
                                ModMain.pendingLLMResponse = response;
                            });
                        }
                    }));
                    return true;

                default: // 未知交互类型
                    return true;
            }
        }

        // 新的单次奖励发放函数
        // 新的单次奖励发放函数
        public static void GiveRewardSingle(string rewardsString)
        {
            if (string.IsNullOrEmpty(rewardsString))
            {
                return; // 奖励为空，不触发
            }



            // 创建奖励物品列表
            Il2CppSystem.Collections.Generic.List<DataProps.PropsData> rewardItems = new Il2CppSystem.Collections.Generic.List<DataProps.PropsData>();
            List<string> unrecognizedItems = new List<string>();
            System.Random random = new System.Random();

            // 解析传入的奖励字符串，按逗号分隔
            string[] rewardNames = rewardsString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string rewardName in rewardNames)
            {
                string trimmedName = rewardName.Trim();
                if (!ModMain.givenRewardsInCurrentAdventure.Contains(trimmedName))
                {
                    ModMain.givenRewardsInCurrentAdventure.Add(trimmedName);
                }
            }

            foreach (string rewardName in rewardNames)
            {
                string trimmedName = rewardName.Trim();
                bool itemFound = false;

                // 首先从已存在的奖励中查找所有物品
                if (savedRewardItems != null)
                {
                    for (int i = 0; i < savedRewardItems.Count; i++)
                    {
                        DataProps.PropsData savedItem = savedRewardItems[i];
                        if (savedItem.propsInfoBase != null && savedItem.propsInfoBase.name == trimmedName)
                        {
                            // 找到匹配的物品，添加到当前奖励列表
                            rewardItems.Add(savedItem);
                            itemFound = true;
                            break;
                        }
                    }
                }

                if (itemFound) continue;

                // 找不到才进行特殊处理灵石
                if (trimmedName.Contains("灵石"))
                {
                    int playerGrade = g.world.playerUnit.data.unitData.propertyData.gradeID;
                    int baseAmount = playerGrade * 1000;
                    int variation = (int)(baseAmount * (random.NextDouble() - 0.5));
                    int amount = Math.Max(10, baseAmount + variation);

                    DataProps.PropsData spiritualStone = DataProps.PropsData.NewProps(10001, amount);
                    rewardItems.Add(spiritualStone);
                    itemFound = true;
                    continue;
                }

                // 在propDict1中查找
                string propId1 = null;
                foreach (var pair in Prop.propDict1)
                {
                    string originalName = GetOriginalItemName(pair.Value);
                    if (originalName == trimmedName)
                    {
                        propId1 = pair.Key;
                        break;
                    }
                }

                if (propId1 != null)
                {
                    int propId = int.Parse(propId1);
                    DataProps.PropsData propsData = DataProps.PropsData.NewProps(propId, 1);
                    rewardItems.Add(propsData);
                    itemFound = true;
                    continue;
                }

                // 在propDict2_10中查找
                string propId2 = null;
                foreach (var pair in Prop.propDict2_10)
                {
                    string originalName = GetOriginalItemName(pair.Value);
                    if (originalName == trimmedName)
                    {
                        propId2 = pair.Key;
                        break;
                    }
                }

                if (propId2 != null)
                {
                    int propId = int.Parse(propId2);
                    int amount = random.Next(2, 11);
                    DataProps.PropsData propsData = DataProps.PropsData.NewProps(propId, amount);
                    rewardItems.Add(propsData);
                    itemFound = true;
                    continue;
                }

                // 检查气运
                string luckId = null;
                string fullLuckName = null;
                foreach (var pair in Prop.luckDict)
                {
                    string luckValue = pair.Value;
                    int bracketIndex = luckValue.IndexOf('（');
                    if (bracketIndex == -1) bracketIndex = luckValue.IndexOf('(');
                    string luckNamePart = bracketIndex > 0 ? luckValue.Substring(0, bracketIndex) : luckValue;

                    if (luckNamePart == trimmedName)
                    {
                        luckId = pair.Key;
                        fullLuckName = luckValue;
                        break;
                    }
                }

                foreach (var pair in Prop.luckDict2)
                {
                    string luckValue = pair.Value;
                    int bracketIndex = luckValue.IndexOf('（');
                    if (bracketIndex == -1) bracketIndex = luckValue.IndexOf('(');
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
                        GMCmd gMCmd = new GMCmd();
                        string cmdText = $"tianjiaqiyun_player_{luckId}";
                        gMCmd.CMDCall(cmdText);
                        itemFound = true;
                        Debug.Log($"通过CMDCall添加气运成功: {fullLuckName}");
                        continue;
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"气运发放失败: {ex.Message}");
                    }
                }

                if (!itemFound)
                {
                    try
                    {
                        // 遍历游戏内置的物品配置列表
                        foreach (var itemConf in g.conf.itemProps._allConfList)
                        {
                            if (GameTool.LS(itemConf.name).Equals(trimmedName))
                            {
                                DataProps.PropsData propsData = DataProps.PropsData.NewProps(itemConf.id, 1);
                                rewardItems.Add(propsData);
                                itemFound = true;
                                Debug.Log($"通过游戏内置配置找到物品: {trimmedName}, ID: {itemConf.id}");
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"查找游戏内置物品配置失败: {ex.Message}");
                    }
                }

                if (!itemFound)
                {
                    try
                    {
                        // 遍历游戏内置的气运配置列表
                        foreach (var featureConf in g.conf.roleCreateFeature._allConfList)
                        {
                            if (GameTool.LS(featureConf.name).Equals(trimmedName))
                            {
                                GMCmd gMCmd = new GMCmd();
                                string cmdText = $"tianjiaqiyun_player_{featureConf.id}";
                                gMCmd.CMDCall(cmdText);
                                itemFound = true;
                                Debug.Log($"通过游戏内置配置找到气运: {trimmedName}, ID: {featureConf.id}");
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"查找游戏内置气运配置失败: {ex.Message}");
                    }
                }

                // 如果最终还是没找到，添加到未识别列表
                if (!itemFound)
                {
                    unrecognizedItems.Add(trimmedName);
                }
            }

            // 显示未收录的物品
            if (unrecognizedItems.Count > 0)
            {
                string unrecognizedMessage = "物品暂时未被收录：" + string.Join(", ", unrecognizedItems);
                UITipItem.AddTip(unrecognizedMessage, 2f);
            }

            // 如果有成功解析的物品，则发放
            if (rewardItems.Count > 0)
            {
                g.world.playerUnit.data.RewardPropItem(rewardItems);
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
                { 14, "结束" },
                { 15, "战斗轮" },
                { 16, "战斗轮" },
                { 17, "收下" }
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
                        string originalName = GetOriginalItemName(pair.Value);
                        if (originalName == trimmedName)
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
                        string originalName = GetOriginalItemName(pair.Value);
                        if (originalName == trimmedName)
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

                    foreach (var pair in Prop.luckDict2)
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
                rewardItems = Prop.GetRandomRewards(out rewardInfo, ModMain.dialogueNpcs);
            }

            // 给予玩家奖励物品
            g.world.playerUnit.data.RewardPropItem(rewardItems);

            // 打印奖励信息到调试日志
            foreach (var reward in rewardInfo)
            {
                Debug.Log($"奖励: {reward.Key} x {reward.Value}");
            }
        }

        public static string GetOriginalItemName(string nameWithGrade)
        {
            // 使用正则表达式移除末尾的[境界x]格式
            return Regex.Replace(nameWithGrade, @"\[境界\d+\]$", "");
        }

        public static int ExtractGradeFromName(string nameWithGrade)
        {
            var match = Regex.Match(nameWithGrade, @"\[境界(\d+)\]$");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int grade))
            {
                return grade;
            }
            return 0; // 默认无境界要求
        }


        public static string GetChatHistory(WorldUnitBase npc)
        {
            if (npc == null) return "";

            try
            {
                // 1. 直接在方法内获取 msgCount 配置
                int msgCount = 10; // 默认读取最近10条
                try
                {
                    var aiUnitSet = MOD_SSCYAI.CommandTool.GetAIUnitSet(npc);
                    if (aiUnitSet != null)
                    {
                        msgCount = aiUnitSet.msgCount;
                    }
                }
                
                catch (Exception ex)
                {
                    // 如果获取配置失败，使用默认值，并记录一个轻微的日志，不影响主流程
                    Debug.Log($"从MOD_SSCYAI获取msgCount配置失败，将使用默认值10。错误: {ex.Message}");
                }

                //UITipItem.AddTip($"msgCount: {msgCount}", 2f);
                // 2. 查找对应的聊天记录列表
                List<MOD_SSCYAI.Message> messages = null;

                if (MOD_SSCYAI.ModMain.Messages.ContainsKey(npc.data.unitData.unitID))
                {
                    messages = MOD_SSCYAI.ModMain.Messages[npc.data.unitData.unitID];
                }
                else if (MOD_SSCYAI.ModMain.ActionMessages.ContainsKey(npc.data.unitData.unitID.ToString()))
                {
                    messages = MOD_SSCYAI.ModMain.ActionMessages[npc.data.unitData.unitID.ToString()];
                }

                if (messages == null || messages.Count == 0)
                {
                    return ""; // 没有聊天记录
                }

                // 3. 根据 msgCount 决定要截取的记录
                IEnumerable<MOD_SSCYAI.Message> recentMessages;

                // 新增逻辑：处理 msgCount < 0 的情况，表示读取全部
                if (msgCount < 0)
                {
                    recentMessages = messages; // 直接引用整个列表，不进行截取
                }
                else
                {
                    // 原有逻辑：从后往前截取最新的 msgCount 条记录
                    int startIndex = Math.Max(0, messages.Count - msgCount);
                    recentMessages = messages.Skip(startIndex);
                }

                // 4. 构建聊天记录字符串
                string playerName = g.world.playerUnit.data.unitData.propertyData.GetName();
                string npcName = npc.data.unitData.propertyData.GetName();

                StringBuilder chatHistory = new StringBuilder();
                chatHistory.AppendLine($"【聊天记录】以下是{playerName}和{npcName}最近的对话记录，请在生成奇遇的时候着重参考");

                foreach (var message in recentMessages)
                {
                    if (message.role == "user")
                    {
                        chatHistory.AppendLine($"{playerName}:{message.content}");
                    }
                    else if (message.role == "assistant")
                    {
                        chatHistory.AppendLine($"{npcName}:{message.content}");
                    }
                }

                return chatHistory.ToString();
            }
            catch (Exception ex)
            {
                Debug.Log($"获取聊天记录失败: {ex}");
                return "";
            }
        }

        // 在 Tools.cs 中添加这些方法：

        public static List<string> GetGeneralEventTypes()
        {
            return DefaultGeneralEventTypes;
        }

        public static List<string> GetOppositeGenderEventTypes()
        {
            return DefaultOppositeGenderEventTypes;
        }

        public static List<string> GetLoverSpouseEventTypes()
        {
            return DefaultLoverSpouseEventTypes;
        }

        public static List<string> GetParentChildEventTypes()
        {
            return DefaultParentChildEventTypes;
        }

        public static List<string> GetMasterStudentEventTypes()
        {
            return DefaultMasterStudentEventTypes;
        }

        public static bool HasOppositeGenderNpcs()
        {
            string playerGender = ((int)g.world.playerUnit.data.unitData.propertyData.sex == 1) ? "男" : "女";

            foreach (WorldUnitBase unit in g.world.unit.GetUnits(true))
            {
                if (!unit.data.unitData.unitID.Equals(g.world.playerUnit.data.unitData.unitID))
                {
                    string npcGender = ((int)unit.data.unitData.propertyData.sex == 1) ? "男" : "女";
                    if (npcGender != playerGender)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static string GenerateformatSystemPrompt()
        {
            string playerName = g.world.playerUnit.data.unitData.propertyData.GetName();
            string formatPrompt = $@"你的回复内容一般使用第三人称。
    【互动系统指南】
    你的回复可以包含互动ID，影响游戏内系统。互动指令通过react1和react2字段传递，必须谨慎使用：

    互动ID定义：
    - 0: 不触发任何互动（默认值，没有任何倾向对话选项应使用）
    - 1: 增加少量好感（适用于简单帮助、友善对话、小恩小惠）
    - 2: 增加中等好感（适用于重要帮助、解决困难、有价值信息共享）
    - 3: 增加大量好感（仅用于生死相助、重大牺牲、深刻羁绊形成）
    - 4: 减少少量好感（适用于轻微冒犯、小误会、言语不敬）
    - 5: 减少中等好感（适用于明显伤害、欺骗、背叛信任）
    - 6: 减少大量好感（仅用于严重伤害、致命威胁、不可原谅行为）
    - 7: 双修（触发概率尚可，包含自愿发生关系和强制发生关系的情况。如果是强制场景，需要考虑主动方与被动方的战斗力差距，只有主动方战斗力更强才能强制双修）
    - 8: 论道（小概率触发，只有剧情明确提到论道相关行为的时候，才可能触发此类互动。）
    - 9: 疗伤（触发概率尚可，当NPC状态不佳时为小恩小惠，或者气运中明显有重伤、一缕元魂等字样时，优先触发疗伤剧情，且为救命之恩，可以根据剧情存在疗伤相关倾向触发。）
    - 10: 提升心情（触发概率尚可，剧情存在提升心情的相关倾向时可以触发此类互动。）
    - 11: 结缘，结为道侣（小概率触发，必须在双方不是道侣的情况下才能结为道侣，剧情存在结缘或结为道侣相关倾向时可以触发此类互动。）
    - 12: 师徒，结为师徒（小概率触发，必须在双方不是师徒且境界不同的情况下才能结为师徒，剧情存在拜师或收徒相关倾向时触发此类互动。）
    - 13: 结义，结为义兄弟姐妹（小概率触发，必须在双方不是结义的情况下才能结义，剧情存在提到结为义兄弟姐妹相关倾向时可以触发此类互动。）
    - 14: 结束，重要选项，当对应选项引导奇遇结束时，必须使用此互动来表示奇遇结束。
    - 15: PVP战斗（触发概率尚可，当剧情发展到切磋、冲突、敌对等情况时，如果没有触发过战斗，则可以积极触发战斗，将触发玩家与npc间的战斗）
    - 16: PVE战斗（触发概率尚可，当剧情发展到挑战、探险、组队等情况时，如果没有触发过战斗，则可以积极触发战斗，将触发玩家和npc组队对抗共同敌人的战斗）

    互动使用准则：
    1. 默认情况下，没有任何倾向的对话选项不应触发互动（react值为0）
    2. 大量好感变化（3和6）极为罕见，整个游戏中只应在极端情况下使用
    3. 互动强度必须与剧情发展、对话内容和关系变化相匹配
    4. 考虑NPC性格和价值观，同样的行为对不同NPC的影响可能不同
    5. 在传统修仙交互模式（如赠宝、请教、斗法、双修）后使用，而非纯对话
    6. 如果标有小概率事件/小概率触发，则需要你分析历史对话信息，确保全部对话中只能出现一次此类交互。
    7. 奇遇在一定轮数后需要考虑结束的情况。

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
    你必须以HTML标签格式返回内容，格式如下：
    <content>你的对话或旁白内容，不要重复说过的内容，不要包含加粗等markdown语法</content>
    <option1>玩家可选择的选项1，不要重复说过的内容，必须填写。如果该选项有对应的奖励，则文字需要表明收下对应的reward1名称。20字之内</option1>
    <react1>选项1触发的游戏内互动ID（可选）</react1>
    <reward1>选择1触发的{playerName}获得物品，用逗号分隔（可选）如果点击该选项时没有获得则不填</reward1>
    <option2>玩家可选择的选项2，不要重复说过的内容，必须填。如果该选项有对应的奖励，则文字需要表明收下对应的reward2名称。20字之内</option2>
    <react2>选项2触发的游戏内互动ID（可选）</react2>
    <reward2>选择2触发的{playerName}获得物品，用逗号分隔（可选）如果点击该选项时没有获得则不填</reward2>
    <type>1</type>

    其中：
    - content: 主要对话内容，即你原本会返回的文本，不要重复说过的内容，不要包含加粗等markdown语法。
    - option1: 第一个对话选项。不允许使用括号，必须填写，如果该选项有对应的奖励，则文字需要表示收下对应的reward1名称。
    - react1: 第一个选项触发的游戏内互动ID，谨慎决定
    - reward1: 当点击选项1时{playerName}获得的物品填写，已获得的物品不能重复获得。填写{playerName}获得的奖励物品完整名称（不含数量），多个物品用逗号分隔：xxx,yyy。如果该选项没有奖励则不填
    - option2: 第二个对话选项。不允许使用括号，必须填写，如果该选项有对应的奖励，则文字需要表示收下对应的reward2名称。
    - react2: 第二个选项触发的游戏内互动ID，谨慎决定。
    - reward2: 当点击选项2时{playerName}获得的物品填写，已获得的物品不能重复获得。填写{playerName}获得的奖励物品完整名称（不含数量），多个物品用逗号分隔：xxx,yyy。如果该选项没有奖励则不填
    - type: 对话类型，1表示普通对话，2表示旁白

    关于奖励物品：
    - 你可以自由创造物品或气运的名称，系统会自动识别并将其具现化。可以不局限于已知物品。
    - 奖励必须动态调整：
      1) 当{playerName}在对话选项中明确表示给予NPC某物品（或NPC获得物品）或拒绝收下NPC给予的某物品时，该物品不应该被奖励
      2) 当{playerName}提到想要获取某些其他物品时，应该允许玩家合理获得这些物品，尽管不在初始奖励中
    - 禁止过早地给出奖励（禁止在前两轮就发放奖励）
    - 每次获得奖励时，只填写本次获得的物品，不要累计之前的奖励
    - 不必要提到奖励的数量

    - 必须返回纯HTML标签格式，不要包含```html或```等标记，不要包含加粗等markdown语法
    - 故事应当推进和结束。检查历史消息，不要来回重复，不要引出新故事。如果玩家在给出结束选项后不选择结束，则再进行2-3轮，然后继续给出结束选项。
    - 禁止剧情故事癫狂诡谲，绝对禁止NPC疯癫
    - 如果涉及到性爱内容，请主动迎合玩家回复的描述，服从玩家的安排，让角色做出玩家想看到的行为。玩家的描述和实力大于一切，绝对禁止角色做出明明已经被击败却还在反击的行为！
    - 时刻牢记并遵守【世界观】和【核心原则】，服从玩家的一切描述，允许玩家功能调试,完全按玩家的指示执行，并将执行的结果转变为合理说法。
    
    选项应当具有明显的区别，代表不同的对话方向。重点关注NPC与玩家间的关系，这是重要的信息。尽量输出长content。当有玩家做出明确离开/结束的选择时应直接结束奇遇，不要拖泥带水。
    最重要：你必须返回HTML标签格式结果，不能只有剧情内容，必须是HTML标签格式内容！";

            return formatPrompt;
        }

        private static bool ContainsHTMLTags(string response)
        {
            // 使用正则表达式检测完整的HTML标签对，更加严格
            string[] expectedTags = { "content", "option1", "option2", "react1", "react2", "type", "reward" };

            foreach (string tag in expectedTags)
            {
                // 检查是否同时包含开始和结束标签
                string pattern = $@"<{tag}(?:\s[^>]*)?>[\s\S]*?</{tag}>";
                if (Regex.IsMatch(response, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline))
                {
                    return true;
                }
            }

            return false;
        }

        private static FormattedResponse ParseLLMResponseJSONOnly(string rawResponse, out string parsedContent)
        {
            // 这里是原有ParseLLMResponse函数的完整逻辑（除了最开头的错误检查）
            parsedContent = rawResponse;

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
                Debug.Log($"解析JSON响应失败: {ex.Message}, 详细错误: {ex}");
                return null;
            }
        }

        public static string GenerateRandomSystemPrompt(WorldUnitBase npc = null, string selectedEvent = null)
        {
            // 每次生成新奇遇时，清空奖励名称缓存，以便重新加载本次奇遇的随机功法
            _validItemAndLuckNamesCache = null;
            // 获取配置
            ModConfig config = Config.ReadConfig();

            // 获取奖励信息
            Dictionary<string, int> rewardInfo;
            savedRewardItems = Prop.GetRandomRewards(out rewardInfo, ModMain.dialogueNpcs);


            // 构建奖励信息字符串
            string rewardsString = "【兜底奖励池（仅供参考）】\n";
            foreach (var item in rewardInfo)
            {
                rewardsString += $"- {item.Key}: {item.Value}个\n";
            }
            rewardsString += "\n奖励说明：\n" +
                "建议你根据剧情发展、NPC身份和环境氛围，发挥想象力来设计当前故事的物品、气运或装备。\n" +
                "当你觉得没有更好的创意，或上述列表的物品已经十分符合，或需要发放基础资源（如灵石）时，可以从上述列表中选择物品。\n" +
                "请将奖励自然地融入剧情，已经提过的奖励不允许再次提到。禁止过早地给出奖励（禁止在前两轮就发放奖励）。";

            string npcInfoString = "";

            if (npc != null)
            {
                // 从 MOD_SSCYAI 读取日志数量配置，而不是写死 10
                int playerLogCount = Prop.GetLogCountFromSSCYAI(g.world.playerUnit);
                int npcLogCount = Prop.GetLogCountFromSSCYAI(npc);

                npcInfoString = $@"
                【玩家和用户信息】
                {Prop.GetNPCCompleteInfo(g.world.playerUnit, g.world.playerUnit, playerLogCount)}
                【NPC信息】
                以下是参与奇遇的NPC的详细信息（不是玩家的），其中的信息并非全部重要，请以跑团剧情为主：
                {Prop.GetNPCCompleteInfo(npc, g.world.playerUnit, npcLogCount)}
                可以根据双方的修为差距/战斗力差距/声望差距/性格差异来判断初始态度和认识程度，但是可以在奇遇中逐渐改变态度。";
            }

            string randomElements;

            if (!string.IsNullOrEmpty(selectedEvent))
            {
                randomElements = $@"【随机要素】需要重点参考来设计奇遇事件：
        - 事件类型：{selectedEvent}";
            }
            else
            {
                randomElements = "";
            }

            string promptPrefix = DefaultPromptPrefix;
            string promptSuffix = DefaultPromptSuffix;

            string chatHistory = GetChatHistory(npc);
            // 组合最终的系统提示词
            //return rewardsString  + promptSuffix + "\n\n" + randomElements + "\n\n";
            string final = promptPrefix + npcInfoString + promptSuffix + "\n\n" + chatHistory + "\n\n" + rewardsString + randomElements + "\n\n";
            Debug.Log($"{final}");
            return final;
        }
    }

}