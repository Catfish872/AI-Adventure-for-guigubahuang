using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Text;
using System.Linq;
using static MOD_kqAfiU.Tools;
using System.Web.WebPages;
using UnhollowerBaseLib;

namespace MOD_kqAfiU
{
    [Serializable]
    public class ShortEventOption
    {
        public string text { get; set; }
        public string ending { get; set; }
        public string reward { get; set; }

        public ShortEventOption()
        {
            text = "";
            ending = "";
            reward = "";
        }
    }

    [Serializable]
    public class ShortEventResponse
    {
        public string content { get; set; }
        public List<ShortEventOption> options { get; set; }

        public ShortEventResponse()
        {
            content = "";
            options = new List<ShortEventOption>();
        }
    }

    public class ShortEvent
    {
        public static Il2CppSystem.Collections.Generic.List<DataProps.PropsData> rewardItems = null;
        // 短奇遇类型列表
        public static readonly List<string> HardcodedShortEventTypes = new List<string>
{
    "灵药采集(发现珍稀灵草/药材成熟/灵果显现)",
    "宝物发现(古物出土/法器遗落/灵石矿脉)",
    "机缘巧合(功法残页/修行感悟/境界顿悟)",
    "自然奇观(灵气汇聚/天象异变/地脉共鸣)",
    "遗迹探索(古阵残留/洞府入口/秘境裂缝)",
    "灵兽踪迹(幼兽孤立/灵兽留宝/妖兽巢穴)",
    "天材地宝(灵石结晶/仙金显露/神木枝叶)",
    "修行契机(打坐入定/内息调和/经脉贯通)",
    "神秘事件(空间波动/时光逆流/因果纠缠)",
    "环境机遇(灵泉涌现/仙雾弥漫/星辰照耀)",
    "人际遇合(前辈指点/同道相助/宿敌和解)",
    "战斗试炼(强敌挑战/生死磨砺/武技精进)",
    "交易机会(奇珍拍卖/以物易物/商贾奇货)",
    "师承传授(名师收徒/道统传承/秘法相授)",
    "险境脱困(绝地逢生/危中求机/劫后重生)",
    "符箓制作(古符图案/灵墨调配/符纸感应)",
    "炼丹机缘(丹方获得/炼丹感悟/丹劫显现)",
    "阵法领悟(阵图参悟/阵眼洞察/阵势共鸣)",
    "风水宝地(龙脉交汇/穴位显露/气运汇聚)",
    "异界接触(界壁松动/异域信息/跨界交流)",
    "灵魂感应(神识共鸣/灵魂碎片/意念传承)",
    "血脉觉醒(古血复苏/血脉共鸣/先祖传承)",
    "法则领悟(大道感应/规则洞察/天道共鸣)",
    "劫难化机(雷劫淬体/心魔转化/厄运逆转)",
    "神通显现(天赋觉醒/神通初现/异能突破)",
    "时空异常(时间停滞/空间折叠/维度交错)",
    "元素共鸣(五行调和/阴阳平衡/元素融合)",
    "器灵沟通(器魂苏醒/灵器认主/器灵指引)",
    "业力因果(善恶报应/宿命纠缠/因果循环)",
    "禁制破解(封印松动/禁制漏洞/封锁破除)",
    "传说现世(神话重现/传说验证/古老预言)",
    "能量暴走(灵力失控/真气逆流/能量共振)",
    "记忆碎片(前世回忆/他人记忆/时空记忆)",
    "维度裂缝(空间裂隙/次元通道/平行世界)",
    "道心磨砺(心境考验/意志试炼/道心坚固)"
};

        public static List<string> ShortEventTypes = InitializeShortEventTypes();

        private static List<string> InitializeShortEventTypes()
        {
            try
            {
                ModConfig config = Config.ReadConfig();
                if (config != null && config.ShortEventTypes != null && config.ShortEventTypes.Count > 0)
                {
                    return config.ShortEventTypes;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"读取短奇遇事件类型配置失败，使用默认值: {ex.Message}");
            }
            return HardcodedShortEventTypes;
        }

