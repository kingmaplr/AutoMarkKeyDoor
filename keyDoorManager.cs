using System.Collections.Generic;
using System.Text.RegularExpressions;
using ItemStatsSystem;
using UnityEngine;

namespace AutoMarkKeyDoor
{
    /// <summary>
    /// 门信息数据结构类，存储门的关键信息
    /// 门对象是场景预制体，会随场景加载/卸载销毁，此数据结构支持跨场景使用
    /// </summary>
    public class DoorInfo
    {
        #region 预编译正则表达式
        
        /// <summary>
        /// 匹配 "xxx钥匙卡" 格式
        /// </summary>
        private static readonly Regex _regexCard = new Regex(@"^(.+?)钥匙卡$", RegexOptions.Compiled);
        
        /// <summary>
        /// 匹配 "xx钥匙-n" 格式（带数字编号）
        /// </summary>
        private static readonly Regex _regexWithNumber = new Regex(@"^(.+?)钥匙[-－]\d+$", RegexOptions.Compiled);
        
        /// <summary>
        /// 匹配 "xx钥匙" 格式（不带编号）
        /// </summary>
        private static readonly Regex _regexSimple = new Regex(@"^(.+?)钥匙$", RegexOptions.Compiled);
        
        #endregion
        
        /// <summary>
        /// 门的唯一标识符（基于位置计算）
        /// </summary>
        public int UniqueKey { get; set; }
        
        /// <summary>
        /// 门所在的场景ID
        /// </summary>
        public string SceneID { get; set; }
        
        /// <summary>
        /// 门所在的子场景ID (SubSceneID)
        /// </summary>
        public string SubSceneID { get; set; }
        
        /// <summary>
        /// 门的世界坐标位置
        /// </summary>
        public Vector3 Position { get; set; }
        
        /// <summary>
        /// 是否不需要额外道具即可开启
        /// true = 不需要钥匙，false = 需要钥匙
        /// </summary>
        public bool NoRequireItem { get; set; }
        
        /// <summary>
        /// 所需道具的ID (当 NoRequireItem 为 false 时有效)
        /// </summary>
        public int RequireItemId { get; set; }
        
        /// <summary>
        /// 门的名称（从钥匙物品名称解析而来）
        /// 用于在地图上标记时显示
        /// </summary>
        public string DoorName { get; set; }
        
        /// <summary>
        /// 根据位置生成唯一标识符
        /// 使用与游戏内 Door.GetKey() 相同的算法
        /// </summary>
        public static int CalculateUniqueKey(Vector3 position)
        {
            Vector3 scaledPos = position * 10f;
            int x = Mathf.RoundToInt(scaledPos.x);
            int y = Mathf.RoundToInt(scaledPos.y);
            int z = Mathf.RoundToInt(scaledPos.z);
            Vector3Int vec = new Vector3Int(x, y, z);
            return $"Door_{vec}".GetHashCode();
        }
        
        /// <summary>
        /// 根据钥匙物品名称解析门的名称
        /// 支持三种格式：
        /// 1. "xx钥匙" -> "xx"
        /// 2. "xx钥匙-n" -> "xx" (n为数字编号)
        /// 3. "xxx钥匙卡" -> "xxx"
        /// </summary>
        /// <param name="keyItemName">钥匙物品的显示名称</param>
        /// <returns>解析后的门名称，如果无法解析则返回原名称</returns>
        public static string ParseDoorNameFromKeyName(string keyItemName)
        {
            if (string.IsNullOrEmpty(keyItemName))
            {
                return "未知";
            }
            
            // 使用预编译的正则表达式进行匹配，按优先级尝试：
            // 1. "xxx钥匙卡" -> "xxx"
            // 2. "xx钥匙-n" -> "xx" (n为数字编号)
            // 3. "xx钥匙" -> "xx"
            
            var matchCard = _regexCard.Match(keyItemName);
            if (matchCard.Success)
            {
                return matchCard.Groups[1].Value;
            }
            
            var matchWithNumber = _regexWithNumber.Match(keyItemName);
            if (matchWithNumber.Success)
            {
                return matchWithNumber.Groups[1].Value;
            }
            
            var matchSimple = _regexSimple.Match(keyItemName);
            if (matchSimple.Success)
            {
                return matchSimple.Groups[1].Value;
            }
            
            // 如果都不匹配，返回原名称
            return keyItemName;
        }
        
