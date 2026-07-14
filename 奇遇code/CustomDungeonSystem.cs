using System;
using System.Collections.Generic;
using UnityEngine;

namespace MOD_kqAfiU
{
    public enum DungeonBattleType
    {
        Random,
        Normal,
        Hard,
        Boss,
        HumanBoss,
        LongBoss,
        LongHumanBoss
    }

    public sealed class CustomDungeonDefinition
    {
        public int TaskId;
        public DungeonBattleType BattleType;
        public int PlayerLevel;
        public int OrdinaryRoomCount;
        public int RoomPoint;
        public int WaveNum;
        public int WavePointGrow;
        public int EliteWave;
        public int[] NormalAttrIds;
        public int[] EliteAttrIds;
        public int BossAttrId;
        public int CustomBossAttrId;
        public string CustomBossName;
        public bool HumanBoss;
        public int HumanBossBaseId;
        public int HumanBossSourceAttrId;
        public List<int[]> NormalGroups = new List<int[]>();
    }

    public static class CustomDungeonFactory
    {
        private static readonly int[] NormalCatalog = new int[]
        {
            1101, 1102, 1103, 1104, 1106, 1107, 1201, 1202, 1203, 1301, 1302, 1303, 1304,
            1401, 1402, 1403, 1404, 1501, 1502, 1503, 1504, 2011, 2021, 2331, 2341, 2351,
            2451, 2461, 2471, 2501, 2511, 2521, 2571, 2581, 2591, 25411
        };

        private static readonly int[] EliteCatalog = new int[]
        {
            5001, 5002, 1204, 1405, 5003, 5004, 5005, 5006, 5007, 5008, 5009, 5010,
            5471, 5481, 5491, 5521, 5531
        };

        private static readonly int[] BeastBossCatalog = new int[]
        {
            7010, 7020, 7040, 7050, 7060, 7080, 7100, 7110, 7150, 7201,
            7210, 7220, 7341, 7351, 7361, 7371, 7381, 7881, 3100132, 950128
        };

        private static readonly int[] HumanBossBaseCatalog = new int[]
        {
            2001, 2002, 2003, 2004, 2005, 2006, 2011, 2012, 2013, 2014, 2015, 2016,
            2101, 2102, 2103, 2104, 2105, 2106, 2111, 2112, 2113, 2114, 2115, 2116
        };

        private static readonly int[] HumanBossSourceAttrCatalog = new int[]
        {
            320171, 320172, 320173, 320174, 320175, 320176, 320177, 320178, 320179, 320180, 320181, 320182,
            320183, 320184, 320185, 320186, 320187, 320188, 320189, 320190, 320191, 320192, 320193, 320194
        };

        private static readonly Dictionary<int, CustomDungeonDefinition> Definitions = new Dictionary<int, CustomDungeonDefinition>();
        private static readonly List<DungeonBattleType> BattleTypeBag = new List<DungeonBattleType>();

        public static DungeonBattleType RollBattleType()
        {
            if (BattleTypeBag.Count == 0)
            {
                BattleTypeBag.Add(DungeonBattleType.Normal);
                BattleTypeBag.Add(DungeonBattleType.Hard);
                BattleTypeBag.Add(DungeonBattleType.Boss);
                BattleTypeBag.Add(DungeonBattleType.HumanBoss);
                BattleTypeBag.Add(DungeonBattleType.LongBoss);
                BattleTypeBag.Add(DungeonBattleType.LongHumanBoss);
                for (int i = BattleTypeBag.Count - 1; i > 0; i--)
                {
                    int swapIndex = UnityEngine.Random.Range(0, i + 1);
                    DungeonBattleType temp = BattleTypeBag[i];
                    BattleTypeBag[i] = BattleTypeBag[swapIndex];
                    BattleTypeBag[swapIndex] = temp;
                }
            }

            DungeonBattleType result = BattleTypeBag[BattleTypeBag.Count - 1];
            BattleTypeBag.RemoveAt(BattleTypeBag.Count - 1);
            return result;
        }

