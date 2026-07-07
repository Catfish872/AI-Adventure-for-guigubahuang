using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace MOD_kqAfiU
{
    // 已禁用 Harmony 自动补丁：保留神识传音（SSCYAI）外部造物监听实现代码，
    // 但不再通过监听 SSCYAI 的行动选择来自动创建气运或道具，避免聊天/行动侧隐式触发造物。
    // 如需恢复该监听，在下方类声明前重新启用 [HarmonyPatch] 即可。
    public static class SSCYAIExternalCreationHook
    {

        private enum ForcedCreationKind
        {
            Luck,
            Item
        }

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            try
            {
                // 优先命中当前已验证可触发的类型
                var aiType = AccessTools.TypeByName("MOD_SSCYAI.UnitActionAI");
                if (aiType != null)
                {
                    var aiMethod = AccessTools.Method(aiType, "SelectAction", Type.EmptyTypes);
                    if (aiMethod != null)
                    {
                        return aiMethod;
                    }
                }

                // 次选旧版 UnitAction
                var actionType = AccessTools.TypeByName("MOD_SSCYAI.UnitAction");
                if (actionType != null)
                {
                    var actionMethod = AccessTools.Method(actionType, "SelectAction", Type.EmptyTypes);
                    if (actionMethod != null)
                    {
                        return actionMethod;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        [HarmonyPostfix]
        private static void Postfix(object __instance)
        {
            try
            {
                if (__instance == null || g.world == null || g.world.playerUnit == null) return;

                int index = GetIntMember(__instance, "index", "Index");
                if (index != 3 && index != 36) return;

                var unitA = GetMemberValue(__instance, "unitA", "UnitA") as WorldUnitBase;
                var unitB = GetMemberValue(__instance, "unitB", "UnitB") as WorldUnitBase;
                if (unitA == null || unitB == null) return;

                string reqName = ResolveRequestNameFromStaticActionMessage(index);
                if (string.IsNullOrWhiteSpace(reqName)) return;

                reqName = reqName.Trim();

                if (index == 3)
                {
                    HandleLuckCreationIfMissing(reqName, unitA, unitB);
                }
                else
                {
                    HandleItemCreationIfMissing(reqName, unitA, unitB, __instance);
                }
            }
            catch { }
        }

        private static void HandleLuckCreationIfMissing(string reqName, WorldUnitBase unitA, WorldUnitBase unitB)
        {
            if (LuckExists(reqName)) return;

            UITipItem.AddTip($"{SafeUnitName(unitA)}提到的气运【{reqName}】似乎失传了，天地正在补全机缘…", 2f);

            RequestExternalCreation(ForcedCreationKind.Luck, reqName, unitA, unitB, 1);
        }

        private static void HandleItemCreationIfMissing(string reqName, WorldUnitBase unitA, WorldUnitBase unitB, object actionInstance)
        {
            bool unitAIsPlayer = IsPlayer(unitA);
            bool unitBIsPlayer = IsPlayer(unitB);
            if (unitAIsPlayer || !unitBIsPlayer) return;

            if (ItemExists(reqName)) return;

            int count = GetRequestedCount(actionInstance);
            UITipItem.AddTip($"{SafeUnitName(unitA)}索要的物件【{reqName}】暂无现世记录，正在寻觅机缘…", 2f);

            RequestExternalCreation(ForcedCreationKind.Item, reqName, unitA, unitB, count);
        }

        private static void RequestExternalCreation(ForcedCreationKind forcedKind, string reqName, WorldUnitBase source, WorldUnitBase target, int requestedCount)
        {
            var request = new LLMDialogueRequest();
            string systemPrompt = BuildSystemPrompt(forcedKind);
            string userPrompt = BuildUserPrompt(forcedKind, reqName, source, target, requestedCount);

            request.AddSystemMessage(systemPrompt);
            request.AddUserMessage(userPrompt);

            Tools.SendLLMRequest(request, (response) =>
            {
                try
                {
                    HandleCreationResponse(forcedKind, reqName, response, target, requestedCount);
                }
                catch
                {
                    UITipItem.AddTip("机缘推演中断，未能完成此物塑形。", 2f);
                }
            });
        }

        private static void HandleCreationResponse(ForcedCreationKind forcedKind, string reqName, string response, WorldUnitBase target, int requestedCount)
        {
            if (string.IsNullOrEmpty(response) || response.StartsWith("错误"))
            {
                UITipItem.AddTip("机缘未成，暂时未得到清晰回应。", 2f);
                return;
            }

            var data = ParseFirstCreationData(response);
            if (data == null)
            {
                UITipItem.AddTip("机缘散乱，未能凝成可用之物。", 2f);
                return;
            }

            NormalizeCreationData(data, forcedKind, reqName, target);

            if (forcedKind == ForcedCreationKind.Luck)
            {
                CreationSystem.CreateLuck(data.BaseInfo, data.Effects, data.ExtraInfo);
                TryGrantCreatedLuck(reqName, target);
                return;
            }

            string t = (data.Type ?? string.Empty).Trim();
            if (t.Equals("Vehicle", StringComparison.OrdinalIgnoreCase) || t.Equals("Equip", StringComparison.OrdinalIgnoreCase))
            {
                CreationSystem.CreateEquip(data.BaseInfo, data.Effects, data.ExtraInfo);
            }
            else if (t.Equals("Carried", StringComparison.OrdinalIgnoreCase) || t.Equals("Ring", StringComparison.OrdinalIgnoreCase))
            {
                CreationSystem.CreateRing(data.BaseInfo, data.Effects, data.ExtraInfo);
            }
            else
            {
                CreationSystem.CreateConsumer(data.BaseInfo, data.Effects, data.ExtraInfo);
            }

            TryGrantCreatedItem(reqName, target, requestedCount, t);
        }

        private static AICreationResponse ParseFirstCreationData(string response)
        {
            string json = ExtractJson(response);
            try
            {
                if (json.StartsWith("["))
                {
                    var arr = JsonConvert.DeserializeObject<List<AICreationResponse>>(json);
                    return arr?.FirstOrDefault();
                }

                return JsonConvert.DeserializeObject<AICreationResponse>(json);
            }
            catch
            {
                return null;
            }
        }

        private static void NormalizeCreationData(AICreationResponse data, ForcedCreationKind forcedKind, string reqName, WorldUnitBase target)
        {
            if (data.BaseInfo == null) data.BaseInfo = new CreationBaseInfo();
            if (data.ExtraInfo == null) data.ExtraInfo = new CreationExtraInfo();

            data.BaseInfo.Name = reqName;
            if (data.BaseInfo.Grade < 1 || data.BaseInfo.Grade > 6) data.BaseInfo.Grade = 3;

            if (string.IsNullOrWhiteSpace(data.BaseInfo.Description))
            {
                data.BaseInfo.Description = forcedKind == ForcedCreationKind.Luck ? "外部交互触发的气运造物。" : "外部交互触发的道具造物。";
            }

            int targetGrade = 1;
            try
            {
                if (target != null) targetGrade = Math.Max(1, target.data.unitData.propertyData.gradeID);
            }
            catch { }

            if (forcedKind == ForcedCreationKind.Luck)
            {
                data.Type = "Luck";
                data.BaseInfo.IconCategory = string.Empty;
                data.ExtraInfo.Worth = 0;
                data.ExtraInfo.RealmReq = 0;
                if (data.ExtraInfo.Duration == 0) data.ExtraInfo.Duration = 6;
                if (string.IsNullOrWhiteSpace(data.Effects)) data.Effects = "luck_1_10";
                return;
            }

            string type = (data.Type ?? string.Empty).Trim();
            if (type.Equals("Luck", StringComparison.OrdinalIgnoreCase)) type = "Consumer";
            if (string.IsNullOrEmpty(type)) type = "Consumer";
            data.Type = type;

            if (data.ExtraInfo.RealmReq <= 0) data.ExtraInfo.RealmReq = targetGrade;
            if (data.ExtraInfo.Worth <= 0) data.ExtraInfo.Worth = Math.Max(500, targetGrade * 1500);
            data.ExtraInfo.Duration = 0;
            if (string.IsNullOrWhiteSpace(data.Effects)) data.Effects = "atk_0_3|def_0_3";
        }

        private static void TryGrantCreatedLuck(string reqName, WorldUnitBase target)
        {
            if (target == null) return;

            if (!CreationSystem.LatestItemIdByName.TryGetValue(reqName, out int luckId) || luckId <= 0)
            {
                UITipItem.AddTip($"机缘已显，但尚未能定位气运【{reqName}】。", 2f);
                return;
            }

            try
            {
                if (IsPlayer(target))
                {
                    var gm = new GMCmd();
                    gm.CMDCall($"tianjiaqiyun_player_{luckId}");
                }
                else
                {
                    var conf = g.conf.roleCreateFeature.GetItem(luckId);
                    string duration = conf != null && !string.IsNullOrEmpty(conf.duration) ? conf.duration : "-1";
                    DramaFunctionTool.OptionsFunction($"addNPCLuck_0_{luckId}_{duration}", new DramaFunctionData(target));
                }

                UITipItem.AddTip($"机缘已成，已获得气运【{reqName}】。", 2f);
            }
            catch
            {
                UITipItem.AddTip($"气运【{reqName}】塑形完成，但发放时受阻。", 2f);
            }
        }

        private static void TryGrantCreatedItem(string reqName, WorldUnitBase target, int requestedCount, string createdType)
        {
            if (target == null) return;

            if (!CreationSystem.LatestItemIdByName.TryGetValue(reqName, out int itemId) || itemId <= 0)
            {
                UITipItem.AddTip($"机缘已显，但尚未能定位物件【{reqName}】。", 2f);
                return;
            }

            int count = Math.Max(1, requestedCount);
            bool isEquipLike =
                string.Equals(createdType, "Vehicle", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(createdType, "Equip", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(createdType, "Carried", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(createdType, "Ring", StringComparison.OrdinalIgnoreCase);
            if (isEquipLike) count = 1;

            try
            {
                target.data.RewardPropItem(itemId, count, true);
                UITipItem.AddTip($"机缘已成，已获得【{reqName}】x{count}。", 2f);
            }
            catch
            {
                UITipItem.AddTip($"物件【{reqName}】塑形完成，但发放时受阻。", 2f);
            }
        }

        private static bool LuckExists(string reqName)
        {
            try
            {
                if (CreationSystem.LatestItemIdByName.ContainsKey(reqName))
                {
                    int id = CreationSystem.LatestItemIdByName[reqName];
                    if (id >= 8000000 && id < 9000000) return true;
                }

                foreach (var conf in g.conf.roleCreateFeature._allConfList)
                {
                    if (conf == null || string.IsNullOrEmpty(conf.name)) continue;
                    if (string.Equals(GameTool.LS(conf.name), reqName, StringComparison.Ordinal)) return true;
                }
            }
            catch { }

            return false;
        }

        private static bool ItemExists(string reqName)
        {
            try
            {
                if (CreationSystem.LatestItemIdByName.ContainsKey(reqName))
                {
                    int id = CreationSystem.LatestItemIdByName[reqName];
                    if (id >= 9000000) return true;
                }

                foreach (var conf in g.conf.itemProps._allConfList)
                {
                    if (conf == null || string.IsNullOrEmpty(conf.name)) continue;
                    var n = GameTool.LS(conf.name);
                    if (!string.IsNullOrEmpty(n) && n.Contains(reqName)) return true;
                }
            }
            catch { }

            return false;
        }

        private static int GetRequestedCount(object actionInstance)
        {
            int index = GetIntMember(actionInstance, "index", "Index");
            return ResolveRequestedCountFromStaticActionMessage(index);
        }

        private static string ResolveRequestNameFromStaticActionMessage(int index)
        {
            try
            {
                var msgObj = GetStaticActionMessage();
                if (msgObj == null) return string.Empty;

                int msgIndex = 0;
                var idxObj = GetMemberValue(msgObj, "xingweixuhao", "Xingweixuhao", "index", "Index");
                if (idxObj != null) int.TryParse(idxObj.ToString(), out msgIndex);

                if (msgIndex > 0 && index > 0 && msgIndex != index) return string.Empty;

                var nameObj = GetMemberValue(msgObj,
                    "xingdongcanshu1", "Xingdongcanshu1",
                    "actionParameter1", "ActionParameter1");

                var name = nameObj?.ToString()?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return name;
                }
            }
            catch { }

            return string.Empty;
        }

        private static int ResolveRequestedCountFromStaticActionMessage(int index)
        {
            try
            {
                var msgObj = GetStaticActionMessage();
                if (msgObj == null) return 1;

                int msgIndex = 0;
                var idxObj = GetMemberValue(msgObj, "xingweixuhao", "Xingweixuhao", "index", "Index");
                if (idxObj != null) int.TryParse(idxObj.ToString(), out msgIndex);

                if (msgIndex > 0 && index > 0 && msgIndex != index) return 1;

                var cObj = GetMemberValue(msgObj, "xingdongcanshu2", "Xingdongcanshu2", "actionParameter2", "ActionParameter2");
                if (cObj != null && int.TryParse(cObj.ToString(), out int c) && c > 0) return c;
            }
            catch { }

            return 1;
        }

        private static object GetStaticActionMessage()
        {
            try
            {
                var uiType = AccessTools.TypeByName("MOD_SSCYAI.UIChatAINew");
                if (uiType == null) return null;

                return GetStaticMemberValue(uiType, "aIActionMessage", "AIActionMessage", "aiactionmessage");
            }
            catch
            {
                return null;
            }
        }

        private static object GetStaticMemberValue(Type type, params string[] candidateNames)
        {
            if (type == null || candidateNames == null || candidateNames.Length == 0) return null;

            foreach (var name in candidateNames)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;

                var field = type.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                if (field != null)
                {
                    try { return field.GetValue(null); } catch { }
                }

                var prop = type.GetProperty(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                if (prop != null)
                {
                    try { return prop.GetValue(null, null); } catch { }
                }
            }

            return null;
        }

        private static int GetIntMember(object obj, params string[] candidateNames)
        {
            var value = GetMemberValue(obj, candidateNames);
            if (value == null) return 0;

            try
            {
                if (value is int i) return i;
                if (int.TryParse(value.ToString(), out int n)) return n;
            }
            catch { }

            return 0;
        }

        private static object GetMemberValue(object obj, params string[] candidateNames)
        {
            if (obj == null || candidateNames == null || candidateNames.Length == 0) return null;
            var type = obj.GetType();

            foreach (var name in candidateNames)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;

                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                if (field != null)
                {
                    try { return field.GetValue(obj); } catch { }
                }

                var prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                if (prop != null)
                {
                    try { return prop.GetValue(obj, null); } catch { }
                }
            }

            return null;
        }

        private static bool IsPlayer(WorldUnitBase unit)
        {
            if (unit == null || g.world == null || g.world.playerUnit == null) return false;
            try
            {
                return unit.data.unitData.unitID.Equals(g.world.playerUnit.data.unitData.unitID);
            }
            catch
            {
                return false;
            }
        }

        private static string BuildSystemPrompt(ForcedCreationKind kind)
        {
            try
            {
                if (g.world?.playerUnit == null)
                {
                    return "你是《鬼谷八荒》数值策划。请严格输出 JSON 对象，字段必须包含 Type, BaseInfo, Effects, ExtraInfo。";
                }

                var csType = typeof(CreationSystem);
                var getRealmName = csType.GetMethod("GetRealmName", BindingFlags.Static | BindingFlags.NonPublic);
                var getDynamicStatGuidance = csType.GetMethod("GetDynamicStatGuidance", BindingFlags.Static | BindingFlags.NonPublic);
                var buildSystemPrompt = csType.GetMethod("BuildSystemPrompt", BindingFlags.Static | BindingFlags.NonPublic);

                if (getRealmName == null || getDynamicStatGuidance == null || buildSystemPrompt == null)
                {
                    return "你是《鬼谷八荒》数值策划。请严格输出 JSON 对象，字段必须包含 Type, BaseInfo, Effects, ExtraInfo。";
                }

                int gradeId = Math.Max(1, g.world.playerUnit.data.unitData.propertyData.gradeID);
                string gradeName = (string)getRealmName.Invoke(null, new object[] { gradeId });
                string statGuidelines = (string)getDynamicStatGuidance.Invoke(null, new object[] { g.world.playerUnit, gradeId });
                string prompt = (string)buildSystemPrompt.Invoke(null, new object[] { gradeName, statGuidelines });

                if (!string.IsNullOrWhiteSpace(prompt)) return prompt;
            }
            catch { }

            return "你是《鬼谷八荒》数值策划。请严格输出 JSON 对象，字段必须包含 Type, BaseInfo, Effects, ExtraInfo。";
        }

        private static string BuildUserPrompt(ForcedCreationKind kind, string reqName, WorldUnitBase source, WorldUnitBase target, int count)
        {
            string sourceName = SafeUnitName(source);
            string targetName = SafeUnitName(target);
            int sourceGrade = 1;
            int targetGrade = 1;

            try { if (source != null) sourceGrade = Math.Max(1, source.data.unitData.propertyData.gradeID); } catch { }
            try { if (target != null) targetGrade = Math.Max(1, target.data.unitData.propertyData.gradeID); } catch { }

            WorldUnitBase npc = IsPlayer(source) ? target : source;
            string chatHistory = BuildLimitedChatHistory(npc, 20);

            string baseContext =
                "背景信息：\n" +
                $"- 施动者：{sourceName}（境界ID：{sourceGrade}）\n" +
                $"- 目标：{targetName}（境界ID：{targetGrade}）\n" +
                $"- 指定名称：{reqName}\n" +
                $"- 私聊记录（最多20轮）：\n{chatHistory}\n\n";

            if (kind == ForcedCreationKind.Luck)
            {
                return baseContext +
                       "请基于上述背景，设计一个严格符合系统规则的单一奖励对象。\n" +
                       "硬性要求：\n" +
                       "1) 仅输出单个 JSON 对象，不要 markdown。\n" +
                       "2) Type 必须是 Luck。\n" +
                       "3) BaseInfo.Name 必须严格等于给定名称，不得改名。\n" +
                       "4) 必须严格遵守系统提示中的数值与图标规则。\n" +
                       "5) 气运必须满足：IconCategory为空，Worth=0，RealmReq=0，Duration 为 -1 或正整数。";
            }

            return baseContext +
                   $"- 请求数量：{Math.Max(1, count)}\n\n" +
                   "请基于上述背景，设计一个严格符合系统规则的单一道具对象。\n" +
                   "硬性要求：\n" +
                   "1) 仅输出单个 JSON 对象，不要 markdown。\n" +
                   "2) Type 只能是 Consumer / Vehicle / Carried 之一，禁止 Luck。\n" +
                   "3) BaseInfo.Name 必须严格等于给定名称，不得改名。\n" +
                   "4) 必须严格遵守系统提示中的图标分类与数值规则。\n" +
                   "5) ExtraInfo.Duration 必须为 0。";
        }

        private static string SafeUnitName(WorldUnitBase unit)
        {
            try
            {
                if (unit == null) return "未知";
                return unit.data.unitData.propertyData.GetName();
            }
            catch
            {
                return "未知";
            }
        }

        private static string BuildLimitedChatHistory(WorldUnitBase npc, int maxRounds)
        {
            try
            {
                if (npc == null) return "（无）";
                string full = Tools.GetChatHistory(npc);
                if (string.IsNullOrWhiteSpace(full)) return "（无）";

                var lines = full.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                                .Where(l => !string.IsNullOrWhiteSpace(l))
                                .ToList();
                if (lines.Count == 0) return "（无）";

                string header = lines[0];
                var dialogueLines = lines.Skip(1).ToList();
                int keep = Math.Max(1, maxRounds * 2);
                var tail = dialogueLines.Count > keep ? dialogueLines.Skip(dialogueLines.Count - keep).ToList() : dialogueLines;

                var merged = new List<string> { header };
                merged.AddRange(tail);
                return string.Join("\n", merged);
            }
            catch
            {
                return "（无）";
            }
        }

        private static void WriteDebug(string msg) { }

        private static string ExtractJson(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "{}";

            string clean = raw.Trim();
            if (clean.StartsWith("```"))
            {
                int firstLine = clean.IndexOf('\n');
                if (firstLine > 0) clean = clean.Substring(firstLine + 1);
                int tail = clean.LastIndexOf("```");
                if (tail > 0) clean = clean.Substring(0, tail);
            }

            clean = clean.Trim();

            int startArr = clean.IndexOf('[');
            int startObj = clean.IndexOf('{');
            if (startArr < 0 && startObj < 0) return "{}";

            bool arrayFirst = startArr >= 0 && (startObj < 0 || startArr < startObj);
            if (arrayFirst)
            {
                int endArr = clean.LastIndexOf(']');
                if (endArr > startArr) return clean.Substring(startArr, endArr - startArr + 1);
            }
            else
            {
                int endObj = clean.LastIndexOf('}');
                if (endObj > startObj) return clean.Substring(startObj, endObj - startObj + 1);
            }

            return clean;
        }
    }
}