        /// <summary>
        /// 根据物品ID获取钥匙名称并解析为门名称
        /// </summary>
        /// <param name="itemId">物品ID</param>
        /// <returns>解析后的门名称</returns>
        public static string GetDoorNameFromItemId(int itemId)
        {
            if (itemId <= 0)
            {
                return "无钥匙";
            }
            
            try
            {
                // 通过 ItemAssetsCollection 获取物品的显示名称
                ItemMetaData metaData = ItemAssetsCollection.GetMetaData(itemId);
                string displayName = metaData.DisplayName;
                return ParseDoorNameFromKeyName(displayName);
            }
            catch (System.Exception e)
            {
                ModLogger.LogWarning("DoorInfo", $"获取物品名称失败, ItemId={itemId}: {e.Message}");
            }
            
            return $"钥匙#{itemId}";
        }
        
        public override string ToString()
        {
            return $"DoorInfo[Key={UniqueKey}, Scene={SceneID}, SubScene={SubSceneID}, Pos={Position}, NoRequireItem={NoRequireItem}, RequireItemId={RequireItemId}, DoorName={DoorName}]";
        }
    }
    
    /// <summary>
    /// 门信息管理器 - 静态类，管理所有已注册的门信息
    /// </summary>
    public static class KeyDoorManager
    {
        private const string Category = "KeyDoorManager";
        
        /// <summary>
        /// 缓存所有已注册的门信息
        /// Key: 门的唯一标识符 (基于位置计算的 HashCode)
        /// Value: DoorInfo 对象
        /// </summary>
        private static readonly Dictionary<int, DoorInfo> _doorCache = new Dictionary<int, DoorInfo>();
        
        /// <summary>
        /// 获取所有已注册的门信息（只读）
        /// </summary>
        public static IReadOnlyDictionary<int, DoorInfo> AllDoors => _doorCache;
        
        /// <summary>
        /// 已注册的门数量
        /// </summary>
        public static int DoorCount => _doorCache.Count;
        
        /// <summary>
        /// 从 Door 对象提取信息并注册到缓存
        /// </summary>
        /// <param name="door">Door 对象</param>
        /// <param name="sceneId">场景ID</param>
        /// <param name="subSceneId">子场景ID</param>
        /// <returns>创建或更新的 DoorInfo</returns>
        public static DoorInfo RegisterDoor(Door door, string sceneId, string subSceneId)
        {
            if (door == null)
            {
                ModLogger.LogWarning(Category, "尝试注册一个 null 的 Door 对象");
                return null;
            }
            
            Vector3 position = door.transform.position;
            int uniqueKey = DoorInfo.CalculateUniqueKey(position);
            
            // 获取门的交互信息
            bool noRequireItem = door.NoRequireItem;
            int requireItemId = 0;
            
            // 如果需要道具，获取道具ID
            if (!noRequireItem && door.Interact != null)
            {
                requireItemId = door.Interact.requireItemId;
            }
            
            // 检查是否已注册
            if (_doorCache.TryGetValue(uniqueKey, out DoorInfo existingInfo))
            {
                // 更新现有信息
                existingInfo.NoRequireItem = noRequireItem;
                existingInfo.RequireItemId = requireItemId;
                existingInfo.SceneID = sceneId;
                existingInfo.SubSceneID = subSceneId;
                existingInfo.DoorName = noRequireItem ? "普通门" : DoorInfo.GetDoorNameFromItemId(requireItemId);
                
                ModLogger.LogVerbose(Category, $"门已存在，更新信息: {existingInfo}");
                return existingInfo;
            }
            
            // 根据钥匙物品ID解析门名称
            string doorName = noRequireItem ? "普通门" : DoorInfo.GetDoorNameFromItemId(requireItemId);
            
            // 创建新的 DoorInfo
            DoorInfo doorInfo = new DoorInfo
            {
                UniqueKey = uniqueKey,
                SceneID = sceneId,
                SubSceneID = subSceneId,
                Position = position,
                NoRequireItem = noRequireItem,
                RequireItemId = requireItemId,
                DoorName = doorName
            };
            
            // 添加到缓存
            _doorCache[uniqueKey] = doorInfo;
            
            ModLogger.Log(Category, $"成功注册门: {doorInfo}");
            
            return doorInfo;
        }
        