        public static CustomDungeonDefinition Roll(int taskId, DungeonBattleType type, int playerLevel, string customBossName = "")
        {
            CustomDungeonDefinition existing;
            if (Definitions.TryGetValue(taskId, out existing)) return existing;
            if (type == DungeonBattleType.Random) type = RollBattleType();

            CustomDungeonDefinition def = new CustomDungeonDefinition
            {
                TaskId = taskId,
                BattleType = type,
                PlayerLevel = playerLevel,
                CustomBossName = customBossName ?? "",
                WavePointGrow = 40,
                NormalAttrIds = new int[0],
                EliteAttrIds = new int[0]
            };

            switch (type)
            {
                case DungeonBattleType.Normal:
                    ConfigureRooms(def, 1, 4, new int[] { 300, 350, 400 }, 2, 4, 0);
                    def.NormalAttrIds = PickDistinct(NormalCatalog, 3, 7);
                    break;
                case DungeonBattleType.Hard:
                    ConfigureRooms(def, 2, 5, new int[] { 350, 400, 450 }, 3, 4, 1);
                    def.NormalAttrIds = PickDistinct(NormalCatalog, 4, 8);
                    def.EliteAttrIds = PickDistinct(EliteCatalog, 1, 4);
                    break;
                case DungeonBattleType.Boss:
                    def.RoomPoint = 301;
                    def.BossAttrId = PickOne(BeastBossCatalog);
                    break;
                case DungeonBattleType.HumanBoss:
                    def.RoomPoint = 301;
                    PickHumanBoss(def);
                    break;
                case DungeonBattleType.LongBoss:
                    ConfigureLongRooms(def);
                    def.BossAttrId = PickOne(BeastBossCatalog);
                    break;
                case DungeonBattleType.LongHumanBoss:
                    ConfigureLongRooms(def);
                    PickHumanBoss(def);
                    break;
                default:
                    return null;
            }

            def.NormalGroups = BuildNormalGroups(def.NormalAttrIds);
            Definitions[taskId] = def;
            TaskSystem.AppendDebugLog(new List<string>
            {
                "[CustomDungeon] 生成定义",
                "taskId=" + taskId + " type=" + type + " playerLevel=" + playerLevel,
                "rooms=" + def.OrdinaryRoomCount + " roomPoint=" + def.RoomPoint + " waves=" + def.WaveNum + " eliteWave=" + def.EliteWave,
                "normalAttrs=" + Join(def.NormalAttrIds) + " eliteAttrs=" + Join(def.EliteAttrIds) + " bossSourceAttr=" + GetBossSourceAttrId(def) + " humanBoss=" + def.HumanBoss,
                "bossName=" + (string.IsNullOrEmpty(def.CustomBossName) ? "<保留来源原名>" : def.CustomBossName)
            });
            return def;
        }

        public static void ClearRuntimeState()
        {
            Definitions.Clear();
        }

        private static void ConfigureRooms(CustomDungeonDefinition def, int roomMin, int roomMax, int[] roomPoints, int waveMin, int waveMax, int eliteWave)
        {
            def.OrdinaryRoomCount = UnityEngine.Random.Range(roomMin, roomMax);
            def.RoomPoint = PickOne(roomPoints);
            def.WaveNum = UnityEngine.Random.Range(waveMin, waveMax);
            def.EliteWave = eliteWave;
        }

        private static void ConfigureLongRooms(CustomDungeonDefinition def)
        {
            ConfigureRooms(def, 3, 6, new int[] { 350, 400, 450 }, 3, 4, 1);
            def.NormalAttrIds = PickDistinct(NormalCatalog, 5, 9);
            def.EliteAttrIds = PickDistinct(EliteCatalog, 2, 5);
        }

        private static void PickHumanBoss(CustomDungeonDefinition def)
        {
            int index = UnityEngine.Random.Range(0, HumanBossBaseCatalog.Length);
            def.HumanBoss = true;
            def.HumanBossBaseId = HumanBossBaseCatalog[index];
            def.HumanBossSourceAttrId = HumanBossSourceAttrCatalog[index];
        }

        private static int GetBossSourceAttrId(CustomDungeonDefinition def)
        {
            if (def == null) return 0;
            return def.HumanBoss ? def.HumanBossSourceAttrId : def.BossAttrId;
        }