        public static readonly string HardcodedShortEventPrompt = @"
你是修仙世界的场景描述者，专门创造短暂但精彩的奇遇体验。
【世界观】
建立《鬼谷八荒》修仙世界观，规范境界、地图、道具逻辑，确保设定一致。
境界划分：炼气、筑基、结晶、金丹、具灵、元婴、化神、悟道、羽化、登仙，每境分初、中、后期。
地图解锁顺序：白源区→永宁州→雷泽→华封州→十万大山→云陌州→永恒冰原→暮仙州→迷途荒漠→赤幽州→天元山。
【短奇遇特点】
- 无NPC参与，专注环境、物品、突发事件
- 快节奏，一次选择即结束
- 重在选择的后果差异和风险收益
- 获得的物品/效果直接且明确，且必须是上下文中明确出现的物品/效果，禁止想象不存在的物品和效果。
【创作要求】
1. 开场描述要有画面感，让玩家身临其境，融入修仙世界氛围。但是游戏剧情在此之前就开始了，所以不需要介绍玩家身份。
2. 提供2-4个明确的选择方向，每个选择风险收益不同,可能有收获，也可能一无所获（get为空代表无论从物质还是精神上都一无所获）
3. 每个选择的结果要有惊喜感，避免平淡无奇
4. 文风古雅但不晦涩，通俗易懂，符合修仙语境
5. 将获得的物品/效果自然融入剧情，不要生硬堆砌（只需要填写获得的物品/效果的完整名称，不要填写数量）";

        public static string ShortEventPrompt = InitializeShortEventPrompt();

        private static string InitializeShortEventPrompt()
        {
            try
            {
                ModConfig config = Config.ReadConfig();
                if (config != null && !string.IsNullOrEmpty(config.ShortEventPrompt))
                {
                    return config.ShortEventPrompt;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"读取短奇遇提示词配置失败，使用默认值: {ex.Message}");
            }
            return HardcodedShortEventPrompt;
        }

        public static string ShortEventFormat = @"
【输出格式】必须严格按照以下HTML格式：
<content>场景描述，营造代入感，描述玩家遇到的情况和可以做出的选择，在200字左右</content>
<option1>第一个选择，简洁明确，15字以内</option1>
<end1>选择1的结果描述，要有画面感和代入感</end1>
<get1>选择1的物品/效果完整名称（不含数量），用逗号分隔，无则留空</get1>
<option2>第二个选择，简洁明确，15字以内</option2>
<end2>选择2的结果描述，要有画面感和代入感</end2>
<get2>选择2的物品/效果完整名称（不含数量），用逗号分隔，无则留空</get2>
<option3>第三个选择，简洁明确，15字以内</option3>
<end3>选择3的结果描述，要有画面感和代入感</end3>
<get3>选择3的物品/效果完整名称（不含数量），用逗号分隔，无则留空</get3>
<option4>第四个选择，简洁明确，15字以内</option4>
<end4>选择4的结果描述，要有画面感和代入感</end4>
<get4>选择4的物品/效果完整名称（不含数量），用逗号分隔，无则留空</get4>

注意：option3/end3/get3和option4/end4/get4为可选，至少要有2个选项，最多4个选项。如果只有2-3个选项，不要输出空的option4等标签。每个选项的物品/效果禁止重复。
";

        public static void TriggerShortEvent()
        {
            try
            {
                // 获取存储的短奇遇内容
                string content = g.data.dataObj.data.GetString("ShortEventContent");
                if (string.IsNullOrEmpty(content))
                {
                    Debug.Log("没有找到存储的短奇遇内容");
                    return;
                }

                // 解析并创建对话
                var eventResponse = ParseShortEventResponse(content);
                if (eventResponse != null)
                {
                    CreateShortEventDialogue(eventResponse);
                }
                else
                {
                    UITipItem.AddTip("短奇遇解析失败", 2f);
                    // 清除无效内容
                    g.data.dataObj.data.SetString("ShortEventContent", "");
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"触发短奇遇失败: {ex.Message}");
                // 清除内容并重置状态
                g.data.dataObj.data.SetString("ShortEventContent", "");
            }
        }

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

