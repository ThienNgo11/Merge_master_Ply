# Project Rules & Architecture Boundaries

This document defines the strict boundaries and rules for AI agents and developers working on this project.

## 1. Directory Architecture & Modification Rules

The project is split into three distinct layers. You **MUST** respect these boundaries at all times:

### 🔴 GameTool (STRICTLY READ-ONLY)
- **Path:** `Assets/GameTool/`
- **Rule:** **DO NOT MODIFY** any files in this directory under any circumstances.
- **Description:** Contains the core systems (UI, Object Pooling, Audio, IAP, Analytics API, Notifications). These are standard base modules shared across multiple projects.

### 🟡 GameToolSample (MODIFIABLE - CORE EXTENSIONS)
- **Path:** `Assets/GameToolSample/`
- **Rule:** **MODIFICATION ALLOWED**, but strictly for integrating core systems.
- **Description:** Acts as a bridge between `GameTool` and the specific game. Contains auto-generated scripts (e.g., `eUIName.cs`), Canvas configurations, and global game data structures.

### 🟢 _ProjectTemplate / Game Folder (ACTIVE DEVELOPMENT)
- **Path:** `Assets/_ProjectTemplate/` (or the specific game's folder)
- **Rule:** **PRIMARY WORKSPACE**. All new gameplay features must be implemented here.
- **Description:** Contains all specific game logic, controllers, levels, UI prefabs, and assets. Developers should only focus their coding efforts here to build the game.

## 2. Coding Conventions & Best Practices
- **UI Management:** Never hardcode UI popup names. Always declare them in `GameToolSample.UIManager.eUIName` and use the built-in UI/Canvas managers to show/hide them.
- **Object Spawning:** Do not use raw `Instantiate()` or `Destroy()` for frequent objects (like VFX or bullets). Always utilize the `PoolingManager` in `GameTool/ObjectPool`.
- **Data & Configs:** Keep level data and ScriptableObjects localized to the `_ProjectTemplate` folder to ensure the base GameTool remains agnostic of specific game logic.
- **IAP & Reward Handling:** When processing purchases in `IAPManager.cs`, grant non-coin items (Boosters, Unlimited Heart) directly via `GameData.Instance.CollectItem`. For Coin resources, always display a visual reward using `AddItemEffectPopup.CollectCoinAnim` (fallback to `GameData` if `CanvasManager` is invalid).
- **Encapsulation & Properties:** Avoid `public` fields in `MonoBehaviour`. Use `[SerializeField] private` for inspector variables. Do not use auto-properties if the value needs to be serialized. Use explicit getters/setters instead (e.g., `[SerializeField] private int _speed; public int Speed { get { return _speed; } set { _speed = value; } }`).
- **DRY Principle (Don't Repeat Yourself):** Avoid code duplication. Utilize inheritance, reusable components, or common utility functions instead of copy-pasting code.

## 3. Design Patterns & Core Principles
- **Singleton Pattern:** Managers (like `GameplayManager`, `AudioManager`, etc.) typically use Singletons. Access them via their `.Instance` property to avoid unneeded `FindObjectOfType` calls.
- **Observer Pattern (Events/Actions):** Use C# `Action` or `UnityEvent` for inter-system communication. Avoid tight coupling between unrelated systems (e.g., UI should listen to Gameplay events, rather than Gameplay directly updating UI components).
- **Dictionary (`Dictionary<K,V>`):** Always prefer `Dictionary` over looping through `List` or `Array` when you need frequent lookups by ID or Key.
- **Ads & Tracking:** Ensure Ads (AppLovin MAX / AdMob) and Tracking (Firebase/AppsFlyer) logic strictly use the base wrappers inside `GameTool/API` or `GameTool/IAP`. Do not write raw SDK calls inside gameplay code.

## 4. Game Flow & Entry Point
- **GameplayManager:** The game loop **MUST** always start and be managed at `GameplayManager` (located in `_ProjectTemplate`). This is the central brain of the game session, controlling initialization, states (Win, Lose, Playing), and triggering events for other systems to listen to.
- **Scene Testing vs Release:**
  - **Testing in Editor:** You can drag the `GameToolService` prefab into any scene (like `GamePlay`) to test logic directly without going through the Splash screen.
  - **Release / Build:** The build **MUST** start at the `SPL` (Splash) scene. All other scenes **MUST NOT** contain the `GameToolService` prefab to prevent duplicates and initialization bugs.

## 5. Asset Management & Utilities
- **Addressables:** Typically used primarily for storing levels to optimize build size. Only use this if the game requires it, specified by the `#USE_ADDRESSABLE` macro.
- **Animations:** For complex animation callbacks, use `Datdev.AnimatorController` (from `DatdevUlts`) instead of writing custom listeners.

## 6. Hyper-Casual Optimization Rules
Due to the performance constraints of mobile devices (battery, thermal throttling) and the high entity count of hyper-casual games, adhere to these strict optimization rules:
- **Zero Allocations in Update:** NEVER use `new`, `string` concatenation, or LINQ (`.Where`, `.ToList`) in `Update()`, `FixedUpdate()`, or `LateUpdate()`.
- **Reference Management (Prefer Drag & Drop):** Limit the use of `GetComponent<T>()`, `GameObject.Find()`, or `Camera.main` as much as possible. Prioritize using `[SerializeField]` and manual drag-and-drop in the Inspector to assign references. This ensures better safety and performance. Only use `GetComponent` when dynamically initializing objects from a pool where manual assignment is impossible.
- **Non-Alloc Physics:** Prefer `Physics.RaycastNonAlloc` or `Physics.OverlapSphereNonAlloc` over their allocating counterparts. Avoid MeshColliders; use primitive colliders (Box, Sphere, Capsule) or Compound Colliders.
- **Canvas Separation:** Do not mix dynamic UI (e.g., jumping score texts) with static UI (backgrounds, buttons) in the same Canvas. Separate them to avoid rebuilding the entire Canvas when only one element changes.
- **Materials & Batching:** Ensure `Enable GPU Instancing` is checked for materials used on multiple identical objects (like enemies or coins). Prefer Mobile (e.g. `Mobile/Unlit`) shaders over the Standard shader.
