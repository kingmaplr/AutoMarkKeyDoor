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
        private const string Category = "DoorPatcher";
        
        /// <summary>
        /// Door.Start 方法的 Postfix 补丁
        /// 在门初始化完成后提取信息并注册
        /// </summary>
        /// <param name="__instance">Door 实例</param>
        public static void Postfix(Door __instance)
        {
            ModLogger.LogVerbose(Category, ">>> Door.Start Postfix 触发 <<<");
            
            if (__instance == null)
            {
                ModLogger.LogWarning(Category, "Door 实例为 null，跳过注册");
                return;
            }
            
            try
            {
                // 获取场景信息
                string sceneId = GetCurrentSceneID();
                string subSceneId = GetCurrentSubSceneID();
                
                ModLogger.LogVerbose(Category, $"捕获到门初始化:");
                ModLogger.LogVerbose(Category, $"  - 位置: {__instance.transform.position}");
                ModLogger.LogVerbose(Category, $"  - 场景ID: {sceneId}");
                ModLogger.LogVerbose(Category, $"  - 子场景ID: {subSceneId}");
                ModLogger.LogVerbose(Category, $"  - NoRequireItem: {__instance.NoRequireItem}");
                ModLogger.LogVerbose(Category, $"  - IsOpen: {__instance.IsOpen}");
                
                if (__instance.Interact != null)
                {
                    ModLogger.LogVerbose(Category, $"  - Interact.requireItem: {__instance.Interact.requireItem}");
                    ModLogger.LogVerbose(Category, $"  - Interact.requireItemId: {__instance.Interact.requireItemId}");
                }
                else
                {
                    ModLogger.LogVerbose(Category, "  - Interact: null (门无交互组件)");
                }
                
                // 注册门到管理器
                DoorInfo doorInfo = KeyDoorManager.RegisterDoor(__instance, sceneId, subSceneId);
                
                if (doorInfo != null)
                {
                    ModLogger.Log(Category, $"门注册成功，当前总数: {KeyDoorManager.DoorCount}");
                }
            }
            catch (System.Exception e)
            {
                ModLogger.LogError(Category, $"处理 Door.Start Postfix 时发生错误: {e.Message}\n{e.StackTrace}");
            }
        }
        
        #region 辅助方法
        
        /// <summary>
        /// 获取当前场景ID
        /// </summary>
        private static string GetCurrentSceneID() => SceneHelper.GetCurrentSceneID();
        
        /// <summary>
        /// 获取当前子场景ID
        /// </summary>
        private static string GetCurrentSubSceneID() => SceneHelper.GetCurrentSubSceneID();
        
        #endregion
    }
}