                // 首先从ShortEvent的rewardItems中查找所有物品
                if (ShortEvent.rewardItems != null)
                {
                    for (int i = 0; i < ShortEvent.rewardItems.Count; i++)
                    {
                        DataProps.PropsData rewardItem = ShortEvent.rewardItems[i];
                        if (rewardItem.propsInfoBase != null && rewardItem.propsInfoBase.name == trimmedName)
                        {
                            // 找到匹配的物品，添加到当前奖励列表
                            rewardItems.Add(rewardItem);
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

        public static void RequestShortEvent()
        {
            try
            {
                // 获取随机短奇遇类型
                System.Random random = new System.Random();
                string selectedEventType = ShortEventTypes[random.Next(ShortEventTypes.Count)];

                // 获取奖励信息
                Dictionary<string, int> rewardInfo;
                var tempRewardItems = Prop.GetShortEventRewards(out rewardInfo);

                // 存储到静态字段供GiveRewardSingle使用
                ShortEvent.rewardItems = tempRewardItems;


                // 构建奖励信息字符串
                string rewardsString = "【可能的奖励】\n";
                foreach (var item in rewardInfo)
                {
                    rewardsString += $"- {item.Key}: {item.Value}个\n";
                }
                rewardsString += "\n请将部分奖励融入到选项结果中，不是每个选项都需要有奖励。";

                // 获取玩家信息
                int playerLogCount = Prop.GetLogCountFromSSCYAI(g.world.playerUnit);
                string playerInfo = GetCompleteInfoShort(g.world.playerUnit);

                // 创建请求
                var request = new LLMDialogueRequest();
                var sysstr = ShortEventPrompt + "\n" + $"【玩家信息】\n{playerInfo}" + "\n" + $"{ShortEventFormat}\n" + $"【奇遇类型】{selectedEventType}\n" + rewardsString;
                request.AddSystemMessage(sysstr);
                request.AddUserMessage("请为我生成一个短奇遇，必须严格按照HTML格式返回。");

                // 设置等待状态
                ModMain.waitingForShortEventResponse = true;
                ModMain.llmRequestStartTime = Time.time;

                // 发送请求
                Tools.SendLLMRequest(request, (response) => {
                    ModMain.pendingShortEventResponse = response;
                });
            }
            catch (Exception ex)
            {
                Debug.Log($"触发短奇遇失败: {ex.Message}");
                ModMain.waitingForShortEventResponse = false;
            }
        }



        public static string GetNPCInfoString(WorldUnitBase unit)
        {
            if (unit == null)
            {
                return "错误：无效的NPC单位";
            }

            try
            {
                StringBuilder info = new StringBuilder();

                // 1. 基本身份信息
                string name = unit.data.unitData.propertyData.GetName();
                string gender = ((int)unit.data.unitData.propertyData.sex == 1) ? "男" : "女";
                string realm = Prop.MartialUtil.getGradeName(unit);
                int age = unit.data.unitData.propertyData.age / 12; // 游戏中年龄以月为单位

                // 种族信息
                string race = "人族";
                int raceId;
                if (unit.data.unitData.objData.ContainsKey("roleRace"))
                {
                    raceId = unit.data.unitData.objData.GetInt("roleRace");
                }
                else
                {
                    raceId = unit.data.dynUnitData.race.baseValue;
                }
                ConfRoleRaceItem raceItem = g.conf.roleRace.GetItem(raceId);
                if (raceItem != null)
                {
                    race = GameTool.LS(raceItem.race);
                }

                // 特殊身份
                string identity = "普通";
                if (UnitConditionTool.Condition("superHero_0_1", new UnitConditionData(unit, null)))
                {
                    identity = "天骄";
                }

                // 2. 社会关系信息
                // 宗门与职位
                string sect = (unit.data.school == null) ? "散修" : unit.data.school.name;
                if (unit.data.school != null && unit.data.school.schoolData != null)
                {
                    string position = unit.data.school.schoolData.GetPostTypeName(unit.data.unitData.unitID);
                    if (!string.IsNullOrEmpty(position))
                    {
                        sect += position;
                    }
                }

                // 当前位置
                string location = Prop.MartialUtil.getAreaName(unit);

               

                // 婚姻状况
                string marriageStatus = string.IsNullOrEmpty(unit.data.unitData.relationData.married)
                    ? "未婚"
                    : ("已婚\t" + unit.data.unitData.relationData.lover.Count.ToString() + "个道侣");

                // 3. 性格和爱好
                // 性格特质
                List<string> characterTraits = new List<string>();
                foreach (int id in unit.data.GetCharacter())
                {
                    characterTraits.Add(Prop.MartialUtil.GetCharacterName(id));
                }
                string characterStr = string.Join(" ", characterTraits);
                if (StringExtensions.IsEmpty(characterStr))
                {
                    characterStr = "无";
                }

                // 爱好
                List<string> hobbies = new List<string>();
                foreach (ConfRoleCreateHobbyItem item in unit.data.GetHobbyItems())
                {
                    hobbies.Add(GameTool.LS(item.name));
                }
                string hobbyStr = string.Join(" ", hobbies);
                if (StringExtensions.IsEmpty(hobbyStr))
                {
                    hobbyStr = "无";
                }

                // 4. 状态与能力信息
                // 近期烦恼
                List<string> troubles = new List<string>();
                foreach (UnitTroubleBase trouble in unit.allTroubles)
                {
                    troubles.Add(GameTool.LS(trouble.GetDesc()));
                }
                string troubleStr = string.Join(" ", troubles);
                if (StringExtensions.IsEmpty(troubleStr))
                {
                    troubleStr = "无";
                }

                // 能力值
                int beauty = unit.data.unitData.propertyData.beauty;
                int power = FormulaTool.UnitPower.TotalPower(unit.data);
                int mood = unit.data.unitData.propertyData.mood;
                int energy = unit.data.unitData.propertyData.energy;
                int reputation = unit.data.unitData.propertyData.reputation;
 

                // 6. 功法与装备
                // 气运
                List<string> luckList = new List<string>();
                foreach (WorldUnitLuckBase luck in unit.allLuck)
                {

                    luckList.Add(GameTool.LS(luck.luckConf.name));
                }
                string luckStr = string.Join(" ", luckList);
                if (StringExtensions.IsEmpty(luckStr))
                {
                    luckStr = "无";
                }

                // 功法信息
                List<string> martialList = new List<string>();
                DataUnit.UnitInfoData unitData = unit.data.unitData;

                // 武技
                DataUnit.ActionMartialData actionMartial = unitData.GetActionMartial(unitData.skillLeft);
                if (actionMartial != null)
                {
                    DataProps.MartialData martialData = actionMartial.data.To<DataProps.MartialData>();
                    martialList.Add("武技为" + GameTool.LS(martialData.martialInfo.name));
                }

                // 绝技
                DataUnit.ActionMartialData actionMartial2 = unitData.GetActionMartial(unitData.skillRight);
                if (actionMartial2 != null)
                {
                    DataProps.MartialData martialData = actionMartial2.data.To<DataProps.MartialData>();
                    martialList.Add("绝技为" + GameTool.LS(martialData.martialInfo.name));
                }

                // 身法
                DataUnit.ActionMartialData actionMartial3 = unitData.GetActionMartial(unitData.step);
                if (actionMartial3 != null)
                {
                    DataProps.MartialData martialData = actionMartial3.data.To<DataProps.MartialData>();
                    martialList.Add("身法为" + GameTool.LS(martialData.martialInfo.name));
                }

                // 神通
                DataUnit.ActionMartialData actionMartial4 = unitData.GetActionMartial(unitData.ultimate);
                if (actionMartial4 != null)
                {
                    DataProps.MartialData martialData = actionMartial4.data.To<DataProps.MartialData>();
                    martialList.Add("神通为" + GameTool.LS(martialData.martialInfo.name));
                }

                // 心法
                List<string> abilities = new List<string>();
                foreach (string id in unit.data.unitData.abilitys)
                {
                    DataUnit.ActionMartialData abilityAction = unitData.GetActionMartial(id);
                    if (abilityAction != null)
                    {
                        DataProps.MartialData abilityData = abilityAction.data.To<DataProps.MartialData>();
                        abilities.Add(GameTool.LS(abilityData.martialInfo.name));
                    }
                }
                if (abilities.Count > 0)
                {
                    martialList.Add("心法:" + string.Join(" ", abilities));
                }

                string martialStr = string.Join("\n", martialList);
                if (StringExtensions.IsEmpty(martialStr))
                {
                    martialStr = "无";
                }

                // 装备
                string equipment = "";
                Il2CppStringArray equips = unit.data.unitData.equips;
                if (equips != null && equips.Count > 0)
                {
                    for (int j = 0; j < equips.Count; j++)
                    {
                        DataProps.PropsData prop = unit.data.unitData.propData.GetProps(equips[j]);
                        if (prop != null)
                        {
                            equipment += GameTool.LS(prop.propsItem.name) + " ";
                        }
                    }
                }
                if (StringExtensions.IsEmpty(equipment))
                {
                    equipment = "无";
                }

                // 道具与资源
                string items = "";
                List<DataProps.PropsData> allPropsList = new List<DataProps.PropsData>();

                // 将所有物品添加到临时列表
                foreach (DataProps.PropsData item in unit.data.unitData.propData.allProps)
                {
                    allPropsList.Add(item);
                }

                // 如果物品数量超过5个，随机选择5个
                if (allPropsList.Count > 5)
                {
                    // 随机打乱列表
                    System.Random random = new System.Random();
                    int n = allPropsList.Count;
                    while (n > 1)
                    {
                        n--;
                        int k = random.Next(n + 1);
                        DataProps.PropsData value = allPropsList[k];
                        allPropsList[k] = allPropsList[n];
                        allPropsList[n] = value;
                    }

                    // 只取前5个
                    allPropsList = allPropsList.Take(5).ToList();
                }

                // 构建物品信息字符串
                foreach (DataProps.PropsData item in allPropsList)
                {
                    items += item.propsInfoBase.name + "*" + item.propsCount.ToString() + " ";
                }

                if (StringExtensions.IsEmpty(items))
                {
                    items = "无";
                }

                // 灵石数量
                int money = unit.data.unitData.propData.GetPropsNum(10001);

                // 格式化输出最终字符串
                info.AppendLine($"姓名：{name}");
                info.AppendLine($"基本设定：{identity}修士");
                info.AppendLine($"性别：{gender}");
                info.AppendLine($"境界：{realm}");
                info.AppendLine($"当前年龄：{age}");
                info.AppendLine($"宗门（职位）：{sect}");
                info.AppendLine($"当前位置：{location}");
                info.AppendLine($"性格：{characterStr}");
                info.AppendLine($"爱好：{hobbyStr}（只是参考，除非事件类型特意提到，否则不要作为事件核心）");
                info.AppendLine($"近期烦恼：{troubleStr}");
                info.AppendLine($"魅力值：{beauty}（默认最高1000）");
                info.AppendLine($"战斗力：{power}");
                info.AppendLine($"当前心情：{mood}(默认最高100）");
                info.AppendLine($"当前精力：{energy}（默认最高80）");
                info.AppendLine($"婚姻情况：{marriageStatus}");
                info.AppendLine($"当前声望：{reputation}");
                info.AppendLine($"携带气运：{luckStr} 重点观察重伤、一丝元魂、性奴等气运，是奇遇的核心影响因素");
                info.AppendLine($"携带功法：{martialStr}");
                info.AppendLine($"携带装备：{equipment}");
                info.AppendLine($"拥有道具及数量（不是奖励）：{items}");
                info.AppendLine($"灵石数量：{money}");
                info.AppendLine($"种族：{race}");

                return info.ToString();
            }
            catch (Exception ex)
            {
                Debug.Log($"GetNPCInfoString出错: {ex.Message}\n{ex.StackTrace}");
                return $"提取NPC信息时发生错误: {ex.Message}";
            }
        }

        public static string GetCompleteInfoShort(WorldUnitBase unit)
        {
            if (unit == null) return "无效NPC";


            StringBuilder info = new StringBuilder();

            try
            {
                // 获取基本信息
                string basicInfo = GetNPCInfoString(unit);
                info.AppendLine("【基本资料】");
                info.AppendLine(basicInfo);
            }
            catch (Exception ex)
            {
                Debug.Log($"获取NPC完整信息时出错: {ex.Message}");
                return $"获取NPC信息失败: {ex.Message}";
            }

            return info.ToString();
        }

        public static ShortEventResponse ParseShortEventResponse(string rawResponse)
        {
            Debug.Log($"{rawResponse}");
            try
            {
                if (string.IsNullOrEmpty(rawResponse) || rawResponse.StartsWith("错误："))
                {
                    return null;
                }

                string processedResponse = rawResponse.Trim();

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

                var response = new ShortEventResponse();

                // 提取content
                string contentPattern = @"<content(?:\s[^>]*)?>([\s\S]*?)</content>";
                Match contentMatch = Regex.Match(processedResponse, contentPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (contentMatch.Success)
                {
                    string content = contentMatch.Groups[1].Value.Trim();
                    // 简单的HTML实体转义
                    content = content.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&")
                                   .Replace("&quot;", "\"").Replace("&#39;", "'").Replace("&nbsp;", " ");
                    response.content = content;
                }

                // 提取选项（最多4个）
                for (int i = 1; i <= 4; i++)
                {
                    string optionPattern = $@"<option{i}(?:\s[^>]*)?>([\s\S]*?)</option{i}>";
                    string endPattern = $@"<end{i}(?:\s[^>]*)?>([\s\S]*?)</end{i}>";
                    string getPattern = $@"<get{i}(?:\s[^>]*)?>([\s\S]*?)</get{i}>";

                    Match optionMatch = Regex.Match(processedResponse, optionPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    Match endMatch = Regex.Match(processedResponse, endPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    Match getMatch = Regex.Match(processedResponse, getPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    // 只有当option和end都存在时才添加选项
                    if (optionMatch.Success && endMatch.Success)
                    {
                        string optionText = optionMatch.Groups[1].Value.Trim();
                        string endingText = endMatch.Groups[1].Value.Trim();
                        string rewardText = getMatch.Success ? getMatch.Groups[1].Value.Trim() : "";

                        // HTML实体转义
                        optionText = optionText.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&")
                                             .Replace("&quot;", "\"").Replace("&#39;", "'").Replace("&nbsp;", " ");
                        endingText = endingText.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&")
                                             .Replace("&quot;", "\"").Replace("&#39;", "'").Replace("&nbsp;", " ");
                        rewardText = rewardText.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&")
                                             .Replace("&quot;", "\"").Replace("&#39;", "'").Replace("&nbsp;", " ");

                        var option = new ShortEventOption
                        {
                            text = optionText,
                            ending = endingText,
                            reward = rewardText
                        };
                        response.options.Add(option);
                    }
                }

                // 验证至少有2个选项
                if (response.options.Count >= 2 && !string.IsNullOrEmpty(response.content))
                {
                    Debug.Log($"短奇遇解析成功，content: {response.content}, 选项数量: {response.options.Count}");
                    return response;
                }

                Debug.Log("短奇遇解析失败：选项数量不足或缺少content");
                return null;
            }
            catch (Exception ex)
            {
                Debug.Log($"解析短奇遇响应失败: {ex.Message}");
                return null;
            }
        }

        public static void CreateShortEventDialogue(ShortEventResponse eventResponse)
        {
            try
            {
                var options = new Dictionary<int, string>();
                var callbacks = new Dictionary<int, Action>();

                // 添加选项
                for (int i = 0; i < eventResponse.options.Count; i++)
                {
                    int optionId = 11400 + i;
                    var option = eventResponse.options[i];

                    // 如果有奖励，在选项文本后添加提示
                    string optionText = option.text;

                    options[optionId] = optionText;

                    // 闭包捕获当前选项
                    var currentOption = option;
                    callbacks[optionId] = () => {
                        ProcessShortEventChoice(currentOption);
                    };
                }

                // 创建对话
                //Tools.CreateDialogue(ModMain.playerTalk, eventResponse.content, g.world.playerUnit, null, options, callbacks);
                ModMain.CreateMultiPartDialogue(ModMain.playerTalk, eventResponse.content, g.world.playerUnit, null, options, callbacks);
            }
            catch (Exception ex)
            {
                Debug.Log($"创建短奇遇对话失败: {ex.Message}");
                // 重置状态
                ModMain.waitingForShortEventResponse = false;
                ModMain.pendingShortEventResponse = null;
            }
        }

        private static void ProcessShortEventChoice(ShortEventOption option)
        {
            try
            {
                // 创建结果对话
                var resultOptions = new Dictionary<int, string> { { 11451, "确定" } };
                var resultCallbacks = new Dictionary<int, Action> {
                    { 11451, () => {
                        // 在确认时发放奖励
                        if (!string.IsNullOrEmpty(option.reward))
                        {
                            GiveRewardSingle(option.reward);  // 改为调用ShortEvent的方法
                        }
                
                        // 清除短奇遇内容并重置状态
                        g.data.dataObj.data.SetString("ShortEventContent", "");
                        ModMain.waitingForShortEventResponse = false;
                        ModMain.pendingShortEventResponse = null;
                        ModMain.llmRequestStartTime = 0f;
                    }}
                };
                //Tools.CreateDialogue(ModMain.playerTalk, option.ending, g.world.playerUnit, null, resultOptions, null);
                Tools.CreateDialogue(ModMain.playerTalk, option.ending, g.world.playerUnit, null, resultOptions, resultCallbacks);
            }
            catch (Exception ex)
            {
                Debug.Log($"处理短奇遇选择失败: {ex.Message}");
                // 重置状态并清除内容
                g.data.dataObj.data.SetString("ShortEventContent", "");
                ModMain.waitingForShortEventResponse = false;
                ModMain.pendingShortEventResponse = null;
                ModMain.llmRequestStartTime = 0f;
            }
        }
    }
}