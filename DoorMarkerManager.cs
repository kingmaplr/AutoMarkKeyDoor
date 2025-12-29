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
        private const string Category = "MarkerManager";
        
        /// <summary>
        /// 单例实例
        /// </summary>
        public static DoorMarkerManager Instance { get; private set; }
        
        /// <summary>
        /// 当前已创建的标记对象列表
        /// 预估初始容量为32，减少扩容次数
        /// </summary>
        private HashSet<GameObject> _markerObjects = new HashSet<GameObject>(32);
        
        /// <summary>
        /// 地图是否处于打开状态
        /// </summary>
        private bool _mapActive = false;
        
        #region Unity 生命周期
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                ModLogger.LogWarning(Category, "检测到重复的 DoorMarkerManager 实例，已销毁。");
                Destroy(this);
                return;
            }
            Instance = this;
            ModLogger.Log(Category, "实例已初始化。");
        }
        
        private void OnEnable()
        {
            ModLogger.Log(Category, "已启用。订阅事件...");
            
            // 订阅视图切换事件（地图打开/关闭）
            View.OnActiveViewChanged += OnActiveViewChanged;
            
            // 订阅场景加载事件
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            // 订阅筛选器变更事件
            if (DoorFilterUI.Instance != null)
            {
                DoorFilterUI.Instance.OnFilterChanged += OnFilterChanged;
            }
            
            ModLogger.Log(Category, "事件订阅完成。");
        }
        
        private void OnDisable()
        {
            ModLogger.Log(Category, "已禁用。取消订阅事件并清理...");
            
            // 取消订阅事件
            View.OnActiveViewChanged -= OnActiveViewChanged;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            
            // 取消订阅筛选器变更事件
            if (DoorFilterUI.Instance != null)
            {
                DoorFilterUI.Instance.OnFilterChanged -= OnFilterChanged;
            }
            
            // 清理所有标记
            ClearAllMarkers();
            
            ModLogger.Log(Category, "事件已取消订阅，标记已清理。");
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                ModLogger.Log(Category, "实例已销毁。");
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
            ModLogger.Log(Category, $"场景 '{scene.name}' 已加载。清理旧标记...");
            ClearAllMarkers();
            _mapActive = false;
        }
        
        /// <summary>
        /// 筛选条件变更事件处理
        /// </summary>
        private void OnFilterChanged()
        {
            if (_mapActive)
            {
                ModLogger.Log(Category, "筛选条件已变更，重新绘制标记...");
                DrawAllDoorMarkers();
            }
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
                ModLogger.LogVerbose(Category, "地图已经打开，跳过重复绘制。");
                return;
            }
            
            ModLogger.Log(Category, "地图已打开，开始绘制门标记...");
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
                ModLogger.Log(Category, "地图已关闭，清理门标记...");
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
            
            ModLogger.LogVerbose(Category, $"当前场景: {currentSceneId}, 子场景: {currentSubSceneId}");
            ModLogger.LogVerbose(Category, $"已注册门总数: {KeyDoorManager.DoorCount}");
            
            // 根据筛选条件获取要显示的门列表
            List<DoorInfo> doorsToShow = GetFilteredDoorList();
            ModLogger.LogVerbose(Category, $"筛选后门数量: {doorsToShow.Count}");
            
            int markersCreated = 0;
            
            foreach (DoorInfo door in doorsToShow)
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
                    ModLogger.LogVerbose(Category, $"跳过门 [Key={door.UniqueKey}]: 场景不匹配 (门场景={door.SceneID}, 当前={currentSceneId})");
                }
            }
            
            ModLogger.Log(Category, $"绘制完成，共创建 {markersCreated} 个标记。");
        }
        
        /// <summary>
        /// 根据筛选条件获取门列表
        /// </summary>
        private List<DoorInfo> GetFilteredDoorList()
        {
            // 如果有筛选UI，使用其筛选条件
            if (DoorFilterUI.Instance != null)
            {
                return DoorFilterUI.Instance.GetFilteredDoors();
            }
            
            // 默认返回所有需要钥匙的门
            return KeyDoorManager.GetAllLockedDoors();
        }
        
        /// <summary>
        /// 绘制单个门标记
        /// </summary>
        /// <param name="door">门信息</param>
        private void DrawDoorMarker(DoorInfo door)
        {
            ModLogger.LogVerbose(Category, $"正在为门 [Key={door.UniqueKey}, Name={door.DoorName}] 创建标记，位置: {door.Position}");
            
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
                ModLogger.LogError(Category, $"AddComponent<SimplePointOfInterest> 失败: {e.Message}");
                Destroy(markerObject);
                return;
            }
            
            // 获取图标
            Sprite iconToUse = GetDoorIcon();
            
            // 使用门名称作为标记名称
            string markerName = !string.IsNullOrEmpty(door.DoorName) ? door.DoorName : $"门#{door.RequireItemId}";
            
            // 设置标记
            try
            {
                poi.Setup(iconToUse, markerName, followActiveScene: true);
            }
            catch (Exception e)
            {
                ModLogger.LogError(Category, $"poi.Setup 失败: {e.Message}");
                Destroy(markerObject);
                return;
            }
            
            // 根据是否拥有钥匙设置不同颜色
            bool hasKey = KeyItemHelper.HasKeyForDoor(door);
            poi.Color = hasKey ? Color.green : Color.yellow;  // 拥有钥匙为绿色，否则为黄色
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
            
            ModLogger.LogVerbose(Category, $"门标记创建成功 [Key={door.UniqueKey}, Name={markerName}, HasKey={hasKey}]");
        }
        
        /// <summary>
        /// 清理所有标记
        /// </summary>
        private void ClearAllMarkers()
        {
            ModLogger.LogVerbose(Category, $"正在清理 {_markerObjects.Count} 个标记...");
            
            foreach (GameObject marker in _markerObjects)
            {
                if (marker != null)
                {
                    marker.SetActive(false);
                    Destroy(marker);
                }
            }
            
            _markerObjects.Clear();
            ModLogger.LogVerbose(Category, "所有标记已清理。");
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
                
                ModLogger.LogWarning(Category, "MapMarkerManager.Icons 为空，无法获取图标。");
            }
            catch (Exception e)
            {
                ModLogger.LogError(Category, $"获取图标失败: {e.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// 检查场景是否匹配
        /// </summary>
        private bool IsSceneMatch(string doorSceneId, string currentSceneId) 
            => SceneHelper.IsSceneMatch(doorSceneId, currentSceneId);
        
        /// <summary>
        /// 获取当前场景ID
        /// </summary>
        private string GetCurrentSceneID() => SceneHelper.GetCurrentSceneID();
        
        /// <summary>
        /// 获取当前子场景ID
        /// </summary>
        private string GetCurrentSubSceneID() => SceneHelper.GetCurrentSubSceneID();
        
        #endregion
    }
}
