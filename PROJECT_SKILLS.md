# Project Skills & Capabilities (Base Project)

This document outlines the built-in systems (skills) available in the base project. When developing gameplay in `_ProjectTemplate`, agents and developers should utilize these existing systems instead of reinventing the wheel.

## 1. UI Management System
- **Location:** `GameTool/UI` & `GameToolSample/UIManager`
- **Capability:** Centralized control of Canvases, Menus, Popups, and always-on-top overlays.
- **Usage:**
  - Define new UI identifiers in `eUIName.cs` (e.g., `VictoryPopup`, `SettingPopup`).
  - Assign layers using `eUIType` (Menu, Popup, AlwaysOnTop, BgBanner).
  - Interfaces like `CanvasManager` handle the lifecycle and display logic of these UI elements.

## 2. Object Pooling System
- **Location:** `GameTool/ObjectPool`
- **Capability:** Optimized instantiation of GameObjects to prevent memory fragmentation and GC spikes.
- **Usage:**
  - `PoolingManager.cs` acts as the global spawner.
  - Objects are defined via `PoolingTable` and `PoolAsset`.
  - Use `BasePooling.cs` on prefabs to handle auto-despawning and resetting state.

## 3. Audio & Haptics System
- **Location:** `GameTool/Audio` & `GameTool/Vibration`
- **Capability:** Manage Background Music (BGM), Sound Effects (SFX), and device haptics.
- **Usage:**
  - Use the `AudioManager` to play/stop clips globally.
  - Use the built-in Vibration module (NiceVibrations wrapper) for tactile feedback.

## 4. Monetization & Analytics
- **Location:** `GameTool/IAP` & `GameTool/API`
- **Capability:** Built-in hooks for In-App Purchases and Ads (AppLovin MAX, Firebase).
- **Usage:**
  - The base project already contains the flow for rewarding ads, removing ads, and tracking user events. Call the respective API managers when completing a level or purchasing items.
  - **IAP Integration:** Use `IAPManager.cs` (in `GameToolSample`) to handle `ProcessPendingProduct`. Use `GameData.Instance.CollectItem` to grant items directly, and `AddItemEffectPopup.CollectCoinAnim` to visually animate Coin rewards into the UI.

## 5. Utility Systems
- **Datdev Utilities (`GameTool/Assistants/DatdevUlts`):** A highly important toolkit for gameplay development. Key features include:
  - `CanvasPositioning`: Crucial for mapping 3D world positions to UI Canvas positions.
  - `MoveUlts`: Helper utilities for object movement and tweening logic.
  - `AnimatorController`: For handling complex animation callbacks cleanly.
  - `LocalizationController`, `TMPMaterialSelector`, and pooling extensions. **Always explore this folder before writing custom utility scripts!**
- **Addressables:** Used for loading levels dynamically to save space. Activated via the `#USE_ADDRESSABLE` macro.
- **Save System (`GameTool/GameDataScripts`):** Use `SaveGameManager` and `SaveGameData` instead of raw `PlayerPrefs` to ensure structured data persistence.
- **Scene Loading (`GameTool/LoadScene`):** Handles smooth transitions and loading screens between game scenes.
- **Mobile Notifications (`GameTool/MobileNotification`):** Manages local push notifications for user retention.
