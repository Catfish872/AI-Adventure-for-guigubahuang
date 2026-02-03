using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;
using UnhollowerBaseLib;
using System.Text;

namespace MOD_kqAfiU
{
    public class CreationSystem
    {

        private static string SavePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods", "MOD_kqAfiU", "AI_Custom_Items.json");

        // 随机数生成器
        private static System.Random _rng = new System.Random();

        // 图标映射表 (Key -> 游戏内图标ID列表)
        public static Dictionary<string, List<int>> IconMapper = new Dictionary<string, List<int>>()
        {
           // ================= 丹药 (按颜色/质感) =================
            // 红色/粉色/赤色丹药 (活血、复生、融血、赤阳、嗜血、血煞)
            { "Pill_Red", new List<int> {
                1011061, 1011071, 1011081, // 活血, 复生, 融血
                1011171, 1011181, 1011201, // 赤阳, 嗜血, 血煞
                1011945, 1011955, // 蕴血, 蓄血
                1011381, 1011741, 1011271 // 复容, 护心, 延寿(通常偏红/暖色)
            } },

            // 蓝色/青色/冰色丹药 (复灵、融灵、冰心、聚气)
            { "Pill_Blue", new List<int> {
                1011131, 1011421, 1011661, // 复灵, 融灵, 冰心
                1011925, 1011935, // 融灵丸, 合灵丸
                1011101, 1011845, // 聚气丹/丸 (通常偏蓝白)
                1011751, 1031021, 1011531 // 复明, 移星, 云中
            } },

            // 金色/黄色/橙色丹药 (筑基、黄龙、蓄力、龙力、化神、金身)
            { "Pill_Gold", new List<int> {
                1011461, 1011651, 1011161, 1011191, // 筑基, 黄龙, 蓄力, 龙力
                1011591, 1011211, 1011111, // 化神, 塑骨, 培元
                1011281, 1011291, 1011091, // 壮元, 补天, 九转圣阳
                1011481, 1011491, 1011501 // 天乾, 地坤, 万河
            } },

            // 华丽/发光/多彩丹药 (悟道、羽化、登仙、九转、灵果)
            { "Pill_Fancy", new List<int> {
                1011601, 1011621, 1011631, // 悟道, 羽化, 登仙
                1011724, 1011641, 1011681, // 九转还魂, 鸿蒙, 星罗
                1051072, 1051082, 1051092, 1051102, 1051112, 1051122 // 五行灵果
            } },

            // 暗色/绿色/紫色丹药 (毒雾、幽冥、怪异)
            { "Pill_Dark", new List<int> {
                1021011, 1011151, // 毒雾, 幽冥饮魄
                1011691, 1011571 // 无相, 破茧 (偏暗或怪异)
            } },

            // ================= 坐骑 (按模型形态) =================
            // 剑形/长条形 (飞剑)
            { "Mount_Sword", new List<int> {
                3021041, 3021051, 3021061, 3021071, 3021081, 3021091, // 飞剑
                8103241, 8103251, 8103261, 8103271, 8103281, 8103291  // 古·飞剑
            } },
            
            // 兽形/生物 (灵兽：驺吾、乘黄、当扈等)
            { "Mount_Beast", new List<int> {
                3021011, 3021021, 3021226, 3021236, 3021101, 3021111, 3021121, 3021141, 3021176, // 普通坐骑
                8103211, 8103221, 8103301, 8103311, 8103321, // 古·坐骑
                7000005, 7000050 // 幽魂/暗影兽
            } },

            // 云雾/气团 (祥云、灵云)
            { "Mount_Cloud", new List<int> {
                3021151, 3021186, 3021216
            } },

            // 舟船/车辇 (飞舟)
            { "Mount_Boat", new List<int> {
                3021161, 3021206, 7071034 // 飞舟, 沙漠之舟
            } },

            // 特殊形状 (葫芦、异卵、星盘)
            { "Mount_Special", new List<int> {
                3021131, 3021196, 3021241, 3021256, // 葫芦, 异卵, 冰虹, 星盘
                8103331 // 古·葫芦
            } },

            // ================= 装备与饰品 (按形态) =================
            // 戒指/环状物
            { "Item_Ring", new List<int> {
                3011011, 3011021, 3011031, 3011041, 3011051, 3011061, 3011071, // 各类戒指
                3011111, 3011121, 3011341, 3011361, 3011381, 3011401,
                3011441, 3011461, 3011481, 3011501 // 幽凝系列
            } },

            // 符箓/纸张/牌子 (界元石、卜辞)
            { "Item_Paper", new List<int> {
                2022111, 2022112, 2022113, 2022114, 2022115, 2022116, // 界元石
                7011015, 7101014 // 祭祀卜辞, 灵龟卜甲
            } },

            // 书本 (通论、杂记、谱)
            { "Item_Book", new List<int> {
                1051196, 1051206, 1051216, 1051226, 1051236, // 炼丹通论
                1051246, 1051296, 1051346, 1051396, // 风水/药材/矿材/画符通论
                6161392, 6161397, 6161402, 6161407, 6161412, 6161417 // 炼器/金丹/青囊/百草
            } },

            // 卷轴/华丽秘籍 (功法、神通)
            { "Item_Scroll", new List<int> {
                1000101, 1000102, 1000201, 1000301, 1000401, 1000501, // 通用书
                242101, 242102, 242103, 242104, 242105, 242106, // 刀枪剑拳掌指心法
                242201, 242301, 242401 // 功/法/神通
            } },

            // ================= 材料 (按材质) =================
            // 矿石/金属/硬物 (Mat_Ore)
            { "Mat_Ore", new List<int> {
                5011011, 5011012, 5011013, 5011014, 5011015, 5011016, // 各品级矿石
                5011021, 5021134, 5021314, // 高级铁矿, 精铁, 玄铁片
                5301108, 5342101, 5121191, // 流光玄金, 赤玄金, 星硫铁
                5081725, 5081745 // 太乙真金, 青冥矿
            } },

            // 草药/花朵/植物 (Mat_Plant)
            { "Mat_Plant", new List<int> {
                5031011, 5031025, 5031041, 5031051, 5031061, 5031071, // 各类草药/狐媚仙草
                5081045, 5081135, 5081455, 5081515, 5081525, // 朱果, 凤凰果, 蕴仙芝, 碧桃...
                5031234, 5031254, 5021496 // 雪莲, 土银花, 不老藤
            } },

            // 生物组织 (牙/骨/皮/角) (Mat_Monster)
            { "Mat_Monster", new List<int> {
                5021022, 5021032, 5021062, 5021072, // 胆, 囊, 血玉, 血骨
                5021124, 5021284, 5021304, 5021364, // 皮, 爪, 牙, 利爪
                7000188, 5041015, 5041025 // 饕餮角, 魂珠
            } },

            // 宝玉/珠子/发光球体 (Mat_Gem)
            { "Mat_Gem", new List<int> {
                5061016, 5061026, 5061056, // 神石, 玄玉, 紫天晶
                5081245, 5021324, 5021334, // 龙晶, 红玉, 玉镯
                5031784, 5031794, 5031804, 5031814, 5031824, 5031834, // 五行灵珠
                5031904, 5031914, 5031964 // 斗珠, 天道之气(球体)
            } },

            // 珍宝/法宝残片/器物 (Mat_Treasure)
            { "Mat_Treasure", new List<int> {
                5081015, 5081025, 5081115, 5081445, // 葬沙骨, 朝夕露, 星辰砂, 五彩神石
                7071012, 7071022, 7071045, // 青铜器 (龙首, 神鸟, 夔龙壶)
                7091012, 7091022, 5121111, 5121121 // 画卷, 号角
            } },
        };

