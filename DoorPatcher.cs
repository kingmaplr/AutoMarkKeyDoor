using Duckov.Scenes;
using HarmonyLib;
using UnityEngine;

namespace AutoMarkKeyDoor
{
    /// <summary>
    /// Door.Start 方法的 Harmony 补丁
    /// 使用 HarmonyPatch 特性在门初始化时捕获门对象信息
    /// </summary>
    [HarmonyPatch(typeof(Door), "Start")]
    public static class DoorStartPatcher
    {
        private const string LogPrefix = "[AutoMarkKeyDoor][DoorPatcher] ";
        
        /// <summary>
        /// Door.Start 方法的 Postfix 补丁
        /// 在门初始化完成后提取信息并注册
        /// </summary>
        /// <param name="__instance">Door 实例</param>
        public static void Postfix(Door __instance)
        {
            Debug.Log(LogPrefix + $">>> Door.Start Postfix 触发 <<<");
            
            if (__instance == null)
            {
                Debug.LogWarning(LogPrefix + "Door 实例为 null，跳过注册");
                return;
            }
            
            try
            {
                // 获取场景信息
                string sceneId = GetCurrentSceneID();
                string subSceneId = GetCurrentSubSceneID();
                
                Debug.Log(LogPrefix + $"捕获到门初始化:");
                Debug.Log(LogPrefix + $"  - 位置: {__instance.transform.position}");
                Debug.Log(LogPrefix + $"  - 场景ID: {sceneId}");
                Debug.Log(LogPrefix + $"  - 子场景ID: {subSceneId}");
                Debug.Log(LogPrefix + $"  - NoRequireItem: {__instance.NoRequireItem}");
                Debug.Log(LogPrefix + $"  - IsOpen: {__instance.IsOpen}");
                
                if (__instance.Interact != null)
                {
                    Debug.Log(LogPrefix + $"  - Interact.requireItem: {__instance.Interact.requireItem}");
                    Debug.Log(LogPrefix + $"  - Interact.requireItemId: {__instance.Interact.requireItemId}");
                }
                else
                {
                    Debug.Log(LogPrefix + $"  - Interact: null (门无交互组件)");
                }
                
                // 注册门到管理器
                DoorInfo doorInfo = KeyDoorManager.RegisterDoor(__instance, sceneId, subSceneId);
                
                if (doorInfo != null)
                {
                    Debug.Log(LogPrefix + $"门注册成功，当前总数: {KeyDoorManager.DoorCount}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(LogPrefix + $"处理 Door.Start Postfix 时发生错误: {e.Message}\n{e.StackTrace}");
            }
        }
        
        #region 辅助方法
        
        /// <summary>
        /// 获取当前场景ID
        /// </summary>
        private static string GetCurrentSceneID()
        {
            try
            {
                if (MultiSceneCore.Instance != null)
                {
                    var levelInfo = LevelManager.GetCurrentLevelInfo();
                    return levelInfo.sceneName ?? "Unknown";
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning(LogPrefix + $"获取场景ID失败: {e.Message}");
            }
            
            return "Unknown";
        }
        
        /// <summary>
        /// 获取当前子场景ID
        /// </summary>
        private static string GetCurrentSubSceneID()
        {
            try
            {
                var levelInfo = LevelManager.GetCurrentLevelInfo();
                return levelInfo.activeSubSceneID ?? "Default";
            }
            catch (System.Exception e)
            {
                Debug.LogWarning(LogPrefix + $"获取子场景ID失败: {e.Message}");
            }
            
            return "Default";
        }
        
        #endregion
    }
}
