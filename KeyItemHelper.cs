using System.Collections.Generic;
using System.Linq;
using Duckov.MasterKeys;
using ItemStatsSystem;
using UnityEngine;

namespace AutoMarkKeyDoor
{
    /// <summary>
    /// 钥匙物品辅助类
    /// 用于检测玩家已拥有/激活的钥匙，以及获取所有可能的钥匙类型
    /// </summary>
    public static class KeyItemHelper
    {
        private const string Category = "KeyItemHelper";
        
        /// <summary>
        /// 缓存钥匙ID到门名称的映射
        /// </summary>
        private static Dictionary<int, string> _keyIdToDoorNameCache = new Dictionary<int, string>(16);
        
        /// <summary>
        /// 获取所有可能的钥匙物品ID列表
        /// </summary>
        public static List<int> GetAllPossibleKeyIds()
        {
            try
            {
                return MasterKeysManager.AllPossibleKeys;
            }
            catch (System.Exception e)
            {
                ModLogger.LogWarning(Category, $"获取所有钥匙ID列表失败: {e.Message}");
                return new List<int>();
            }
        }
        
        /// <summary>
        /// 检查指定钥匙是否已激活（玩家已拥有万能钥匙权限）
        /// </summary>
        /// <param name="keyItemId">钥匙物品ID</param>
        /// <returns>是否已激活</returns>
        public static bool IsKeyActivated(int keyItemId)
        {
            try
            {
                return MasterKeysManager.IsActive(keyItemId);
            }
            catch (System.Exception e)
            {
                ModLogger.LogWarning(Category, $"检查钥匙激活状态失败, ItemId={keyItemId}: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 获取所有已激活的钥匙ID列表
        /// </summary>
        /// <returns>已激活的钥匙ID列表</returns>
        public static List<int> GetAllActivatedKeyIds()
        {
            List<int> result = new List<int>();
            
            try
            {
                List<int> allKeys = GetAllPossibleKeyIds();
                foreach (int keyId in allKeys)
                {
                    if (IsKeyActivated(keyId))
                    {
                        result.Add(keyId);
                    }
                }
                
                ModLogger.LogVerbose(Category, $"找到 {result.Count} 个已激活的钥匙");
            }
            catch (System.Exception e)
            {
                ModLogger.LogWarning(Category, $"获取已激活钥匙列表失败: {e.Message}");
            }
            
            return result;
        }
        
        /// <summary>
        /// 获取钥匙物品的显示名称
        /// </summary>
        /// <param name="keyItemId">钥匙物品ID</param>
        /// <returns>显示名称，如果获取失败则返回 "钥匙#ID"</returns>
        public static string GetKeyDisplayName(int keyItemId)
        {
            try
            {
                ItemMetaData metaData = ItemAssetsCollection.GetMetaData(keyItemId);
                return metaData.DisplayName;
            }
            catch (System.Exception e)
            {
                ModLogger.LogWarning(Category, $"获取钥匙名称失败, ItemId={keyItemId}: {e.Message}");
            }
            
            return $"钥匙#{keyItemId}";
        }
        
        /// <summary>
        /// 根据钥匙物品ID获取对应的门名称
        /// 使用缓存提高性能
        /// </summary>
        /// <param name="keyItemId">钥匙物品ID</param>
        /// <returns>解析后的门名称</returns>
        public static string GetDoorNameByKeyId(int keyItemId)
        {
            // 先检查缓存
            if (_keyIdToDoorNameCache.TryGetValue(keyItemId, out string cachedName))
            {
                return cachedName;
            }
            
            // 解析并缓存
            string doorName = DoorInfo.GetDoorNameFromItemId(keyItemId);
            _keyIdToDoorNameCache[keyItemId] = doorName;
            
            return doorName;
        }
        
        /// <summary>
        /// 获取当前场景中用到的钥匙类型列表
        /// 返回包含钥匙ID和对应门名称的字典
        /// </summary>
        /// <returns>钥匙ID -> 门名称 的映射</returns>
        public static Dictionary<int, string> GetRegisteredKeyTypesByCurrentScene()
        {
            string sceneId = SceneHelper.GetCurrentSceneID();
            string subSceneId = SceneHelper.GetCurrentSubSceneID();
            Dictionary<int, string> result = new Dictionary<int, string>();
            
            foreach (var kvp in KeyDoorManager.AllDoors)
            {
                DoorInfo doorInfo = kvp.Value;
                if (!doorInfo.NoRequireItem && doorInfo.RequireItemId > 0 && (sceneId==doorInfo.SceneID || subSceneId == doorInfo.SceneID))
                {
                    if (!result.ContainsKey(doorInfo.RequireItemId))
                    {
                        result[doorInfo.RequireItemId] = doorInfo.DoorName;
                    }
                }
            }
            
            ModLogger.LogVerbose(Category, $"找到 {result.Count} 种不同的钥匙类型");
            return result;
        }
        
        /// <summary>
        /// 获取所有已注册门中用到的钥匙类型列表
        /// 返回包含钥匙ID和对应门名称的字典
        /// </summary>
        /// <returns>钥匙ID -> 门名称 的映射</returns>
        public static Dictionary<int, string> GetAllRegisteredKeyTypes()
        {
            Dictionary<int, string> result = new Dictionary<int, string>();
            
            foreach (var kvp in KeyDoorManager.AllDoors)
            {
                DoorInfo doorInfo = kvp.Value;
                if (!doorInfo.NoRequireItem && doorInfo.RequireItemId > 0)
                {
                    if (!result.ContainsKey(doorInfo.RequireItemId))
                    {
                        result[doorInfo.RequireItemId] = doorInfo.DoorName;
                    }
                }
            }
            
            ModLogger.LogVerbose(Category, $"找到 {result.Count} 种不同的钥匙类型");
            return result;
        }
        
        /// <summary>
        /// 检查玩家是否拥有指定门所需的钥匙
        /// </summary>
        /// <param name="doorInfo">门信息</param>
        /// <returns>是否拥有对应钥匙</returns>
        public static bool HasKeyForDoor(DoorInfo doorInfo)
        {
            if (doorInfo == null)
            {
                return false;
            }
            
            // 如果门不需要钥匙，返回 true
            if (doorInfo.NoRequireItem)
            {
                return true;
            }
            
            // 检查是否已激活该钥匙
            return IsKeyActivated(doorInfo.RequireItemId);
        }
        
        /// <summary>
        /// 获取所有玩家拥有钥匙的门
        /// </summary>
        /// <returns>玩家已拥有对应钥匙的门列表</returns>
        public static List<DoorInfo> GetDoorsWithOwnedKeys()
        {
            List<DoorInfo> result = new List<DoorInfo>();
            
            foreach (var kvp in KeyDoorManager.AllDoors)
            {
                DoorInfo doorInfo = kvp.Value;
                if (!doorInfo.NoRequireItem && HasKeyForDoor(doorInfo))
                {
                    result.Add(doorInfo);
                }
            }
            
            ModLogger.LogVerbose(Category, $"找到 {result.Count} 个玩家已拥有钥匙的门");
            return result;
        }
        
        /// <summary>
        /// 清除缓存
        /// </summary>
        public static void ClearCache()
        {
            _keyIdToDoorNameCache.Clear();
            ModLogger.Log(Category, "已清除钥匙名称缓存");
        }
        
        /// <summary>
        /// 输出调试信息
        /// </summary>
        [System.Diagnostics.Conditional("DEBUG")]
        public static void DebugPrintKeyInfo()
        {
            ModLogger.Log(Category, "======= 钥匙信息调试输出 =======");
            
            // 输出所有可能的钥匙
            List<int> allKeys = GetAllPossibleKeyIds();
            ModLogger.Log(Category, $"游戏中共有 {allKeys.Count} 种钥匙");
            
            // 输出已激活的钥匙
            List<int> activatedKeys = GetAllActivatedKeyIds();
            ModLogger.Log(Category, $"玩家已激活 {activatedKeys.Count} 种钥匙:");
            foreach (int keyId in activatedKeys)
            {
                string displayName = GetKeyDisplayName(keyId);
                string doorName = GetDoorNameByKeyId(keyId);
                ModLogger.Log(Category, $"  - ID={keyId}, 名称={displayName}, 门名称={doorName}");
            }
            
            // 输出已注册门中用到的钥匙类型
            var registeredKeyTypes = GetAllRegisteredKeyTypes();
            ModLogger.Log(Category, $"已注册门中使用了 {registeredKeyTypes.Count} 种钥匙:");
            foreach (var kvp in registeredKeyTypes)
            {
                bool owned = IsKeyActivated(kvp.Key);
                ModLogger.Log(Category, $"  - ID={kvp.Key}, 门名称={kvp.Value}, 已拥有={owned}");
            }
            
            ModLogger.Log(Category, "======= 钥匙信息输出结束 =======");
        }
    }
}
