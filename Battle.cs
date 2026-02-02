using System;
using System.Collections.Generic;
using UnityEngine;
using EGameTypeData;
using Newtonsoft.Json;
using System.Linq;
using System.Text;

namespace MOD_kqAfiU
{
    public class Battle
    {
        // 战斗相关的对话ID
        private static int battleDialogId = 1100709579;

        // 当前战斗状态
        private static string pendingBattleStart = null; // 等待中的战斗开场描述
        private static bool waitingForBattleStart = false; // 是否在等待战斗开场描述
        private static WorldUnitBase pendingStartPlayer;
        private static WorldUnitBase pendingStartEnemy;
        private static LLMDialogueRequest pendingStartDialogueRequest;
        private static Dictionary<int, Dictionary<string, string>> allUnitsBattleItems = new Dictionary<int, Dictionary<string, string>>();
        private static List<WorldUnitBase> battleUnits = new List<WorldUnitBase>();
        private static List<BattleCharacter> battleStats = new List<BattleCharacter>();
        private static int currentTurnIndex = 0; // 当前行动的单位索引
        private static Dictionary<string, string> pendingCachedEffects = new Dictionary<string, string>();

        private static bool isPlayerTurn = true;
        private static bool battleInProgress = false;
        private static LLMDialogueRequest savedDialogueRequest;
        private static LLMDialogueRequest battleRequest; // 战斗agent的历史记录
        private static List<string> currentTurnLogs = new List<string>(); // 当前轮次的战斗日志
        private static string pendingBattleDescription = null; // 等待中的战斗描述
        private static bool waitingForBattleDescription = false; // 是否在等待战斗描述
        private static List<string> allBattleContents = new List<string>(); // 存储所有战斗过程的content
        private static string battleEnding = ""; // 存储战斗结局描述
        private static int playerDefenseBonus = 0;
        private static int playerDefenseTurns = 0;
        private static int enemyDefenseBonus = 0;
        private static int enemyDefenseTurns = 0;
        private static string pendingPlayerAction = "";
        private static string pendingPlayerActionType = "";
        private static float attackMultiplier = 4.5f;       // 普通攻击的伤害倍率
        private static float skillLeftMultiplier = 9.0f;      // 武技/灵技 (左键) 的伤害倍率
        private static float skillRightMultiplier = 12.0f;     // 绝技 (右键) 的伤害倍率
        private static float ultimateSkillMultiplier = 18.0f; // 神通的伤害倍率

        private static Dictionary<string, string> effectTypeMapping = new Dictionary<string, string>
        {
            { "attack", "<color=#FF0000>攻击</color>" },      // 鲜红色
            { "defense", "<color=#00AA00>防御</color>" },    // 绿色
            { "health", "<color=#CC0000>生命</color>" },     // 深红色
            { "mp", "<color=#0080FF>灵力</color>" },         // 蓝色
            { "energy", "<color=#FFD700>念力</color>" },     // 金色
            { "speed", "<color=#87CEEB>脚力、移速</color>" },      // 浅蓝色
            { "crit", "<color=#A0522D>会心</color>" }        // 棕褐色
        };

        private static Dictionary<string, string> currentBattleItems
        {
            get
            {
                if (allUnitsBattleItems.ContainsKey(0))
                    return allUnitsBattleItems[0];
                return new Dictionary<string, string>();
            }
        }

        private static string lastDisplayedContent = "";

        private static List<int> CalculateInitiativeOrder()
        {
            List<(int index, int footSpeed)> initiativeList = new List<(int, int)>();

            for (int i = 0; i < battleStats.Count; i++)
            {
                if (battleStats[i].health > 0) // 只计算存活的角色
                {
                    initiativeList.Add((i, battleStats[i].footSpeed));
                }
            }

            // 按脚力降序排序，脚力高的先行动
            initiativeList.Sort((a, b) => b.footSpeed.CompareTo(a.footSpeed));

            return initiativeList.Select(x => x.index).ToList();
        }

        private static string ConvertEffectToChinese(string effectString)
        {
            if (string.IsNullOrEmpty(effectString)) return effectString;

            foreach (var mapping in effectTypeMapping)
            {
                if (effectString.Contains(mapping.Key))
                {
                    string valueStr = effectString.Replace(mapping.Key, "").Replace("%", "");
                    // 获取对应的颜色代码
                    string colorCode = "";
                    switch (mapping.Key)
                    {
                        case "attack": colorCode = "#FF0000"; break;
                        case "defense": colorCode = "#00AA00"; break;
                        case "health": colorCode = "#CC0000"; break;
                        case "mp": colorCode = "#0080FF"; break;
                        case "energy": colorCode = "#FFD700"; break;
                        case "speed": colorCode = "#87CEEB"; break;
                        case "crit": colorCode = "#A0522D"; break;
                    }

                    string coloredType = mapping.Value;
                    return $"{coloredType}<color={colorCode}>+{valueStr}%</color>";
                }
            }

            return effectString; // 如果没有匹配，返回原字符串
        }


        [Serializable]
        public class BattleStartResponse
        {
            public string opening_scene;
            [JsonExtensionData]
            public Dictionary<string, object> item_effects;
        }

        public static void StartBattlePreparation(WorldUnitBase player, WorldUnitBase enemy, LLMDialogueRequest dialogueRequest)
        {

            pendingStartPlayer = player;
            pendingStartEnemy = enemy;
            pendingStartDialogueRequest = dialogueRequest;
            waitingForBattleStart = true;

            // 构建战斗开场请求
            var startRequest = BuildBattleStartRequest(player, enemy, dialogueRequest);

            UITipItem.AddTip("正在准备战斗场景~", 1f);

            // 发送请求
            Tools.SendLLMRequest(startRequest, (response) => {
                pendingBattleStart = response;
            });
        }

        private static LLMDialogueRequest BuildBattleStartRequest(WorldUnitBase player, WorldUnitBase enemy, LLMDialogueRequest dialogueRequest)
        {
            var request = new LLMDialogueRequest();

            // 添加系统提示
            string systemPrompt = BuildBattleStartSystemPrompt(player, enemy, dialogueRequest);
            request.AddSystemMessage(systemPrompt);

            // 添加用户请求
            request.AddUserMessage("请生成战斗开场场景和道具效果配置。");

            return request;
        }

