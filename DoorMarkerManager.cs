using System;
using System.Collections.Generic;
using Duckov.MiniMaps;
using Duckov.MiniMaps.UI;
using Duckov.Scenes;
using Duckov.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AutoMarkKeyDoor
{
    /// <summary>
    /// 门标记管理器
    /// 负责在地图上创建、管理和销毁门的标记点
    /// </summary>
    public class DoorMarkerManager : MonoBehaviour
    {
        private const string LogPrefix = "[AutoMarkKeyDoor][MarkerManager] ";
        
        /// <summary>
        /// 单例实例
        /// </summary>
        public static DoorMarkerManager Instance { get; private set; }
        
        /// <summary>
        /// 当前已创建的标记对象列表
        /// </summary>
        private HashSet<GameObject> _markerObjects = new HashSet<GameObject>();
        
        /// <summary>
        /// 地图是否处于打开状态
        /// </summary>
        private bool _mapActive = false;
        
        #region Unity 生命周期
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning(LogPrefix + "检测到重复的 DoorMarkerManager 实例，已销毁。");
                Destroy(this);
                return;
            }
            Instance = this;
            Debug.Log(LogPrefix + "实例已初始化。");
        }
        
        private void OnEnable()
        {
            Debug.Log(LogPrefix + "已启用。订阅事件...");
            
            // 订阅视图切换事件（地图打开/关闭）
            View.OnActiveViewChanged += OnActiveViewChanged;
            
            // 订阅场景加载事件
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            Debug.Log(LogPrefix + "事件订阅完成。");
        }
        
        private void OnDisable()
        {
            Debug.Log(LogPrefix + "已禁用。取消订阅事件并清理...");
            
            // 取消订阅事件
            View.OnActiveViewChanged -= OnActiveViewChanged;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            
            // 清理所有标记
            ClearAllMarkers();
            
            Debug.Log(LogPrefix + "事件已取消订阅，标记已清理。");
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                Debug.Log(LogPrefix + "实例已销毁。");
            }
        }
        
        #endregion
        
        #region 事件处理
        
        /// <summary>
        /// 视图切换事件处理
        /// </summary>
        private void OnActiveViewChanged()
        {
            if (IsMapOpen())
            {
                BeginDrawMarkers();
            }
            else
            {
                EndDrawMarkers();
            }
        }
        
        /// <summary>
        /// 场景加载事件处理
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log(LogPrefix + $"场景 '{scene.name}' 已加载。清理旧标记...");
            ClearAllMarkers();
            _mapActive = false;
        }
        
        #endregion
        
        #region 标记管理
        
        /// <summary>
        /// 开始绘制门标记
        /// </summary>
        private void BeginDrawMarkers()
        {
            if (_mapActive)
            {
                Debug.Log(LogPrefix + "地图已经打开，跳过重复绘制。");
                return;
            }
            
            Debug.Log(LogPrefix + "地图已打开，开始绘制门标记...");
            _mapActive = true;
            
            DrawAllDoorMarkers();
        }
        
        /// <summary>
        /// 结束绘制门标记
        /// </summary>
        private void EndDrawMarkers()
        {
            if (_mapActive)
            {
                Debug.Log(LogPrefix + "地图已关闭，清理门标记...");
                _mapActive = false;
                ClearAllMarkers();
            }
        }
        
        /// <summary>
        /// 绘制所有门标记
        /// </summary>
        private void DrawAllDoorMarkers()
        {
            // 先清理旧标记
            ClearAllMarkers();
            
            // 获取当前场景信息
            string currentSceneId = GetCurrentSceneID();
            string currentSubSceneId = GetCurrentSubSceneID();
            
            Debug.Log(LogPrefix + $"当前场景: {currentSceneId}, 子场景: {currentSubSceneId}");
            Debug.Log(LogPrefix + $"已注册门总数: {KeyDoorManager.DoorCount}");
            
            // 获取所有需要钥匙的门
            List<DoorInfo> lockedDoors = KeyDoorManager.GetAllLockedDoors();
            Debug.Log(LogPrefix + $"需要钥匙的门数量: {lockedDoors.Count}");
            
            int markersCreated = 0;
            
            foreach (DoorInfo door in lockedDoors)
            {
                // 检查是否属于当前场景
                bool sceneMatches = IsSceneMatch(door.SceneID, currentSceneId);
                
                if (sceneMatches)
                {
                    DrawDoorMarker(door);
                    markersCreated++;
                }
                else
                {
                    Debug.Log(LogPrefix + $"跳过门 [Key={door.UniqueKey}]: 场景不匹配 (门场景={door.SceneID}, 当前={currentSceneId})");
                }
            }
            
            Debug.Log(LogPrefix + $"绘制完成，共创建 {markersCreated} 个标记。");
        }
        
        /// <summary>
        /// 绘制单个门标记
        /// </summary>
        /// <param name="door">门信息</param>
        private void DrawDoorMarker(DoorInfo door)
        {
            Debug.Log(LogPrefix + $"正在为门 [Key={door.UniqueKey}, ItemId={door.RequireItemId}] 创建标记，位置: {door.Position}");
            
            // 创建标记对象
            GameObject markerObject = new GameObject($"DoorMarker_{door.UniqueKey}");
            markerObject.transform.position = door.Position;
            
            // 添加 SimplePointOfInterest 组件
            SimplePointOfInterest poi;
            try
            {
                poi = markerObject.AddComponent<SimplePointOfInterest>();
            }
            catch (Exception e)
            {
                Debug.LogError(LogPrefix + $"AddComponent<SimplePointOfInterest> 失败: {e.Message}");
                Destroy(markerObject);
                return;
            }
            
            // 获取图标
            Sprite iconToUse = GetDoorIcon();
            
            // 设置标记
            try
            {
                string markerName = $"Door_{door.RequireItemId}";
                poi.Setup(iconToUse, markerName, followActiveScene: true);
            }
            catch (Exception e)
            {
                Debug.LogError(LogPrefix + $"poi.Setup 失败: {e.Message}");
                Destroy(markerObject);
                return;
            }
            
            // 设置标记样式 - 不需要区域半径
            poi.Color = Color.yellow;  // 使用黄色区分门标记
            poi.IsArea = false;        // 不显示区域圆圈
            poi.AreaRadius = 0f;       // 半径为0
            
            poi.ShadowColor = Color.black;
            poi.ShadowDistance = 0f;
            
            // 移动到主场景
            if (MultiSceneCore.MainScene.HasValue)
            {
                SceneManager.MoveGameObjectToScene(markerObject, MultiSceneCore.MainScene.Value);
            }
            
            // 添加到管理列表
            _markerObjects.Add(markerObject);
            
            Debug.Log(LogPrefix + $"门标记创建成功 [Key={door.UniqueKey}]");
        }
        
        /// <summary>
        /// 清理所有标记
        /// </summary>
        private void ClearAllMarkers()
        {
            Debug.Log(LogPrefix + $"正在清理 {_markerObjects.Count} 个标记...");
            
            foreach (GameObject marker in _markerObjects)
            {
                if (marker != null)
                {
                    marker.SetActive(false);
                    Destroy(marker);
                }
            }
            
            _markerObjects.Clear();
            Debug.Log(LogPrefix + "所有标记已清理。");
        }
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 检查地图是否打开
        /// </summary>
        private bool IsMapOpen()
        {
            MiniMapView view = MiniMapView.Instance;
            if (view != null)
            {
                return view == View.ActiveView;
            }
            return false;
        }
        
        /// <summary>
        /// 获取门图标
        /// </summary>
        private Sprite GetDoorIcon()
        {
            try
            {
                List<Sprite> allIcons = MapMarkerManager.Icons;
                
                if (allIcons != null && allIcons.Count > 0)
                {
                    // 使用第一个图标作为默认图标
                    return allIcons[0];
                }
                
                Debug.LogWarning(LogPrefix + "MapMarkerManager.Icons 为空，无法获取图标。");
            }
            catch (Exception e)
            {
                Debug.LogError(LogPrefix + $"获取图标失败: {e.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// 检查场景是否匹配
        /// </summary>
        private bool IsSceneMatch(string doorSceneId, string currentSceneId)
        {
            if (string.IsNullOrEmpty(doorSceneId) || string.IsNullOrEmpty(currentSceneId))
            {
                return false;
            }
            
            // 精确匹配或包含匹配（处理场景ID格式不一致的情况）
            return doorSceneId == currentSceneId ||
                   doorSceneId.Contains(currentSceneId) ||
                   currentSceneId.Contains(doorSceneId);
        }
        
        /// <summary>
        /// 获取当前场景ID
        /// </summary>
        private string GetCurrentSceneID()
        {
            try
            {
                var levelInfo = LevelManager.GetCurrentLevelInfo();
                return levelInfo.sceneName ?? "Unknown";
            }
            catch (Exception e)
            {
                Debug.LogWarning(LogPrefix + $"获取场景ID失败: {e.Message}");
            }
            
            return "Unknown";
        }
        
        /// <summary>
        /// 获取当前子场景ID
        /// </summary>
        private string GetCurrentSubSceneID()
        {
            try
            {
                var levelInfo = LevelManager.GetCurrentLevelInfo();
                return levelInfo.activeSubSceneID ?? "Default";
            }
            catch (Exception e)
            {
                Debug.LogWarning(LogPrefix + $"获取子场景ID失败: {e.Message}");
            }
            
            return "Default";
        }
        
        #endregion
    }
}
