using System;
using System.Collections.Generic;
using System.Linq;
using Duckov.MiniMaps.UI;
using Duckov.UI;
using UnityEngine;

namespace AutoMarkKeyDoor
{
    /// <summary>
    /// 门标记筛选模式枚举
    /// </summary>
    public enum DoorFilterMode
    {
        /// <summary>
        /// 不标记任何门
        /// </summary>
        None,
        
        /// <summary>
        /// 标记所有门（包括不需要钥匙的门）
        /// </summary>
        AllDoors,
        
        /// <summary>
        /// 仅标记需要钥匙的门
        /// </summary>
        AllKeyDoors,
        
        /// <summary>
        /// 仅标记玩家已拥有钥匙的门
        /// </summary>
        OwnedKeyDoors,
        
        /// <summary>
        /// 按特定钥匙类型筛选
        /// </summary>
        ByKeyType
    }
    
    /// <summary>
    /// 门筛选UI管理器
    /// 在地图打开时显示，允许用户选择标记模式和筛选条件
    /// </summary>
    public class DoorFilterUI : MonoBehaviour
    {
        private const string LogPrefix = "[AutoMarkKeyDoor][FilterUI] ";
        
        /// <summary>
        /// 单例实例
        /// </summary>
        public static DoorFilterUI Instance { get; private set; }
        
        #region 配置属性
        
        /// <summary>
        /// 当前筛选模式
        /// </summary>
        public DoorFilterMode CurrentMode { get; private set; } = DoorFilterMode.AllKeyDoors;
        
        /// <summary>
        /// 当前选中的钥匙类型ID（当 Mode 为 ByKeyType 时使用）
        /// 如果为空，表示选中所有类型
        /// </summary>
        public HashSet<int> SelectedKeyTypeIds { get; private set; } = new HashSet<int>();
        
        /// <summary>
        /// 筛选条件变更事件
        /// </summary>
        public event Action OnFilterChanged;
        
        #endregion
        
        #region UI 状态
        
        private bool _showUI = false;
        private Rect _windowRect = new Rect(10, 100, 280, 400);
        private Vector2 _keyListScrollPos = Vector2.zero;
        
        // 可用的钥匙类型列表（从已注册的门中获取）
        private Dictionary<int, string> _availableKeyTypes = new Dictionary<int, string>();
        
        // 统计信息缓存（避免每帧调用）
        private int _cachedTotalDoors = 0;
        private int _cachedKeyDoors = 0;
        private int _cachedOwnedKeyDoors = 0;
        private bool _statisticsDirty = true;
        
        // UI样式
        private GUIStyle _windowStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _toggleStyle;
        private GUIStyle _labelStyle;
        private bool _stylesInitialized = false;
        
        #endregion
        