        private static int PickOne(int[] source)
        {
            return source == null || source.Length == 0 ? 0 : source[UnityEngine.Random.Range(0, source.Length)];
        }

        private static int[] PickDistinct(int[] source, int minInclusive, int maxExclusive)
        {
            int count = UnityEngine.Random.Range(minInclusive, maxExclusive);
            List<int> pool = new List<int>(source);
            List<int> result = new List<int>();
            while (pool.Count > 0 && result.Count < count)
            {
                int index = UnityEngine.Random.Range(0, pool.Count);
                result.Add(pool[index]);
                pool.RemoveAt(index);
            }
            return result.ToArray();
        }

        private static List<int[]> BuildNormalGroups(int[] attrs)
        {
            List<int[]> groups = new List<int[]>();
            if (attrs == null) return groups;
            for (int i = 0; i < attrs.Length; i++)
            {
                groups.Add(new int[] { attrs[i] });
                int sameCount = UnityEngine.Random.Range(2, 4);
                int[] same = new int[sameCount];
                for (int j = 0; j < same.Length; j++) same[j] = attrs[i];
                groups.Add(same);
            }
            int mixedCount = Math.Max(1, attrs.Length / 2);
            for (int i = 0; i < mixedCount && attrs.Length >= 2; i++)
            {
                int count = attrs.Length >= 3 ? UnityEngine.Random.Range(2, 4) : 2;
                groups.Add(PickDistinct(attrs, count, count + 1));
            }
            return groups;
        }

        private static string Join(int[] values)
        {
            return values == null || values.Length == 0 ? "0" : string.Join("|", Array.ConvertAll(values, delegate (int value) { return value.ToString(); }));
        }
    }

    public static class CustomDungeonConfigRegistry
    {
        private const int RangeSize = 20000000;
        private const int DungeonStart = 1500000000;
        private const int UnitStart = 1520000000;
        private const int ScaleItemStart = 1560000000;
        private const int ScaleGroupStart = 1580000000;
        private const int BossAttrStart = 1600000000;

        private static readonly Dictionary<string, int> AllocatedIds = new Dictionary<string, int>();
        private static readonly Dictionary<int, int> RegisteredDungeonIds = new Dictionary<int, int>();

        public static int EnsureRegistered(CustomDungeonDefinition definition)
        {
            if (definition == null) return 0;
            string dungeonManagerName = ResolveManagerName("dungeonBase", "dungeon");
            int existing;
            if (RegisteredDungeonIds.TryGetValue(definition.TaskId, out existing) && TaskSystem.GetConfigItem(dungeonManagerName, existing) != null)
                return existing;

            try
            {
                TaskSystem.AppendDebugLog(new List<string> { "[CustomDungeon] 开始注册 taskId=" + definition.TaskId + " type=" + definition.BattleType });
                int bossSourceAttrId = GetBossSourceAttrId(definition);
                int bossAttrId = 0;
                if (bossSourceAttrId > 0)
                {
                    bossAttrId = RegisterCustomBossAttr(definition, bossSourceAttrId);
                    if (bossAttrId <= 0) return Fail(definition, "boss_attr");
                }

                List<int> normalUnitIds = RegisterNormalGroups(definition);
                if (normalUnitIds.Count != definition.NormalGroups.Count) return Fail(definition, "normal_units");
                List<int> eliteUnitIds = RegisterSingleGroups(definition, definition.EliteAttrIds, "elite", true);
                if (eliteUnitIds.Count != definition.EliteAttrIds.Length) return Fail(definition, "elite_units");
                int bossUnitId = bossAttrId > 0 ? RegisterBossGroup(definition, bossAttrId) : 0;
                if (bossAttrId > 0 && bossUnitId <= 0) return Fail(definition, "boss_unit");

                int scaleId = 0;
                if (definition.BattleType == DungeonBattleType.Normal ||
                    definition.BattleType == DungeonBattleType.Hard ||
                    definition.BattleType == DungeonBattleType.LongBoss ||
                    definition.BattleType == DungeonBattleType.LongHumanBoss)
                {
                    scaleId = RegisterScale(definition);
                    if (scaleId <= 0) return Fail(definition, "scale");
                }

                int dungeonId = RegisterDungeon(definition, normalUnitIds, eliteUnitIds, bossUnitId, scaleId);
                if (dungeonId <= 0) return Fail(definition, "dungeon");
                RegisteredDungeonIds[definition.TaskId] = dungeonId;
                TaskSystem.AppendDebugLog(new List<string>
                {
                    "[CustomDungeon] 注册完成 dungeonId=" + dungeonId + " taskId=" + definition.TaskId,
                    "normalUnitBase=" + Join(normalUnitIds) + " eliteUnitBase=" + Join(eliteUnitIds) + " bossUnitBase=" + bossUnitId + " scaleBase=" + scaleId,
                    "bossSourceAttr=" + bossSourceAttrId + " customBossAttr=" + definition.CustomBossAttrId + " bossBaseID=" + (definition.HumanBoss ? definition.HumanBossBaseId : 0) + " bossName=" + (string.IsNullOrEmpty(definition.CustomBossName) ? "<保留来源原名>" : definition.CustomBossName)
                });
                return dungeonId;
            }
            catch (Exception ex)
            {
                TaskSystem.AppendDebugLog(new List<string> { "[CustomDungeon] 注册异常 taskId=" + definition.TaskId, ex.ToString() });
                return 0;
            }
        }