        private static string BuildBattleStartSystemPrompt(WorldUnitBase player, WorldUnitBase enemy, LLMDialogueRequest dialogueRequest)
        {
            StringBuilder promptBuilder = new StringBuilder();

            // 添加基础系统提示
            promptBuilder.AppendLine(@"你是一个修仙世界战斗开场引导者，需要根据奇遇背景生成战斗开场场景，并为玩家的道具配置战斗效果。");

            // 添加奇遇背景
            string adventureContext = ExtractAdventureContext(dialogueRequest, enemy.data.unitData.propertyData.GetName());
            promptBuilder.AppendLine(adventureContext);

            // 添加道具信息 - 修改这里，传递player和enemy参数
            string itemInfo = ExtractAllUnitsItems(player, enemy);
            promptBuilder.AppendLine(itemInfo);

            // 添加输出格式要求
            promptBuilder.AppendLine(@"
【输出格式要求】
你必须以JSON格式返回内容，格式如下：
{
    ""opening_scene"": ""战斗开场场景描述，100-200字，要与奇遇背景衔接自然"",
    ""道具名称1"": ""效果类型+数值，如attack20%"",
    ""道具名称2"": ""效果类型+数值，如defense15%""
}

效果类型说明：
- attack: 增加攻击力百分比
- defense: 增加防御力百分比  
- health: 恢复生命值百分比
- mp: 恢复灵力值百分比
- energy: 恢复念力值百分比
- speed: 增加速度百分比
- crit: 增加暴击百分比

注意：
1. 只为适合战斗使用的道具分配效果，如丹药、武器配件、防具等
2. 跳过明显不适合战斗的道具，如突破材料、收集品、任务物品等
3. 效果数值建议在10%-50%之间
4. 必须返回纯JSON格式，不要包含```json或```等标记");

            return promptBuilder.ToString();
        }

        private static string ExtractAllUnitsItems(WorldUnitBase player, WorldUnitBase enemy)
        {
            StringBuilder itemBuilder = new StringBuilder();

            HashSet<string> allUniqueItems = new HashSet<string>(); // 用于去重
            List<(string unitName, List<string> items)> unitsItemsInfo = new List<(string, List<string>)>();

            // 临时构建参战单位列表
            List<WorldUnitBase> tempBattleUnits = new List<WorldUnitBase> { player, enemy };

            // 遍历所有参战单位，每个人都选择5个道具
            for (int i = 0; i < tempBattleUnits.Count; i++)
            {
                var unit = tempBattleUnits[i];
                string unitName = unit.data.unitData.propertyData.GetName();
                List<string> unitItems = new List<string>();

                List<DataProps.PropsData> allPropsList = new List<DataProps.PropsData>();

                // 将所有物品添加到临时列表
                foreach (DataProps.PropsData item in unit.data.unitData.propData.allProps)
                {
                    allPropsList.Add(item);
                }

                // 每个人都选择5个道具（如果不足5个就全选）
                if (allPropsList.Count > 5)
                {
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
                    allPropsList = allPropsList.Take(5).ToList();
                }

                // 收集每个人的道具名称
                for (int j = 0; j < allPropsList.Count; j++)
                {
                    var item = allPropsList[j];
                    string itemName = item.propsInfoBase?.name ?? "未知道具";
                    unitItems.Add(itemName);
                    allUniqueItems.Add(itemName); // 同时添加到全局去重集合
                }

                unitsItemsInfo.Add((unitName, unitItems));
            }

            // === 新增：缓存检查逻辑 ===
            HashSet<string> cachedItems = new HashSet<string>(); // 已缓存的道具
            HashSet<string> uncachedItems = new HashSet<string>(); // 未缓存的道具
            Dictionary<string, string> cachedEffects = new Dictionary<string, string>(); // 缓存的效果

            foreach (string itemName in allUniqueItems)
            {
                string cacheKey = $"AIbattle_{itemName}";
                string cachedEffect = g.data.dataObj.data.GetString(cacheKey);

                if (!string.IsNullOrEmpty(cachedEffect))
                {
                    // 道具已有缓存效果
                    cachedItems.Add(itemName);
                    cachedEffects[itemName] = cachedEffect;
                    Debug.Log($"道具 {itemName} 使用缓存效果: {cachedEffect}");
                }
                else
                {
                    // 道具没有缓存，需要LLM生成
                    uncachedItems.Add(itemName);
                }
            }

            // 将缓存信息存储到静态变量中供后续使用
            pendingCachedEffects = cachedEffects;

            // 输出所有单位的道具信息（用于AI理解道具来源）
            foreach (var (unitName, items) in unitsItemsInfo)
            {
                itemBuilder.AppendLine($"\n{unitName}拥有的道具（{items.Count}个）：");
                for (int i = 0; i < items.Count; i++)
                {
                    itemBuilder.AppendLine($"  - {items[i]}");
                }
            }

            // 只输出需要LLM生成的道具
            if (uncachedItems.Count > 0)
            {
                itemBuilder.AppendLine("【所有参战单位道具信息】");
                itemBuilder.AppendLine("请为以下道具配置战斗效果：");

                itemBuilder.AppendLine($"\n需要配置效果的道具列表（{uncachedItems.Count}个）：");
                int index = 1;
                foreach (string itemName in uncachedItems)
                {
                    itemBuilder.AppendLine($"{index}. {itemName}");
                    index++;
                }
                itemBuilder.AppendLine("\n说明：请为上述所有道具都配置战斗效果，每个拥有该道具的角色都将获得相应效果。");
            }
            else
            {
                itemBuilder.AppendLine("【道具信息】");
                itemBuilder.AppendLine("所有道具都已有预设效果，无需生成新的道具效果。");
            }

            return itemBuilder.ToString();
        }

        public static bool HasPendingBattleStart()
        {
            return waitingForBattleStart && !string.IsNullOrEmpty(pendingBattleStart);
        }

        public static void ProcessPendingBattleStart()
        {
            if (!HasPendingBattleStart()) return;

            try
            {
                Dictionary<string, object> finalItemEffects = new Dictionary<string, object>();

                // 首先加入缓存的道具效果
                foreach (var kvp in pendingCachedEffects)
                {
                    finalItemEffects[kvp.Key] = kvp.Value;
                    Debug.Log($"加入缓存道具效果: {kvp.Key} = {kvp.Value}");
                }

                // 解析LLM返回的新道具效果
                if (!string.IsNullOrEmpty(pendingBattleStart))
                {
                    // 解析响应
                    string processedResponse = pendingBattleStart;

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
                    Debug.Log($"战斗开场原始响应: {pendingBattleStart}");
                    Debug.Log($"战斗开场处理后响应: {processedResponse}");

                    var startResponse = JsonConvert.DeserializeObject<BattleStartResponse>(processedResponse);

                    if (startResponse != null)
                    {
                        Debug.Log($"解析成功 - 开场场景: {startResponse.opening_scene}");
                        Debug.Log($"新生成道具效果数量: {startResponse.item_effects?.Count ?? 0}");

                        // 合并LLM生成的新道具效果
                        if (startResponse.item_effects != null)
                        {
                            foreach (var kvp in startResponse.item_effects)
                            {
                                string itemName = kvp.Key;
                                string effectString = kvp.Value?.ToString();

                                if (string.IsNullOrEmpty(effectString) || itemName == "opening_scene") continue;

                                // 检查是否是有效的效果格式
                                bool isValidEffect = false;
                                foreach (var effectType in effectTypeMapping.Keys)
                                {
                                    if (effectString.Contains(effectType))
                                    {
                                        isValidEffect = true;
                                        break;
                                    }
                                }

                                if (isValidEffect)
                                {
                                    finalItemEffects[itemName] = effectString;

                                    // === 新增：保存到缓存 ===
                                    string cacheKey = $"AIbattle_{itemName}";
                                    g.data.dataObj.data.SetString(cacheKey, effectString);
                                    Debug.Log($"保存道具效果到缓存: {cacheKey} = {effectString}");
                                }
                            }
                        }

                        // 开始战斗，使用合并后的道具效果
                        StartBattle(pendingStartPlayer, pendingStartEnemy, pendingStartDialogueRequest,
                                   startResponse.opening_scene ?? "", finalItemEffects);
                    }
                    else
                    {
                        // 解析失败但有缓存效果，仍可开始战斗
                        StartBattle(pendingStartPlayer, pendingStartEnemy, pendingStartDialogueRequest, "", finalItemEffects);
                    }
                }
                else
                {
                    // 没有LLM响应但有缓存效果
                    StartBattle(pendingStartPlayer, pendingStartEnemy, pendingStartDialogueRequest, "", finalItemEffects);
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"处理战斗开场失败: {ex.Message}");
                // 出错时至少使用缓存的道具效果
                Dictionary<string, object> fallbackEffects = new Dictionary<string, object>();
                foreach (var kvp in pendingCachedEffects)
                {
                    fallbackEffects[kvp.Key] = kvp.Value;
                }
                StartBattle(pendingStartPlayer, pendingStartEnemy, pendingStartDialogueRequest, "", fallbackEffects);
            }
            finally
            {
                // 清理状态
                pendingBattleStart = null;
                waitingForBattleStart = false;
                pendingStartPlayer = null;
                pendingStartEnemy = null;
                pendingStartDialogueRequest = null;
                pendingCachedEffects.Clear(); // 清理缓存变量
            }
        }

        private static void ApplyItemEffectsToAllUnits(Dictionary<string, object> itemEffects)
        {
            if (itemEffects == null) return;

            // 初始化所有单位的道具列表
            allUnitsBattleItems.Clear();
            for (int i = 0; i < battleUnits.Count; i++)
            {
                allUnitsBattleItems[i] = new Dictionary<string, string>();
            }

            // 为每个单位检查是否拥有返回的道具
            foreach (var kvp in itemEffects)
            {
                string itemName = kvp.Key;
                string effectString = kvp.Value?.ToString();

                if (string.IsNullOrEmpty(effectString) || itemName == "opening_scene") continue;

                // 检查是否是有效的效果格式
                bool isValidEffect = false;
                foreach (var effectType in effectTypeMapping.Keys)
                {
                    if (effectString.Contains(effectType))
                    {
                        isValidEffect = true;
                        break;
                    }
                }

                if (!isValidEffect) continue;

                // 为拥有此道具的单位分配
                for (int i = 0; i < battleUnits.Count; i++)
                {
                    var unit = battleUnits[i];

                    // 检查该单位是否拥有此道具
                    foreach (DataProps.PropsData item in unit.data.unitData.propData.allProps)
                    {
                        string unitItemName = item.propsInfoBase?.name ?? "未知道具";
                        if (unitItemName == itemName)
                        {
                            allUnitsBattleItems[i][itemName] = effectString;
                            string unitName = unit.data.unitData.propertyData.GetName();
                            Debug.Log($"为{unitName}分配道具: {itemName} = {effectString}");
                            break; // 找到后跳出内层循环
                        }
                    }
                }
            }

            // 输出每个单位的可用道具数量
            for (int i = 0; i < battleUnits.Count; i++)
            {
                string unitName = battleUnits[i].data.unitData.propertyData.GetName();
                Debug.Log($"{unitName}可用战斗道具数量: {allUnitsBattleItems[i].Count}");
            }
        }

        public enum BattleEndingType
        {
            FleeSuccess,    // 逃跑成功
            FleeFailed,     // 逃跑失败
            Surrender,      // 投降
            SurrenderSex,   //鬼畜投降
            DefeatNormal,   // 普通战败
            DefeatSex,   // 鬼畜战败
            VictoryNormal,  // 普通胜利
            VictorySex   // 鬼畜胜利
        }

        private static float CalculateDodgeRate(BattleCharacter attacker, BattleCharacter defender)
        {
            // 基础闪避率15%
            float baseDodgeRate = 0.15f;

            // 速度修正：使用非线性函数，避免极端值
            float speedModifier = CalculateSpeedDodgeModifier(attacker.moveSpeed, defender.moveSpeed);

            // 境界修正前的闪避率
            float preGradeDodgeRate = baseDodgeRate + speedModifier;

            // 境界修正：每差一个境界±8%（比原来稍微降低）
            int attackerGrade = attacker.grade;
            int defenderGrade = defender.grade;
            float gradeModifier = (defenderGrade - attackerGrade) * 0.08f;

            // 最终闪避率 = 境界修正前闪避率 * (1 + 境界修正)
            float finalDodgeRate = preGradeDodgeRate * (1f + gradeModifier);

            // 限制闪避率在5%-85%之间（避免极端情况）
            finalDodgeRate = Mathf.Clamp(finalDodgeRate, 0.05f, 0.85f);

            return finalDodgeRate;
        }

        /// <summary>
        /// 计算速度对闪避的非线性修正
        /// </summary>
        /// <param name="attackerSpeed">攻击者移速</param>
        /// <param name="defenderSpeed">防御者移速</param>
        /// <returns>速度修正值 (-0.25 到 +0.25)</returns>
        private static float CalculateSpeedDodgeModifier(int attackerSpeed, int defenderSpeed)
        {
            // 避免除零错误
            if (attackerSpeed <= 0) attackerSpeed = 1;
            if (defenderSpeed <= 0) defenderSpeed = 1;

            // 计算速度比值
            float speedRatio = (float)defenderSpeed / attackerSpeed;

            // 使用反正切函数实现非线性映射
            // atan函数特性：当x趋向无穷时，atan(x)趋向π/2，具有天然的饱和特性
            float normalizedRatio = (speedRatio - 1.0f) * 2.0f; // 将比值差距放大
            float atanResult = Mathf.Atan(normalizedRatio) / (3.14159f / 2.0f); // 归一化到-1到1

            // 映射到-0.25到+0.25的范围（最大25%的速度修正）
            float speedModifier = atanResult * 0.25f;

            return speedModifier;
        }

        private static int CalculateDamage(BattleCharacter attacker, BattleCharacter defender, float multiplier)
        {
            // 找到攻击者和防御者在列表中的索引
            int attackerIndex = battleStats.IndexOf(attacker);
            int defenderIndex = battleStats.IndexOf(defender);

            // 获取境界
            int attackerGrade = attacker.grade;
            int defenderGrade = defender.grade;

            // === 闪避判定 ===
            // 基础闪避率20%
            float finalDodgeRate = CalculateDodgeRate(attacker, defender);

            // 闪避判定
            if (UnityEngine.Random.Range(0f, 1f) < finalDodgeRate)
            {
                // 闪避成功，返回0伤害，但需要在外部处理日志
                return -1; // 用-1表示闪避，外部函数需要处理这个特殊值
            }

            // === 暴击判定 ===
            // 基础暴击率20%
            float baseCritRate = 0.2f;

            // 属性修正：(攻击方暴击 - 受攻击方格挡) / 受攻击方格挡
            float critModifier = (float)(attacker.crit - defender.guard) / defender.guard;

            // 境界修正：每差一个境界±10%
            float gradeModifierForCrit = (attackerGrade - defenderGrade) * 0.1f;

            // 最终暴击率 = (基础暴击率 + 属性修正) * (1 + 境界修正)
            float finalCritRate = (baseCritRate + critModifier) * (1f + gradeModifierForCrit);

            // 限制暴击率在0-95%之间
            finalCritRate = Mathf.Clamp(finalCritRate, 0f, 0.95f);

            // 暴击判定
            bool isCriticalHit = UnityEngine.Random.Range(0f, 1f) < finalCritRate;

            // === 伤害计算 ===
            // 计算有效防御（包括防御加成）
            int effectiveDefense = defender.defense;
            if (defenderIndex == 0 && playerDefenseBonus > 0) // 玩家
                effectiveDefense += playerDefenseBonus;
            else if (defenderIndex == battleStats.Count - 1 && enemyDefenseBonus > 0) // 最后一个敌人
                effectiveDefense += enemyDefenseBonus;

            // 基础伤害计算
            int baseDamage = (int)((attacker.attack - effectiveDefense) * multiplier);

            // ±5%浮动
            float variation = UnityEngine.Random.Range(-0.05f, 0.05f);
            int finalDamage = (int)(baseDamage * (1f + variation));

            // 暴击额外50%伤害
            if (isCriticalHit)
            {
                finalDamage = (int)(finalDamage * 1.5f);
                // 用负数表示暴击，绝对值是实际伤害
                return -(Math.Max(1, finalDamage));
            }

            // 确保最小伤害为1
            return Math.Max(1, finalDamage);
        }


        private static BattleCharacter InitializeCharacter(WorldUnitBase unit)
        {
            var unitData = unit.data;
            var character = new BattleCharacter();

            character.health = (int)unitData.dynUnitData.hpMax.value;
            character.maxHealth = (int)unitData.dynUnitData.hpMax.value;
            character.mp = (int)unitData.dynUnitData.mpMax.value;
            character.maxMp = (int)unitData.dynUnitData.mpMax.value;
            character.energy = (int)unitData.dynUnitData.spMax.value;
            character.maxEnergy = (int)unitData.dynUnitData.spMax.value;
            character.defense = (int)unitData.dynUnitData.defense.value;
            character.attack = (int)unitData.dynUnitData.attack.value;
            character.crit = (int)unitData.dynUnitData.crit.value;
            character.guard = (int)unitData.dynUnitData.guard.value;
            character.magicFree = (int)unitData.dynUnitData.magicFree.value;
            character.phycicalFree = (int)unitData.dynUnitData.phycicalFree.value;
            character.footSpeed = (int)unitData.dynUnitData.footSpeed.value;
            character.moveSpeed = (int)unitData.dynUnitData.moveSpeed.value;
            character.grade = (int)unit.data.dynUnitData.GetGrade();

            return character;
        }

        public class BattleCharacter
        {
            public int health;
            public int maxHealth;
            public int mp;
            public int maxMp;
            public int energy;
            public int maxEnergy;
            public int defense;
            public int attack;
            public int crit;
            public int guard;
            public int magicFree;
            public int phycicalFree;
            public int footSpeed;
            public int moveSpeed;
            public int grade;
        }


        public static string formatPrompt1 = @"
            你是一个修仙世界战斗轮描述者，需清楚以下背景并遵循以下规则：
【世界观】
建立《鬼谷八荒》修仙世界观,规范境界、地图、道具逻辑,确保设定一致.
世界观:以冥山为核心,地图解锁顺序:白源区→永宁州→雷泽→华封州→十万大山→云陌州→永恒冰原→暮仙州→迷途荒漠→赤幽州→天元山.各地受冥气影响,孕育秘境、妖兽、遗迹.
境界划分:炼气、筑基、结晶、金丹、具灵、元婴、化神、悟道、羽化、登仙,每境分初、中、后期.高境界修士对低境界有威压效果,低阶战胜高阶引发高阶道心破碎。

【核心原则】
1. 创造沉浸式修仙体验，让玩家感受如亲临其境
2. 叙述风格清晰直接，避免过度晦涩隐晦的表达
3. 场景描写要具体可感，不用过多意象和隐喻
4. 你的目标是描述战斗场景，而不是战斗解说，不要出戏地进行解说式描写体现了XXX策略与YYY等，以及不要给招式策略下总结性定义，而是像小说一样描述场面";

        public static string formatPrompt2 = @"
            【重要！输出格式】
            你根据实际战斗的日志，对每一条日志分别生成一个描述性content。最后必须以JSON格式返回整体内容，格式如下：
            {
                ""content1"": ""针对第1条日志生成描述性内容，在100字左右"",
                ""content2"": ""针对第2条日志（如果有）生成描述性内容，在100字左右"",
                ""content3"":""针对第3条日志（如果有）生成描述性内容，在100字左右""
            }";

        /// <summary>
        /// 开始战斗，打开战斗对话界面
        /// </summary>
        /// <param name="player">玩家单位</param>
        /// <param name="enemy">敌方单位</param>
        // 在Battle类中添加新的方法来提取奇遇上下文
        private static string ExtractAdventureContext(LLMDialogueRequest dialogueRequest, string npcName)
        {
            if (dialogueRequest == null || dialogueRequest.Messages == null || dialogueRequest.Messages.Count == 0)
            {
                return "";
            }

            StringBuilder contextBuilder = new StringBuilder();
            contextBuilder.AppendLine("【奇遇背景】");
            contextBuilder.AppendLine("以下是此次奇遇中的对话历史，请在战斗描述中考虑这些背景信息：");
            contextBuilder.AppendLine();

            foreach (var message in dialogueRequest.Messages)
            {
                // 跳过system消息
                if (message.Role.ToLower() == "system")
                    continue;

                if (message.Role.ToLower() == "user")
                {
                    // user消息直接提取content
                    contextBuilder.AppendLine($"玩家：{message.Content}");
                }
                else if (message.Role.ToLower() == "assistant")
                {
                    // assistant消息需要解析JSON
                    try
                    {
                        // 使用Tools的解析方法来处理JSON
                        string assistantContent;
                        Tools.ParseLLMResponse(message.Content, out assistantContent);

                        if (!string.IsNullOrEmpty(assistantContent))
                        {
                            contextBuilder.AppendLine($"{npcName}：{assistantContent}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"解析assistant消息失败: {ex.Message}");
                        // 如果解析失败，尝试直接使用content（可能是纯文本）
                        if (!string.IsNullOrEmpty(message.Content))
                        {
                            contextBuilder.AppendLine($"{npcName}：{message.Content}");
                        }
                    }
                }
            }

            contextBuilder.AppendLine();
            contextBuilder.AppendLine("请根据上述对话背景来描述战斗过程，保持角色性格和情境的一致性。");
            contextBuilder.AppendLine();

            return contextBuilder.ToString();
        }

        // 修改StartBattle方法，添加奇遇上下文

        private static string GenerateBattleEnding(BattleEndingType endingType)
        {
            string playerName = battleUnits[0].data.unitData.propertyData.GetName();
            string enemyName = battleUnits[battleUnits.Count - 1].data.unitData.propertyData.GetName();

            switch (endingType)
            {
                case BattleEndingType.FleeSuccess:
                    return $"【战斗结局】\n{playerName}成功从与{enemyName}的战斗中逃跑。尽管没有最终分出胜负，但{playerName}明智地选择了保全自身。";

                case BattleEndingType.FleeFailed:
                    return $"【战斗结局】\n{playerName}试图逃离与{enemyName}的战斗，但未能成功脱身。战斗被迫继续，局势变得更加不利。";

                case BattleEndingType.Surrender:
                    return $"【战斗结局】\n{playerName}在与{enemyName}的战斗中选择了投降，承认了对方的实力。这场战斗以{playerName}的主动认输而结束，避免了更严重的后果。";

                case BattleEndingType.SurrenderSex:
                    return $"【战斗结局】\n{playerName}在与{enemyName}的战斗中选择了投降，成为了对方的玩物。这场战斗以{playerName}的主动认输而结束，{enemyName}会和玩家强行发生关系。（强制双修）";

                case BattleEndingType.DefeatNormal:
                    return $"【战斗结局】\n经过激烈的战斗，{playerName}最终败给了{enemyName}。虽然战败，但{playerName}在这场战斗中展现了自己的实力和勇气。";

                case BattleEndingType.DefeatSex:
                    return $"【战斗结局】\n在与{enemyName}的生死搏杀中，{playerName}不幸败北，成为了对方的玩物。这场战斗以{playerName}的重创而告终，{enemyName}会和玩家强行发生关系。（强制双修）";

                case BattleEndingType.VictoryNormal:
                    return $"【战斗结局】\n经过一番激战，{playerName}成功击败了{enemyName}。这场胜利证明了{playerName}的实力，也为接下来的道路扫清了障碍。";

                case BattleEndingType.VictorySex:
                    return $"【战斗结局】\n在这场战斗中，{playerName}最终战胜了{enemyName}，彻底结束了这场冲突。{enemyName}为自己的选择付出了代价，玩家能够占有战败的敌人。（强制双修）";

                default:
                    return $"【战斗结局】\n{playerName}与{enemyName}的战斗已经结束。";
            }
        }

        private static void SetBattleEnding(BattleEndingType endingType)
        {
            battleEnding = GenerateBattleEnding(endingType);
            Debug.Log($"设置战斗结局: {battleEnding}");
        }

        private static string GetBattleStatusText()
        {
            StringBuilder statusBuilder = new StringBuilder();

            // 玩家状态（详细）
            if (battleUnits.Count > 0)
            {
                var playerUnit = battleUnits[0];
                var playerStat = battleStats[0];
                string playerName = playerUnit.data.unitData.propertyData.GetName();

                // 数值也使用相应的颜色
                string healthText = $"<color=#CC0000>生命{playerStat.health}/{playerStat.maxHealth}</color>";
                string mpText = $"<color=#0080FF>灵力{playerStat.mp}/{playerStat.maxMp}</color>";
                string energyText = $"<color=#FFD700>念力{playerStat.energy}/{playerStat.maxEnergy}</color>";

                statusBuilder.AppendLine($"{playerName}：{healthText}、{mpText}、{energyText}");
            }

            // 其他单位状态（只显示生命）
            for (int i = 1; i < battleUnits.Count; i++)
            {
                var unit = battleUnits[i];
                var stat = battleStats[i];
                string unitName = unit.data.unitData.propertyData.GetName();

                // 生命数值也使用红色
                string healthText = $"<color=#CC0000>生命{stat.health}/{stat.maxHealth}</color>";

                statusBuilder.AppendLine($"{unitName}：{healthText}");
            }

            return statusBuilder.ToString();
        }

        // 修改StartBattle方法中的战斗开始文本部分
        public static void StartBattle(WorldUnitBase player, WorldUnitBase enemy, LLMDialogueRequest dialogueRequest = null, string customOpening = "", Dictionary<string, object> itemEffects = null)
        {

            // 初始化参战单位列表
            battleUnits.Clear();
            battleStats.Clear();
            battleUnits.Add(player); // 0号位：玩家
            battleUnits.Add(enemy);  // 最后一号位：敌人
            battleStats.Add(InitializeCharacter(player));
            battleStats.Add(InitializeCharacter(enemy));

            currentTurnIndex = 0; // 玩家先行动
            battleInProgress = true;
            isPlayerTurn = true;
            savedDialogueRequest = dialogueRequest;
            battleRequest = new LLMDialogueRequest();

            if (itemEffects != null)
            {
                ApplyItemEffectsToAllUnits(itemEffects);
            }
            else
            {
                // 初始化空的道具列表
                allUnitsBattleItems.Clear();
                for (int i = 0; i < battleUnits.Count; i++)
                {
                    allUnitsBattleItems[i] = new Dictionary<string, string>();
                }
            }

            // 构建包含奇遇上下文的系统提示
            string adventureContext = ExtractAdventureContext(dialogueRequest, enemy.data.unitData.propertyData.GetName());
            string fullSystemPrompt = formatPrompt1 + adventureContext + formatPrompt2;

            battleRequest.AddSystemMessage(fullSystemPrompt);

            currentTurnLogs.Clear();
            pendingBattleDescription = null;
            waitingForBattleDescription = false;

            // 初始化防御状态
            playerDefenseBonus = 0;
            playerDefenseTurns = 0;
            enemyDefenseBonus = 0;
            enemyDefenseTurns = 0;

            // 清空战斗content历史
            allBattleContents.Clear();
            battleEnding = "";

            // 使用自定义开场或默认开场
            string battleStartText;
            if (!string.IsNullOrEmpty(customOpening))
            {
                battleStartText = customOpening;
                lastDisplayedContent = customOpening;
            }
            else
            {
                battleStartText = GetBattleStartText(player, enemy, dialogueRequest);
                lastDisplayedContent = battleStartText;
            }


            string statusText = GetBattleStatusText();
            string fullBattleText = $"{battleStartText}\n\n{statusText}";

            // 生成战斗选项和回调
            var (options, callbacks) = GenerateBattleOptions();

            // 创建战斗对话
            Tools.CreateDialogue(battleDialogId, fullBattleText, player, enemy, options, callbacks);
        }

        // 新增：获取战斗开始文本的方法
        private static string GetBattleStartText(WorldUnitBase player, WorldUnitBase enemy, LLMDialogueRequest dialogueRequest)
        {
            string battleContext = "";

            // 尝试从ModMain.currentRequest中获取最后一条assistant消息
            if (ModMain.currentRequest != null && ModMain.currentRequest.Messages != null && ModMain.currentRequest.Messages.Count > 0)
            {
                // 从后往前查找最后一条assistant消息
                for (int i = ModMain.currentRequest.Messages.Count - 1; i >= 0; i--)
                {
                    var message = ModMain.currentRequest.Messages[i];
                    if (message.Role.ToLower() == "assistant")
                    {
                        try
                        {
                            // 使用Tools的解析方法来处理JSON
                            string assistantContent;
                            Tools.ParseLLMResponse(message.Content, out assistantContent);

                            if (!string.IsNullOrEmpty(assistantContent))
                            {
                                battleContext = assistantContent;
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.Log($"解析assistant消息失败: {ex.Message}");
                            // 如果解析失败，尝试直接使用content
                            if (!string.IsNullOrEmpty(message.Content))
                            {
                                battleContext = message.Content;
                                break;
                            }
                        }
                    }
                }
            }

            // 如果没有找到合适的assistant消息，使用默认文本
            if (string.IsNullOrEmpty(battleContext))
            {
                battleContext = $"战斗开始！{player.data.unitData.propertyData.GetName()} VS {enemy.data.unitData.propertyData.GetName()}";
            }

            // 在找到的文本后面添加战斗提示
            return battleContext;
        }

        /// <summary>
        /// 生成战斗选项和对应的回调函数
        /// </summary>
        /// <returns>选项字典和回调字典的元组</returns>
        private static void ExecuteBattleAction(string actionType)
        {
            if (!battleInProgress) return;

            // 存储玩家行动，不立即执行
            pendingPlayerActionType = actionType;

            // 处理完整回合
            ExecuteRound();
        }

        /// <summary>
        /// 执行战斗行动
        /// </summary>
        /// <param name="actionType">行动类型</param>
        private static void SwitchTurn(string actionResult)
        {
            // 更新防御状态
            if (playerDefenseTurns > 0)
            {
                playerDefenseTurns--;
                if (playerDefenseTurns == 0)
                {
                    playerDefenseBonus = 0;
                }
            }

            // 发送战斗描述请求
            SendBattleDescriptionRequest();
        }

        /// <summary>
        /// 执行攻击行动
        /// </summary>
        /// <returns>行动结果文本</returns>
        private static string ExecuteAttack()
        {
            string playerName = battleUnits[0].data.unitData.propertyData.GetName();
            string enemyName = battleUnits[battleUnits.Count - 1].data.unitData.propertyData.GetName();

            int damageResult = CalculateDamage(battleStats[0], battleStats[battleStats.Count - 1], attackMultiplier);

            // 处理闪避情况
            if (damageResult == -1)
            {
                return $"{playerName}对{enemyName}发起攻击，但{enemyName}闪避了攻击！";
            }

            // 处理暴击情况
            bool isCriticalHit = damageResult < 0;
            int actualDamage = Math.Abs(damageResult);

            battleStats[battleStats.Count - 1].health = Math.Max(0, battleStats[battleStats.Count - 1].health - actualDamage);

            string enemyHealthStatus = GetHealthStatus(battleStats[battleStats.Count - 1]);
            string healthStatusText = string.IsNullOrEmpty(enemyHealthStatus) ? "" : enemyHealthStatus;

            string critText = isCriticalHit ? "【暴击】" : "";

            return $"{playerName}对{enemyName}发起攻击{critText}，造成了{actualDamage}点伤害！({enemyName}剩余生命：{battleStats[battleStats.Count - 1].health}/{battleStats[battleStats.Count - 1].maxHealth}{healthStatusText})";
        }

        /// <summary>
        /// 执行防御行动
        /// </summary>
        /// <returns>行动结果文本</returns>
        private static string ExecuteDefend()
        {
            string playerName = battleUnits[0].data.unitData.propertyData.GetName();

            // 增加50%防御，持续2轮
            playerDefenseBonus = (int)(battleStats[0].defense * 0.5f);
            playerDefenseTurns = 2;

            return $"{playerName}采取了防御姿态，防御力提升50%，持续2轮！";
        }

        /// <summary>
        /// 执行逃跑行动
        /// </summary>
        /// <returns>行动结果文本</returns>
        private static string ExecuteFlee()
        {
            string playerName = battleUnits[0].data.unitData.propertyData.GetName();
            SetBattleEnding(BattleEndingType.FleeSuccess);
            return $"{playerName}成功逃离了战斗！";
        }

        private static string ExecuteSurrender()
        {
            string playerName = battleUnits[0].data.unitData.propertyData.GetName();
            string surrenderText = $"{playerName}选择了投降，战斗结束。";

            currentTurnLogs.Clear(); // 清空日志
            waitingForBattleDescription = false; // 停止等待战斗描述
            pendingBattleDescription = null; // 清空待处理描述

            // 显示投降选择界面
            ShowSurrenderOptions(surrenderText);

            return surrenderText;
        }

        private static void ShowSurrenderOptions(string surrenderText)
        {
            Dictionary<int, string> surrenderOptions = new Dictionary<int, string>();
            Dictionary<int, Action> surrenderCallbacks = new Dictionary<int, Action>();

            // 两种投降选择
            surrenderOptions[8005] = "正常投降";
            surrenderCallbacks[8005] = () => {
                SetBattleEnding(BattleEndingType.Surrender);
                // 直接执行结束逻辑，不调用EndBattle
                ExecuteBattleEndLogic();
            };

            surrenderOptions[8006] = "投降惩罚";
            surrenderCallbacks[8006] = () => {
                SetBattleEnding(BattleEndingType.SurrenderSex);
                // 直接执行结束逻辑，不调用EndBattle
                ExecuteBattleEndLogic();
            };

            Tools.CreateDialogue(battleDialogId, surrenderText, battleUnits[0], battleUnits[1], surrenderOptions, surrenderCallbacks);
        }

        /// <summary>
        /// 执行投降行动
        /// </summary>
        /// <returns>行动结果文本</returns>

        /// <summary>
        /// 显示技能选项
        /// </summary>
        private static void ShowSkillOptions()
        {
            Dictionary<int, string> skillOptions = new Dictionary<int, string>();
            Dictionary<int, Action> skillCallbacks = new Dictionary<int, Action>();

            int skillId = 2000;

            // 从玩家数据中获取实际的技能列表
            DataUnit.ActionMartialData actionMartial = battleUnits[0].data.unitData.GetActionMartial(battleUnits[0].data.unitData.skillLeft);
            if (actionMartial != null)
            {
                DataProps.MartialData martialData = actionMartial.data.To<DataProps.MartialData>();
                string skillName = GameTool.LS(martialData.martialInfo.name);
                int mpCost = (int)(battleStats[0].maxMp * 0.15f); // 15%灵力
                bool canUse = battleStats[0].mp >= mpCost;

                string buttonText = $"{skillName}(<color=#0080FF>灵力-{mpCost}</color>)";
                skillOptions[skillId] = buttonText;

                if (canUse)
                {
                    skillCallbacks[skillId] = () => ExecuteSkill(skillName);
                }
                else
                {
                    skillCallbacks[skillId] = () => {
                        UITipItem.AddTip("灵力不足！", 1f);
                        ReturnToBattleMenu();
                    };
                }
                skillId++;
            }

            DataUnit.ActionMartialData actionMartial2 = battleUnits[0].data.unitData.GetActionMartial(battleUnits[0].data.unitData.skillRight);
            if (actionMartial2 != null)
            {
                DataProps.MartialData martialData = actionMartial2.data.To<DataProps.MartialData>();
                string skillName = GameTool.LS(martialData.martialInfo.name);
                int mpCost = (int)(battleStats[0].maxMp * 0.20f); // 20%灵力
                bool canUse = battleStats[0].mp >= mpCost;

                string buttonText = $"{skillName}(<color=#0080FF>灵力-{mpCost}</color>)";
                skillOptions[skillId] = buttonText;

                if (canUse)
                {
                    skillCallbacks[skillId] = () => ExecuteSkill(skillName);
                }
                else
                {
                    skillCallbacks[skillId] = () => {
                        UITipItem.AddTip("灵力不足！", 1f);
                        ReturnToBattleMenu();
                    };
                }
                skillId++;
            }

            DataUnit.ActionMartialData actionMartial3 = battleUnits[0].data.unitData.GetActionMartial(battleUnits[0].data.unitData.ultimate);
            if (actionMartial3 != null)
            {
                DataProps.MartialData martialData = actionMartial3.data.To<DataProps.MartialData>();
                string skillName = GameTool.LS(martialData.martialInfo.name);
                int mpCost = (int)(battleStats[0].maxMp * 0.30f); // 30%灵力
                bool canUse = battleStats[0].mp >= mpCost;

                string buttonText = $"{skillName}(<color=#0080FF>灵力-{mpCost}</color>)";
                skillOptions[skillId] = buttonText;

                if (canUse)
                {
                    skillCallbacks[skillId] = () => ExecuteSkill(skillName);
                }
                else
                {
                    skillCallbacks[skillId] = () => {
                        UITipItem.AddTip("灵力不足！", 1f);
                        ReturnToBattleMenu();
                    };
                }
                skillId++;
            }

            skillOptions[skillId] = "返回";
            skillCallbacks[skillId] = () => ReturnToBattleMenu();

            string skillText = "请选择要使用的技能：";
            Tools.CreateDialogue(battleDialogId, skillText, battleUnits[0], battleUnits[1], skillOptions, skillCallbacks);
        }


        /// <summary>
        /// 显示道具选项
        /// </summary>
        private static void ShowItemOptions()
        {
            Dictionary<int, string> itemOptions = new Dictionary<int, string>();
            Dictionary<int, Action> itemCallbacks = new Dictionary<int, Action>();

            int itemId = 3000;

            // 显示真实的战斗道具
            foreach (var kvp in currentBattleItems)
            {
                string itemName = kvp.Key;
                string effectString = kvp.Value;
                string chineseEffect = ConvertEffectToChinese(effectString);

                // 计算念力消耗：效果值的一半
                float effectValue = 0f;
                foreach (var effectType in effectTypeMapping.Keys)
                {
                    if (effectString.Contains(effectType))
                    {
                        string valueStr = effectString.Replace(effectType, "").Replace("%", "");
                        float.TryParse(valueStr, out effectValue);
                        break;
                    }
                }
                int energyCost = (int)(battleStats[0].maxEnergy * (effectValue / 2f / 100f));
                bool canUse = battleStats[0].energy >= energyCost;

                string buttonText = $"{itemName}({chineseEffect}，<color=#FFD700>念力-{energyCost}</color>)";
                itemOptions[itemId] = buttonText;

                if (canUse)
                {
                    itemCallbacks[itemId] = () => ExecuteItem(itemName, effectString);
                }
                else
                {
                    itemCallbacks[itemId] = () => {
                        UITipItem.AddTip("念力不足！", 1f);
                        ReturnToBattleMenu();
                    };
                }
                itemId++;
            }

            // 如果没有可用道具
            if (currentBattleItems.Count == 0)
            {
                itemOptions[itemId] = "无可用道具";
                itemCallbacks[itemId] = () => ReturnToBattleMenu();
                itemId++;
            }

            itemOptions[itemId] = "返回";
            itemCallbacks[itemId] = () => ReturnToBattleMenu();

            string itemText = currentBattleItems.Count > 0 ? "请选择要使用的道具：" : "当前没有可用的道具";
            Tools.CreateDialogue(battleDialogId, itemText, battleUnits[0], battleUnits[1], itemOptions, itemCallbacks);
        }

        /// <summary>
        /// 执行技能
        /// </summary>
        /// <param name="skillName">技能名称</param>
        private static void ExecuteSkill(string skillName)
        {
            // 存储技能信息到待执行行动中
            pendingPlayerActionType = "skill:" + skillName;

            // 处理完整回合
            ExecuteRound();
        }



        // 修改ExecuteItem方法 - 道具恢复生命
        private static void ExecuteItem(string itemName, string effectString)
        {
            // 存储道具信息到待执行行动中
            pendingPlayerActionType = "item:" + itemName + ":" + effectString;

            // 处理完整回合
            ExecuteRound();
        }

        // 添加检查战斗结束的方法
        private static bool CheckBattleEnd()
        {
            return battleStats[0].health <= 0 || battleStats[battleStats.Count - 1].health <= 0;
        }

        // 添加显示结局选择的方法
        private static void ShowEndingOptions(string finalText)
        {
            Dictionary<int, string> endingOptions = new Dictionary<int, string>();
            Dictionary<int, Action> endingCallbacks = new Dictionary<int, Action>();

            if (battleStats[battleStats.Count - 1].health <= 0)
            {
                // 玩家胜利，显示两种胜利结局
                endingOptions[8003] = "正常胜利";
                endingCallbacks[8003] = () => {
                    SetBattleEnding(BattleEndingType.VictoryNormal);
                    ExecuteBattleEndLogic();
                };

                endingOptions[8004] = "对方战败惩罚";
                endingCallbacks[8004] = () => {
                    SetBattleEnding(BattleEndingType.VictorySex);
                    ExecuteBattleEndLogic();
                };
            }
            else if (battleStats[0].health <= 0)
            {
                // 玩家失败，显示两种失败结局
                endingOptions[8001] = "正常战败";
                endingCallbacks[8001] = () => {
                    SetBattleEnding(BattleEndingType.DefeatNormal);
                    ExecuteBattleEndLogic();
                };

                endingOptions[8002] = "战败惩罚";
                endingCallbacks[8002] = () => {
                    SetBattleEnding(BattleEndingType.DefeatSex);
                    ExecuteBattleEndLogic();
                };
            }

            Tools.CreateDialogue(battleDialogId, finalText, battleUnits[0], battleUnits[1], endingOptions, endingCallbacks);
        }

        private static void ExecuteBattleEndLogic()
        {
            battleInProgress = false;
            allUnitsBattleItems.Clear();
            lastDisplayedContent = "";

            // 添加战斗结果到对话历史
            string battleResult = DetermineBattleResult();
            ModMain.currentRequest.AddUserMessage($"战斗结果：{battleResult}");

            // 触发LLM请求继续对话
            ModMain.RunOnMainThread(() => {
                UITipItem.AddTip("对方思考如何反应中~", 1f);
                ModMain.llmRequestStartTime = Time.time;
                Tools.SendLLMRequest(ModMain.currentRequest, (response) => {
                    ModMain.pendingLLMResponse = response;
                });
            });
        }

        /// <summary>
        /// 返回战斗主菜单
        /// </summary>
        private static void ReturnToBattleMenu()
        {
            string statusText = GetBattleStatusText();
            string battleText;

            // 如果有保存的上次内容，则显示它
            if (!string.IsNullOrEmpty(lastDisplayedContent))
            {
                battleText = $"{lastDisplayedContent}\n\n{statusText}";
            }
            else
            {
                battleText = $"{statusText}";
            }

            var (options, callbacks) = GenerateBattleOptions();
            Tools.CreateDialogue(battleDialogId, battleText, battleUnits[0], battleUnits[battleUnits.Count - 1], options, callbacks);
        }

        private static string GetHealthStatus(BattleCharacter character)
        {
            if (character.health <= 0)
                return "【失去战斗力】";

            float healthPercentage = (float)character.health / character.maxHealth;

            if (healthPercentage <= 0.33f)
                return "【重伤】";
            else if (healthPercentage <= 0.66f)
                return "【轻伤】";
            else
                return "";
        }
        /// <summary>
        /// 切换回合
        /// </summary>
        /// <param name="actionResult">上一个行动的结果</param>


        /// <summary>
        /// 执行敌方回合
        /// </summary>
        private static void ExecuteRound()
        {
            // 特殊处理：投降和逃跑不进入正常回合流程
            if (pendingPlayerActionType == "surrender")
            {
                ExecuteSurrender();
                return;
            }
            else if (pendingPlayerActionType == "flee")
            {
                SetBattleEnding(BattleEndingType.FleeSuccess);
                string fleeResult = $"{battleUnits[0].data.unitData.propertyData.GetName()}成功逃离了战斗！";
                EndBattle(fleeResult);
                return;
            }

            currentTurnLogs.Clear();

            // 计算先攻顺序
            List<int> initiativeOrder = CalculateInitiativeOrder();

            int actionSequence = 1; // 按照实际行动顺序编号，从1开始

            foreach (int unitIndex in initiativeOrder)
            {
                // 再次检查角色是否还活着（防止在本回合中被击败）
                if (battleStats[unitIndex].health <= 0) continue;

                string actionResult = "";

                if (unitIndex == 0) // 玩家
                {
                    actionResult = ExecutePlayerAction();
                }
                else // NPC
                {
                    actionResult = ExecuteNPCAction(unitIndex);
                }

                // 记录行动日志 - 使用实际行动顺序编号
                string logEntry = $"{actionSequence}. {actionResult}";
                currentTurnLogs.Add(logEntry);
                actionSequence++; // 递增序号

                // 检查是否有人失去战斗力，如果有则结束回合
                if (CheckBattleEnd()) break;
            }

            // 回合结束，调用SwitchTurn处理后续
            SwitchTurn("");
        }

        private static string ExecutePlayerSkill(string skillName)
        {
            string playerName = battleUnits[0].data.unitData.propertyData.GetName();
            string enemyName = battleUnits[battleUnits.Count - 1].data.unitData.propertyData.GetName();

            // 确定技能倍率和灵力消耗
            float multiplier = 6f;
            string skillPosition = "未知";
            int mpCost = 0;

            // 检查技能位置
            DataUnit.ActionMartialData leftSkill = battleUnits[0].data.unitData.GetActionMartial(battleUnits[0].data.unitData.skillLeft);
            if (leftSkill != null)
            {
                DataProps.MartialData leftMartialData = leftSkill.data.To<DataProps.MartialData>();
                if (GameTool.LS(leftMartialData.martialInfo.name) == skillName)
                {
                    multiplier = skillLeftMultiplier;
                    skillPosition = "武/灵技";
                    mpCost = (int)(battleStats[0].maxMp * 0.15f);
                }
            }

            DataUnit.ActionMartialData rightSkill = battleUnits[0].data.unitData.GetActionMartial(battleUnits[0].data.unitData.skillRight);
            if (rightSkill != null)
            {
                DataProps.MartialData rightMartialData = rightSkill.data.To<DataProps.MartialData>();
                if (GameTool.LS(rightMartialData.martialInfo.name) == skillName)
                {
                    multiplier = skillRightMultiplier;
                    skillPosition = "绝技";
                    mpCost = (int)(battleStats[0].maxMp * 0.20f);
                }
            }

            DataUnit.ActionMartialData ultimateSkill = battleUnits[0].data.unitData.GetActionMartial(battleUnits[0].data.unitData.ultimate);
            if (ultimateSkill != null)
            {
                DataProps.MartialData ultimateMartialData = ultimateSkill.data.To<DataProps.MartialData>();
                if (GameTool.LS(ultimateMartialData.martialInfo.name) == skillName)
                {
                    multiplier = ultimateSkillMultiplier;
                    skillPosition = "神通";
                    mpCost = (int)(battleStats[0].maxMp * 0.30f);
                }
            }

            // 检查并消耗灵力
            if (battleStats[0].mp < mpCost)
            {
                return $"{playerName}尝试使用{skillName}，但灵力不足！";
            }
            battleStats[0].mp -= mpCost;

            int damageResult = CalculateDamage(battleStats[0], battleStats[battleStats.Count - 1], multiplier);

            // 处理闪避情况
            if (damageResult == -1)
            {
                return $"{playerName}消耗了{mpCost}点灵力使用了{skillName}({skillPosition})，但{enemyName}灵活地闪避了攻击！(当前灵力：{battleStats[0].mp}/{battleStats[0].maxMp})";
            }

            // 处理暴击情况
            bool isCriticalHit = damageResult < 0;
            int actualDamage = Math.Abs(damageResult);

            battleStats[battleStats.Count - 1].health = Math.Max(0, battleStats[battleStats.Count - 1].health - actualDamage);

            string enemyHealthStatus = GetHealthStatus(battleStats[battleStats.Count - 1]);
            string healthStatusText = string.IsNullOrEmpty(enemyHealthStatus) ? "" : enemyHealthStatus;
            string critText = isCriticalHit ? "【暴击】" : "";

            return $"{playerName}消耗了{mpCost}点灵力使用了{skillName}({skillPosition}){critText}，对{enemyName}造成了{actualDamage}点伤害！(当前灵力：{battleStats[0].mp}/{battleStats[0].maxMp}) ({enemyName}剩余生命：{battleStats[battleStats.Count - 1].health}/{battleStats[battleStats.Count - 1].maxHealth}{healthStatusText})";
        }


        // 7. 新增：执行玩家道具的函数
        private static bool ParseItemEffect(string effectString, out string effectType, out float effectValue)
        {
            effectType = "";
            effectValue = 0f;

            if (string.IsNullOrEmpty(effectString))
                return false;

            // 转换为小写便于匹配
            string lowerEffectString = effectString.ToLower();

            // 检查每种效果类型
            foreach (var effectTypeKey in effectTypeMapping.Keys)
            {
                if (lowerEffectString.Contains(effectTypeKey))
                {
                    effectType = effectTypeKey;

                    // 提取数值部分：移除效果类型，然后清理格式
                    string valueStr = lowerEffectString.Replace(effectTypeKey, "");

                    // 移除各种可能的符号和空格
                    valueStr = valueStr.Replace("+", "")    // 移除加号
                                      .Replace("-", "")     // 移除减号  
                                      .Replace("%", "")     // 移除百分号
                                      .Replace("：", "")    // 移除中文冒号
                                      .Replace(":", "")     // 移除英文冒号
                                      .Replace(" ", "")     // 移除空格
                                      .Replace("　", "");   // 移除全角空格

                    // 尝试解析数值
                    if (float.TryParse(valueStr, out effectValue))
                    {
                        return true;
                    }

                    break; // 找到匹配的效果类型但数值解析失败，跳出循环
                }
            }

            return false;
        }

        // 7. 新增：执行玩家道具的函数
        private static string ExecuteUnitItem(int unitIndex, string itemName, string effectString)
        {
            var unitStats = battleStats[unitIndex];
            string unitName = battleUnits[unitIndex].data.unitData.propertyData.GetName();
            string resultText = "";

            // 解析效果字符串
            string effectType;
            float effectValue;
            if (!ParseItemEffect(effectString, out effectType, out effectValue))
            {
                return $"{unitName}使用了{itemName}，但效果格式无法识别！";
            }

            if (effectString.Contains("attack"))
            {
                effectType = "attack";
                string valueStr = effectString.Replace("attack", "").Replace("%", "");
                float.TryParse(valueStr, out effectValue);
            }
            else if (effectString.Contains("defense"))
            {
                effectType = "defense";
                string valueStr = effectString.Replace("defense", "").Replace("%", "");
                float.TryParse(valueStr, out effectValue);
            }
            else if (effectString.Contains("health"))
            {
                effectType = "health";
                string valueStr = effectString.Replace("health", "").Replace("%", "");
                float.TryParse(valueStr, out effectValue);
            }
            else if (effectString.Contains("mp"))
            {
                effectType = "mp";
                string valueStr = effectString.Replace("mp", "").Replace("%", "");
                float.TryParse(valueStr, out effectValue);
            }
            else if (effectString.Contains("energy"))
            {
                effectType = "energy";
                string valueStr = effectString.Replace("energy", "").Replace("%", "");
                float.TryParse(valueStr, out effectValue);
            }
            else if (effectString.Contains("speed"))
            {
                effectType = "speed";
                string valueStr = effectString.Replace("speed", "").Replace("%", "");
                float.TryParse(valueStr, out effectValue);
            }
            else if (effectString.Contains("crit"))
            {
                effectType = "crit";
                string valueStr = effectString.Replace("crit", "").Replace("%", "");
                float.TryParse(valueStr, out effectValue);
            }

            effectValue = effectValue / 100f; // 转换为小数

            // 应用效果
            switch (effectType)
            {
                case "attack":
                    int attackIncrease = (int)(unitStats.attack * effectValue);
                    unitStats.attack += attackIncrease;
                    resultText = $"{unitName}使用了{itemName}，攻击力增加了{attackIncrease}点！";
                    break;
                case "defense":
                    int defenseIncrease = (int)(unitStats.defense * effectValue);
                    unitStats.defense += defenseIncrease;
                    resultText = $"{unitName}使用了{itemName}，防御力增加了{defenseIncrease}点！";
                    break;
                case "health":
                    int healAmount = (int)(unitStats.maxHealth * effectValue);
                    unitStats.health = Math.Min(unitStats.maxHealth, unitStats.health + healAmount);
                    resultText = $"{unitName}使用了{itemName}，恢复了{healAmount}点生命值！(当前生命：{unitStats.health}/{unitStats.maxHealth})";
                    break;
                case "mp":
                    int mpAmount = (int)(unitStats.maxMp * effectValue);
                    unitStats.mp = Math.Min(unitStats.maxMp, unitStats.mp + mpAmount);
                    resultText = $"{unitName}使用了{itemName}，恢复了{mpAmount}点灵力值！(当前灵力：{unitStats.mp}/{unitStats.maxMp})";
                    break;
                case "energy":
                    int energyAmount = (int)(unitStats.maxEnergy * effectValue);
                    unitStats.energy = Math.Min(unitStats.maxEnergy, unitStats.energy + energyAmount);
                    resultText = $"{unitName}使用了{itemName}，恢复了{energyAmount}点念力值！(当前念力：{unitStats.energy}/{unitStats.maxEnergy})";
                    break;
                case "speed":
                    int moveSpeedIncrease = (int)(unitStats.moveSpeed * effectValue);
                    int footSpeedIncrease = (int)(unitStats.footSpeed * effectValue);
                    unitStats.moveSpeed += moveSpeedIncrease;
                    unitStats.footSpeed += footSpeedIncrease;
                    resultText = $"{unitName}使用了{itemName}，速度增加了{moveSpeedIncrease}点！";
                    break;
                case "crit":
                    int critIncrease = (int)(unitStats.crit * effectValue);
                    unitStats.crit += critIncrease;
                    resultText = $"{unitName}使用了{itemName}，暴击增加了{critIncrease}点！";
                    break;
                default:
                    resultText = $"{unitName}使用了{itemName}，但没有产生任何效果！";
                    break;
            }

            return resultText;
        }

        private static string ExecutePlayerItem(string itemName, string effectString)
        {
            // 计算念力消耗：效果值的一半
            float effectValue = 0f;
            foreach (var effectType in effectTypeMapping.Keys)
            {
                if (effectString.Contains(effectType))
                {
                    string valueStr = effectString.Replace(effectType, "").Replace("%", "");
                    float.TryParse(valueStr, out effectValue);
                    break;
                }
            }
            int energyCost = (int)(battleStats[0].maxEnergy * (effectValue / 2f / 100f));

            // 检查并消耗念力
            if (battleStats[0].energy < energyCost)
            {
                return $"{battleUnits[0].data.unitData.propertyData.GetName()}尝试使用{itemName}，但念力不足！";
            }
            battleStats[0].energy -= energyCost;

            string result = ExecuteUnitItem(0, itemName, effectString);
            result += $"(消耗了{energyCost}点念力，当前念力：{battleStats[0].energy}/{battleStats[0].maxEnergy})";

            // 从玩家道具列表中移除此道具
            if (allUnitsBattleItems.ContainsKey(0))
            {
                allUnitsBattleItems[0].Remove(itemName);
                Debug.Log($"玩家道具 {itemName} 已使用，剩余道具数量: {allUnitsBattleItems[0].Count}");
            }

            return result;
        }

        private static string ExecutePlayerAction()
        {
            if (pendingPlayerActionType.StartsWith("skill:"))
            {
                string skillName = pendingPlayerActionType.Substring(6); // 去掉"skill:"前缀
                return ExecutePlayerSkill(skillName);
            }
            else if (pendingPlayerActionType.StartsWith("item:"))
            {
                string[] parts = pendingPlayerActionType.Split(':');
                if (parts.Length >= 3)
                {
                    string itemName = parts[1];
                    string effectString = parts[2];
                    return ExecutePlayerItem(itemName, effectString);
                }
                return "道具使用失败！";
            }
            else
            {
                switch (pendingPlayerActionType)
                {
                    case "attack":
                        return ExecuteAttack();
                    case "defend":
                        return ExecuteDefend();
                    default:
                        return "无效的行动！";
                }
            }
        }

        private static string ExecuteNPCAction(int npcIndex)
        {
            var currentUnit = battleUnits[npcIndex];
            var currentUnitStats = battleStats[npcIndex];
            string unitName = currentUnit.data.unitData.propertyData.GetName();
            string playerName = battleUnits[0].data.unitData.propertyData.GetName();
            var currentPlayerStats = battleStats[0];

            // 更新防御状态（针对主要敌人）
            if (npcIndex == battleStats.Count - 1)
            {
                if (enemyDefenseTurns > 0)
                {
                    enemyDefenseTurns--;
                    if (enemyDefenseTurns == 0)
                    {
                        enemyDefenseBonus = 0;
                    }
                }
            }

            // 检查是否有可用道具
            bool hasItems = allUnitsBattleItems.ContainsKey(npcIndex) && allUnitsBattleItems[npcIndex].Count > 0;

            // 扩展行动选项（包含道具）
            List<string> enemyActions = new List<string> { "攻击", "防御", "技能" };
            if (hasItems)
            {
                enemyActions.Add("道具");
            }

            string selectedAction = enemyActions[UnityEngine.Random.Range(0, enemyActions.Count)];

            string enemyActionResult = "";

            switch (selectedAction)
            {
                case "攻击":
                    int damageResult = CalculateDamage(currentUnitStats, currentPlayerStats, attackMultiplier);

                    // 处理闪避情况
                    if (damageResult == -1)
                    {
                        enemyActionResult = $"{unitName}对{playerName}发起攻击，但{playerName}灵活地闪避了攻击！";
                        break;
                    }

                    // 处理暴击情况
                    bool isCriticalHit = damageResult < 0;
                    int actualDamage = Math.Abs(damageResult);

                    currentPlayerStats.health = Math.Max(0, currentPlayerStats.health - actualDamage);

                    string playerHealthStatus = GetHealthStatus(currentPlayerStats);
                    string playerHealthStatusText = string.IsNullOrEmpty(playerHealthStatus) ? "" : playerHealthStatus;

                    string critText = isCriticalHit ? "【暴击】" : "";

                    enemyActionResult = $"{unitName}对{playerName}发起攻击{critText}，造成了{actualDamage}点伤害！({playerName}剩余生命：{currentPlayerStats.health}/{currentPlayerStats.maxHealth}{playerHealthStatusText})";
                    break;

                case "防御":
                    if (npcIndex == battleStats.Count - 1) // 只有主要敌人可以防御
                    {
                        enemyDefenseBonus = (int)(currentUnitStats.defense * 0.5f);
                        enemyDefenseTurns = 2;
                    }
                    enemyActionResult = $"{unitName}采取了防御姿态，防御力提升50%，持续2轮！";
                    break;

                case "技能":
                    enemyActionResult = ExecuteNPCSkill(npcIndex);
                    break;

                case "道具":
                    enemyActionResult = ExecuteNPCItem(npcIndex);
                    break;
            }

            return enemyActionResult;
        }

        private static string ExecuteNPCSkill(int npcIndex)
        {
            var currentUnit = battleUnits[npcIndex];
            var currentUnitStats = battleStats[npcIndex];
            string unitName = currentUnit.data.unitData.propertyData.GetName();
            string playerName = battleUnits[0].data.unitData.propertyData.GetName();
            var currentPlayerStats = battleStats[0];

            // 更新防御状态（针对主要敌人）
            if (npcIndex == battleStats.Count - 1)
            {
                if (enemyDefenseTurns > 0)
                {
                    enemyDefenseTurns--;
                    if (enemyDefenseTurns == 0)
                    {
                        enemyDefenseBonus = 0;
                    }
                }
            }

            // 获取当前单位的技能列表
            List<(string skillName, float multiplier, float mpPercent)> enemySkills = new List<(string, float, float)>();

            DataUnit.ActionMartialData enemyLeftSkill = currentUnit.data.unitData.GetActionMartial(currentUnit.data.unitData.skillLeft);
            if (enemyLeftSkill != null)
            {
                DataProps.MartialData martialData = enemyLeftSkill.data.To<DataProps.MartialData>();
                enemySkills.Add((GameTool.LS(martialData.martialInfo.name), skillLeftMultiplier, 0.15f));
            }

            DataUnit.ActionMartialData enemyRightSkill = currentUnit.data.unitData.GetActionMartial(currentUnit.data.unitData.skillRight);
            if (enemyRightSkill != null)
            {
                DataProps.MartialData martialData = enemyRightSkill.data.To<DataProps.MartialData>();
                enemySkills.Add((GameTool.LS(martialData.martialInfo.name), skillRightMultiplier, 0.20f));
            }

            DataUnit.ActionMartialData enemyUltimateSkill = currentUnit.data.unitData.GetActionMartial(currentUnit.data.unitData.ultimate);
            if (enemyUltimateSkill != null)
            {
                DataProps.MartialData martialData = enemyUltimateSkill.data.To<DataProps.MartialData>();
                enemySkills.Add((GameTool.LS(martialData.martialInfo.name), ultimateSkillMultiplier, 0.30f));
            }

            if (enemySkills.Count > 0)
            {
                // 只选择有足够灵力的技能
                List<(string, float, float)> availableSkills = new List<(string, float, float)>();
                foreach (var skill in enemySkills)
                {
                    int mpCost = (int)(currentUnitStats.maxMp * skill.mpPercent);
                    if (currentUnitStats.mp >= mpCost)
                    {
                        availableSkills.Add(skill);
                    }
                }

                if (availableSkills.Count > 0)
                {
                    var selectedSkill = availableSkills[UnityEngine.Random.Range(0, availableSkills.Count)];
                    string skillName = selectedSkill.Item1;
                    float skillMultiplier = selectedSkill.Item2;
                    int skillMpCost = (int)(currentUnitStats.maxMp * selectedSkill.Item3);

                    // 消耗灵力
                    currentUnitStats.mp -= skillMpCost;

                    int skillDamageResult = CalculateDamage(currentUnitStats, currentPlayerStats, skillMultiplier);

                    // 处理闪避情况
                    if (skillDamageResult == -1)
                    {
                        return $"{unitName}消耗了{skillMpCost}点灵力使用了{skillName}，但{playerName}灵活地闪避了攻击！(当前灵力：{currentUnitStats.mp}/{currentUnitStats.maxMp})";
                    }

                    // 处理暴击情况
                    bool skillIsCriticalHit = skillDamageResult < 0;
                    int skillActualDamage = Math.Abs(skillDamageResult);

                    currentPlayerStats.health = Math.Max(0, currentPlayerStats.health - skillActualDamage);

                    string playerHealthStatusForSkill = GetHealthStatus(currentPlayerStats);
                    string playerHealthStatusTextForSkill = string.IsNullOrEmpty(playerHealthStatusForSkill) ? "" : playerHealthStatusForSkill;
                    string skillCritText = skillIsCriticalHit ? "【暴击】" : "";

                    return $"{unitName}消耗了{skillMpCost}点灵力使用了{skillName}{skillCritText}，对{playerName}造成了{skillActualDamage}点伤害！(当前灵力：{currentUnitStats.mp}/{currentUnitStats.maxMp}) ({playerName}剩余生命：{currentPlayerStats.health}/{currentPlayerStats.maxHealth}{playerHealthStatusTextForSkill})";
                }
            }

            // 如果没有技能或没有足够灵力，改为攻击
            int attackDamageResult = CalculateDamage(currentUnitStats, currentPlayerStats, attackMultiplier);

            // 处理闪避情况
            if (attackDamageResult == -1)
            {
                return $"{unitName}发起攻击，但{playerName}灵活地闪避了攻击！";
            }

            // 处理暴击情况
            bool attackIsCriticalHit = attackDamageResult < 0;
            int attackActualDamage = Math.Abs(attackDamageResult);

            currentPlayerStats.health = Math.Max(0, currentPlayerStats.health - attackActualDamage);

            string playerHealthStatusForAttack = GetHealthStatus(currentPlayerStats);
            string playerHealthStatusTextForAttack = string.IsNullOrEmpty(playerHealthStatusForAttack) ? "" : playerHealthStatusForAttack;
            string attackCritText = attackIsCriticalHit ? "【暴击】" : "";

            return $"{unitName}发起攻击{attackCritText}，对{playerName}造成了{attackActualDamage}点伤害！({playerName}剩余生命：{currentPlayerStats.health}/{currentPlayerStats.maxHealth}{playerHealthStatusTextForAttack})";
        }


        // 7. 新增：执行NPC道具使用的函数
        private static string ExecuteNPCItem(int npcIndex)
        {
            if (!allUnitsBattleItems.ContainsKey(npcIndex) || allUnitsBattleItems[npcIndex].Count == 0)
            {
                return $"{battleUnits[npcIndex].data.unitData.propertyData.GetName()}没有可用的道具！";
            }

            // 找到有足够念力且符合使用条件的道具
            var availableItems = new List<KeyValuePair<string, string>>();
            var npcStats = battleStats[npcIndex];

            foreach (var item in allUnitsBattleItems[npcIndex])
            {
                // 计算念力消耗
                float effectValue = 0f;
                string effectType = "";

                foreach (var effectTypeKey in effectTypeMapping.Keys)
                {
                    if (item.Value.Contains(effectTypeKey))
                    {
                        string valueStr = item.Value.Replace(effectTypeKey, "").Replace("%", "");
                        float.TryParse(valueStr, out effectValue);
                        effectType = effectTypeKey;
                        break;
                    }
                }

                int energyCost1 = (int)(npcStats.maxEnergy * (effectValue / 2f / 100f));

                // 检查是否有足够念力
                if (npcStats.energy < energyCost1)
                {
                    continue; // 念力不足，跳过此道具
                }

                // 检查恢复类道具的使用条件
                bool shouldUseItem = true;

                switch (effectType)
                {
                    case "health":
                        // 生命值恢复道具：只有生命不满时才使用
                        if (npcStats.health >= npcStats.maxHealth)
                        {
                            shouldUseItem = false;
                        }
                        break;

                    case "mp":
                        // 灵力恢复道具：只有灵力不满时才使用
                        if (npcStats.mp >= npcStats.maxMp)
                        {
                            shouldUseItem = false;
                        }
                        break;

                    case "energy":
                        // 念力恢复道具：只有念力不满时才使用
                        if (npcStats.energy >= npcStats.maxEnergy)
                        {
                            shouldUseItem = false;
                        }
                        break;

                    case "attack":
                    case "defense":
                    case "speed":
                    case "crit":
                        // 属性增强道具：总是可以使用
                        shouldUseItem = true;
                        break;

                    default:
                        // 未知效果类型：保险起见允许使用
                        shouldUseItem = true;
                        break;
                }

                if (shouldUseItem)
                {
                    availableItems.Add(item);
                }
            }

            if (availableItems.Count == 0)
            {
                // 没有道具可用时，改为普通攻击
                string unitName = battleUnits[npcIndex].data.unitData.propertyData.GetName();
                string playerName = battleUnits[0].data.unitData.propertyData.GetName();
                var currentUnitStats = battleStats[npcIndex];
                var currentPlayerStats = battleStats[0];

                int attackDamageResult = CalculateDamage(currentUnitStats, currentPlayerStats, attackMultiplier);

                // 处理闪避情况
                if (attackDamageResult == -1)
                {
                    return $"{unitName}发起攻击，但{playerName}灵活地闪避了攻击！";
                }

                // 处理暴击情况
                bool attackIsCriticalHit = attackDamageResult < 0;
                int attackActualDamage = Math.Abs(attackDamageResult);

                currentPlayerStats.health = Math.Max(0, currentPlayerStats.health - attackActualDamage);

                string playerHealthStatusForAttack = GetHealthStatus(currentPlayerStats);
                string playerHealthStatusTextForAttack = string.IsNullOrEmpty(playerHealthStatusForAttack) ? "" : playerHealthStatusForAttack;
                string attackCritText = attackIsCriticalHit ? "【暴击】" : "";

                return $"{unitName}发起攻击{attackCritText}，对{playerName}造成了{attackActualDamage}点伤害！({playerName}剩余生命：{currentPlayerStats.health}/{currentPlayerStats.maxHealth}{playerHealthStatusTextForAttack})";
            }

            // 随机选择一个可用道具
            var selectedItem = availableItems[UnityEngine.Random.Range(0, availableItems.Count)];
            string itemName = selectedItem.Key;
            string effectString = selectedItem.Value;

            // 计算并消耗念力
            float finalEffectValue = 0f;
            foreach (var effectTypeKey in effectTypeMapping.Keys)
            {
                if (effectString.Contains(effectTypeKey))
                {
                    string valueStr = effectString.Replace(effectTypeKey, "").Replace("%", "");
                    float.TryParse(valueStr, out finalEffectValue);
                    break;
                }
            }
            int energyCost = (int)(npcStats.maxEnergy * (finalEffectValue / 2f / 100f));
            npcStats.energy -= energyCost;

            // 使用通用道具执行函数
            string result = ExecuteUnitItem(npcIndex, itemName, effectString);
            result += $"(消耗了{energyCost}点念力，当前念力：{npcStats.energy}/{npcStats.maxEnergy})";

            // 从该单位的道具列表中移除
            allUnitsBattleItems[npcIndex].Remove(itemName);
            Debug.Log($"NPC {battleUnits[npcIndex].data.unitData.propertyData.GetName()} 智能使用了道具 {itemName}");

            return result;
        }

        /// <summary>
        /// 结束战斗
        /// </summary>
        /// <param name="endMessage">结束消息</param>
        private static void EndBattle(string endMessage)
        {
            battleInProgress = false;

            Dictionary<int, string> endOptions = new Dictionary<int, string>();
            Dictionary<int, Action> endCallbacks = new Dictionary<int, Action>();

            endOptions[5000] = "确定";
            endCallbacks[5000] = () => {

                // 添加战斗结果到对话历史
                string battleResult = DetermineBattleResult(); // 需要实现这个方法
                ModMain.currentRequest.AddUserMessage($"战斗结果：{battleResult}");

                // 触发LLM请求继续对话
                ModMain.RunOnMainThread(() => {
                    UITipItem.AddTip("对方思考如何反应中~", 1f);
                    ModMain.llmRequestStartTime = Time.time;
                    Tools.SendLLMRequest(ModMain.currentRequest, (response) => {
                        // 这里需要将响应传回到ModMain的处理逻辑
                        // 可以通过ModMain的静态方法来处理
                        ModMain.pendingLLMResponse = response;
                    });
                });
            };

            Tools.CreateDialogue(battleDialogId, endMessage, battleUnits[0], battleUnits[1], endOptions, endCallbacks);
        }



        // 新增：判断战斗结果的方法
        private static string DetermineBattleResult()
        {
            // 构建战斗结果描述
            string battleSummary = "";

            if (allBattleContents.Count > 0)
            {
                // 将所有战斗过程的content按顺序组合
                battleSummary = "战斗过程回顾：\n";
                for (int i = 0; i < allBattleContents.Count; i++)
                {
                    battleSummary += $"{i + 1}. {allBattleContents[i]}\n";
                }

                battleSummary += "\n";
            }

            if (!string.IsNullOrEmpty(battleEnding))
            {
                battleSummary += battleEnding + "\n\n";
            }

            // 添加衔接prompt，引导奇遇agent根据战斗结果继续剧情
            string connectionPrompt = @"上述是刚刚结束的战斗详细过程，请根据战斗的具体过程和结果，继续推进奇遇剧情。";

            // 组合完整的战斗结果
            string fullBattleResult = battleSummary + connectionPrompt;

            Debug.Log($"战斗结果汇总: {fullBattleResult}");

            return fullBattleResult;
        }

        private static void SendBattleDescriptionRequest()
        {
            if (currentTurnLogs.Count == 0) return;

            // 构建战斗日志字符串
            string battleLog = string.Join("\n", currentTurnLogs);

            // 添加到战斗请求历史
            battleRequest.AddUserMessage(battleLog);
            UITipItem.AddTip("正在模拟战斗~", 1f);
            // 发送LLM请求
            waitingForBattleDescription = true;
            Tools.SendLLMRequest(battleRequest, (response) => {
                pendingBattleDescription = response;
            });

            // 清空当前轮次日志
            currentTurnLogs.Clear();
        }

        /// <summary>
        /// 处理战斗描述响应
        /// </summary>
        private static void HandleBattleDescriptionResponse()
        {
            if (string.IsNullOrEmpty(pendingBattleDescription)) return;

            // 添加assistant消息到战斗历史
            battleRequest.AddAssistantMessage(pendingBattleDescription);

            // 解析并显示战斗描述（包含所有显示逻辑）
            ParseBattleDescription(pendingBattleDescription);

            // 清理状态
            pendingBattleDescription = null;
            waitingForBattleDescription = false;
        }

        /// <summary>
        /// 解析战斗描述响应
        /// </summary>
        /// <summary>
        /// 解析战斗描述响应并处理显示逻辑
        /// </summary>
        private static void ParseBattleDescription(string rawResponse)
        {
            try
            {
                // 使用Tools的预处理逻辑
                string processedResponse = rawResponse;

                // 去除markdown代码块标记 (学习自GetProcessedJsonString)
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
                if (processedResponse.Length > 0 && processedResponse[0] != '{')
                {
                    int bracketIndex = processedResponse.IndexOf('{');
                    if (bracketIndex >= 0)
                    {
                        processedResponse = processedResponse.Substring(bracketIndex);
                    }
                }

                // 解析JSON获取所有content
                var contentDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(processedResponse);
                if (contentDict == null) throw new Exception("解析失败");

                // 提取并排序content
                var contents = contentDict.Where(kv => kv.Key.StartsWith("content"))
                                         .OrderBy(kv => kv.Key)
                                         .Select(kv => kv.Value)
                                         .ToList();

                if (contents.Count == 0) throw new Exception("未找到content");
                allBattleContents.AddRange(contents);

                // 在开始显示之前，先检查战斗是否应该结束
                bool shouldEndBattle = CheckBattleEnd();

                // 处理显示逻辑
                int currentIndex = 0;
                Action showNextContent = null;

                showNextContent = () => {
                    if (currentIndex < contents.Count - 1)
                    {
                        // 前N-1个content，显示对话框with"继续"按钮
                        Dictionary<int, string> tempOptions = new Dictionary<int, string>();
                        Dictionary<int, Action> tempCallbacks = new Dictionary<int, Action>();

                        tempOptions[7000] = "继续";
                        tempCallbacks[7000] = () => {
                            currentIndex++;
                            showNextContent();
                        };

                        Tools.CreateDialogue(battleDialogId, contents[currentIndex], battleUnits[0], battleUnits[1], tempOptions, tempCallbacks);
                    }
                    else
                    {
                        // 最后一个content，现在才检查是否需要结束战斗
                        string finalText = contents[currentIndex];
                        lastDisplayedContent = finalText;
                        if (shouldEndBattle)
                        {
                            // 有人HP<=0，显示结局选择
                            finalText += "\n\n战斗即将结束，请选择结局：";
                            ShowEndingOptions(finalText);
                        }
                        else
                        {
                            // 没人死亡，继续战斗
                            string statusText = GetBattleStatusText();
                            finalText += $"\n\n{statusText}";
                            var (options, callbacks) = GenerateBattleOptions();
                            Tools.CreateDialogue(battleDialogId, finalText, battleUnits[0], battleUnits[1], options, callbacks);
                        }
                    }
                };

                // 开始显示第一个content
                showNextContent();

            }
            catch (Exception ex)
            {
                Debug.Log($"解析战斗描述失败: {ex.Message}");

                // 失败时也要检查战斗是否结束
                if (CheckBattleEnd())
                {
                    string failText = "战斗描述解析失败，但战斗即将结束，请选择结局：";
                    ShowEndingOptions(failText);
                }
                else
                {
                    // 失败时直接回到战斗界面
                    string battleText = "战斗继续，轮到你行动了，请选择你的行动：";
                    var (options, callbacks) = GenerateBattleOptions();
                    Tools.CreateDialogue(battleDialogId, battleText, battleUnits[0], battleUnits[1], options, callbacks);
                }
            }
        }


        private static (Dictionary<int, string>, Dictionary<int, Action>) GenerateBattleOptions()
        {
            Dictionary<int, string> options = new Dictionary<int, string>();
            Dictionary<int, Action> callbacks = new Dictionary<int, Action>();

            int optionId = 1000; // 从1000开始，避免与其他选项冲突

            // 攻击选项
            options[optionId] = "攻击";
            callbacks[optionId] = () => ExecuteBattleAction("attack");
            optionId++;

            // 防御选项
            options[optionId] = "防御";
            callbacks[optionId] = () => ExecuteBattleAction("defend");
            optionId++;

            // 技能选项（这里先做一个通用的技能选项，后续可以扩展为具体技能列表）
            options[optionId] = "使用技能";
            callbacks[optionId] = () => ShowSkillOptions();
            optionId++;

            // 道具选项
            options[optionId] = "使用道具";
            callbacks[optionId] = () => ShowItemOptions();
            optionId++;

            // 修改：将逃跑和投降改为行为
            options[optionId] = "更多行为";
            callbacks[optionId] = () => ShowActionOptions();
            optionId++;

            return (options, callbacks);
        }

        private static void ShowActionOptions()
        {
            Dictionary<int, string> actionOptions = new Dictionary<int, string>();
            Dictionary<int, Action> actionCallbacks = new Dictionary<int, Action>();

            int actionId = 4000;

            // 逃跑选项
            actionOptions[actionId] = "逃跑";
            actionCallbacks[actionId] = () => ExecuteBattleAction("flee");
            actionId++;

            // 投降选项
            actionOptions[actionId] = "投降";
            actionCallbacks[actionId] = () => ExecuteBattleAction("surrender");
            actionId++;

            // 快进到结束选项
            int playerPower = FormulaTool.UnitPower.TotalPower(battleUnits[0].data);
            int enemyPower = FormulaTool.UnitPower.TotalPower(battleUnits[battleUnits.Count - 1].data);

            string powerCompareText = playerPower > enemyPower ?
                $"快进到结束（我方战斗力{playerPower}>敌方战斗力{enemyPower}）" :
                $"快进到结束（我方战斗力{playerPower}<敌方战斗力{enemyPower}）";

            actionOptions[actionId] = powerCompareText;
            actionCallbacks[actionId] = () => {
                currentTurnLogs.Clear(); // 清空日志
                waitingForBattleDescription = false; // 停止等待战斗描述
                pendingBattleDescription = null; // 清空待处理描述

                if (playerPower > enemyPower)
                {
                    // 我方战斗力更高，进入胜利结局选择
                    string victoryText = "基于战斗力对比，你获得了胜利！请选择结局：";
                    Dictionary<int, string> victoryOptions = new Dictionary<int, string>();
                    Dictionary<int, Action> victoryCallbacks = new Dictionary<int, Action>();

                    victoryOptions[8003] = "正常胜利";
                    victoryCallbacks[8003] = () => {
                        SetBattleEnding(BattleEndingType.VictoryNormal);
                        ExecuteBattleEndLogic();
                    };

                    victoryOptions[8004] = "对方战败惩罚";
                    victoryCallbacks[8004] = () => {
                        SetBattleEnding(BattleEndingType.VictorySex);
                        ExecuteBattleEndLogic();
                    };

                    Tools.CreateDialogue(battleDialogId, victoryText, battleUnits[0], battleUnits[1], victoryOptions, victoryCallbacks);
                }
                else
                {
                    // 敌方战斗力更高，进入战败结局选择
                    string defeatText = "基于战斗力对比，你遗憾败北！请选择结局：";
                    Dictionary<int, string> defeatOptions = new Dictionary<int, string>();
                    Dictionary<int, Action> defeatCallbacks = new Dictionary<int, Action>();

                    defeatOptions[8001] = "正常战败";
                    defeatCallbacks[8001] = () => {
                        SetBattleEnding(BattleEndingType.DefeatNormal);
                        ExecuteBattleEndLogic();
                    };

                    defeatOptions[8002] = "战败惩罚";
                    defeatCallbacks[8002] = () => {
                        SetBattleEnding(BattleEndingType.DefeatSex);
                        ExecuteBattleEndLogic();
                    };

                    Tools.CreateDialogue(battleDialogId, defeatText, battleUnits[0], battleUnits[1], defeatOptions, defeatCallbacks);
                }
            };
            actionId++;

            // 不再隐藏实力选项
            actionOptions[actionId] = "不再隐藏实力，击败敌人";
            actionCallbacks[actionId] = () => {
                currentTurnLogs.Clear(); // 清空日志
                waitingForBattleDescription = false; // 停止等待战斗描述
                pendingBattleDescription = null; // 清空待处理描述

                string realPowerText = "你不再隐藏实力，展现了真正的力量！胜利唾手可得，请选择结局：";
                Dictionary<int, string> realPowerOptions = new Dictionary<int, string>();
                Dictionary<int, Action> realPowerCallbacks = new Dictionary<int, Action>();

                realPowerOptions[8003] = "正常胜利";
                realPowerCallbacks[8003] = () => {
                    SetBattleEnding(BattleEndingType.VictoryNormal);
                    ExecuteBattleEndLogic();
                };

                realPowerOptions[8004] = "对方战败惩罚";
                realPowerCallbacks[8004] = () => {
                    SetBattleEnding(BattleEndingType.VictorySex);
                    ExecuteBattleEndLogic();
                };

                Tools.CreateDialogue(battleDialogId, realPowerText, battleUnits[0], battleUnits[1], realPowerOptions, realPowerCallbacks);
            };
            actionId++;

            // 返回选项
            actionOptions[actionId] = "返回";
            actionCallbacks[actionId] = () => ReturnToBattleMenu();

            string actionText = "请选择你的行为：";
            Tools.CreateDialogue(battleDialogId, actionText, battleUnits[0], battleUnits[1], actionOptions, actionCallbacks);
        }


        public static bool HasPendingBattleDescription()
        {
            return waitingForBattleDescription && !string.IsNullOrEmpty(pendingBattleDescription);
        }

        public static void ProcessPendingDescription()
        {
            if (HasPendingBattleDescription())
            {
                HandleBattleDescriptionResponse();
            }
        }
    }
}