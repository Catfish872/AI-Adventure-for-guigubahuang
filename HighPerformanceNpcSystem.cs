using System;
using System.Collections.Generic;
using UnityEngine;

namespace MOD_kqAfiU
{
    /// <summary>
    /// 高性能NPC事件系统优化器
    /// 通过算法和数据结构优化提升GetRandomNpcWithEvent的性能
    /// </summary>
    public static class HighPerformanceNpcSystem
    {
        // 预分配的数组容量，避免动态扩容
        private const int INITIAL_CAPACITY = 10000;

        // 使用数组池模式，减少GC压力
        private static WorldUnitBase[] generalSameAreaBuffer = new WorldUnitBase[INITIAL_CAPACITY];
        private static WorldUnitBase[] generalOtherAreaBuffer = new WorldUnitBase[INITIAL_CAPACITY];
        private static WorldUnitBase[] oppositeSameAreaBuffer = new WorldUnitBase[INITIAL_CAPACITY];
        private static WorldUnitBase[] oppositeOtherAreaBuffer = new WorldUnitBase[INITIAL_CAPACITY];
        private static WorldUnitBase[] loverSameAreaBuffer = new WorldUnitBase[INITIAL_CAPACITY];
        private static WorldUnitBase[] loverOtherAreaBuffer = new WorldUnitBase[INITIAL_CAPACITY];
        private static WorldUnitBase[] parentSameAreaBuffer = new WorldUnitBase[INITIAL_CAPACITY];
        private static WorldUnitBase[] parentOtherAreaBuffer = new WorldUnitBase[INITIAL_CAPACITY];
        private static WorldUnitBase[] masterSameAreaBuffer = new WorldUnitBase[INITIAL_CAPACITY];
        private static WorldUnitBase[] masterOtherAreaBuffer = new WorldUnitBase[INITIAL_CAPACITY];

        // 计数器，替代List.Count
        private static int generalSameAreaCount = 0;
        private static int generalOtherAreaCount = 0;
        private static int oppositeSameAreaCount = 0;
        private static int oppositeOtherAreaCount = 0;
        private static int loverSameAreaCount = 0;
        private static int loverOtherAreaCount = 0;
        private static int parentSameAreaCount = 0;
        private static int parentOtherAreaCount = 0;
        private static int masterSameAreaCount = 0;
        private static int masterOtherAreaCount = 0;

        /// <summary>
        /// 高性能版本的GetRandomNpcWithEvent
        /// </summary>
        public static (WorldUnitBase npc, string selectedEvent) GetRandomNpcWithEvent()
        {
            // 重置所有计数器
            ResetCounters();

            // 获取玩家信息（只获取一次）
            var playerUnit = g.world.playerUnit;
            var playerUnitID = playerUnit.data.unitData.unitID;
            int playerAreaId = playerUnit.data.unitData.pointGridData.areaBaseID;
            int playerSex = (int)playerUnit.data.unitData.propertyData.sex;
            var playerRelation = playerUnit.data.unitData.relationData;

            // 预构建关系HashSet，将Contains操作从O(n)优化到O(1)
            var relationSets = BuildRelationSets(playerRelation);

            // 单次遍历分类所有NPC
            ClassifyNpcsOptimized(playerUnitID, playerAreaId, playerSex, relationSets);

            // 构建事件库（逻辑完全一致）
            var (allEvents, eventSources) = BuildEventLibrary();

            if (allEvents.Count == 0)
            {
                return (null, null);
            }

            // 随机选择事件
            int selectedIndex = UnityEngine.Random.Range(0, allEvents.Count);
            string selectedEvent = allEvents[selectedIndex];
            string selectedRelationType = eventSources[selectedIndex];

            // 高效选择NPC
            return SelectNpcByRelationType(selectedRelationType, selectedEvent);
        }

        /// <summary>
        /// 重置所有计数器
        /// </summary>
        private static void ResetCounters()
        {
            generalSameAreaCount = 0;
            generalOtherAreaCount = 0;
            oppositeSameAreaCount = 0;
            oppositeOtherAreaCount = 0;
            loverSameAreaCount = 0;
            loverOtherAreaCount = 0;
            parentSameAreaCount = 0;
            parentOtherAreaCount = 0;
            masterSameAreaCount = 0;
            masterOtherAreaCount = 0;
        }