        public static void ClearRuntimeState()
        {
            AllocatedIds.Clear();
            RegisteredDungeonIds.Clear();
        }

        private static int RegisterCustomBossAttr(CustomDungeonDefinition def, int sourceAttrId)
        {
            string managerName = ResolveManagerName("battleUnitAttr", "battleUnitAttrBase");
            object source = TaskSystem.GetConfigItem(managerName, sourceAttrId);
            if (source == null) return 0;

            int id = AllocateFreeId(managerName, BossAttrStart, def.TaskId, "bossAttr", 0, false);
            def.CustomBossAttrId = id;
            if (TaskSystem.GetConfigItem(managerName, id) != null) return id;

            object clone = TaskSystem.CloneConfigItem(source);
            if (clone == null) return 0;

            const string stage = "boss_attr";
            if (!SetRequired(clone, stage, "id", id)) return 0;
            if (!string.IsNullOrEmpty(def.CustomBossName) &&
                !SetRequired(clone, stage, "name", def.CustomBossName)) return 0;

            if (def.HumanBoss &&
                (!SetRequired(clone, stage, "baseID", def.HumanBossBaseId) ||
                 !SetRequired(clone, stage, "growPercent", 100) ||
                 !SetRequired(clone, stage, "attrScale", "0") ||
                 !SetRequired(clone, stage, "dropID", 0) ||
                 !SetRequired(clone, stage, "firstReward", 0) ||
                 !SetRequired(clone, stage, "exp", 0))) return 0;

            return TaskSystem.RegisterConfigItem(managerName, clone, id) ? id : 0;
        }

        private static List<int> RegisterNormalGroups(CustomDungeonDefinition def)
        {
            List<int> result = new List<int>();
            for (int i = 0; i < def.NormalGroups.Count; i++)
            {
                int[] group = def.NormalGroups[i];
                int id = RegisterUnitGroup(def, group, "normal", i, 150, -1, group.Length * 15, 1000, PickWeightGrow());
                if (id > 0) result.Add(id);
            }
            return result;
        }

        private static List<int> RegisterSingleGroups(CustomDungeonDefinition def, int[] attrs, string kind, bool elite)
        {
            List<int> result = new List<int>();
            for (int i = 0; attrs != null && i < attrs.Length; i++)
            {
                int id = RegisterUnitGroup(def, new int[] { attrs[i] }, kind, i, elite ? 100 : 150, elite ? 1 : -1, elite ? 50 : 20, elite ? 10000 : 1000, 0);
                if (id > 0) result.Add(id);
            }
            return result;
        }

        private static int RegisterBossGroup(CustomDungeonDefinition def, int attrId)
        {
            return RegisterUnitGroup(def, new int[] { attrId }, "boss", 0, 1, -1, 0, 10000, 0);
        }

