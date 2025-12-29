﻿using UnityEngine;
using HarmonyLib;

namespace AutoMarkKeyDoor
{
    /// <summary>
    /// AutoMarkKeyDoor Mod 的主入口类
    /// 负责初始化 Harmony 补丁和管理 Mod 生命周期
    /// </summary>
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private const string Category = "Main";
        private const string HarmonyId = "com.automarkeydoor.mod";
        
        private Harmony _harmony;
        private DoorMarkerManager _markerManager;
        private DoorFilterUI _filterUI;
        
        /// <summary>
        /// Mod 单例实例
        /// </summary>
        public static ModBehaviour Instance { get; private set; }
        
        void Awake()
        {
            ModLogger.Log(Category, "Mod 初始化中...");
            
            // 单例设置
            if (Instance != null && Instance != this)
            {
                ModLogger.LogWarning(Category, "已存在 ModBehaviour 实例，销毁当前实例");
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
            
            // 创建 Harmony 实例
            _harmony = new Harmony(HarmonyId);
            
            ModLogger.Log(Category, "Mod 初始化完成");
        }
        
        void OnEnable()
        {
            ModLogger.Log(Category, "Mod 已启用，正在应用补丁...");
            
            try
            {
                // 使用 PatchAll 自动应用所有带 HarmonyPatch 特性的补丁
                _harmony.PatchAll(typeof(ModBehaviour).Assembly);
                ModLogger.Log(Category, "Harmony 补丁应用成功");
            }
            catch (System.Exception e)
            {
                ModLogger.LogError(Category, $"应用补丁失败: {e.Message}\n{e.StackTrace}");
            }
            
            // 创建筛选UI（必须在门标记管理器之前创建，以便管理器能订阅其事件）
            try
            {
                if (_filterUI == null)
                {
                    _filterUI = gameObject.AddComponent<DoorFilterUI>();
                    ModLogger.Log(Category, "DoorFilterUI 组件已创建");
                }
            }
            catch (System.Exception e)
            {
                ModLogger.LogError(Category, $"创建 DoorFilterUI 失败: {e.Message}");
            }
            
            // 创建门标记管理器
            try
            {
                if (_markerManager == null)
                {
                    _markerManager = gameObject.AddComponent<DoorMarkerManager>();
                    ModLogger.Log(Category, "DoorMarkerManager 组件已创建");
                }
            }
            catch (System.Exception e)
            {
                ModLogger.LogError(Category, $"创建 DoorMarkerManager 失败: {e.Message}");
            }
        }
        
        void OnDisable()
        {
            ModLogger.Log(Category, "Mod 已禁用，正在卸载补丁...");
            
            try
            {
                // 卸载所有补丁
                _harmony?.UnpatchAll(HarmonyId);
                ModLogger.Log(Category, "Harmony 补丁卸载成功");
            }
            catch (System.Exception e)
            {
                ModLogger.LogError(Category, $"卸载补丁失败: {e.Message}");
            }
            
            // 销毁门标记管理器
            if (_markerManager != null)
            {
                Destroy(_markerManager);
                _markerManager = null;
                ModLogger.Log(Category, "DoorMarkerManager 组件已销毁");
            }
            
            // 销毁筛选UI
            if (_filterUI != null)
            {
                Destroy(_filterUI);
                _filterUI = null;
                ModLogger.Log(Category, "DoorFilterUI 组件已销毁");
            }
            
            // 输出最终的门统计信息
            ModLogger.Log(Category, $"Mod 禁用时共记录了 {KeyDoorManager.DoorCount} 个门");
            KeyDoorManager.DebugPrintAllDoors();
        }
        
        void OnDestroy()
        {
            ModLogger.Log(Category, "Mod 正在销毁...");
            
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
            
            // 确保筛选UI被销毁
            if (_filterUI != null)
            {
                Destroy(_filterUI);
                _filterUI = null;
            }
            
            // 清除钥匙缓存
            KeyItemHelper.ClearCache();
            
            ModLogger.Log(Category, "Mod 已销毁");
        }
    }
}