        /// <summary>
        /// 预构建关系HashSet，优化关系检查性能
        /// </summary>
        private static RelationSets BuildRelationSets(DataUnit.RelationData relationData)
        {
            var sets = new RelationSets();
            sets.Initialize();

            // 将List转换为HashSet，Contains操作从O(n)变为O(1)
            foreach (var id in relationData.lover)
            {
                sets.LoverSet.Add(id.ToString());
            }

            foreach (var id in relationData.parent)
            {
                sets.ParentSet.Add(id.ToString());
            }

            foreach (var id in relationData.children)
            {
                sets.ChildrenSet.Add(id.ToString());
            }

            foreach (var id in relationData.parentBack)
            {
                sets.ParentBackSet.Add(id.ToString());
            }

            foreach (var id in relationData.childrenBack)
            {
                sets.ChildrenBackSet.Add(id.ToString());
            }

            foreach (var id in relationData.childrenPrivate)
            {
                sets.ChildrenPrivateSet.Add(id.ToString());
            }

            foreach (var id in relationData.master)
            {
                sets.MasterSet.Add(id.ToString());
            }

            foreach (var id in relationData.student)
            {
                sets.StudentSet.Add(id.ToString());
            }

            // 预处理married字段
            if (!string.IsNullOrEmpty(relationData.married))
            {
                sets.MarriedID = relationData.married;
            }

            return sets;
        }

        /// <summary>
        /// 优化版本的NPC分类函数
        /// </summary>
        private static void ClassifyNpcsOptimized(string playerUnitID, int playerAreaId, int playerSex, RelationSets relationSets)
        {
            var allUnits = g.world.unit.GetUnits(true);

            foreach (WorldUnitBase unit in allUnits)
            {
                // 快速排除玩家和死亡NPC - 修正为!isDie
                if (unit.data.unitData.unitID.Equals(playerUnitID) || unit.isDie)
                    continue;

                var npcUnitID = unit.data.unitData.unitID.ToString();
                bool isSameArea = unit.data.unitData.pointGridData.areaBaseID == playerAreaId;

                // 使用位运算优化关系检查
                int relationFlags = CalculateRelationFlags(npcUnitID, relationSets);

                // 根据关系标志位分类NPC
                ClassifyNpcByRelationFlags(unit, isSameArea, relationFlags, playerSex);
            }
        }

        /// <summary>
        /// 使用位运算计算关系标志
        /// </summary>
        private static int CalculateRelationFlags(string npcUnitID, RelationSets relationSets)
        {
            int flags = 0;

            if (relationSets.LoverSet.Contains(npcUnitID) || relationSets.MarriedID == npcUnitID)
                flags |= 1; // bit 0: lover/spouse

            if (relationSets.ParentSet.Contains(npcUnitID) || relationSets.ChildrenSet.Contains(npcUnitID) ||
                relationSets.ParentBackSet.Contains(npcUnitID) || relationSets.ChildrenBackSet.Contains(npcUnitID) ||
                relationSets.ChildrenPrivateSet.Contains(npcUnitID))
                flags |= 2; // bit 1: parent/child

            if (relationSets.MasterSet.Contains(npcUnitID) || relationSets.StudentSet.Contains(npcUnitID))
                flags |= 4; // bit 2: master/student

            return flags;
        }

        /// <summary>
        /// 根据关系标志位分类NPC
        /// </summary>
        private static void ClassifyNpcByRelationFlags(WorldUnitBase unit, bool isSameArea, int relationFlags, int playerSex)
        {
            // 检查特殊关系（优先级：lover > parent > master）
            if ((relationFlags & 1) != 0) // lover/spouse
            {
                AddToBuffer(unit, isSameArea, loverSameAreaBuffer, ref loverSameAreaCount,
                           loverOtherAreaBuffer, ref loverOtherAreaCount);
            }
            else if ((relationFlags & 2) != 0) // parent/child
            {
                AddToBuffer(unit, isSameArea, parentSameAreaBuffer, ref parentSameAreaCount,
                           parentOtherAreaBuffer, ref parentOtherAreaCount);
            }
            else if ((relationFlags & 4) != 0) // master/student
            {
                AddToBuffer(unit, isSameArea, masterSameAreaBuffer, ref masterSameAreaCount,
                           masterOtherAreaBuffer, ref masterOtherAreaCount);
            }
            else // 通用关系
            {
                // 通用NPC
                AddToBuffer(unit, isSameArea, generalSameAreaBuffer, ref generalSameAreaCount,
                           generalOtherAreaBuffer, ref generalOtherAreaCount);

                // 检查异性（避免重复获取性别）
                int npcSex = (int)unit.data.unitData.propertyData.sex;
                if (npcSex != playerSex)
                {
                    AddToBuffer(unit, isSameArea, oppositeSameAreaBuffer, ref oppositeSameAreaCount,
                               oppositeOtherAreaBuffer, ref oppositeOtherAreaCount);
                }
            }
        }

        /// <summary>
        /// 高效的缓冲区添加函数
        /// </summary>
        private static void AddToBuffer(WorldUnitBase unit, bool isSameArea,
            WorldUnitBase[] sameAreaBuffer, ref int sameAreaCount,
            WorldUnitBase[] otherAreaBuffer, ref int otherAreaCount)
        {
            if (isSameArea)
            {
                if (sameAreaCount < sameAreaBuffer.Length)
                {
                    sameAreaBuffer[sameAreaCount] = unit;
                    sameAreaCount++;
                }
            }
            else
            {
                if (otherAreaCount < otherAreaBuffer.Length)
                {
                    otherAreaBuffer[otherAreaCount] = unit;
                    otherAreaCount++;
                }
            }
        }