        /// <summary>
        /// 初始化系统：在ModMain.OnIntoWorld或Init时调用
        /// 负责读取本地JSON，恢复所有历史生成的物品定义
        /// </summary>
        public static void Init()
        {
            try
            {
                if (!File.Exists(SavePath)) return;

                string json = File.ReadAllText(SavePath);
                var savedItems = JsonConvert.DeserializeObject<List<SavedItemData>>(json);

                if (savedItems == null) return;

                Debug.Log($"[CreationSystem] 开始恢复 {savedItems.Count} 个自定义物品...");

                foreach (var item in savedItems)
                {
                    // 恢复时，isRestoring = true，只注册定义，不发奖，不重复保存
                    switch (item.Type)
                    {
                        case "Luck":
                            CreateLuck(item.BaseInfo, item.Effects, item.ExtraInfo, true, item.GeneratedID);
                            break;
                        case "Consumer":
                            CreateConsumer(item.BaseInfo, item.Effects, item.ExtraInfo, true, item.GeneratedID);
                            break;
                        case "Equip":
                            CreateEquip(item.BaseInfo, item.Effects, item.ExtraInfo, true, item.GeneratedID);
                            break;
                        case "Ring":
                            CreateRing(item.BaseInfo, item.Effects, item.ExtraInfo, true, item.GeneratedID);
                            break;
                    }
                }
                typeof(Tools).GetField("_validItemAndLuckNamesCache", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);
                Debug.Log("[CreationSystem] 自定义物品恢复完成。");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CreationSystem] 初始化失败: {ex}");
            }
        }