        #region Unity 生命周期
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning(LogPrefix + "检测到重复的 DoorFilterUI 实例，已销毁。");
                Destroy(this);
                return;
            }
            Instance = this;
            Debug.Log(LogPrefix + "实例已初始化。");
        }
        
        private void OnEnable()
        {
            Debug.Log(LogPrefix + "已启用。订阅事件...");
            View.OnActiveViewChanged += OnActiveViewChanged;
        }
        
        private void OnDisable()
        {
            Debug.Log(LogPrefix + "已禁用。取消订阅事件...");
            View.OnActiveViewChanged -= OnActiveViewChanged;
            _showUI = false;
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
        
        private void OnActiveViewChanged()
        {
            bool mapOpen = IsMapOpen();
            
            if (mapOpen && !_showUI)
            {
                ShowUI();
            }
            else if (!mapOpen && _showUI)
            {
                HideUI();
            }
        }
        
        private bool IsMapOpen()
        {
            MiniMapView view = MiniMapView.Instance;
            return view != null && view == View.ActiveView;
        }
        
        #endregion
        
        #region UI 显示控制
        
        /// <summary>
        /// 显示筛选UI
        /// </summary>
        public void ShowUI()
        {
            Debug.Log(LogPrefix + "显示筛选UI");
            _showUI = true;
            RefreshKeyTypeList();
        }
        
        /// <summary>
        /// 隐藏筛选UI
        /// </summary>
        public void HideUI()
        {
            Debug.Log(LogPrefix + "隐藏筛选UI");
            _showUI = false;
        }
        
        /// <summary>
        /// 切换UI显示状态
        /// </summary>
        public void ToggleUI()
        {
            if (_showUI)
            {
                HideUI();
            }
            else
            {
                ShowUI();
            }
        }
        
        /// <summary>
        /// 刷新可用钥匙类型列表
        /// </summary>
        public void RefreshKeyTypeList()
        {
            _availableKeyTypes = KeyItemHelper.GetAllRegisteredKeyTypes();
            _statisticsDirty = true; // 标记统计信息需要刷新
            LogDebug($"刷新钥匙类型列表，共 {_availableKeyTypes.Count} 种");
        }
        
        /// <summary>
        /// 刷新统计信息缓存
        /// </summary>
        private void RefreshStatisticsCache()
        {
            if (!_statisticsDirty) return;
            
            _cachedTotalDoors = KeyDoorManager.DoorCount;
            _cachedKeyDoors = KeyDoorManager.GetAllLockedDoors().Count;
            _cachedOwnedKeyDoors = KeyItemHelper.GetDoorsWithOwnedKeys().Count;
            _statisticsDirty = false;
        }
        
        /// <summary>
        /// 标记统计信息需要刷新
        /// </summary>
        public void InvalidateStatistics()
        {
            _statisticsDirty = true;
        }
        
        #endregion
        
        #region OnGUI 绘制
        
        private void OnGUI()
        {
            if (!_showUI) return;
            
            InitializeStyles();
            
            // 绘制窗口
            _windowRect = GUI.Window(
                GetInstanceID(), 
                _windowRect, 
                DrawWindow, 
                "门标记筛选", 
                _windowStyle
            );
            
            // 限制窗口在屏幕范围内
            _windowRect.x = Mathf.Clamp(_windowRect.x, 0, Screen.width - _windowRect.width);
            _windowRect.y = Mathf.Clamp(_windowRect.y, 0, Screen.height - _windowRect.height);
        }
        
        private void InitializeStyles()
        {
            if (_stylesInitialized) return;
            
            // 窗口样式
            _windowStyle = new GUIStyle(GUI.skin.window)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            _windowStyle.normal.textColor = Color.white;
            
            // 标题样式
            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            _headerStyle.normal.textColor = new Color(1f, 0.9f, 0.5f);
            
            // 按钮样式
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Normal
            };
            
            // Toggle样式
            _toggleStyle = new GUIStyle(GUI.skin.toggle)
            {
                fontSize = 12
            };
            
            // 标签样式
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12
            };
            
            _stylesInitialized = true;
        }
        
        private void DrawWindow(int windowId)
        {
            GUILayout.BeginVertical();
            
            // 模式选择区域
            DrawModeSelection();
            
            GUILayout.Space(10);
            
            // 钥匙类型选择区域（仅在 ByKeyType 模式时显示）
            if (CurrentMode == DoorFilterMode.ByKeyType)
            {
                DrawKeyTypeSelection();
            }
            
            GUILayout.Space(10);
            
            // 统计信息
            DrawStatistics();
            
            GUILayout.Space(10);
            
            // 操作按钮
            DrawActionButtons();
            
            GUILayout.EndVertical();
            
            // 使窗口可拖拽
            GUI.DragWindow(new Rect(0, 0, _windowRect.width, 25));
        }
        
        private void DrawModeSelection()
        {
            GUILayout.Label("标记模式:", _headerStyle);
            
            // 不标记
            if (GUILayout.Toggle(CurrentMode == DoorFilterMode.None, " 不标记门", _toggleStyle))
            {
                if (CurrentMode != DoorFilterMode.None)
                {
                    SetFilterMode(DoorFilterMode.None);
                }
            }
            
            // 标记所有门
            if (GUILayout.Toggle(CurrentMode == DoorFilterMode.AllDoors, " 标记所有门", _toggleStyle))
            {
                if (CurrentMode != DoorFilterMode.AllDoors)
                {
                    SetFilterMode(DoorFilterMode.AllDoors);
                }
            }
            
            // 仅标记钥匙门
            if (GUILayout.Toggle(CurrentMode == DoorFilterMode.AllKeyDoors, " 仅标记钥匙门", _toggleStyle))
            {
                if (CurrentMode != DoorFilterMode.AllKeyDoors)
                {
                    SetFilterMode(DoorFilterMode.AllKeyDoors);
                }
            }
            
            // 仅标记已拥有钥匙的门
            if (GUILayout.Toggle(CurrentMode == DoorFilterMode.OwnedKeyDoors, " 仅标记已有钥匙的门", _toggleStyle))
            {
                if (CurrentMode != DoorFilterMode.OwnedKeyDoors)
                {
                    SetFilterMode(DoorFilterMode.OwnedKeyDoors);
                }
            }
            
            // 按钥匙类型筛选
            if (GUILayout.Toggle(CurrentMode == DoorFilterMode.ByKeyType, " 按钥匙类型筛选", _toggleStyle))
            {
                if (CurrentMode != DoorFilterMode.ByKeyType)
                {
                    SetFilterMode(DoorFilterMode.ByKeyType);
                }
            }
        }
        
        private void DrawKeyTypeSelection()
        {
            GUILayout.Label("选择钥匙类型:", _headerStyle);
            
            if (_availableKeyTypes.Count == 0)
            {
                GUILayout.Label("  (暂无已注册的钥匙门)", _labelStyle);
                return;
            }
            
            // 全选/全不选按钮
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("全选", _buttonStyle, GUILayout.Width(60)))
            {
                SelectAllKeyTypes();
            }
            if (GUILayout.Button("全不选", _buttonStyle, GUILayout.Width(60)))
            {
                DeselectAllKeyTypes();
            }
            GUILayout.EndHorizontal();
            
            // 滚动视图显示钥匙类型列表
            _keyListScrollPos = GUILayout.BeginScrollView(_keyListScrollPos, GUILayout.Height(150));
            
            foreach (var kvp in _availableKeyTypes)
            {
                int keyId = kvp.Key;
                string doorName = kvp.Value;
                bool isSelected = SelectedKeyTypeIds.Contains(keyId);
                bool isOwned = KeyItemHelper.IsKeyActivated(keyId);
                
                // 显示钥匙名称和拥有状态
                string displayText = isOwned ? $" ✓ {doorName}" : $"   {doorName}";
                
                bool newSelected = GUILayout.Toggle(isSelected, displayText, _toggleStyle);
                
                if (newSelected != isSelected)
                {
                    if (newSelected)
                    {
                        SelectedKeyTypeIds.Add(keyId);
                    }
                    else
                    {
                        SelectedKeyTypeIds.Remove(keyId);
                    }
                    NotifyFilterChanged();
                }
            }
            
            GUILayout.EndScrollView();
        }
        
        private void DrawStatistics()
        {
            GUILayout.Label("统计信息:", _headerStyle);
            
            // 使用缓存的统计信息，避免每帧调用
            RefreshStatisticsCache();
            
            GUILayout.Label($"  已注册门总数: {_cachedTotalDoors}", _labelStyle);
            GUILayout.Label($"  需要钥匙的门: {_cachedKeyDoors}", _labelStyle);
            GUILayout.Label($"  已有钥匙的门: {_cachedOwnedKeyDoors}", _labelStyle);
            GUILayout.Label($"  钥匙类型数量: {_availableKeyTypes.Count}", _labelStyle);
        }
        
        private void DrawActionButtons()
        {
            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button("刷新标记", _buttonStyle))
            {
                _statisticsDirty = true; // 刷新时同时更新统计信息
                NotifyFilterChanged();
            }
            
            if (GUILayout.Button("刷新钥匙", _buttonStyle))
            {
                RefreshKeyTypeList();
            }
            
            GUILayout.EndHorizontal();
            
