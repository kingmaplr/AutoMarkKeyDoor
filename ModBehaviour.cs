using UnityEngine;
using HarmonyLib;

namespace AutoMarkKeyDoor
{
    /// <summary>
    /// AutoMarkKeyDoor Mod 的主入口类
    /// 负责初始化 Harmony 补丁和管理 Mod 生命周期
    /// </summary>
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private const string LogPrefix = "[AutoMarkKeyDoor] ";
        private const string HarmonyId = "com.automarkeydoor.mod";
        
        private Harmony _harmony;
        private DoorMarkerManager _markerManager;
        
        /// <summary>
        /// Mod 单例实例
        /// </summary>
        public static ModBehaviour Instance { get; private set; }
        
        void Awake()
        {
            Debug.Log(LogPrefix + "Mod 初始化中...");
            
            // 单例设置
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning(LogPrefix + "已存在 ModBehaviour 实例，销毁当前实例");
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
            
            // 创建 Harmony 实例
            _harmony = new Harmony(HarmonyId);
            
            Debug.Log(LogPrefix + "Mod 初始化完成");
        }
        
        void OnEnable()
        {
            Debug.Log(LogPrefix + "Mod 已启用，正在应用补丁...");
            
            try
            {
                // 使用 PatchAll 自动应用所有带 HarmonyPatch 特性的补丁
                _harmony.PatchAll(typeof(ModBehaviour).Assembly);
                Debug.Log(LogPrefix + "Harmony 补丁应用成功");
            }
            catch (System.Exception e)
            {
                Debug.LogError(LogPrefix + $"应用补丁失败: {e.Message}\n{e.StackTrace}");
            }
            
            // 创建门标记管理器
            try
            {
                if (_markerManager == null)
                {
                    _markerManager = gameObject.AddComponent<DoorMarkerManager>();
                    Debug.Log(LogPrefix + "DoorMarkerManager 组件已创建");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(LogPrefix + $"创建 DoorMarkerManager 失败: {e.Message}");
            }
        }
        
        void OnDisable()
        {
            Debug.Log(LogPrefix + "Mod 已禁用，正在卸载补丁...");
            
            try
            {
                // 卸载所有补丁
                _harmony?.UnpatchAll(HarmonyId);
                Debug.Log(LogPrefix + "Harmony 补丁卸载成功");
            }
            catch (System.Exception e)
            {
                Debug.LogError(LogPrefix + $"卸载补丁失败: {e.Message}");
            }
            
            // 销毁门标记管理器
            if (_markerManager != null)
            {
                Destroy(_markerManager);
                _markerManager = null;
                Debug.Log(LogPrefix + "DoorMarkerManager 组件已销毁");
            }
            
            // 输出最终的门统计信息
            Debug.Log(LogPrefix + $"Mod 禁用时共记录了 {KeyDoorManager.DoorCount} 个门");
            KeyDoorManager.DebugPrintAllDoors();
        }
        
        void OnDestroy()
        {
            Debug.Log(LogPrefix + "Mod 正在销毁...");
            
            if (Instance == this)
            {
                Instance = null;
            }
            
            // 确保补丁被卸载
            _harmony?.UnpatchAll(HarmonyId);
            
            // 确保管理器被销毁
            if (_markerManager != null)
            {
                Destroy(_markerManager);
                _markerManager = null;
            }
            
            Debug.Log(LogPrefix + "Mod 已销毁");
        }
    }
}