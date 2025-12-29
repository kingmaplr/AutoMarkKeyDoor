using System;
using UnityEngine;

namespace AutoMarkKeyDoor
{
    /// <summary>
    /// 场景工具类
    /// 提供获取场景ID等通用方法，供多个类复用
    /// </summary>
    public static class SceneHelper
    {
        private const string LogPrefix = "[AutoMarkKeyDoor][SceneHelper] ";
        
        /// <summary>
        /// 获取当前场景ID
        /// </summary>
        /// <returns>场景名称，获取失败返回 "Unknown"</returns>
        public static string GetCurrentSceneID()
        {
            try
            {
                var levelInfo = LevelManager.GetCurrentLevelInfo();
                return levelInfo.sceneName ?? "Unknown";
            }
            catch (Exception e)
            {
                LogWarning($"获取场景ID失败: {e.Message}");
            }
            
            return "Unknown";
        }
        
        /// <summary>
        /// 获取当前子场景ID
        /// </summary>
        /// <returns>子场景ID，获取失败返回 "Default"</returns>
        public static string GetCurrentSubSceneID()
        {
            try
            {
                var levelInfo = LevelManager.GetCurrentLevelInfo();
                return levelInfo.activeSubSceneID ?? "Default";
            }
            catch (Exception e)
            {
                LogWarning($"获取子场景ID失败: {e.Message}");
            }
            
            return "Default";
        }
        
        /// <summary>
        /// 检查两个场景ID是否匹配
        /// 支持精确匹配和包含匹配（处理场景ID格式不一致的情况）
        /// </summary>
        /// <param name="sceneId1">场景ID 1</param>
        /// <param name="sceneId2">场景ID 2</param>
        /// <returns>是否匹配</returns>
        public static bool IsSceneMatch(string sceneId1, string sceneId2)
        {
            if (string.IsNullOrEmpty(sceneId1) || string.IsNullOrEmpty(sceneId2))
            {
                return false;
            }
            
            // 精确匹配或包含匹配
            return sceneId1 == sceneId2 ||
                   sceneId1.Contains(sceneId2) ||
                   sceneId2.Contains(sceneId1);
        }
        
        /// <summary>
        /// 检查门是否属于当前场景
        /// </summary>
        /// <param name="doorSceneId">门所在的场景ID</param>
        /// <returns>是否匹配当前场景</returns>
        public static bool IsDoorInCurrentScene(string doorSceneId)
        {
            string currentSceneId = GetCurrentSceneID();
            return IsSceneMatch(doorSceneId, currentSceneId);
        }
        
        /// <summary>
        /// 获取当前场景信息的简要描述
        /// </summary>
        /// <returns>场景描述字符串</returns>
        public static string GetCurrentSceneDescription()
        {
            string sceneId = GetCurrentSceneID();
            string subSceneId = GetCurrentSubSceneID();
            return $"Scene={sceneId}, SubScene={subSceneId}";
        }
        
        #region 日志辅助
        
        /// <summary>
        /// 调试日志输出（仅在 DEBUG 模式下生效）
        /// </summary>
        [System.Diagnostics.Conditional("DEBUG")]
        private static void LogDebug(string message)
        {
            Debug.Log(LogPrefix + message);
        }
        
        /// <summary>
        /// 警告日志输出（仅在 DEBUG 模式下生效）
        /// </summary>
        [System.Diagnostics.Conditional("DEBUG")]
        private static void LogWarning(string message)
        {
            Debug.LogWarning(LogPrefix + message);
        }
        
        #endregion
    }
}