#if DEBUG
            // 调试按钮仅在 DEBUG 模式下显示
            if (GUILayout.Button("调试输出", _buttonStyle))
            {
                KeyDoorManager.DebugPrintAllDoors();
                KeyItemHelper.DebugPrintKeyInfo();
            }
#endif
        }
        
        #endregion
        
        #region 筛选逻辑
        
        /// <summary>
        /// 设置筛选模式
        /// </summary>
        public void SetFilterMode(DoorFilterMode mode)
        {
            if (CurrentMode != mode)
            {
                Debug.Log(LogPrefix + $"切换筛选模式: {CurrentMode} -> {mode}");
                CurrentMode = mode;
                NotifyFilterChanged();
            }
        }
        
        /// <summary>
        /// 选中所有钥匙类型
        /// </summary>
        public void SelectAllKeyTypes()
        {
            SelectedKeyTypeIds.Clear();
            foreach (int keyId in _availableKeyTypes.Keys)
            {
                SelectedKeyTypeIds.Add(keyId);
            }
            NotifyFilterChanged();
        }
        
        /// <summary>
        /// 取消选中所有钥匙类型
        /// </summary>
        public void DeselectAllKeyTypes()
        {
            SelectedKeyTypeIds.Clear();
            NotifyFilterChanged();
        }
        
        /// <summary>
        /// 通知筛选条件已更改
        /// </summary>
        private void NotifyFilterChanged()
        {
            _statisticsDirty = true; // 筛选变更时刷新统计信息
            LogDebug($"筛选条件已更改，当前模式: {CurrentMode}");
            OnFilterChanged?.Invoke();
        }
        
        /// <summary>
        /// 根据当前筛选条件获取应该显示的门列表
        /// </summary>
        /// <returns>筛选后的门列表</returns>
        public List<DoorInfo> GetFilteredDoors()
        {
            List<DoorInfo> result = new List<DoorInfo>();
            
            switch (CurrentMode)
            {
                case DoorFilterMode.None:
                    // 不显示任何门
                    break;
                    
                case DoorFilterMode.AllDoors:
                    // 显示所有门
                    result = KeyDoorManager.AllDoors.Values.ToList();
                    break;
                    
                case DoorFilterMode.AllKeyDoors:
                    // 仅显示需要钥匙的门
                    result = KeyDoorManager.GetAllLockedDoors();
                    break;
                    
                case DoorFilterMode.OwnedKeyDoors:
                    // 仅显示玩家已拥有钥匙的门
                    result = KeyItemHelper.GetDoorsWithOwnedKeys();
                    break;
                    
                case DoorFilterMode.ByKeyType:
                    // 按选中的钥匙类型筛选
                    foreach (var kvp in KeyDoorManager.AllDoors)
                    {
                        DoorInfo door = kvp.Value;
                        if (!door.NoRequireItem && SelectedKeyTypeIds.Contains(door.RequireItemId))
                        {
                            result.Add(door);
                        }
                    }
                    break;
            }
            
            LogDebug($"筛选结果: {result.Count} 个门 (模式: {CurrentMode})");
            return result;
        }
        
        /// <summary>
        /// 检查指定门是否应该被显示
        /// </summary>
        public bool ShouldShowDoor(DoorInfo door)
        {
            if (door == null) return false;
            
            switch (CurrentMode)
            {
                case DoorFilterMode.None:
                    return false;
                    
                case DoorFilterMode.AllDoors:
                    return true;
                    
                case DoorFilterMode.AllKeyDoors:
                    return !door.NoRequireItem;
                    
                case DoorFilterMode.OwnedKeyDoors:
                    return !door.NoRequireItem && KeyItemHelper.HasKeyForDoor(door);
                    
                case DoorFilterMode.ByKeyType:
                    return !door.NoRequireItem && SelectedKeyTypeIds.Contains(door.RequireItemId);
                    
                default:
                    return false;
            }
        }
        
        #endregion
        
        #region 日志辅助
        
        /// <summary>
        /// 调试日志输出（仅在 DEBUG 模式下生效）
        /// </summary>
        [System.Diagnostics.Conditional("DEBUG")]
        private void LogDebug(string message)
        {
            Debug.Log(LogPrefix + message);
        }
        
        /// <summary>
        /// 警告日志输出
        /// </summary>
        [System.Diagnostics.Conditional("DEBUG")]
        private void LogWarning(string message)
        {
            Debug.LogWarning(LogPrefix + message);
        }
        
        #endregion
    }
}