        /// <summary>
        /// 触发造物流程：发送请求 -> 解析JSON -> 分发创建
        /// </summary>
        /// <param name="sysPrompt">系统提示词（包含JSON格式要求）</param>
        /// <param name="userPrompt">用户指令（包含物品名和剧情背景）</param>
        public static void TriggerCreation(string sysPrompt, string userPrompt)
        {
            UITipItem.AddTip("天道正在推演造物...", 3f);
            Debug.Log("[CreationSystem] 开始请求造物...");

            var request = new LLMDialogueRequest();
            request.AddSystemMessage(sysPrompt);
            request.AddUserMessage(userPrompt);

            Tools.SendLLMRequest(request, (response) =>
            {
                if (string.IsNullOrEmpty(response) || response.StartsWith("错误"))
                {
                    UITipItem.AddTip("造物推演失败：连接中断", 3f);
                    Debug.LogError($"[CreationSystem] LLM响应错误: {response}");

                    // 失败解锁
                    RunOnMainThread(() => {
                        // 【新增】反射强行清空Tools缓存，防止死循环
                        typeof(Tools).GetField("_validItemAndLuckNamesCache", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);
                        ModMain.isCreatingItems = false;
                    });
                    return;
                }

                try
                {
                    // 1. 清洗字符串
                    string jsonStr = CleanJsonString(response);
                    Debug.Log($"[CreationSystem] 清洗后的JSON: {jsonStr}");

                    List<AICreationResponse> dataList = new List<AICreationResponse>();

                    // 2. 智能解析
                    if (jsonStr.StartsWith("["))
                    {
                        var list = JsonConvert.DeserializeObject<List<AICreationResponse>>(jsonStr);
                        if (list != null) dataList = list;
                    }
                    else
                    {
                        var single = JsonConvert.DeserializeObject<AICreationResponse>(jsonStr);
                        if (single != null) dataList.Add(single);
                    }

                    if (dataList.Count == 0)
                    {
                        Debug.LogError("[CreationSystem] 反序列化后数据为空");
                        // 解析为空也要解锁
                        RunOnMainThread(() => {
                            // 【新增】反射清空缓存
                            typeof(Tools).GetField("_validItemAndLuckNamesCache", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);
                            ModMain.isCreatingItems = false;
                        });
                        return;
                    }

                    // 3. 遍历分发
                    foreach (var item in dataList)
                    {
                        RunOnMainThread(() => { DispatchCreation(item); });
                    }

                    // 成功解锁：确保在物品分发之后执行
                    RunOnMainThread(() => {
                        // 【新增】反射清空缓存，确保下一帧Tools能读到新注册的物品
                        typeof(Tools).GetField("_validItemAndLuckNamesCache", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);
                        ModMain.isCreatingItems = false;
                        //UITipItem.AddTip("天道推演完成，机缘已至！", 2f);
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CreationSystem] 解析或分发异常: {ex}");
                    UITipItem.AddTip("造物法则崩坏（解析失败）", 3f);

                    // 【修改点3】异常解锁
                    RunOnMainThread(() => {
                        // 【新增】反射清空缓存
                        typeof(Tools).GetField("_validItemAndLuckNamesCache", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);
                        ModMain.isCreatingItems = false;
                    });
                }
            });
        }

        private static void DispatchCreation(AICreationResponse data)
        {
            if (data.BaseInfo == null)
            {
                Debug.LogError("[CreationSystem] BaseInfo 缺失");
                return;
            }

            Debug.Log($"[CreationSystem] 正在创建类型: {data.Type}, 名称: {data.BaseInfo.Name}");

            switch (data.Type)
            {
                case "Luck":
                    // 气运需要 ExtraInfo.Duration
                    if (data.ExtraInfo == null) data.ExtraInfo = new CreationExtraInfo { Duration = -1 }; // 保底
                    CreateLuck(data.BaseInfo, data.Effects, data.ExtraInfo);
                    break;

                case "Consumer":
                    // 消耗品需要 Worth, RealmReq
                    if (data.ExtraInfo == null) data.ExtraInfo = new CreationExtraInfo { Worth = 1000, RealmReq = 1 }; // 保底
                    CreateConsumer(data.BaseInfo, data.Effects, data.ExtraInfo);
                    break;

                case "Vehicle":
                    // 载具类 (原Equip逻辑: 坐骑/飞剑)
                    if (data.ExtraInfo == null) data.ExtraInfo = new CreationExtraInfo { Worth = 5000, RealmReq = 1 };
                    CreateEquip(data.BaseInfo, data.Effects, data.ExtraInfo);
                    break;

                case "Carried":
                    // 携带类 (新增逻辑: 戒指/饰品)
                    if (data.ExtraInfo == null) data.ExtraInfo = new CreationExtraInfo { Worth = 3000, RealmReq = 1 };
                    CreateRing(data.BaseInfo, data.Effects, data.ExtraInfo);
                    break;

                default:
                    Debug.LogWarning($"[CreationSystem] 未知的造物类型: {data.Type}，尝试作为消耗品处理");
                    CreateConsumer(data.BaseInfo, data.Effects, data.ExtraInfo ?? new CreationExtraInfo());
                    break;
            }
        }

        /// <summary>
        /// 辅助：清洗 AI 返回的字符串，提取 JSON 部分
        /// </summary>
        private static string CleanJsonString(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "{}";

            string clean = raw.Trim();

            // 1. 去除 Markdown 代码块标记 ```json ... ```
            if (clean.StartsWith("```"))
            {
                int firstNewline = clean.IndexOf('\n');
                if (firstNewline > 0) clean = clean.Substring(firstNewline + 1);
                int lastBackticks = clean.LastIndexOf("```");
                if (lastBackticks > 0) clean = clean.Substring(0, lastBackticks);
            }

            clean = clean.Trim();

            // 2. 智能定位最外层括号
            int startArr = clean.IndexOf('[');
            int startObj = clean.IndexOf('{');

            // 如果找不到任何括号，直接返回原样(等死)或空对象
            if (startArr == -1 && startObj == -1) return "{}";

            // 判断谁在前面：如果 '[' 存在且比 '{' 靠前，说明是数组
            bool isArray = (startArr != -1) && (startObj == -1 || startArr < startObj);

            if (isArray)
            {
                int endArr = clean.LastIndexOf(']');
                if (endArr > startArr)
                {
                    return clean.Substring(startArr, endArr - startArr + 1);
                }
            }
            else
            {
                int endObj = clean.LastIndexOf('}');
                if (endObj > startObj)
                {
                    return clean.Substring(startObj, endObj - startObj + 1);
                }
            }

            return clean;
        }

        // 为了在回调中安全操作Unity API，简单的切换到主线程的 helper (复用 ModMain 的队列机制)
        private static void RunOnMainThread(Action action)
        {
            ModMain.RunOnMainThread(action);
        }
        private static string GetRealmName(int gradeId)
        {
            switch (gradeId)
            {
                case 1: return "炼气境";
                case 2: return "筑基境";
                case 3: return "结晶境";
                case 4: return "金丹境";
                case 5: return "具灵境";
                case 6: return "元婴境";
                case 7: return "化神境";
                case 8: return "悟道境";
                case 9: return "羽化境";
                case 10: return "登仙境";
                default: return "未知境界";
            }
        }
        public static void StartCreationProcess(List<string> rewardNames, List<MessageItem> dialogueHistory)
        {
            // 0. 判空保护
            if (g.world.playerUnit == null || rewardNames == null || rewardNames.Count == 0) return;

            // 1. 获取玩家基础信息
            var player = g.world.playerUnit;
            string playerName = player.data.unitData.propertyData.GetName();
            int gradeId = player.data.unitData.propertyData.gradeID;
            string gradeName = GetRealmName(gradeId);

            // 2. 组装对话上下文
            string storyContext = FormatDialogueHistory(dialogueHistory, playerName);

            // 3. 计算动态数值指导
            string statGuidelines = GetDynamicStatGuidance(player, gradeId);

            // 4. 组装系统提示词
            string sysPrompt = BuildSystemPrompt(gradeName, statGuidelines);

            // 5. 组装用户指令 (User Prompt) - [核心修改]
            // 将列表拼接成 "【物品A】、【物品B】"
            string namesStr = string.Join("】、【", rewardNames);

            string userPrompt = $"当前情境：{storyContext}\n\n" +
                                $"请根据上述剧情，为以下 {rewardNames.Count} 个指定名称的物品/气运进行设计：【{namesStr}】。\n" +
                                $"要求：\n" +
                                $"1. 输出必须是一个包含 {rewardNames.Count} 个对象的 JSON 数组。\n" +
                                $"2. 数组中的每个对象必须对应上述一个名称，名称不可修改。\n" +
                                $"3. 请判断每个物品合理的稀有度（1-6），并从数值指导表中选择属性。";

            // 6. 触发请求
            TriggerCreation(sysPrompt, userPrompt);
        }

        private static string BuildSystemPrompt(string playerRealm, string statGuidelines)
        {
            return $@"你是一个《鬼谷八荒》的游戏数值策划。你的任务是根据剧情设计合理的奖励物品。
请严格遵守以下规则：

### 1. 物品类型判断
请根据奖励名称和剧情判断类型 Type：
- ""Luck"" (气运): 某种身体状态、顿悟、BUFF。需设定持续时间。
- ""Consumer"" (丹药/消耗品): 吃了就没的物品。
- ""Vehicle"" (载具类装备): 某种可以骑乘或驾驶的宏大器物（如飞剑、灵兽、云雾、飞舟等）。
- ""Carried"" (携带类装备): 某种随身佩戴的精巧饰品（如戒指、护符、玉佩、令牌等）。

### 2. 输出格式 (严格JSON)
请输出一个 JSON 对象，**或者** 一个包含多个对象的 JSON 数组（如果你想同时发放多个奖励）。
不要包含Markdown标记。

**单物品示例**:
{{ ""Type"": ... }}

**多物品示例**:
[
  {{ ""Type"": ""Luck"", ... }},
  {{ ""Type"": ""Carried"", ... }}
]

**字段结构模板**:
{{
  ""Type"": ""Luck"", 
  ""BaseInfo"": {{
    ""Name"": ""物品名"",
    ""Grade"": 4, // 1灰 2绿 3蓝 4紫 5橙 6红
    ""IconCategory"": ""Pill_Red"", // 如果是气运则不需要填写，非气运需选其一: Pill_Red/Blue/Gold/Dark/Fancy, Mount_Sword/Beast/Cloud/Boat/Special, Item_Ring(适合Carried类), Item_Paper, Item_Book, Item_Scroll, Mat_Ore/Plant/Monster/Gem/Treasure
    ""Description"": ""若是气运(Luck)和携带类装备(Carried)，必须包含背景故事+数值效果文本(如：受神力加持，攻击+10%)；若是物品，只写背景故事，不要写数值。""
  }},
  ""Effects"": ""atk_0_10|def_1_20"", // 属性字符串，详见下文
  ""ExtraInfo"": {{
    ""Worth"": 5000, // 售价 (气运填0)
    ""RealmReq"": 1, // 境界需求1-10 (气运填0)
    ""Duration"": 6 // 气运持续月数 (非气运填0, 永久气运填-1)
  }}
}}

### 3. 属性字符串格式 (Effects)
格式为: `key_type_val`，多个属性用 `|` 分隔。
- key: 属性代码 (见下表)
- type: 0 表示百分比加成(%), 1 表示固定数值加成(+)
- val: 数值 (整数)

**示例**: 
- ""atk_0_10"": 攻击增加 10%
- ""storage_1_30"": 背包容量增加 30个 (仅Carried类有效)

### 4. 属性代码对照表
- 攻击: atk | 防御: def | 体力: hpMax | 灵力: mpMax | 念力（sp代表念力，不是精力，不支持对精力属性进行调整）: spMax
- 会心: crit | 护心: guard | 寿命: life | 魅力: beauty | 幸运: luck | 声望: reputation
- 脚力: fsp | 战斗移速: msp
- 储物空间: storage (仅限 Carried 类物品使用，且 type 只能为 1)
- 资质类: basSword(剑), basSpear(枪), basBlade(刀), basFist(拳), basPalm(掌), basFinger(指), basFire(火), basFroze(水), basThunder(雷), basWind(风), basEarth(土), basWood(木)

### 5. 数值设计指导 (基于玩家当前境界: {playerRealm})
{statGuidelines}

**特殊规则**:
- 如果是【期限型气运】(Duration > 0)，上述推荐数值可以翻 3 倍。
- 请在推荐范围内随机取值，不要总是取整数，保持随机性。
";
        }

        private static string GetDynamicStatGuidance(WorldUnitBase player, int gradeId)
        {
            var data = player.data.dynUnitData;
            StringBuilder sb = new StringBuilder();

            // 辅助函数：生成 1-6 级稀有度的范围字符串
            // scalingType: 1 (1-12% 高加成), 2 (1-6% 低加成)
            string GenerateRanges(string attrName, float currentVal, int scalingType)
            {
                sb.AppendLine($"【{attrName}】参考值 (当前基准: {currentVal:F0}):");
                for (int i = 1; i <= 6; i++)
                {
                    // 比例系数
                    float minPct = 0, maxPct = 0;
                    if (scalingType == 1) { minPct = (i * 2 - 1); maxPct = i * 2; } // 1-2%, 3-4%...
                    else { minPct = i; maxPct = i; } // 1%, 2%...

                    // 计算固定值范围 (type=1)
                    int minFlat = (int)(currentVal * minPct / 100f);
                    int maxFlat = (int)(currentVal * maxPct / 100f);
                    if (minFlat < 1) minFlat = 1;
                    if (maxFlat < minFlat) maxFlat = minFlat + 1;

                    // 写入一行指导
                    string color = GetGradeName(i);
                    sb.AppendLine($"  - {color}色(Lv{i}): 百分比[{minPct}-{maxPct}%] (写 _0_{minPct}-{maxPct}) 或 固定值[{minFlat}-{maxFlat}] (写 _1_{minFlat}-{maxFlat})");
                }
                sb.AppendLine();
                return "";
            }

            // 1. 高加成组 (1-12%)
            GenerateRanges("攻击(atk)", data.attack.value, 1);
            GenerateRanges("防御(def)", data.defense.value, 1);
            GenerateRanges("体力(hpMax)", data.hpMax.value, 1);
            GenerateRanges("灵力(mpMax)", data.mpMax.value, 1);
            GenerateRanges("念力(spMax)", data.spMax.value, 1);
            GenerateRanges("会心(crit)", data.crit.value, 1);
            GenerateRanges("护心(guard)", data.guard.value, 1);

            string[] basNames = new string[] {
                "剑法(basSword)", "枪法(basSpear)", "刀法(basBlade)", "拳法(basFist)", "掌法(basPalm)", "指法(basFinger)",
                "火灵(basFire)", "水灵(basFroze)", "雷灵(basThunder)", "风灵(basWind)", "土灵(basEarth)", "木灵(basWood)"
            };

            sb.AppendLine("【资质/灵根类 (bas*)】参考值:");
            for (int i = 1; i <= 6; i++)
            {
                // 百分比: 1-4, 4-8, 8-12, 12-16, 16-20, 20-24
                int pMin = (i == 1) ? 1 : (i - 1) * 4;
                int pMax = i * 4;

                // 固定值: 境界 * 2 * 品级 (浮动范围)
                int baseFix = gradeId * 2 * i;
                int fMin = (int)(baseFix * 0.9f); // 0.9 - 1.1 浮动
                int fMax = (int)(baseFix * 1.1f);
                if (fMin < 1) fMin = 1;
                if (fMax <= fMin) fMax = fMin + 1;

                string color = GetGradeName(i);
                sb.AppendLine($"  - {color}色(Lv{i}): 百分比[{pMin}-{pMax}%] (写 _0_{pMin}-{pMax}) 或 固定值[{fMin}-{fMax}] (写 _1_{fMin}-{fMax})");
            }
            sb.AppendLine($"  (适用: {string.Join(", ", basNames)})");
            sb.AppendLine();

            // [新增] 背包容量指导 (仅 Carried 类)
            sb.AppendLine("【储物空间 (storage)】参考值 (仅Carried类可用):");
            for (int i = 1; i <= 6; i++)
            {
                // 固定值: 境界 * 10 * 品级
                int baseCap = gradeId * 10 * i;
                int cMin = (int)(baseCap * 0.9f);
                int cMax = (int)(baseCap * 1.1f);

                string color = GetGradeName(i);
                sb.AppendLine($"  - {color}色(Lv{i}): 容量[{cMin}-{cMax}] (写 storage_1_{cMin}-{cMax})");
            }
            sb.AppendLine();

            // 2. 低加成组 (1-6%)
            // 注意：寿命这种属性通常不用百分比，但为了遵循你的公式，这里按玩家最大寿命算
            GenerateRanges("寿命(life)", 100, 2); // 寿命基数设为100岁比较合理，否则百分比太夸张
            GenerateRanges("魅力(beauty)", data.beauty.value, 2);
            GenerateRanges("幸运(luck)", data.luck.value, 2);
            GenerateRanges("声望(reputation)", data.reputation.value, 2);
            GenerateRanges("脚力(fsp)", data.footSpeed.value, 2); 
            GenerateRanges("战斗移速(msp)", data.moveSpeed.value, 2);

            // 3. 价格计算指导
            sb.AppendLine("【推荐价格 (Worth)】:");
            for (int i = 1; i <= 6; i++)
            {
                int minPrice = gradeId * 250 * i;
                int maxPrice = gradeId * 1000 * i;
                sb.AppendLine($"  - {GetGradeName(i)}色(Lv{i}): {minPrice} - {maxPrice} 灵石");
            }

            return sb.ToString();
        }

        private static string GetGradeName(int grade)
        {
            switch (grade) { case 1: return "灰"; case 2: return "绿"; case 3: return "蓝"; case 4: return "紫"; case 5: return "橙"; case 6: return "红"; default: return "未知"; }
        }

        private static string FormatDialogueHistory(List<MessageItem> history, string playerName)
        {
            if (history == null || history.Count == 0) return "玩家在旅途中偶然触发了一次奇遇。";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("以下是奇遇中发生的对话记录：");
            foreach (var msg in history)
            {
                string role = msg.Role == "user" ? playerName : "NPC";
                // 截取过长内容防止Prompt溢出
                string content = msg.Content.Length > 100 ? msg.Content.Substring(0, 100) + "..." : msg.Content;
                sb.AppendLine($"- {role}: {content}");
            }
            return sb.ToString();
        }

        // ====================================================================
        // 核心造物函数 (对应三种类型)
        // ====================================================================

        /// <summary>
        /// 创建气运 (Luck)
        /// </summary>
        public static void CreateLuck(CreationBaseInfo baseInfo, string effectStr, CreationExtraInfo extraInfo, bool isRestoring = false, int fixedId = 0)
        {
            try
            {
                // 1. 生成或使用固定ID
                int destinyId = (isRestoring && fixedId != 0) ? fixedId : GetRandomID(8000000, 8999999, id => g.conf.roleCreateFeature.GetItem(id) != null);
                string effectIds = ParseAndRegisterEffects(effectStr, "Luck");

                // 2. 构建气运数据
                var newFeature = new ConfRoleCreateFeatureItem();

                // [核心修正] 获取源气运模板 (ID 101)
                var sourceFate = g.conf.roleCreateFeature.GetItem(101);
                if (sourceFate != null)
                {
                    newFeature.type = sourceFate.type;   // 复制类型 (通常是1-正面)
                    newFeature.group = sourceFate.group; // 复制分组 (通常是0-不互斥)
                }
                else
                {
                    newFeature.type = 1;
                    newFeature.group = 0;
                }

                // >>> 覆盖自定义字段 <<<
                newFeature.id = destinyId;
                newFeature.name = baseInfo.Name;
                newFeature.tips = baseInfo.Description;
                newFeature.effect = effectIds;
                newFeature.level = baseInfo.Grade;
                newFeature.duration = extraInfo.Duration.ToString(); // int转string

                g.conf.roleCreateFeature._allConfList.Add(newFeature);
                ForceRegisterItem(g.conf.roleCreateFeature, newFeature, newFeature.id);

                // 3. 新造物逻辑 (保持不变)
                if (!isRestoring)
                {
                    SaveItem(new SavedItemData
                    {
                        Type = "Luck",
                        BaseInfo = baseInfo,
                        Effects = effectStr,
                        ExtraInfo = extraInfo,
                        GeneratedID = destinyId
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CreateLuck] 失败: {ex}");
            }
        }

        /// <summary>
        /// 创建消耗品/丹药 (Consumer)
        /// </summary>
        public static void CreateConsumer(CreationBaseInfo baseInfo, string effectStr, CreationExtraInfo extraInfo, bool isRestoring = false, int fixedId = 0)
        {
            try
            {
                // 1. ID与效果
                int itemId = (isRestoring && fixedId != 0) ? fixedId : GetRandomID(9000000, 9499999, id => g.conf.itemProps.GetItem(id) != null);
                string effectIds = ParseAndRegisterEffects(effectStr, "Consumer");
                int refItemId = GetIconId(baseInfo.IconCategory); // 获取参考物品ID
                var refItem = g.conf.itemProps.GetItem(refItemId); // 查表获取配置
                string realIcon = (refItem != null) ? refItem.icon : refItemId.ToString(); // 提取真正的图标文件名

                // 2. 注册 ItemProps (外壳)
                var newProp = new ConfItemPropsItem();
                newProp.id = itemId;
                newProp.name = baseInfo.Name;
                newProp.desc = baseInfo.Description;
                newProp.type = 1; // 消耗品
                newProp.className = 103; // 丹药类
                newProp.level = baseInfo.Grade;
                newProp.worth = extraInfo.Worth;
                newProp.sale = extraInfo.Worth / 2;
                newProp.icon = realIcon;
                newProp.drop = 1;
                newProp.isOverlay = 1;

                g.conf.itemProps._allConfList.Add(newProp);
                ForceRegisterItem(g.conf.itemProps, newProp, newProp.id);

                // 3. 注册 ItemPill (内核) - [核心修正] 严格复刻模板逻辑
                var newPill = new ConfItemPillItem();

                // [获取源丹药模板] 使用参考代码中的 1011271
                var sourcePillData = g.conf.itemPill.GetItem(1011271);
                if (sourcePillData != null)
                {
                    // >>> 复制防崩字段 (Strict Copy) <<<
                    newPill.basType = sourcePillData.basType;
                    newPill.cdGroup = sourcePillData.cdGroup;
                    newPill.operEquip = sourcePillData.operEquip;
                    newPill.autoUse = sourcePillData.autoUse;
                    newPill.spCost = sourcePillData.spCost;
                    newPill.useIgnoreGrade = sourcePillData.useIgnoreGrade;
                    newPill.applyCD = sourcePillData.applyCD;
                    newPill.pillType = sourcePillData.pillType;
                    newPill.fateRandomWeight = sourcePillData.fateRandomWeight;
                    newPill.carryCount = sourcePillData.carryCount;
                    newPill.noUseTogether = sourcePillData.noUseTogether;
                    newPill.cost = sourcePillData.cost;
                    newPill.battleUIHide = sourcePillData.battleUIHide;
                    newPill.consume = sourcePillData.consume;
                }
                else
                {
                    // 保底：万一找不到模板，手动设置一些安全值
                    Debug.LogWarning("[CreateConsumer] 警告：未找到源丹药 1011271，使用保底值");
                    newPill.consume = 1;
                    newPill.operEquip = 1;
                }

                // >>> 覆盖自定义字段 <<<
                newPill.id = itemId;
                newPill.effectValue = effectIds; // 关联生成的属性
                newPill.effectType = 201;        // 必须是201才能支持多重效果字符串
                newPill.basRequire = extraInfo.RealmReq; // 境界需求
                newPill.grade = extraInfo.RealmReq;
                newPill.operUse = 1; // 确保可使用

                g.conf.itemPill._allConfList.Add(newPill);
                ForceRegisterItem(g.conf.itemPill, newPill, newPill.id);

                // 4. 新造物逻辑 (保持不变)
                if (!isRestoring)
                {
                    SaveItem(new SavedItemData
                    {
                        Type = "Consumer",
                        BaseInfo = baseInfo,
                        Effects = effectStr,
                        ExtraInfo = extraInfo,
                        GeneratedID = itemId
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CreateConsumer] 失败: {ex}");
            }
        }

        /// <summary>
        /// 创建装备/坐骑 (Equip)
        /// </summary>
        public static void CreateEquip(CreationBaseInfo baseInfo, string effectStr, CreationExtraInfo extraInfo, bool isRestoring = false, int fixedId = 0)
        {
            try
            {
                // 1. ID与效果
                int itemId = (isRestoring && fixedId != 0) ? fixedId : GetRandomID(9500000, 9999999, id => g.conf.itemProps.GetItem(id) != null);
                string effectIds = ParseAndRegisterEffects(effectStr, "Equip");

                // 2. 选取图标 (默认使用剑)
                int refItemId = GetIconId(baseInfo.IconCategory); // 获取参考物品ID
                var refItem = g.conf.itemProps.GetItem(refItemId); // 查表获取配置
                string realIcon = (refItem != null) ? refItem.icon : refItemId.ToString(); // 提取真正的图标文件名

                // 3. 注册 ItemProps (外壳)
                var newProp = new ConfItemPropsItem();
                newProp.id = itemId;
                newProp.name = baseInfo.Name;
                newProp.desc = baseInfo.Description;
                newProp.type = 3; // 坐骑大类
                newProp.className = 302; // 飞剑
                newProp.level = baseInfo.Grade;
                newProp.worth = extraInfo.Worth;
                newProp.sale = extraInfo.Worth / 2;
                newProp.icon = realIcon;
                newProp.drop = 1;
                newProp.isOverlay = 0; // 装备不可堆叠

                g.conf.itemProps._allConfList.Add(newProp);
                ForceRegisterItem(g.conf.itemProps, newProp, newProp.id);

                // 4. 注册 ItemHorse (坐骑内核)
                var newHorse = new ConfItemHorseItem();
                newHorse.id = itemId;
                newHorse.effectValue = effectIds;
                newHorse.grade = extraInfo.RealmReq;

                // [核心修正] 严格复刻 ExecuteDemoCreation 的逻辑
                // 获取源数据 (ID 3021041) 以保持模型和特性与游戏原生一致，防止崩溃
                var sourceHorseData = g.conf.itemHorse.GetItem(3021041);
                if (sourceHorseData != null)
                {
                    newHorse.model = sourceHorseData.model;      // 复制正确的模型
                    newHorse.feature = sourceHorseData.feature;  // 复制正确的特性 (防止 "1" 这种空指针崩溃)
                }
                else
                {
                    // 极低概率保底：如果找不到 3021041，则给个安全值
                    Debug.LogWarning("[CreateEquip] 警告：未找到源坐骑 3021041，使用默认安全值");
                    newHorse.model = "dao_12";
                    newHorse.feature = "0"; // 0 表示无特性，是安全的
                }

                g.conf.itemHorse._allConfList.Add(newHorse);
                ForceRegisterItem(g.conf.itemHorse, newHorse, newHorse.id);

                // 5. 新造物逻辑
                if (!isRestoring)
                {
                    SaveItem(new SavedItemData
                    {
                        Type = "Equip",
                        BaseInfo = baseInfo,
                        Effects = effectStr,
                        ExtraInfo = extraInfo,
                        GeneratedID = itemId
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CreateEquip] 失败: {ex}");
            }
        }


        public static void CreateRing(CreationBaseInfo baseInfo, string effectStr, CreationExtraInfo extraInfo, bool isRestoring = false, int fixedId = 0)
        {
            try
            {
                // 1. 生成ID (优先固定ID)
                int itemId = (fixedId != 0) ? fixedId : GetRandomID(9800000, 9899999, id => g.conf.itemProps.GetItem(id) != null);

                // 2. 翻译官逻辑 (处理 storage)
                var parts = effectStr.Split('|');
                var normalEffects = new List<string>();
                int addedCapacity = 0;
                foreach (var p in parts)
                {
                    if (string.IsNullOrWhiteSpace(p)) continue;
                    if (p.StartsWith("storage_") || p.StartsWith("capacity_"))
                    {
                        var segs = p.Split('_');
                        if (segs.Length >= 3 && int.TryParse(segs[2], out int val)) addedCapacity += val;
                    }
                    else
                    {
                        normalEffects.Add(p);
                    }
                }
                string effectIds = ParseAndRegisterEffects(string.Join("|", normalEffects), "Ring");

                // 3. 图标逻辑 (只偷图标字符串，不影响内核)
                string targetIcon = "3011011"; // 默认保底图标

                // 从 AI 指定的分类中随机找一个物品，把它的图标偷过来
                string searchCategory = IconMapper.ContainsKey(baseInfo.IconCategory) ? baseInfo.IconCategory : "Item_Ring";
                if (IconMapper.ContainsKey(searchCategory))
                {
                    var list = IconMapper[searchCategory];
                    if (list != null && list.Count > 0)
                    {
                        int randomRefId = list[_rng.Next(list.Count)];
                        var refProp = g.conf.itemProps.GetItem(randomRefId);
                        if (refProp != null && !string.IsNullOrEmpty(refProp.icon))
                        {
                            targetIcon = refProp.icon; // 拿到图标文件名为止，不用管它是剑还是书
                        }
                    }
                }

                // 4. 注册外壳 (ItemProps)
                var newProp = new ConfItemPropsItem();
                // 强制指定为戒指类型，确保显示正确
                newProp.type = 3;
                newProp.className = 301;
                newProp.isOverlay = 0;
                newProp.dieDrop = 1;
                newProp.icon = targetIcon; // 应用随机到的图标

                newProp.id = itemId;
                newProp.name = baseInfo.Name;
                newProp.desc = baseInfo.Description;
                newProp.level = baseInfo.Grade;
                newProp.worth = extraInfo.Worth;
                newProp.sale = extraInfo.Worth / 2;
                newProp.drop = 1;

                g.conf.itemProps._allConfList.Add(newProp);
                ForceRegisterItem(g.conf.itemProps, newProp, newProp.id);

                // ================= 核心：RingBase (JSON手术) =================
                // 这里的原则是：雷打不动，只用 3011011 做模板

                var ringBaseProp = g.conf.GetType().GetProperty("ringBase", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                object ringMgr = ringBaseProp.GetValue(g.conf);

                MethodInfo getItemMethod = null;
                Type curType = ringMgr.GetType();
                while (curType != null)
                {
                    getItemMethod = curType.GetMethod("GetItem", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    if (getItemMethod != null) break;
                    curType = curType.BaseType;
                }

                // 锁定模板为 3011011
                object sourceRing = getItemMethod.Invoke(ringMgr, new object[] { 3011011 });
                if (sourceRing == null) throw new Exception("严重错误：核心模板 3011011 缺失");

                // JSON 克隆 & ID 替换
                string json = JsonConvert.SerializeObject(sourceRing);

                var tSrc = sourceRing.GetType();
                var fId = tSrc.GetField("id") ?? tSrc.GetField("_id");
                int srcId = 3011011;
                if (fId != null) srcId = Convert.ToInt32(fId.GetValue(sourceRing));
                else { var pId = tSrc.GetProperty("id"); if (pId != null) srcId = Convert.ToInt32(pId.GetValue(sourceRing, null)); }

                json = json.Replace($"\"id\":{srcId}", $"\"id\":{itemId}");
                json = json.Replace($"\"_id\":{srcId}", $"\"_id\":{itemId}");

                object newRing = JsonConvert.DeserializeObject(json, sourceRing.GetType());

                // 修改属性
                var t = newRing.GetType();
                t.GetField("grade")?.SetValue(newRing, extraInfo.RealmReq);
                t.GetProperty("grade")?.SetValue(newRing, extraInfo.RealmReq, null);

                var fEffect = t.GetField("effectValue");
                if (fEffect != null) fEffect.SetValue(newRing, effectIds);
                else t.GetProperty("effectValue")?.SetValue(newRing, effectIds, null);

                // 应用容量修改 (翻译官执行结果)
                if (addedCapacity > 0)
                {
                    var fCap = t.GetField("capacity");
                    if (fCap != null)
                    {
                        int baseCap = Convert.ToInt32(fCap.GetValue(newRing));
                        fCap.SetValue(newRing, baseCap + addedCapacity);
                    }
                    else
                    {
                        var pCap = t.GetProperty("capacity");
                        if (pCap != null)
                        {
                            int baseCap = Convert.ToInt32(pCap.GetValue(newRing, null));
                            pCap.SetValue(newRing, baseCap + addedCapacity, null);
                        }
                    }
                }

                // 注册
                MethodInfo addItemMethod = null;
                curType = ringMgr.GetType();
                while (curType != null)
                {
                    var methods = curType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    foreach (var m in methods)
                    {
                        if (m.Name == "AddItem" && m.GetParameters().Length == 1)
                        {
                            addItemMethod = m;
                            goto FoundAdd;
                        }
                    }
                    curType = curType.BaseType;
                }
            FoundAdd:
                if (addItemMethod != null) addItemMethod.Invoke(ringMgr, new object[] { newRing });
                else Debug.LogError("[CreateRing] AddItem 失败");

                // ==========================================================

                if (!isRestoring)
                {
                    SaveItem(new SavedItemData { Type = "Ring", BaseInfo = baseInfo, Effects = effectStr, ExtraInfo = extraInfo, GeneratedID = itemId });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CreateRing] 失败: {ex}");
            }
        }

        // ====================================================================
        // 辅助逻辑函数
        // ====================================================================

        /// <summary>
        /// 解析效果字符串，注册RoleEffect，返回ID串
        /// 格式: "atk_0_10|def_1_20"
        /// </summary>
        private static string ParseAndRegisterEffects(string effectStr, string creationType)
        {
            if (string.IsNullOrEmpty(effectStr)) return "";

            List<string> registeredIds = new List<string>();
            string[] parts = effectStr.Split('|');

            foreach (var part in parts)
            {
                if (string.IsNullOrWhiteSpace(part)) continue;

                int effectId = GetRandomID(8900000, 8999999, id => g.conf.roleEffect.GetItem(id) != null);

                var newEffect = new ConfRoleEffectItem();
                newEffect.id = effectId;

                // --- 核心修改：气运和装备使用类型 1，丹药使用类型 101 ---
                if (creationType == "Luck" || creationType == "Equip" || creationType == "Ring")
                {
                    newEffect.effectType = 1;
                }
                else
                {
                    newEffect.effectType = 101;
                }
                // ---------------------------------------------------

                newEffect.value = part;

                g.conf.roleEffect._allConfList.Add(newEffect);
                ForceRegisterItem(g.conf.roleEffect, newEffect, newEffect.id);

                registeredIds.Add(effectId.ToString());
            }

            return string.Join("|", registeredIds);
        }

        public static void RemoveAllAICustomLuck()
        {
            if (g.world.playerUnit == null) return;

            // 收集所有匹配 AI ID 范围的气运 ID
            var lucksToRemove = new System.Collections.Generic.List<int>();
            foreach (var luck in g.world.playerUnit.allLuck)
            {
                // 逻辑：检查 ID 是否在 AI 生成的范围内
                if (luck.luckConf.id >= 8000000 && luck.luckConf.id <= 8999999)
                {
                    lucksToRemove.Add(luck.luckConf.id);
                }
            }

            // 执行移除
            foreach (int id in lucksToRemove)
            {
                g.world.playerUnit.CreateAction(new UnitActionLuckDel(id));
            }

            Debug.Log($"[CreationSystem] 已尝试移除 {lucksToRemove.Count} 个 AI 气运。");
            UITipItem.AddTip($"天道肃清：已移除 {lucksToRemove.Count} 个生成气运", 3f);
        }

        /// <summary>
        /// 强制注册物品到游戏配置字典 (反射)
        /// </summary>
        public static void ForceRegisterItem(object confManager, object newItem, int id)
        {
            try
            {
                var type = confManager.GetType();
                var prop = type.GetProperty("allConfDic", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

                if (prop != null)
                {
                    var dicObj = prop.GetValue(confManager);
                    if (dicObj != null)
                    {
                        var addMethod = dicObj.GetType().GetMethod("Add");
                        var containsKeyMethod = dicObj.GetType().GetMethod("ContainsKey");

                        if (addMethod != null && containsKeyMethod != null)
                        {
                            bool exists = (bool)containsKeyMethod.Invoke(dicObj, new object[] { id });
                            if (!exists)
                            {
                                addMethod.Invoke(dicObj, new object[] { id, newItem });
                            }
                            return;
                        }
                    }
                }
                // Fallback for field access if property fails
                var field = type.GetField("_allConfDic", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                if (field != null)
                {
                    var dicObj = field.GetValue(confManager);
                    if (dicObj != null)
                    {
                        var addMethod = dicObj.GetType().GetMethod("Add");
                        if (addMethod != null) addMethod.Invoke(dicObj, new object[] { id, newItem });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ForceRegister] 注册 ID {id} 异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 生成唯一随机ID
        /// </summary>
        private static int GetRandomID(int min, int max, Func<int, bool> checkExists)
        {
            int maxAttempts = 1000;
            for (int i = 0; i < maxAttempts; i++)
            {
                int id = _rng.Next(min, max);
                if (!checkExists(id))
                {
                    return id;
                }
            }
            Debug.LogError($"[GetRandomID] 无法在 {min}-{max} 范围内找到空闲ID!");
            return min; // 失败保底
        }

        /// <summary>
        /// 获取图标ID
        /// </summary>
        private static int GetIconId(string category)
        {
            if (string.IsNullOrEmpty(category) || !IconMapper.ContainsKey(category))
            {
                category = "Unknown";
            }
            var list = IconMapper[category];
            return list[_rng.Next(list.Count)];
        }

        /// <summary>
        /// 保存新物品到本地JSON
        /// </summary>
        private static void SaveItem(SavedItemData data)
        {
            try
            {
                List<SavedItemData> list = new List<SavedItemData>();

                // 确保目录存在
                string dir = Path.GetDirectoryName(SavePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                // 读取旧数据
                if (File.Exists(SavePath))
                {
                    try
                    {
                        string oldJson = File.ReadAllText(SavePath);
                        var oldList = JsonConvert.DeserializeObject<List<SavedItemData>>(oldJson);
                        if (oldList != null) list = oldList;
                    }
                    catch { }
                }

                // 添加新数据
                list.Add(data);

                // 写入
                string newJson = JsonConvert.SerializeObject(list, Formatting.Indented);
                File.WriteAllText(SavePath, newJson);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveItem] 保存失败: {ex}");
            }
        }
        /// <summary>
        /// 强制注册物品到游戏配置字典的辅助函数
        /// </summary>
    

        /// <summary>
        /// 执行演示用的造物逻辑（包含全能丹、万神飞剑、创世气运）
        /// 原 ModMain.OnPlayerMove 中的核心逻辑
        /// </summary>
        public static void ExecuteDemoCreation()
        {
            try
            {
                UITipItem.AddTip("正在执行 AI 造物 (V40.0)...", 3f);
                Debug.Log(">>> [ModMain] 开始 AI 造物流程 (V40.0)");

                // =============================================================
                // 0. 【定义全属性字典】(集成 V39 最终验证版)
                // =============================================================
                var attrDict = new Dictionary<string, string>()
                {
                    // --- 核心六维 ---
                    { "atk", "攻击" }, { "def", "防御" },
                    { "hpMax", "体力上限" }, { "mpMax", "灵力上限" }, { "spMax", "念力上限" },
                    
                    // --- RPG 属性 ---
                    { "life", "寿命" },
                    { "beauty", "魅力" },
                    { "luck", "幸运" },
                    { "reputation", "声望" },
                    
                    // --- 战斗高级属性 ---
                    { "crit", "会心" },
                    { "guard", "护心" },
                    { "fsp", "脚力" },    // 大地图移速
                    { "msp", "战斗移速" }, 

                    // --- 12种资质 ---
                    { "basSword", "剑法" }, { "basSpear", "枪法" }, { "basBlade", "刀法" },
                    { "basFist", "拳法" }, { "basPalm", "掌法" }, { "basFinger", "指法" },
                    { "basFire", "火灵" }, { "basFroze", "水灵" }, { "basThunder", "雷灵" },
                    { "basWind", "风灵" }, { "basEarth", "土灵" }, { "basWood", "木灵" }
                };

                // 用于存储所有生成的 RoleEffect ID，最后喂给飞剑
                List<string> validEffectIds = new List<string>();

                int validCount = 0;
                int index = 0;

                // 源数据引用
                var sourcePropPill = g.conf.itemProps.GetItem(1011271);
                var sourcePillData = g.conf.itemPill.GetItem(1011271);
                var sourcePropFruit = g.conf.itemProps.GetItem(5081015);

                if (sourcePropPill != null && sourcePillData != null && sourcePropFruit != null)
                {
                    // =========================================================
                    // 步骤 A: 循环注册所有独立的 RoleEffect
                    // =========================================================
                    foreach (var kvp in attrDict)
                    {
                        string key = kvp.Key;

                        // 智能试毒
                        string realName = "";
                        try { realName = g.conf.roleEffect.GetValueName(key); } catch { }

                        if (string.IsNullOrEmpty(realName))
                        {
                            Debug.LogError($"[V41过滤] ❌ 剔除无效 Key: '{key}'");
                            continue;
                        }

                        int roleEffectId = 8898500 + index; // 独立的 Effect ID

                        // 数值设定
                        int val = 100;
                        if (key == "life") val = 120; // 10年
                        if (key == "beauty" || key == "fsp") val = 500;

                        // 注册独立的 RoleEffect (Type 101)
                        var effectPill = new ConfRoleEffectItem();
                        effectPill.id = roleEffectId;
                        effectPill.value = $"{key}_1_{val}";
                        effectPill.effectType = 101;
                        g.conf.roleEffect._allConfList.Add(effectPill);
                        ForceRegisterItem(g.conf.roleEffect, effectPill, effectPill.id);

                        // 将合法的 ID 加入列表
                        validEffectIds.Add(roleEffectId.ToString());

                        validCount++;
                        index++;
                    }

                    // =========================================================
                    // 步骤 B: 生成【唯一】一颗全属性丹药
                    // =========================================================
                    if (validCount > 0)
                    {
                        int pillId = 8888002;
                        // [核心修改] 将所有 RoleEffect ID 用竖线连接
                        // 效果：吃一颗药，触发这里面所有的 Effect
                        string multiPillString = string.Join("|", validEffectIds);

                        // 1. 注册 ItemProps (外壳)
                        var newProp = new ConfItemPropsItem();
                        newProp.type = 1;
                        newProp.className = 103;
                        newProp.sale = 1000;
                        newProp.worth = 99999; // 价值连城
                        newProp.level = 5;
                        newProp.isOverlay = 1;
                        newProp.drop = 1;
                        newProp.dieDrop = sourcePropPill.dieDrop;
                        newProp.icon = sourcePropFruit.icon;

                        newProp.id = pillId;
                        newProp.name = "赛博全能丹";
                        newProp.desc = $"AI生成的终极丹药。\n食用后增加 {validCount} 种属性 (含脚力/魅力/全资质)。";

                        g.conf.itemProps._allConfList.Add(newProp);
                        ForceRegisterItem(g.conf.itemProps, newProp, newProp.id);

                        // 2. 注册 ItemPill (内核 - 严格复刻 V36/V40 防崩字段)
                        var newPill = new ConfItemPillItem();
                        newPill.basType = sourcePillData.basType;
                        newPill.basRequire = sourcePillData.basRequire;
                        newPill.cdGroup = sourcePillData.cdGroup;
                        newPill.grade = sourcePillData.grade;
                        newPill.operEquip = sourcePillData.operEquip;
                        newPill.autoUse = sourcePillData.autoUse;
                        newPill.spCost = sourcePillData.spCost;
                        newPill.useIgnoreGrade = sourcePillData.useIgnoreGrade;
                        newPill.applyCD = sourcePillData.applyCD;
                        newPill.pillType = sourcePillData.pillType;
                        newPill.fateRandomWeight = sourcePillData.fateRandomWeight;
                        newPill.carryCount = sourcePillData.carryCount;
                        newPill.noUseTogether = sourcePillData.noUseTogether;
                        newPill.cost = sourcePillData.cost;
                        newPill.battleUIHide = sourcePillData.battleUIHide;
                        newPill.consume = sourcePillData.consume;

                        newPill.id = pillId;
                        newPill.operUse = 1;
                        newPill.effectType = 201;
                        // [关键] 这里填入 ID 串 (例如 "8898500|8898501|...")
                        newPill.effectValue = multiPillString;

                        g.conf.itemPill._allConfList.Add(newPill);
                        ForceRegisterItem(g.conf.itemPill, newPill, newPill.id);
                    }
                }


                // =============================================================
                // 2. 【万神飞剑】(使用 ID|ID|ID 形式关联所有效果)
                // =============================================================
                if (validCount > 0)
                {
                    int mountId = 8888999;
                    // [核心] 将所有生成的 RoleEffect ID 用竖线连接
                    // 结果形如: "8898500|8898501|8898502..."
                    string multiEffectString = string.Join("|", validEffectIds);

                    // 2.1 物品数据 (严格复刻，去除了可能报错的字段)
                    var sourceHorseProp = g.conf.itemProps.GetItem(3021041);
                    var sourceHorseData = g.conf.itemHorse.GetItem(3021041);

                    if (sourceHorseProp != null && sourceHorseData != null)
                    {
                        // 外壳
                        var newPropHorse = new ConfItemPropsItem();
                        newPropHorse.type = sourceHorseProp.type;
                        newPropHorse.className = sourceHorseProp.className;
                        newPropHorse.icon = sourceHorseProp.icon;
                        newPropHorse.level = 1;
                        newPropHorse.isOverlay = 0;
                        newPropHorse.drop = 1;
                        newPropHorse.worth = 99999;
                        newPropHorse.sale = 5000;

                        newPropHorse.id = mountId;
                        newPropHorse.name = "万神飞剑 (V40)";
                        newPropHorse.desc = $"聚合了 {validCount} 种属性的神器。\n包含: 脚力、魅力、全资质等。";

                        g.conf.itemProps._allConfList.Add(newPropHorse);
                        ForceRegisterItem(g.conf.itemProps, newPropHorse, newPropHorse.id);

                        // 内核 (字段严格依照你的要求)
                        var newHorseData = new ConfItemHorseItem();
                        newHorseData.model = sourceHorseData.model;
                        newHorseData.feature = sourceHorseData.feature; // 关键
                        newHorseData.grade = sourceHorseData.grade;

                        newHorseData.id = mountId;

                        // [关键] 这里直接填入 ID 串，而不是指向另一个 RoleEffect
                        // 这完全符合官方 "30210111|30210112" 的格式
                        newHorseData.effectValue = multiEffectString;

                        g.conf.itemHorse._allConfList.Add(newHorseData);
                        ForceRegisterItem(g.conf.itemHorse, newHorseData, newHorseData.id);

                        var horseList = new Il2CppSystem.Collections.Generic.List<DataProps.PropsData>();
                        horseList.Add(DataProps.PropsData.NewProps(mountId, 1));
                    }


                    // =============================================================
                    // 3. 【创世气运】(修复被吞部分，同样使用 ID 串)
                    // =============================================================
                    var sourceFate = g.conf.roleCreateFeature.GetItem(101);
                    if (sourceFate != null)
                    {
                        var newFeature = new ConfRoleCreateFeatureItem();
                        newFeature.type = sourceFate.type;
                        newFeature.level = 1;
                        newFeature.group = sourceFate.group;
                        newFeature.duration = sourceFate.duration;

                        newFeature.id = 8888001;
                        newFeature.name = "AI创世之力";

                        // 气运也支持 "ID|ID" 格式
                        newFeature.effect = multiEffectString;
                        newFeature.tips = "汇聚了赛博空间的终极力量，全属性生效。";

                        g.conf.roleCreateFeature._allConfList.Add(newFeature);
                        ForceRegisterItem(g.conf.roleCreateFeature, newFeature, newFeature.id);
                    }
                }

                Debug.Log(">>> [ModMain] 造物流程结束 (V40.0)");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Creation] 造物异常: {ex}");
                UITipItem.AddTip("造物发生错误", 5f);
            }
        }
    }
    [Serializable]
    public class AICreationResponse
    {
        public string Type; // "Luck", "Consumer", "Equip"
        public CreationBaseInfo BaseInfo;
        public string Effects;
        public CreationExtraInfo ExtraInfo;
    }
    [Serializable]
    public class SavedItemData
    {
        public string Type; // "Luck", "Consumer", "Equip"
        public CreationBaseInfo BaseInfo;
        public string Effects; // "atk_0_10|..."
        public CreationExtraInfo ExtraInfo;
        public int GeneratedID; // 记录当时生成的 ID，用于恢复
    }

    [Serializable]
    public class CreationBaseInfo
    {
        public string Name;
        public int Grade; // 1-6
        public string IconCategory; // "Sword"
        public string Description;
    }

    [Serializable]
    public class CreationExtraInfo
    {
        public int Worth; // 售价
        public int RealmReq; // 境界要求 1-10
        public int Duration; // 持续时间 (仅气运)
    }
}