# AutoMarkKeyDoor - 自动标注钥匙门

[English](#english) | [中文](#中文)

---

## English

### Description

**AutoMarkKeyDoor** is a mod for the game **Duckov** that automatically marks doors on the map based on the keys you own. This helps players easily find which doors can be unlocked with their current keys.

### Features

- 🔑 **Automatic Door Marking**: Automatically displays markers on the minimap for doors that match your owned keys
- 🗺️ **Scene-aware**: Only shows doors in the current scene
- 🎨 **Filter UI**: Built-in UI to filter and manage door markers
- ⚡ **Performance Optimized**: Efficient caching mechanism to minimize performance impact

### Installation

1. Make sure you have the Duckov mod loader installed
2. Download the latest release from [Steam Workshop]()
3. Subscribe to the mod in Steam Workshop
4. The mod will be automatically loaded when you start the game

### Manual Installation

1. Download the release package ``
2. Extract the archive to your Duckov mods folder
3. The mod will be automatically loaded when you start the game

### Usage

Once installed, the mod works automatically:
- When you pick up a key, the corresponding door will be marked on the map
- Use the filter UI (if enabled) to customize which doors are shown

### Requirements

- Duckov game
- .NET Standard 2.1
- Harmony (for runtime patching)

### License

This project is open source. Feel free to use, modify, and distribute.

---

## 中文

### 简介

**AutoMarkKeyDoor（自动标注钥匙门）** 是一款 **Duckov** 游戏的 Mod，可以根据你拥有的钥匙自动在地图上标注对应的门。这帮助玩家轻松找到当前钥匙可以打开的门。

### 功能特性

- 🔑 **自动标注门**: 自动在小地图上显示与你持有钥匙匹配的门的标记
- 🗺️ **场景感知**: 只显示当前场景中的门
- 🎨 **筛选界面**: 内置 UI 用于筛选和管理门标记
- ⚡ **性能优化**: 高效的缓存机制，最大限度减少性能影响

### 安装方法

1. 确保你已安装 Duckov 模组加载器
2. 从 [Steam 创意工坊]() 下载最新版本
3. 在 Steam 创意工坊订阅此 Mod
4. 启动游戏时 Mod 将自动加载

### 手动安装

1. 下载发布压缩包 ``
2. 将压缩包解压到 Duckov mods 文件夹
3. 启动游戏时 Mod 将自动加载

### 使用方法

安装后，Mod 自动运行：
- 当你拾取钥匙时，对应的门将在地图上被标记
- 使用筛选界面（如已启用）来自定义显示哪些门

### 系统要求

- Duckov 游戏
- .NET Standard 2.1
- Harmony（用于运行时补丁）

### 开源协议

本项目开源，可自由使用、修改和分发。

---

## Project Structure / 项目结构

```
AutoMarkKeyDoor/
├── ModBehaviour.cs       # Main entry point / 主入口
├── DoorMarkerManager.cs  # Manages door markers / 门标记管理器
├── DoorFilterUI.cs       # Filter UI component / 筛选界面组件
├── DoorPatcher.cs        # Harmony patches / Harmony 补丁
├── KeyDoorManager.cs     # Key-door relationship manager / 钥匙门关系管理
├── KeyItemHelper.cs      # Key item utilities / 钥匙物品工具类
├── SceneHelper.cs        # Scene utilities / 场景工具类
└── ModLogger.cs          # Logging utilities / 日志工具类
```

## Contributing / 贡献

Feel free to submit issues and pull requests!

欢迎提交问题和拉取请求！