        /// <summary>
        /// 构建事件库（逻辑完全一致）
        /// </summary>
        private static (List<string> events, List<string> sources) BuildEventLibrary()
        {
            var allAvailableEvents = new List<string>();
            var eventSources = new List<string>();

            // 通用事件总是可用
            var generalEvents = Tools.GetGeneralEventTypes();
            foreach (var evt in generalEvents)
            {
                allAvailableEvents.Add(evt);
                eventSources.Add("general");
            }

            // 检查异性关系
            if (oppositeSameAreaCount > 0 || oppositeOtherAreaCount > 0)
            {
                var oppositeEvents = Tools.GetOppositeGenderEventTypes();
                foreach (var evt in oppositeEvents)
                {
                    allAvailableEvents.Add(evt);
                    eventSources.Add("opposite_gender");
                }
            }

            // 检查道侣/配偶关系
            if (loverSameAreaCount > 0 || loverOtherAreaCount > 0)
            {
                var loverEvents = Tools.GetLoverSpouseEventTypes();
                foreach (var evt in loverEvents)
                {
                    allAvailableEvents.Add(evt);
                    eventSources.Add("lover_spouse");
                }
            }

            // 检查父母子女关系
            if (parentSameAreaCount > 0 || parentOtherAreaCount > 0)
            {
                var parentEvents = Tools.GetParentChildEventTypes();
                foreach (var evt in parentEvents)
                {
                    allAvailableEvents.Add(evt);
                    eventSources.Add("parent_child");
                }
            }

            // 检查师徒关系
            if (masterSameAreaCount > 0 || masterOtherAreaCount > 0)
            {
                var masterEvents = Tools.GetMasterStudentEventTypes();
                foreach (var evt in masterEvents)
                {
                    allAvailableEvents.Add(evt);
                    eventSources.Add("master_student");
                }
            }

            return (allAvailableEvents, eventSources);
        }

        /// <summary>
        /// 根据关系类型选择NPC
        /// </summary>
        private static (WorldUnitBase, string) SelectNpcByRelationType(string relationType, string selectedEvent)
        {
            WorldUnitBase[] sameAreaBuffer = null;
            WorldUnitBase[] otherAreaBuffer = null;
            int sameAreaCount = 0;
            int otherAreaCount = 0;

            switch (relationType)
            {
                case "general":
                    sameAreaBuffer = generalSameAreaBuffer;
                    otherAreaBuffer = generalOtherAreaBuffer;
                    sameAreaCount = generalSameAreaCount;
                    otherAreaCount = generalOtherAreaCount;
                    break;
                case "opposite_gender":
                    sameAreaBuffer = oppositeSameAreaBuffer;
                    otherAreaBuffer = oppositeOtherAreaBuffer;
                    sameAreaCount = oppositeSameAreaCount;
                    otherAreaCount = oppositeOtherAreaCount;
                    break;
                case "lover_spouse":
                    sameAreaBuffer = loverSameAreaBuffer;
                    otherAreaBuffer = loverOtherAreaBuffer;
                    sameAreaCount = loverSameAreaCount;
                    otherAreaCount = loverOtherAreaCount;
                    break;
                case "parent_child":
                    sameAreaBuffer = parentSameAreaBuffer;
                    otherAreaBuffer = parentOtherAreaBuffer;
                    sameAreaCount = parentSameAreaCount;
                    otherAreaCount = parentOtherAreaCount;
                    break;
                case "master_student":
                    sameAreaBuffer = masterSameAreaBuffer;
                    otherAreaBuffer = masterOtherAreaBuffer;
                    sameAreaCount = masterSameAreaCount;
                    otherAreaCount = masterOtherAreaCount;
                    break;
            }

            // 优先从同区域选择
            if (sameAreaCount > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, sameAreaCount);
                return (sameAreaBuffer[randomIndex], selectedEvent);
            }

            // 其他区域选择
            if (otherAreaCount > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, otherAreaCount);
                return (otherAreaBuffer[randomIndex], selectedEvent);
            }

            return (null, null);
        }

        /// <summary>
        /// 关系集合数据结构
        /// </summary>
        private struct RelationSets
        {
            public HashSet<string> LoverSet;
            public HashSet<string> ParentSet;
            public HashSet<string> ChildrenSet;
            public HashSet<string> ParentBackSet;
            public HashSet<string> ChildrenBackSet;
            public HashSet<string> ChildrenPrivateSet;
            public HashSet<string> MasterSet;
            public HashSet<string> StudentSet;
            public string MarriedID;

            public void Initialize()
            {
                LoverSet = new HashSet<string>();
                ParentSet = new HashSet<string>();
                ChildrenSet = new HashSet<string>();
                ParentBackSet = new HashSet<string>();
                ChildrenBackSet = new HashSet<string>();
                ChildrenPrivateSet = new HashSet<string>();
                MasterSet = new HashSet<string>();
                StudentSet = new HashSet<string>();
                MarriedID = null;
            }
        }
    }
}