        /// <summary>
        /// 根据位置检查门是否已注册
        /// </summary>
        /// <param name="position">门的位置</param>
        /// <returns>是否已注册</returns>
        public static bool IsDoorRegistered(Vector3 position)
        {
            int key = DoorInfo.CalculateUniqueKey(position);
            return _doorCache.ContainsKey(key);
        }
        
        /// <summary>
        /// 根据位置获取门信息
        /// </summary>
        /// <param name="position">门的位置</param>
        /// <returns>DoorInfo 或 null</returns>
        public static DoorInfo GetDoorByPosition(Vector3 position)
        {
            int key = DoorInfo.CalculateUniqueKey(position);
            _doorCache.TryGetValue(key, out DoorInfo info);
            return info;
        }
        
        /// <summary>
        /// 根据唯一Key获取门信息
        /// </summary>
        /// <param name="uniqueKey">唯一标识符</param>
        /// <returns>DoorInfo 或 null</returns>
        public static DoorInfo GetDoorByKey(int uniqueKey)
        {
            _doorCache.TryGetValue(uniqueKey, out DoorInfo info);
            return info;
        }
        
        /// <summary>
        /// 根据道具ID获取所有需要该道具的门
        /// </summary>
        /// <param name="itemId">道具ID</param>
        /// <returns>需要该道具的门列表</returns>
        public static List<DoorInfo> GetDoorsByRequiredItem(int itemId)
        {
            List<DoorInfo> result = new List<DoorInfo>();
            
            foreach (var kvp in _doorCache)
            {
                DoorInfo info = kvp.Value;
                if (!info.NoRequireItem && info.RequireItemId == itemId)
                {
                    result.Add(info);
                }
            }
            
            ModLogger.LogVerbose(Category, $"查找道具ID={itemId}对应的门，找到 {result.Count} 个");
            return result;
        }
        
        /// <summary>
        /// 获取所有需要钥匙的门（排除不需要道具的门）
        /// </summary>
        /// <returns>需要钥匙的门列表</returns>
        public static List<DoorInfo> GetAllLockedDoors()
        {
            List<DoorInfo> result = new List<DoorInfo>();
            
            foreach (var kvp in _doorCache)
            {
                DoorInfo info = kvp.Value;
                if (!info.NoRequireItem)
                {
                    result.Add(info);
                }
            }
            
            ModLogger.LogVerbose(Category, $"查找所有需要钥匙的门，找到 {result.Count} 个");
            return result;
        }
        
        /// <summary>
        /// 获取指定场景的所有门
        /// </summary>
        /// <param name="sceneId">场景ID</param>
        /// <returns>该场景的门列表</returns>
        public static List<DoorInfo> GetDoorsByScene(string sceneId)
        {
            List<DoorInfo> result = new List<DoorInfo>();
            
            foreach (var kvp in _doorCache)
            {
                DoorInfo info = kvp.Value;
                if (info.SceneID == sceneId)
                {
                    result.Add(info);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 获取指定子场景的所有门
        /// </summary>
        /// <param name="subSceneId">子场景ID</param>
        /// <returns>该子场景的门列表</returns>
        public static List<DoorInfo> GetDoorsBySubScene(string subSceneId)
        {
            List<DoorInfo> result = new List<DoorInfo>();
            
            foreach (var kvp in _doorCache)
            {
                DoorInfo info = kvp.Value;
                if (info.SubSceneID == subSceneId)
                {
                    result.Add(info);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 清空所有门缓存
        /// </summary>
        public static void ClearAll()
        {
            int count = _doorCache.Count;
            _doorCache.Clear();
            
            ModLogger.Log(Category, $"已清空所有门缓存，共清理 {count} 个门");
        }
        
        /// <summary>
        /// 输出当前所有门的调试信息
        /// </summary>
        [System.Diagnostics.Conditional("DEBUG")]
        public static void DebugPrintAllDoors()
        {
            ModLogger.Log(Category, $"======= 当前已注册的门列表 (共 {_doorCache.Count} 个) =======");
            
            int lockedCount = 0;
            int unlockedCount = 0;
            
            foreach (var kvp in _doorCache)
            {
                ModLogger.Log(Category, $"  {kvp.Value}");
                
                if (kvp.Value.NoRequireItem)
                    unlockedCount++;
                else
                    lockedCount++;
            }
            
            ModLogger.Log(Category, $"统计: 需要钥匙={lockedCount}, 无需钥匙={unlockedCount}");
            ModLogger.Log(Category, "======= 门列表结束 =======");
        }
    }
}