        private static int RegisterUnitGroup(CustomDungeonDefinition def, int[] attrs, string kind, int ordinal, int range, int num, int pointCost, int weightBase, int weightGrow)
        {
            string managerName = ResolveManagerName("dungeonUnitBase", "dungeonUnit");
            object manager = TaskSystem.GetConfigManager(managerName);
            object source = TaskSystem.GetFirstConfigItem(manager);
            if (source == null || attrs == null || attrs.Length == 0) return 0;
            int id = AllocateFreeId(managerName, UnitStart, def.TaskId, kind, ordinal, false);
            if (TaskSystem.GetConfigItem(managerName, id) != null) return id;
            object clone = TaskSystem.CloneConfigItem(source);
            if (clone == null) return 0;
            string stage = "unit_" + kind + "_" + ordinal;
            if (!SetRequired(clone, stage, "id", id) ||
                !SetRequired(clone, stage, "unitID", Join(attrs)) ||
                !SetRequired(clone, stage, "unitIDWeight", BuildZeros(attrs.Length)) ||
                !SetRequired(clone, stage, "range", range) ||
                !SetRequired(clone, stage, "num", num) ||
                !SetRequired(clone, stage, "pointCost", pointCost) ||
                !SetRequired(clone, stage, "weightBase", weightBase) ||
                !SetRequired(clone, stage, "weightGrow", weightGrow)) return 0;
            return TaskSystem.RegisterConfigItem(managerName, clone, id) ? id : 0;
        }

        private static int RegisterScale(CustomDungeonDefinition def)
        {
            string managerName = ResolveManagerName("dungeonScaleBase", "dungeonScale");
            object manager = TaskSystem.GetConfigManager(managerName);
            object source = TaskSystem.GetFirstConfigItem(manager);
            if (source == null) return 0;
            int itemId = AllocateFreeId(managerName, ScaleItemStart, def.TaskId, "scaleItem", 0, false);
            int scaleId = AllocateFreeId(managerName, ScaleGroupStart, def.TaskId, "scaleGroup", 0, true);
            if (TaskSystem.GetConfigItem(managerName, itemId) != null) return scaleId;
            object clone = TaskSystem.CloneConfigItem(source);
            if (clone == null) return 0;
            if (!SetRequired(clone, "scale", "id", itemId) ||
                !SetRequired(clone, "scale", "scaleID", scaleId) ||
                !SetRequired(clone, "scale", "roomPoint", def.RoomPoint) ||
                !SetRequired(clone, "scale", "waveNum", def.WaveNum) ||
                !SetRequired(clone, "scale", "wavePointGrow", def.WavePointGrow) ||
                !SetRequired(clone, "scale", "eliteWave", def.EliteWave) ||
                !SetRequired(clone, "scale", "weight", 10000) ||
                !SetRequired(clone, "scale", "type", 0) ||
                !SetRequired(clone, "scale", "monsterNum", 0)) return 0;
            return TaskSystem.RegisterConfigItem(managerName, clone, itemId) ? scaleId : 0;
        }

        private static int RegisterDungeon(CustomDungeonDefinition def, List<int> normalUnits, List<int> eliteUnits, int bossUnitId, int scaleId)
        {
            string managerName = ResolveManagerName("dungeonBase", "dungeon");
            int sourceId = def.BattleType == DungeonBattleType.Boss || def.BattleType == DungeonBattleType.HumanBoss ? 1105 : 101;
            object source = TaskSystem.GetConfigItem(managerName, sourceId);
            if (source == null) return 0;
            int id = AllocateFreeId(managerName, DungeonStart, def.TaskId, "dungeon", 0, false);
            if (TaskSystem.GetConfigItem(managerName, id) != null) return id;
            object clone = TaskSystem.CloneConfigItem(source);
            if (clone == null) return 0;
            if (!SetRequired(clone, "dungeon", "id", id) ||
                !SetRequired(clone, "dungeon", "totalPoint", def.OrdinaryRoomCount * def.RoomPoint) ||
                !SetRequired(clone, "dungeon", "unitBaseID", Join(normalUnits)) ||
                !SetRequired(clone, "dungeon", "eliteBaseID", Join(eliteUnits)) ||
                !SetRequired(clone, "dungeon", "bossBaseID", bossUnitId > 0 ? bossUnitId.ToString() : "0") ||
                (scaleId > 0 && !SetRequired(clone, "dungeon", "scaleBaseID", scaleId)) ||
                !SetRequired(clone, "dungeon", "roomBaseID", "101|102|103|701") ||
                !SetRequired(clone, "dungeon", "sceneBaseID", "1111|1171") ||
                !SetRequired(clone, "dungeon", "roomSpecial", "0") ||
                !SetRequired(clone, "dungeon", "timeLimit", -1) ||
                !SetRequired(clone, "dungeon", "victory", def.BattleType == DungeonBattleType.Normal || def.BattleType == DungeonBattleType.Hard ? 2 : 1) ||
                !SetRequired(clone, "dungeon", "winFunction", "0") ||
                !SetRequired(clone, "dungeon", "loseFunction", "0") ||
                !SetRequired(clone, "dungeon", "dungeonEffect", "0") ||
                !SetRequired(clone, "dungeon", "eliteWave", def.EliteWave)) return 0;
            return TaskSystem.RegisterConfigItem(managerName, clone, id) ? id : 0;
        }

