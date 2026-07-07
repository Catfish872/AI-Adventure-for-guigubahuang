using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace MOD_kqAfiU
{
    public class ModTask
    {
        public int Id;
        public int TemplateId = 1110052;
        public string Name = "";
        public string Desc = "";
        public int Type = 110;
        public int Group = 4;              // 已验证：type=110 + group=4 显示“奇”。
        public int Level = 1;
        public int Duration = -1;
        public int Multiple = 0;
        public int IsGive = 0;
        public int Submit = 1;
        public int Remind = 1;
        public int PositionId = 0;         // 原生寻路 ID；不是坐标。0 = 不显示寻路按钮。
        public string Task110FortuitousEventID = "0";
        public int Task110PositionID = 0;
        public string RewardItem = "0";
        public int RewardMoney = 0, RewardReputation = 0, RewardStandUp = 0, RewardStandDown = 0;
        public int Contribution = 0, RewardContribution = 0, RewardToken = 0, RewardFame = 0;
        public string AddTaskFunction = "0", CompleteTaskFunction = "0", FailedTaskFunction = "0", GainFeature = "0";
        public Func<bool> TriggerCondition;
        public Func<bool> CompleteCondition;
        public Action OnComplete;
        public bool TriggerDungeonOnMapEvent = false; // 命中地图事件时进入副本；副本胜利后才完成任务。
        public int DungeonId = 0;                     // 0=按玩家当前境界配置自动推导。
        public int DungeonLevel = 0;                  // 0=按玩家当前境界配置自动推导。
        public bool DungeonTriggered = false;
        public bool DungeonCompleted = false;
        public bool OneShot = true;        // 本次运行内完成后不再自动触发。
        public int MapEventAnchorEventId = 0;              // 大地图事件锚点 ID；0=不自动创建事件锚点。
        public int MapEventAnchorSearchRadius = 8;         // 创建事件锚点时围绕目标坐标搜索的半径。
        public Vector2Int MapEventAnchorPreferredTarget;   // 延迟创建锚点时使用的期望坐标。
        public int MapEventAnchorMapPositionId = 0;        // 延迟创建锚点成功后写入的 ConfMapPosition ID。
        public bool MapEventAnchorMaterialized = false;    // 是否已经把地图事件锚点真实创建出来。

        internal ConfTaskBaseItem ToConfItem()
        {
            ConfTaskBaseItem t = null;
            try { if (TemplateId != 0) t = g.conf.taskBase.GetItem(TemplateId); } catch { }
            return new ConfTaskBaseItem(Id,
                string.IsNullOrEmpty(Name) && t != null ? t.name : Name,
                Group > 0 ? Group : (t != null ? t.group : 4),
                Type > 0 ? Type : (t != null ? t.type : 110),
                t != null ? t.level : Level,
                Duration, Multiple,
                string.IsNullOrEmpty(RewardItem) && t != null ? t.rewardItem : RewardItem,
                RewardStandUp, RewardStandDown, RewardContribution, RewardMoney, RewardToken,
                RewardReputation, Contribution, RewardFame, Submit, IsGive,
                string.IsNullOrEmpty(Desc) && t != null ? t.desc : Desc,
                string.IsNullOrEmpty(AddTaskFunction) && t != null ? t.addTaskFunction : AddTaskFunction,
                string.IsNullOrEmpty(CompleteTaskFunction) && t != null ? t.completeTaskFunction : CompleteTaskFunction,
                string.IsNullOrEmpty(FailedTaskFunction) && t != null ? t.failedTaskFunction : FailedTaskFunction,
                string.IsNullOrEmpty(GainFeature) && t != null ? t.gainFeature : GainFeature,
                PositionId, Remind);
        }
    }

    public static class TaskSystem
    {
        public const int DemoTaskId = 900901137;
        public const int DemoTemplateTaskId = 1110052;
        public const int DemoMapPositionId = 900901138;
        public const int SourceCoordinateMapPositionId = 1026;
        private const int TaskAnchorEventTemplateId = 6;
        public const int DefaultTaskAnchorEventId = 900901139;
        private const string DefaultTaskAnchorEventIcon = "gudingdianqiyuwenhao";
        private const int MaxMapEventAnchorCreateAttempts = 25;

        private static readonly Dictionary<int, ModTask> defs = new Dictionary<int, ModTask>();
        private static readonly HashSet<int> adding = new HashSet<int>();
        private static readonly HashSet<int> completed = new HashSet<int>();
        private static int activeDungeonTaskId = 0;
        private class MapEventAnchorState { public int TaskId; public int EventId; public int MapPositionId; public Vector2Int Point; public object CreatedEvent; public int RuntimeId; }
        private static readonly Dictionary<int, MapEventAnchorState> mapEventAnchors = new Dictionary<int, MapEventAnchorState>();

        public static void InitTest()
        {
            ClearRuntimeState();
            Vector2Int player;
            bool hasPlayerPoint = TryGetPlayerPoint(out player);
            if (!hasPlayerPoint) player = new Vector2Int(0, 0);
            Vector2Int demoTarget = new Vector2Int(player.x + 2, player.y);
            ModTask demo = new ModTask
            {
                Id = DemoTaskId,
                TemplateId = DemoTemplateTaskId,
                Name = "AI奇遇：近处异响",
                Desc = "前往异响所在之处，进入副本查明真相。副本胜利后任务完成。",
                Type = 110,
                Group = 4,
                MapEventAnchorEventId = DefaultTaskAnchorEventId,
                MapEventAnchorSearchRadius = 8,
                TriggerDungeonOnMapEvent = true,
                // 13 已验证是天雷/生存型境界副本；这里改用教程示例里的竹林副本模板，优先验证打怪型副本链路。
                // DungeonLevel 保持 0：进入前按玩家当前境界解析为同境界副本等级，避免固定低级副本。
                DungeonId = 1011,
                DungeonLevel = 0,
                TriggerCondition = delegate { return !completed.Contains(DemoTaskId); }
            };
            RegisterCoordinateTaskWithMapEventAnchor(demo, demoTarget, DemoMapPositionId, demo.MapEventAnchorEventId, demo.MapEventAnchorSearchRadius);
        }

        public static void TriggerTest() { UpdateRegisteredTasks(); }

        public static void OnPlayerMoved() { UpdateRegisteredTasks(); }

        public static void RegisterTask(ModTask def)
        {
            if (def == null) return;
            defs[def.Id] = def;
            EnsureTaskBase(def);
            if (!IsPendingMapEventAnchorTask(def)) EnsureTypedTask(def);
        }

        private static bool IsPendingMapEventAnchorTask(ModTask def)
        {
            return def != null && def.MapEventAnchorEventId != 0 && !def.MapEventAnchorMaterialized && (def.Task110PositionID <= 0 || def.PositionId <= 0);
        }

        public static bool RegisterCoordinateTask(ModTask def, Vector2Int target, int mapPositionId)
        {
            if (def == null) return false;
            if (!CreateCoordinateMapPosition(mapPositionId, target.x, target.y)) return false;
            def.PositionId = mapPositionId;
            def.Task110PositionID = mapPositionId;
            RegisterTask(def);
            return HasRegisteredConf(def.Id);
        }

        public static bool RegisterCoordinateTaskWithMapEventAnchor(ModTask def, Vector2Int preferredTarget, int mapPositionId, int mapEventId, int searchRadius)
        {
            if (def == null || mapEventId == 0) return false;
            def.MapEventAnchorEventId = mapEventId;
            def.MapEventAnchorSearchRadius = Math.Max(1, searchRadius);
            def.MapEventAnchorPreferredTarget = preferredTarget;
            def.MapEventAnchorMapPositionId = mapPositionId;
            def.PositionId = 0;
            def.Task110PositionID = 0;
            def.MapEventAnchorMaterialized = false;
            RegisterTask(def);
            return HasRegisteredTaskBase(def.Id);
        }

        private static bool EnsureMapEventAnchorMaterialized(ModTask def)
        {
            if (def == null) return false;
            if (def.MapEventAnchorEventId == 0) return true;
            if (def.MapEventAnchorMaterialized && mapEventAnchors.ContainsKey(def.Id)) return true;
            MapEventAnchorState anchor;
            string reason;
            int mapPositionId = def.MapEventAnchorMapPositionId != 0 ? def.MapEventAnchorMapPositionId : def.PositionId;
            if (mapPositionId == 0) mapPositionId = DemoMapPositionId;
            if (!TryCreateMapEventAnchor(def.Id, def.MapEventAnchorPreferredTarget, Math.Max(1, def.MapEventAnchorSearchRadius), mapPositionId, def.MapEventAnchorEventId, out anchor, out reason))
            {
                Debug.Log("[TaskSystem] 创建任务地图事件锚点失败: taskId=" + def.Id + " reason=" + reason);
                return false;
            }
            Vector2Int anchorPoint = anchor.Point;
            def.PositionId = mapPositionId;
            def.Task110PositionID = mapPositionId;
            def.MapEventAnchorMapPositionId = mapPositionId;
            def.MapEventAnchorMaterialized = true;
            def.CompleteCondition = delegate { Vector2Int p; return TryGetPlayerPoint(out p) && p == anchorPoint; };
            if (string.IsNullOrEmpty(def.Desc)) def.Desc = "你在附近发现了异样。请前往目标点 (" + anchorPoint.x + "," + anchorPoint.y + ") 查看。";
            if (!CreateCoordinateMapPosition(mapPositionId, anchorPoint.x, anchorPoint.y))
            {
                RemoveMapEventAnchor(def.Id);
                def.MapEventAnchorMaterialized = false;
                return false;
            }
            EnsureTaskBase(def);
            EnsureTypedTask(def);
            ForceRefreshTypedTaskPosition(def);
            return HasRegisteredConf(def.Id);
        }

        public static void UpdateRegisteredTasks() { UpdateRegisteredTasksInternal(); }

        private static void UpdateRegisteredTasksInternal()
        {
            List<ModTask> list = new List<ModTask>(defs.Values);
            for (int i = 0; i < list.Count; i++)
            {
                ModTask def = list[i];
                if (def.OneShot && completed.Contains(def.Id)) continue;
                bool has = HasTask(def.Id);
                if (!has && (def.TriggerCondition == null || def.TriggerCondition()))
                {
                    if (!EnsureMapEventAnchorMaterialized(def)) continue;
                    AddTask(def.Id);
                    has = HasTask(def.Id);
                }
                if (has && def.MapEventAnchorEventId == 0 && def.CompleteCondition != null && def.CompleteCondition())
                    CompleteTask(def.Id);
            }
        }

        // M4：原版地图事件真正“打开事件”的时刻。
        // 坐标任务在这里严格命中；副本任务只触发副本，必须等副本胜利后才完成任务。
        internal static void OnMapEventOpened(MapEventBase mapEvent)
        {
            if (mapEvent == null || mapEventAnchors.Count == 0) return;
            int hitTaskId = 0;
            try
            {
                int eventId = ReadMapEventConfigId(mapEvent);
                Vector2Int eventPoint;
                bool hasEventPoint = TryGetEventPoint(mapEvent, out eventPoint);
                if (!hasEventPoint) eventPoint = new Vector2Int(int.MinValue, int.MinValue);
                Vector2Int playerPoint;
                bool hasPlayerPoint = TryGetPlayerPoint(out playerPoint);

                foreach (KeyValuePair<int, MapEventAnchorState> kv in mapEventAnchors)
                {
                    int taskId = kv.Key;
                    MapEventAnchorState anchor = kv.Value;
                    if (anchor == null) continue;
                    ModTask def;
                    if (!defs.TryGetValue(taskId, out def) || def == null) continue;
                    if (completed.Contains(taskId)) continue;
                    if (!HasTask(taskId)) continue;

                    if (!IsTaskAnchorMapEvent(def, eventId, hasEventPoint, eventPoint, anchor.Point)) continue;
                    if (!hasPlayerPoint || playerPoint != anchor.Point) continue;
                    hitTaskId = taskId; // 记录命中，先跳出循环，避免遍历 mapEventAnchors 时修改集合
                    break;
                }
            }
            catch { }
            if (hitTaskId != 0)
            {
                try
                {
                    ModTask def;
                    if (defs.TryGetValue(hitTaskId, out def) && def != null && def.TriggerDungeonOnMapEvent)
                    {
                        TriggerTaskDungeon(def);
                    }
                    else
                    {
                        CompleteTask(hitTaskId);
                    }
                }
                catch { }
            }
        }

        private static bool IsTaskAnchorMapEvent(ModTask def, int eventId, bool hasEventPoint, Vector2Int eventPoint, Vector2Int target)
        {
            return def != null && def.MapEventAnchorEventId != 0 && eventId == def.MapEventAnchorEventId && hasEventPoint && eventPoint == target;
        }

        private static int ReadMapEventConfigId(MapEventBase mapEvent)
        {
            if (mapEvent == null) return int.MinValue;
            // 直取：MapEventBase.eventBaseItem 为公开属性，其 id/baseID 为公开字段。
            return mapEvent.eventBaseItem.id;
        }

        private static bool TryGetEventPoint(MapEventBase mapEvent, out Vector2Int point)
        {
            point = new Vector2Int(int.MinValue, int.MinValue);
            if (mapEvent == null) return false;
            try { point = mapEvent.GetPoint(); return true; }
            catch { return false; }
        }

        private static void TriggerTaskDungeon(ModTask def)
        {
            if (def == null || def.DungeonCompleted) return;
            if (def.DungeonTriggered && activeDungeonTaskId == def.Id) return;

            int dungeonId;
            int dungeonLevel;

            if (!ResolveDungeonForPlayer(def, out dungeonId, out dungeonLevel))
            {
                UITipItem.AddTip("未能找到适合当前境界的副本入口。", 2f);
                return;
            }

            def.DungeonId = dungeonId;
            def.DungeonLevel = dungeonLevel;
            def.DungeonTriggered = true;
            activeDungeonTaskId = def.Id;
            try
            {
                UITipItem.AddTip("异响化作秘境入口，战胜副本后任务完成。", 2f);
                g.world.battle.IntoBattle(new DataMap.MonstData() { id = dungeonId, level = dungeonLevel });
            }
            catch (Exception ex)
            {
                def.DungeonTriggered = false;
                activeDungeonTaskId = 0;
                UITipItem.AddTip("进入副本失败：" + ex.Message, 2f);
            }
        }

        private static bool ResolveDungeonForPlayer(ModTask def, out int dungeonId, out int dungeonLevel)
        {
            dungeonId = def != null ? def.DungeonId : 0;
            dungeonLevel = def != null ? def.DungeonLevel : 0;
            if (dungeonId > 0 && dungeonLevel > 0) return true;

            // 直取：g.world.playerUnit、gradeID、GetGrade、g.conf.roleGrade 全是公开成员（项目内多处已直接调用）。
            WorldUnitBase player = g.world.playerUnit;
            if (player == null) return false;

            int gradeId = player.data.unitData.propertyData.gradeID;
            int dynGrade = player.data.dynUnitData.GetGrade();

            // 用强类型 ConfRoleGradeItem 取境界副本配置，避免反射（GetGradeItem 返回强类型）。
            ConfRoleGradeItem gradeItem = null;
            int[] gradeCandidates = UniquePositiveInts(dynGrade, gradeId, Math.Max(1, (gradeId + 2) / 3));
            int[] phaseCandidates = new int[] { 1, 2, 3, 4, 5 };
            for (int i = 0; i < gradeCandidates.Length && gradeItem == null; i++)
            {
                for (int j = 0; j < phaseCandidates.Length && gradeItem == null; j++)
                {
                    try
                    {
                        ConfRoleGradeItem item = g.conf.roleGrade.GetGradeItem(gradeCandidates[i], phaseCandidates[j]);
                        if (item != null && item.dungeonID > 0 && item.dungeonLevel > 0) gradeItem = item;
                    }
                    catch { }
                }
            }

            if (gradeItem != null)
            {
                if (dungeonId <= 0) dungeonId = gradeItem.dungeonID;
                if (dungeonLevel <= 0) dungeonLevel = gradeItem.dungeonLevel;
            }

            // 兜底：roleGrade 未命中时，用教程示例副本 ID，等级按当前境界推导，避免固定低级副本。
            if (dungeonId <= 0) dungeonId = 1011;
            if (dungeonLevel <= 0) dungeonLevel = Math.Max(5, (dynGrade > 0 ? dynGrade : Math.Max(1, (gradeId + 2) / 3)) * 5);
            return dungeonId > 0 && dungeonLevel > 0;
        }

        public static void OnDungeonBattleEndEvent(string eventName, ETypeData eventData)
        {
            if (activeDungeonTaskId == 0) return;
            // BattleEndFront 是结算前的前置事件，此时胜负还未落定，跳过；只在 BattleEnd 真正结算后判定。
            if (eventName != null && eventName.IndexOf("Front", StringComparison.OrdinalIgnoreCase) >= 0) return;

            ModTask def;
            if (!defs.TryGetValue(activeDungeonTaskId, out def) || def == null) { activeDungeonTaskId = 0; return; }

            // 根本胜负标志位：g.world.battle.data.lastBattleIsWin。胜利=True，失败=False。
            // 不使用玩家存活/血量/坐标/UI 结算界面，失败复活/传送不影响该字段。
            bool? victory = TryGetLastBattleIsWin();
            activeDungeonTaskId = 0;

            if (victory == true)
            {
                def.DungeonCompleted = true;
                def.DungeonTriggered = false;
                CompleteTask(def.Id);
                UITipItem.AddTip("副本已胜利，任务完成。", 2f);
            }
            else
            {
                // 失败或未读到字段时，保持任务与地图事件入口可重试（玩家会被游戏机制传送走，下次可再来挑战）。
                def.DungeonTriggered = false;
                UITipItem.AddTip(victory == false ? "副本未胜利，任务仍未完成。" : "副本已结束，但未识别胜负；任务暂不完成。", 2f);
            }
        }

        public static void OnDungeonUiEvent(string phase, object eventData)
        {
            // UI 只保留窄口入口，默认不写日志、不做反射探查。
            // 后续需要探测 UI 接口时，只读取已知安全字段（例如 uiType.uiName），不要对 UI/Unity 对象做 DescribeObject 或深度遍历。
        }

        private static bool? TryGetLastBattleIsWin()
        {
            // 直取：g.world.battle 是公开属性（同文件 IntoBattle 已直接调用）。
            // lastBattleIsWin 为探测确认的胜负标志位；若该字段非 public 导致编译失败，再退回 GetMember 单层反射。
            try
            {
                if (g.world == null || g.world.battle == null) return null;
                return g.world.battle.data.lastBattleIsWin;
            }
            catch { return null; }
        }

        // 探测经验：先用桌面日志定点观察最小根对象（如 g.world.battle.data），确认字段后立刻收敛为定点读取。重点是自动化的多层拆解，不然很容易读到一个对象而不知道到底是什么。
        // 避免反射遍历 UI/Unity 对象、避免枚举大型集合；这些对象可能触发 IL2CPP/Unity 原生层崩溃，try/catch 也兜不住。

        public static TaskBase AddTask(int taskId)
        {
            if (!defs.ContainsKey(taskId) || adding.Contains(taskId) || HasTask(taskId)) return FindPlayerTask(taskId);
            adding.Add(taskId);
            try
            {
                EnsureTaskBase(defs[taskId]);
                EnsureTypedTask(defs[taskId]);
                DramaFunctionTool.OptionsFunction("addTask_" + taskId);
                return FindPlayerTask(taskId);
            }
            catch { return null; }
            finally { adding.Remove(taskId); }
        }

        public static bool CompleteTask(int taskId)
        {
            bool ok = InvokeTaskMethod(taskId, "TaskComplete");
            if (ok)
            {
                completed.Add(taskId);
                RemoveMapEventAnchor(taskId);
                ModTask def;
                if (defs.TryGetValue(taskId, out def) && def != null) def.MapEventAnchorMaterialized = false;
                if (defs.TryGetValue(taskId, out def) && def != null && def.OnComplete != null) { try { def.OnComplete(); } catch { } }
            }
            return ok;
        }

        public static bool RemoveTask(int taskId)
        {
            RemoveMapEventAnchor(taskId);
            ModTask def;
            if (defs.TryGetValue(taskId, out def) && def != null) def.MapEventAnchorMaterialized = false;
            return InvokeTaskMethod(taskId, "TaskDel");
        }

        public static bool HasTask(int taskId) { return FindPlayerTask(taskId) != null || FindTaskData(taskId) != null; }

        public static bool HasRegisteredConf(int taskId)
        {
            try
            {
                ConfTaskBaseItem b = g.conf.taskBase.GetItem(taskId);
                if (b == null) return false;
                object mgr = GetMember(g.conf, "task" + b.type);
                return mgr != null && GetItem(mgr, taskId) != null;
            }
            catch { return false; }
        }

        private static bool HasRegisteredTaskBase(int taskId)
        {
            try { return g.conf != null && g.conf.taskBase != null && g.conf.taskBase.GetItem(taskId) != null; }
            catch { return false; }
        }

        public static void ClearRuntimeState() { CleanupAllMapEventAnchors(); defs.Clear(); adding.Clear(); completed.Clear(); activeDungeonTaskId = 0; }

        public static bool TryGetPlayerPoint(out Vector2Int point)
        {
            point = new Vector2Int(int.MinValue, int.MinValue);
            // 直取：g.world.playerUnit / data / unitData / GetPoint() 全是公开成员，无需反射（项目内多处已这样直接调用）。
            try
            {
                if (g.world == null || g.world.playerUnit == null) return false;
                point = g.world.playerUnit.data.unitData.GetPoint();
                return true;
            }
            catch { return false; }
        }

        public static bool CreateCoordinateMapPosition(int positionId, int x, int y) { return CreateCoordinateMapPosition(positionId, x, y, "0", SourceCoordinateMapPositionId); }

        public static bool CreateCoordinateMapPosition(int positionId, int x, int y, string areaId, int sourceId)
        {
            object mgr = GetMember(g.conf, "mapPosition");
            if (mgr == null) return false;
            string posiId = x + "_" + y;
            object exists = GetItem(mgr, positionId);
            if (exists != null)
            {
                SetMember(exists, "areaID", areaId); SetMember(exists, "posiType", 3);
                SetMember(exists, "posiID", posiId); SetMember(exists, "buildingIndex", 0); SetMember(exists, "condition", "0");
                return true;
            }
            object src = GetItem(mgr, sourceId);
            object clone = CloneByConstructor(src, delegate (string n, object old)
            {
                string k = (n ?? "").ToLowerInvariant();
                if (IsId(n)) return positionId;
                if (k == "areaid") return areaId;
                if (k == "positype") return 3;
                if (k == "posiid") return posiId;
                if (k == "buildingindex") return 0;
                if (k == "condition") return "0";
                return old;
            });
            if (clone == null) return false;
            SetMember(clone, "id", positionId); SetMember(clone, "areaID", areaId); SetMember(clone, "posiType", 3);
            SetMember(clone, "posiID", posiId); SetMember(clone, "buildingIndex", 0); SetMember(clone, "condition", "0");
            return AddConfItem(mgr, clone) && GetItem(mgr, positionId) != null;
        }

        public static bool FindNearbyUsableCell(Vector2Int center, int radius, out Vector2Int result)
        {
            result = center;
            for (int d = 1; d <= radius; d++)
                for (int dx = -d; dx <= d; dx++)
                    for (int dy = -d; dy <= d; dy++)
                    {
                        if (Math.Abs(dx) != d && Math.Abs(dy) != d) continue;
                        Vector2Int p = new Vector2Int(center.x + dx, center.y + dy);
                        string reason;
                        if (IsUsableEmptyCell(p, out reason)) { result = p; return true; }
                    }
            return false;
        }

        public static bool IsUsableEmptyCell(Vector2Int point, out string reason)
        {
            reason = "ok";
            try
            {
                if (point.x < 0 || point.y < 0) { reason = "out_of_bounds"; return false; }
                if (g.world == null) { reason = "world_null"; return false; }

                // 管理器对象本身公开，直取；其内部占用字典字段（allBuildInPoint 等）多为 private，仍需反射点查。
                if (RuntimeIndexHasPoint(g.world.build, point, "allBuildInPoint", "allBuildInPointInt", "townPointInErrorBuild", "buildType10005PointInErrorBuild")) { reason = "has_build_index"; return false; }
                if (g.world.build.GetBuild(point) != null) { reason = "has_build"; return false; }

                if (RuntimeIndexHasPoint(g.world.mapEvent, point, "mapEvents")) { reason = "has_mapEvent_index"; return false; }

                object terrMgr = GetMember(g.world, "terr"); // terr 未见公开访问先例，保留反射取管理器
                if (RuntimeIndexHasPoint(terrMgr, point, "allTerrInPoint", "terr1001PointDataError")) { reason = "has_terr_index"; return false; }

                // 这些管理器无固定公开查询方法，用反射探测是否占用（仅在锚点创建时用，非高频路径）。
                string[] managers = { "dramaPackage", "immortalPoint", "pointResources" };
                for (int i = 0; i < managers.Length; i++)
                    if (ManagerHasPointObject(GetMember(g.world, managers[i]), point)) { reason = "has_" + managers[i]; return false; }

                return true;
            }
            catch (Exception ex) { reason = "exception_" + ex.Message; return false; }
        }

        public static object AddGridEventWithNativePlacementChecks(Vector2Int point, int eventId, DataObjectData objData)
        {
            try
            {
                if (g.world == null || g.world.mapEvent == null) return null;
                return g.world.mapEvent.AddGridEvent(point, eventId, objData, true, true);
            }
            catch { return null; }
        }

        private static bool TryCreateMapEventAnchor(int taskId, Vector2Int center, int radius, int mapPositionId, int eventId, out MapEventAnchorState anchor, out string reason)
        {
            anchor = null;
            reason = "not_found";
            if (g.world == null || g.world.mapEvent == null) { reason = "mapEvent_null"; return false; }
            if (!EnsureWorldFortuitousEventConfig(eventId)) { reason = "event_config_missing"; return false; }
            RemoveMapEventAnchor(taskId);
            int nativeAttempts = 0;
            for (int d = 0; d <= radius; d++)
            {
                for (int dx = -d; dx <= d; dx++)
                {
                    for (int dy = -d; dy <= d; dy++)
                    {
                        if (d > 0 && Math.Abs(dx) != d && Math.Abs(dy) != d) continue;
                        Vector2Int p = new Vector2Int(center.x + dx, center.y + dy);
                        string preReason;
                        if (!IsUsableEmptyCell(p, out preReason)) { reason = "prefilter_" + preReason; continue; }
                        if (GetGridEventAt(p) != null) { reason = "already_has_grid_event"; continue; }
                        if (nativeAttempts >= MaxMapEventAnchorCreateAttempts) { reason = "max_native_attempts_reached"; return false; }
                        nativeAttempts++;
                        DataObjectData objData = CreateMapEventAnchorObjData();
                        object result = AddGridEventWithNativePlacementChecks(p, eventId, objData);
                        object afterEvent = GetGridEventAt(p);
                        object createdEvent;
                        int runtimeId;
                        ExtractAddGridEventResult(result, out createdEvent, out runtimeId);
                        bool createdIsActualMapEvent = IsActualMapEvent(createdEvent);
                        bool afterIsActualMapEvent = IsActualMapEvent(afterEvent);
                        if (!afterIsActualMapEvent && !createdIsActualMapEvent) { reason = "native_rejected"; continue; }
                        if (!createdIsActualMapEvent) createdEvent = afterEvent;
                        anchor = new MapEventAnchorState { TaskId = taskId, EventId = eventId, MapPositionId = mapPositionId, Point = p, CreatedEvent = createdEvent, RuntimeId = runtimeId };
                        mapEventAnchors[taskId] = anchor;
                        reason = "ok";
                        Debug.Log("[TaskSystem] 创建任务地图事件锚点成功: taskId=" + taskId + " point=(" + p.x + "," + p.y + ") eventId=" + eventId + " runtimeId=" + runtimeId);
                        return true;
                    }
                }
            }
            return false;
        }

        private static DataObjectData CreateMapEventAnchorObjData()
        {
            try { return Activator.CreateInstance(typeof(DataObjectData)) as DataObjectData; }
            catch { return null; }
        }

        private static object GetGridEventAt(Vector2Int point)
        {
            // 直取：g.world.mapEvent 是公开管理器，GetGridEvent 是其公开方法。
            try
            {
                object ret = g.world.mapEvent.GetGridEvent(point);
                if (ret != null && !(ret is bool)) return ret;
            }
            catch { }
            return null;
        }

        private static void ExtractAddGridEventResult(object result, out object createdEvent, out int runtimeId)
        {
            createdEvent = null;
            runtimeId = 0;
            if (result == null) return;
            if (IsActualMapEvent(result)) { createdEvent = result; return; }
            object t1 = GetMember(result, "t1");
            object t2 = GetMember(result, "t2");
            if (IsActualMapEvent(t1)) createdEvent = t1;
            if (t2 is int) runtimeId = (int)t2;
        }

        private static bool IsActualMapEvent(object obj)
        {
            if (obj == null) return false;
            Type t = obj.GetType();
            if (t.IsGenericType) return false;
            string ownName = (t.FullName ?? t.Name ?? "");
            if (ownName.Contains("DataStruct") || ownName.Contains("Tuple") || ownName.Contains("ValueTuple")) return false;
            while (t != null)
            {
                string n = t.FullName ?? t.Name ?? "";
                if (n.EndsWith("MapEventBase") || n.EndsWith(".MapEventBase")) return true;
                if (n.Contains("DataStruct") || n.Contains("Tuple") || n.Contains("ValueTuple")) return false;
                t = t.BaseType;
            }
            return false;
        }

        private static bool RemoveMapEventAnchor(int taskId)
        {
            MapEventAnchorState anchor;
            if (!mapEventAnchors.TryGetValue(taskId, out anchor) || anchor == null) return true;
            bool ok = RemoveMapEventAnchor(anchor);
            mapEventAnchors.Remove(taskId);
            return ok;
        }

        private static void CleanupAllMapEventAnchors()
        {
            List<int> ids = new List<int>(mapEventAnchors.Keys);
            for (int i = 0; i < ids.Count; i++) RemoveMapEventAnchor(ids[i]);
            mapEventAnchors.Clear();
        }

        private static bool RemoveMapEventAnchor(MapEventAnchorState anchor)
        {
            if (anchor == null || g.world == null || g.world.mapEvent == null) return false;
            object mgr = g.world.mapEvent;
            object target = anchor.CreatedEvent ?? FindMapEventAnchorInList(mgr, anchor.Point) ?? GetGridEventAt(anchor.Point);
            if (target == null) return true; // 已经不存在
            if (InvokeDelGridEvent(mgr, target)) return true;
            Debug.Log("[TaskSystem] 删除地图事件锚点失败: taskId=" + anchor.TaskId + " point=(" + anchor.Point.x + "," + anchor.Point.y + ")");
            return false;
        }

        private static bool InvokeDelGridEvent(object mgr, object mapEvent)
        {
            if (mgr == null || mapEvent == null) return false;
            try
            {
                MethodInfo m = mgr.GetType().GetMethod("DelGridEvent", new Type[] { mapEvent.GetType(), typeof(bool) });
                if (m == null) m = mgr.GetType().GetMethod("DelGridEvent", new Type[] { IsActualMapEvent(mapEvent) ? mapEvent.GetType().BaseType : mapEvent.GetType(), typeof(bool) });
                if (m == null) m = mgr.GetType().GetMethod("RemoveGridEvent", new Type[] { mapEvent.GetType(), typeof(bool) });
                if (m == null) return false;
                m.Invoke(mgr, new object[] { mapEvent, true });
                return true;
            }
            catch { return false; }
        }

        private static object FindMapEventAnchorInList(object mgr, Vector2Int point)
        {
            if (mgr == null) return null;
            object list = GetMember(mgr, "mapEvents");
            if (list == null) return null;
            foreach (object it in EnumerateCollection(list))
            {
                if (it == null) continue;
                object key = GetMember(it, "Key");
                if (key is Vector2Int && (Vector2Int)key == point) return GetMember(it, "Value");
            }
            return null;
        }

        private static IEnumerable<object> EnumerateCollection(object list)
        {
            if (list == null) yield break;
            MethodInfo getEnumerator = null;
            try { getEnumerator = list.GetType().GetMethod("GetEnumerator", Type.EmptyTypes); } catch { }
            if (getEnumerator != null)
            {
                object enumerator = null;
                try { enumerator = getEnumerator.Invoke(list, null); } catch { }
                if (enumerator != null)
                {
                    MethodInfo moveNext = null;
                    try { moveNext = enumerator.GetType().GetMethod("MoveNext", Type.EmptyTypes); } catch { }
                    PropertyInfo currentProp = null;
                    try { currentProp = enumerator.GetType().GetProperty("Current"); } catch { }
                    if (moveNext != null && currentProp != null)
                    {
                        while (true)
                        {
                            object moved = null;
                            try { moved = moveNext.Invoke(enumerator, null); } catch { yield break; }
                            if (moved == null) yield break;
                            try { if (!(bool)moved) yield break; } catch { yield break; }
                            object current = null;
                            try { current = currentProp.GetValue(enumerator, null); } catch { }
                            yield return current;
                        }
                    }
                }
            }
            IEnumerable e = list as IEnumerable;
            if (e != null) { foreach (object x in e) yield return x; yield break; }
        }

        private static bool EnsureWorldFortuitousEventConfig(int eventId)
        {
            try
            {
                if (g.conf == null) return false;
                object mgr = GetMember(g.conf, "worldFortuitousEventBase");
                if (mgr == null) return false;
                if (GetItem(mgr, eventId) != null) return true;
                if (eventId != DefaultTaskAnchorEventId) return false;

                object src = GetItem(mgr, TaskAnchorEventTemplateId);
                if (src == null) return false;

                object clone = CloneByConstructor(src, delegate (string n, object old)
                {
                    string k = (n ?? "").ToLowerInvariant();
                    if (IsId(n)) return eventId;
                    if (k == "name") return "AI奇遇任务锚点";
                    if (k == "icon" || k == "eventicon" || k == "mapicon" || k == "bigmapicon") return DefaultTaskAnchorEventIcon;
                    if (k == "duration" || k == "time" || k == "month") return -1;
                    return old;
                });
                if (clone == null) return false;
                CopyWritableMembers(src, clone);
                SetMember(clone, "id", eventId);
                SetMember(clone, "baseID", eventId);
                SetMember(clone, "name", "AI奇遇任务锚点");
                SetMember(clone, "icon", DefaultTaskAnchorEventIcon);
                SetMember(clone, "eventIcon", DefaultTaskAnchorEventIcon);
                SetMember(clone, "mapIcon", DefaultTaskAnchorEventIcon);
                SetMember(clone, "bigMapIcon", DefaultTaskAnchorEventIcon);
                SetMember(clone, "duration", -1);

                ForceRegisterConfItem(mgr, clone, eventId);
                return GetItem(mgr, eventId) != null;
            }
            catch { return false; }
        }

        private static void CopyWritableMembers(object src, object dst)
        {
            if (src == null || dst == null) return;
            Type t = src.GetType();
            try
            {
                FieldInfo[] fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo f = fields[i];
                    if (f.IsInitOnly || f.IsLiteral) continue;
                    try { f.SetValue(dst, f.GetValue(src)); } catch { }
                }
            }
            catch { }
            try
            {
                PropertyInfo[] props = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0; i < props.Length; i++)
                {
                    PropertyInfo p = props[i];
                    if (!p.CanRead || !p.CanWrite || p.GetIndexParameters().Length != 0) continue;
                    try { p.SetValue(dst, p.GetValue(src, null), null); } catch { }
                }
            }
            catch { }
        }

        private static bool ForceRegisterConfItem(object confManager, object newItem, int id)
        {
            if (confManager == null || newItem == null) return false;
            bool touched = AddConfItem(confManager, newItem);
            touched = TryAddToConfList(confManager, newItem) || touched;
            touched = TryPutConfDictionary(confManager, id, newItem) || touched;
            return touched;
        }

        private static bool TryAddToConfList(object confManager, object newItem)
        {
            object list = GetMember(confManager, "_allConfList");
            if (list == null) list = GetMember(confManager, "allConfList");
            if (list == null) return false;
            try
            {
                foreach (object it in Each(list, 200000)) if (object.ReferenceEquals(it, newItem) || GetInt(it, "id", int.MinValue) == GetInt(newItem, "id", int.MaxValue)) return true;
            }
            catch { }
            try { MethodInfo add = list.GetType().GetMethod("Add"); if (add != null) { add.Invoke(list, new object[] { newItem }); return true; } } catch { }
            return false;
        }

        private static bool TryPutConfDictionary(object confManager, int id, object newItem)
        {
            object dic = GetMember(confManager, "allConfDic");
            if (dic == null) dic = GetMember(confManager, "_allConfDic");
            if (dic == null) return false;
            try { MethodInfo set = dic.GetType().GetMethod("set_Item"); if (set != null) { set.Invoke(dic, new object[] { id, newItem }); return true; } } catch { }
            return false;
        }

        private static bool EnsureTaskBase(ModTask def)
        {
            try { if (g.conf.taskBase.GetItem(def.Id) != null) return true; } catch { }
            return AddConfItem(g.conf.taskBase, def.ToConfItem());
        }

        private static bool EnsureTypedTask(ModTask def)
        {
            ConfTaskBaseItem b = null;
            try { b = g.conf.taskBase.GetItem(def.Id); } catch { }
            if (b == null) return false;
            object mgr = GetMember(g.conf, "task" + b.type);
            if (mgr == null) return false;
            if (GetItem(mgr, def.Id) != null) return true;
            object src = GetItem(mgr, def.TemplateId);
            object clone = CloneByConstructor(src, delegate (string n, object old)
            {
                string k = (n ?? "").ToLowerInvariant();
                if (IsId(n)) return def.Id;
                if (k == "fortuitouseventid" || k == "eventid") return def.Task110FortuitousEventID;
                if (k == "positionid" || k == "position" || k == "targetpositionid") return def.Task110PositionID > 0 ? def.Task110PositionID : def.PositionId;
                return old;
            });
            if (clone == null) return false;
            SetMember(clone, "id", def.Id);
            SetMember(clone, "positionID", def.Task110PositionID > 0 ? def.Task110PositionID : def.PositionId);
            return AddConfItem(mgr, clone);
        }

        private static bool ForceRefreshTypedTaskPosition(ModTask def)
        {
            if (def == null) return false;
            try
            {
                ConfTaskBaseItem b = g.conf.taskBase.GetItem(def.Id);
                if (b == null) return false;
                object mgr = GetMember(g.conf, "task" + b.type);
                if (mgr == null) return false;
                object item = GetItem(mgr, def.Id);
                if (item == null) return EnsureTypedTask(def);
                int positionId = def.Task110PositionID > 0 ? def.Task110PositionID : def.PositionId;
                bool ok = false;
                ok = SetMember(item, "positionID", positionId) || ok;
                ok = SetMember(item, "positionId", positionId) || ok;
                ok = SetMember(item, "position", positionId) || ok;
                ok = SetMember(item, "targetPositionID", positionId) || ok;
                ok = SetMember(item, "targetPositionId", positionId) || ok;
                return ok;
            }
            catch { return false; }
        }

        [HarmonyPatch]
        private static class UnitInfoDataAddTaskPatch
        {
            private static MethodBase TargetMethod() { return AccessTools.Method(typeof(DataUnit.UnitInfoData), "AddTask", new Type[] { typeof(int) }); }
            private static void Postfix(int id, object __result) { if (defs.ContainsKey(id)) PreInitTaskData(__result); }
        }

        [HarmonyPatch]
        private static class WorldUnitBaseCreateTaskPatch
        {
            private static MethodBase TargetMethod() { return AccessTools.Method(typeof(WorldUnitBase), "CreateTask", new Type[] { typeof(DataUnit.TaskData) }); }
            private static void Prefix(object taskData) { int id = GetInt(taskData, "id", int.MinValue); if (defs.ContainsKey(id)) PreInitTaskData(taskData); }
        }

        // M4：Postfix patch MapEventBase.OpenEvent()。纯 Postfix，不改原方法行为；harmony.PatchAll 自动注册。
        [HarmonyPatch]
        private static class MapEventBaseOpenEventPatch
        {
            private static MethodBase TargetMethod() { return AccessTools.Method(typeof(MapEventBase), "OpenEvent", Type.EmptyTypes); }
            private static void Postfix(MapEventBase __instance) { try { OnMapEventOpened(__instance); } catch { } }
        }

        private static void PreInitTaskData(object td)
        {
            if (td == null) return;
            object obj = GetMember(td, "objData");
            if (obj == null) { obj = Activator.CreateInstance(typeof(DataObjectData)); SetMember(td, "objData", obj); }
            object all = GetMember(obj, "allObject");
            if (Count(all) <= 0) FillMinimalObjData(all);
            if (GetInt(td, "curCount", -1) < 0) SetMember(td, "curCount", 0);
        }

        private static void FillMinimalObjData(object all)
        {
            if (all == null) return;
            InvokeNoArg(all, "Clear");
            Type[] a = all.GetType().GetGenericArguments();
            if (a == null || a.Length < 2) return;
            object inner = Activator.CreateInstance(a[1]);
            DictSet(inner, "InitCreateTaskData", "1");
            DictSet(inner, "uiPath", "True");
            DictSet(all, "", inner);
        }

        private static bool InvokeTaskMethod(int taskId, string method)
        {
            try
            {
                TaskBase task = FindPlayerTask(taskId);
                if (task == null) return false;
                MethodInfo m = task.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (m == null) return false;
                m.Invoke(task, null);
                return true;
            }
            catch { return false; }
        }

        private static TaskBase FindPlayerTask(int taskId)
        {
            // playerUnit 公开直取；GetTask/allTask/taskData.id 未知是否 public，保留反射（低频路径）。
            WorldUnitBase player = g.world.playerUnit;
            if (player == null) return null;
            object list = null;
            try { MethodInfo m = player.GetType().GetMethod("GetTask", new Type[] { typeof(int) }); if (m != null) list = m.Invoke(player, new object[] { taskId }); } catch { }
            foreach (object it in Each(list, 50)) { TaskBase t = it as TaskBase; if (t != null) return t; }
            foreach (object it in Each(GetMember(player, "allTask"), 200))
            {
                TaskBase t = it as TaskBase;
                if (t != null && GetInt(GetMember(t, "taskData"), "id", int.MinValue) == taskId) return t;
            }
            return null;
        }

        private static object FindTaskData(int taskId)
        {
            object list = GetMember(g.world.playerUnit.data.unitData, "allTask");
            foreach (object it in Each(list, 300)) if (GetInt(it, "id", int.MinValue) == taskId) return it;
            return null;
        }

        // 任务系统统一桌面日志接口：后续不要另建第二套桌面日志函数。
        // 临时探测游戏接口时使用：WritePositionDebugLog 用于一次新会话/进存档时覆盖清空，AppendPositionDebugLog 用于同一流程内追加。
        // 探测完成后应清除调用点，只保留这组接口；探测策略需要自动化多层拆包，不要只看一层使得需要挤牙膏多次编译才能得知需要的形态，避免反射遍历 UI/Unity 对象或大型集合导致游戏崩溃。
        public static void WritePositionDebugLog(List<string> lines)
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string path = Path.Combine(desktop, "TaskSystem_PositionDebug.txt");
                File.WriteAllLines(path, lines.ToArray());
            }
            catch (Exception ex) { Debug.Log("[TaskSystem] 写入位置调试日志失败: " + ex.Message); }
        }

        public static void AppendPositionDebugLog(List<string> lines)
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string path = Path.Combine(desktop, "TaskSystem_PositionDebug.txt");
                List<string> output = new List<string>();
                output.Add("");
                output.AddRange(lines ?? new List<string>());
                File.AppendAllLines(path, output.ToArray());
            }
            catch (Exception ex) { Debug.Log("[TaskSystem] 追加位置调试日志失败: " + ex.Message); }
        }

        private static int[] UniquePositiveInts(params int[] values)
        {
            List<int> list = new List<int>();
            if (values == null) return list.ToArray();
            for (int i = 0; i < values.Length; i++)
            {
                int v = values[i];
                if (v <= 0 || list.Contains(v)) continue;
                list.Add(v);
            }
            return list.ToArray();
        }

        // ---- 基础反射原语 ----
        // 注意：反射仅用于访问 private/internal 成员（如 mapPosition/worldFortuitousEventBase/taskXXX 等配置管理器、
        // 各类占用索引字典）。凡是 public 成员（playerUnit、battle、build、mapEvent、roleGrade 等）一律直取，不走反射。
        // 反射本身有开销，绝不能放在每帧/高频循环里；当前只在任务注册与锚点创建等低频路径使用。

        private static object GetItem(object mgr, int id) { try { MethodInfo m = mgr.GetType().GetMethod("GetItem", new Type[] { typeof(int) }); if (m != null) return m.Invoke(mgr, new object[] { id }); } catch { } return null; }
        private static object GetMember(object o, string n) { if (o == null) return null; try { PropertyInfo p = o.GetType().GetProperty(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); if (p != null && p.GetIndexParameters().Length == 0) return p.GetValue(o, null); } catch { } try { FieldInfo f = o.GetType().GetField(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); if (f != null) return f.GetValue(o); } catch { } return null; }
        private static object GetMemberIgnoreCase(object o, string n) { if (o == null || n == null) return null; foreach (MemberInfo mi in o.GetType().GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) if (string.Equals(mi.Name, n, StringComparison.OrdinalIgnoreCase)) { PropertyInfo p = mi as PropertyInfo; FieldInfo f = mi as FieldInfo; try { if (p != null && p.GetIndexParameters().Length == 0) return p.GetValue(o, null); if (f != null) return f.GetValue(o); } catch { } } return null; }
        private static bool SetMember(object o, string n, object v) { if (o == null) return false; try { PropertyInfo p = o.GetType().GetProperty(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); if (p != null && p.CanWrite) { p.SetValue(o, Coerce(v, p.PropertyType), null); return true; } } catch { } try { FieldInfo f = o.GetType().GetField(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); if (f != null && !f.IsInitOnly) { f.SetValue(o, Coerce(v, f.FieldType)); return true; } } catch { } return false; }
        private static object Coerce(object v, Type t) { if (v == null) return DefaultValue(t); if (t.IsAssignableFrom(v.GetType())) return v; try { return Convert.ChangeType(v, t); } catch { return v; } }
        private static int GetInt(object o, string n, int d) { try { object v = GetMember(o, n); if (v != null) return Convert.ToInt32(v); } catch { } return d; }
        private static bool IsId(string n) { return string.Equals(n, "id", StringComparison.OrdinalIgnoreCase) || string.Equals(n, "baseID", StringComparison.OrdinalIgnoreCase); }
        private static object DefaultValue(Type t) { if (t == typeof(string)) return "0"; return t.IsValueType ? Activator.CreateInstance(t) : null; }
        private static int Count(object o) { try { PropertyInfo p = o.GetType().GetProperty("Count"); if (p != null) return Convert.ToInt32(p.GetValue(o, null)); } catch { } return -1; }
        private static void InvokeNoArg(object o, string n) { try { MethodInfo m = o.GetType().GetMethod(n, Type.EmptyTypes); if (m != null) m.Invoke(o, null); } catch { } }
        private static void DictSet(object d, object k, object v) { if (d == null) return; try { MethodInfo m = d.GetType().GetMethod("set_Item"); if (m != null) { m.Invoke(d, new object[] { k, v }); return; } } catch { } try { MethodInfo m = d.GetType().GetMethod("Add"); if (m != null) m.Invoke(d, new object[] { k, v }); } catch { } }
        private static object[] Each(object src, int limit) { List<object> r = new List<object>(); if (src == null) return r.ToArray(); IEnumerable e = src as IEnumerable; if (e != null) { try { foreach (object x in e) { r.Add(x); if (r.Count >= limit) return r.ToArray(); } if (r.Count > 0) return r.ToArray(); } catch { r.Clear(); } } int c = Count(src); if (c < 0) c = limit; MethodInfo g = src.GetType().GetMethod("get_Item", new Type[] { typeof(int) }); for (int i = 0; g != null && i < c && r.Count < limit; i++) { try { r.Add(g.Invoke(src, new object[] { i })); } catch { } } return r.ToArray(); }

        private static bool ManagerHasPointObject(object mgr, Vector2Int p)
        {
            if (mgr == null) return false;
            foreach (MethodInfo m in mgr.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!(m.Name.StartsWith("Get") || m.Name.StartsWith("Has") || m.Name.StartsWith("Find") || m.Name.StartsWith("Check"))) continue;
                ParameterInfo[] ps = m.GetParameters();
                if (ps.Length == 1 && ps[0].ParameterType == typeof(Vector2Int))
                {
                    object ret = null; try { ret = m.Invoke(mgr, new object[] { p }); } catch { }
                    if (ret != null && (!(ret is bool) || (bool)ret)) return true;
                }
            }
            return false;
        }

        private static bool RuntimeIndexHasPoint(object root, Vector2Int p, params string[] memberNames)
        {
            if (root == null || memberNames == null) return false;
            for (int i = 0; i < memberNames.Length; i++)
            {
                object index = GetMember(root, memberNames[i]);
                if (IndexContainsPoint(index, p)) return true;
            }
            return false;
        }

        private static bool IndexContainsPoint(object index, Vector2Int p)
        {
            if (index == null) return false;
            object[] keys = GetPointKeyCandidates(p);
            for (int i = 0; i < keys.Length; i++)
            {
                if (CallContainsLike(index, "ContainsKey", keys[i]) || CallContainsLike(index, "Contains", keys[i])) return true;
            }
            return false;
        }

        private static object[] GetPointKeyCandidates(Vector2Int p)
        {
            return new object[] { p, p.x + "_" + p.y, p.x + "," + p.y, p.x + "|" + p.y, p.x * 1000 + p.y, p.x * 10000 + p.y, p.y * 1000 + p.x, p.y * 10000 + p.x };
        }

        private static bool CallContainsLike(object target, string methodName, object key)
        {
            if (target == null || key == null) return false;
            MethodInfo[] methods;
            try { methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); }
            catch { return false; }
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo m = methods[i];
                if (m.Name != methodName) continue;
                ParameterInfo[] ps;
                try { ps = m.GetParameters(); } catch { continue; }
                if (ps.Length != 1) continue;
                object arg = key;
                if (arg != null && !ps[0].ParameterType.IsAssignableFrom(arg.GetType())) arg = CoercePointKey(key, ps[0].ParameterType);
                if (arg == null && ps[0].ParameterType.IsValueType) continue;
                try
                {
                    object ret = m.Invoke(target, new object[] { arg });
                    if (ret is bool && (bool)ret) return true;
                }
                catch { }
            }
            return false;
        }

        private static object CoercePointKey(object key, Type targetType)
        {
            if (targetType == null || key == null) return null;
            if (targetType.IsAssignableFrom(key.GetType())) return key;
            try
            {
                if (targetType == typeof(string)) return key.ToString();
                if (targetType == typeof(int) && key is Vector2Int) { Vector2Int v = (Vector2Int)key; return v.y * 10000 + v.x; }
                if (targetType == typeof(int) && key is string) return Convert.ToInt32((string)key);
                if (targetType == typeof(long) && key is int) return Convert.ToInt64((int)key);
            }
            catch { }
            return null;
        }

        private static object CloneByConstructor(object src, Func<string, object, object> map)
        {
            if (src == null) return null;
            ConstructorInfo[] cs = src.GetType().GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Array.Sort(cs, delegate (ConstructorInfo a, ConstructorInfo b) { return b.GetParameters().Length.CompareTo(a.GetParameters().Length); });
            foreach (ConstructorInfo c in cs)
            {
                ParameterInfo[] ps = c.GetParameters(); object[] args = new object[ps.Length]; bool ok = true;
                for (int i = 0; i < ps.Length; i++)
                {
                    object v = map(ps[i].Name, GetMemberIgnoreCase(src, ps[i].Name));
                    if (v == null) v = DefaultValue(ps[i].ParameterType);
                    if (v != null && !ps[i].ParameterType.IsAssignableFrom(v.GetType())) { try { v = Convert.ChangeType(v, ps[i].ParameterType); } catch { ok = false; } }
                    args[i] = v;
                }
                if (!ok) continue;
                try { return c.Invoke(args); } catch { }
            }
            return null;
        }

        private static bool AddConfItem(object mgr, object item)
        {
            if (mgr == null || item == null) return false;
            foreach (string name in new string[] { "AddItem", "Add" })
                foreach (MethodInfo m in mgr.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    ParameterInfo[] ps = m.GetParameters();
                    if (m.Name == name && ps.Length == 1 && ps[0].ParameterType.IsAssignableFrom(item.GetType())) { try { m.Invoke(mgr, new object[] { item }); return true; } catch { } }
                }
            return false;
        }
    }
}
