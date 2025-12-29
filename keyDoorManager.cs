using System.Collections.Generic;
using UnityEngine;

namespace AutoMarkKeyDoor
{
    /// <summary>
    /// 门信息数据结构类，存储门的关键信息
    /// 门对象是场景预制体，会随场景加载/卸载销毁，此数据结构支持跨场景使用
    /// </summary>
    public class DoorInfo
    {
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
        
        public override string ToString()
        {
            return $"DoorInfo[Key={UniqueKey}, Scene={SceneID}, SubScene={SubSceneID}, Pos={Position}, NoRequireItem={NoRequireItem}, RequireItemId={RequireItemId}]";
        }
    }
    
    /// <summary>
    /// 门信息管理器 - 静态类，管理所有已注册的门信息
    /// </summary>
    public static class KeyDoorManager
    {
        private const string LogPrefix = "[AutoMarkKeyDoor][KeyDoorManager] ";
        
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
                Debug.LogWarning(LogPrefix + "尝试注册一个 null 的 Door 对象");
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
                
                Debug.Log(LogPrefix + $"门已存在，更新信息: {existingInfo}");
                return existingInfo;
            }
            
            // 创建新的 DoorInfo
            DoorInfo doorInfo = new DoorInfo
            {
                UniqueKey = uniqueKey,
                SceneID = sceneId,
                SubSceneID = subSceneId,
                Position = position,
                NoRequireItem = noRequireItem,
                RequireItemId = requireItemId
            };
            
            // 添加到缓存
            _doorCache[uniqueKey] = doorInfo;
            
            Debug.Log(LogPrefix + $"成功注册门: {doorInfo}");
            
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
            
            Debug.Log(LogPrefix + $"查找道具ID={itemId}对应的门，找到 {result.Count} 个");
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
            
            Debug.Log(LogPrefix + $"查找所有需要钥匙的门，找到 {result.Count} 个");
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
            
            Debug.Log(LogPrefix + $"已清空所有门缓存，共清理 {count} 个门");
        }
        
        /// <summary>
        /// 输出当前所有门的调试信息
        /// </summary>
        public static void DebugPrintAllDoors()
        {
            Debug.Log(LogPrefix + $"======= 当前已注册的门列表 (共 {_doorCache.Count} 个) =======");
            
            int lockedCount = 0;
            int unlockedCount = 0;
            
            foreach (var kvp in _doorCache)
            {
                Debug.Log(LogPrefix + $"  {kvp.Value}");
                
                if (kvp.Value.NoRequireItem)
                    unlockedCount++;
                else
                    lockedCount++;
            }
            
            Debug.Log(LogPrefix + $"统计: 需要钥匙={lockedCount}, 无需钥匙={unlockedCount}");
            Debug.Log(LogPrefix + "======= 门列表结束 =======");
        }
    }
}