        private static int AllocateFreeId(string managerName, int rangeStart, int taskId, string kind, int ordinal, bool compareScaleId)
        {
            string key = taskId + ":" + kind + ":" + ordinal;
            int existing;
            if (AllocatedIds.TryGetValue(key, out existing)) return existing;
            uint hash = StableHash(key);
            int offset = (int)(hash % RangeSize);
            object manager = TaskSystem.GetConfigManager(managerName);
            for (int i = 0; i < RangeSize; i++)
            {
                int id = rangeStart + ((offset + i) % RangeSize);
                bool occupied = compareScaleId ? TaskSystem.ConfigListContainsInt(manager, "scaleID", id) : TaskSystem.GetConfigItem(managerName, id) != null;
                if (!occupied)
                {
                    AllocatedIds[key] = id;
                    TaskSystem.AppendDebugLog(new List<string> { "[CustomDungeon] 分配ID " + key + "=" + id });
                    return id;
                }
            }
            throw new InvalidOperationException("自定义配置 ID 区段已满: " + managerName);
        }

        private static uint StableHash(string text)
        {
            uint hash = 2166136261;
            for (int i = 0; i < text.Length; i++)
            {
                hash ^= text[i];
                hash *= 16777619;
            }
            return hash;
        }

        private static int GetBossSourceAttrId(CustomDungeonDefinition def)
        {
            if (def == null) return 0;
            return def.HumanBoss ? def.HumanBossSourceAttrId : def.BossAttrId;
        }

        private static int Fail(CustomDungeonDefinition def, string stage)
        {
            TaskSystem.AppendDebugLog(new List<string> { "[CustomDungeon] 注册失败 taskId=" + def.TaskId + " stage=" + stage });
            return 0;
        }

        private static bool SetRequired(object item, string stage, string memberName, object value)
        {
            if (TaskSystem.SetConfigMember(item, memberName, value)) return true;
            TaskSystem.AppendDebugLog(new List<string> { "[CustomDungeon] 必需字段写入失败 stage=" + stage + " member=" + memberName });
            return false;
        }

        private static string ResolveManagerName(params string[] names)
        {
            for (int i = 0; names != null && i < names.Length; i++)
                if (TaskSystem.GetConfigManager(names[i]) != null) return names[i];
            return names != null && names.Length > 0 ? names[0] : "";
        }

        private static int PickWeightGrow()
        {
            int[] values = new int[] { 0, 500, 1000 };
            return values[UnityEngine.Random.Range(0, values.Length)];
        }

        private static string BuildZeros(int count)
        {
            if (count <= 0) return "0";
            string[] values = new string[count];
            for (int i = 0; i < count; i++) values[i] = "0";
            return string.Join("|", values);
        }

        private static string Join(int[] values)
        {
            if (values == null || values.Length == 0) return "0";
            return string.Join("|", Array.ConvertAll(values, delegate (int value) { return value.ToString(); }));
        }

        private static string Join(List<int> values)
        {
            return values == null || values.Count == 0 ? "0" : string.Join("|", values.ConvertAll(delegate (int value) { return value.ToString(); }).ToArray());
        }
    }
}
