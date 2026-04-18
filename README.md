# Four Corners: ECS Architecture & Documentation

## Overview
This project is a high-performance, scalable horde-simulation built leveraging Unity DOTS (Data-Oriented Technology Stack) physics, Burst compilation, and the C# Job System.

---

## How to Play
1.  **Open the Bootstrapper Scene**
2.  **Press Play** in the Unity Editor.
3.  **Load the Game:** After the initial load, press the "Play Game" button on the Main Menu.
4.  **Observe:** Check the elements wandering the scene.
5.  **Controls:** Increase or decrease the spawn rate via the UI buttons.
6.  **Monitor:** Check the memory reporter in the center of the screen.

---

## Technical Documentation

### 1. Object Pooling & Lifecycle Management
High-frequency instantiations and destructions crash garbage collection. The object pooling system guarantees zero-allocation spawning during gameplay.

*   **Implementation (`PoolPrewarmSystem.cs`, `ReturnToPoolSystem.cs`):** 
    *   `PoolPrewarmSystem` initializes requested batches (e.g., thousands of Minions) during `InitializationSystemGroup`.
    *   `ReturnToPoolSystem` uses an ECB to add `Disabled` tags and stores entity references in a `PooledEntity` buffer.
*   **Performance Optimization:** Caches `BufferLookup<PooledEntity>` in `OnCreate` to prevent expensive lookups during spawn waves.

### 2. Dynamic Spawner System
Manages timing and distribution of unit waves using `IJobEntity`.

*   **Dual-Job Paradigm:** 
    1.  `SpawnerSystem` handles timing and appends `ElementSpawnRequest` to a queue.
    2.  `PoolSpawningSystem` acts as the dispatcher, popping dormant entities from the pool, re-initializing them, and removing the `Disabled` tag.
*   **Catch-up Logic:** Uses a `while (timer >= interval)` loop to ensure unit density is maintained even after frame spikes.

### 3. Collision & Physics System
Uses `ICollisionEventsJob` to process unmanaged physics streams without stalling the main thread.

*   **Resolution:** Hostile collisions tag entities with `ReturnToPool` and trigger particle effects via the `ParticlePool`.
*   **History & Fixes:** 
    *   **Ghost Minions:** Fixed by switching to unique sort keys (`BodyIndexA/B`) in the ECB to prevent data races.
    *   **Scaling:** Replaced `NativeList` with `NativeHashSet` for constant-time (O(1)) collision tracking.

### 4. Organic pathfinding & AI
Locomotion systems for agent movement.

*   **Wandering:** Uses Perlin noise (`noise.cnoise`) for organic spreading and swarming behavior.
*   **Optimization:** Uses `math.distancesq()` for arrival checks to bypass expensive square-root calculations in parallel jobs.

### 5. Viewport Camera & Input
An event-driven system that bridges the ECS world boundaries to a standard camera, utilizing a robust Dependency Injection (DI) Manager architecture.

*   **Architecture & DI Integration:** The camera logic relies on a pure C# `ICameraManager` injected via the `ApplicationManager`'s DI container. This manager fully decouples the `CameraInputHandler` and `CameraBoundsCalculator` from `MonoBehaviour`, enforcing the Single Responsibility Principle and keeping the `CameraController` focused solely on rig transformation.
*   **Platform-Specific Inputs:** Uses preprocessor directives (`#if`) within the Input Handler to cleanly segregate desktop Edge Panning from mobile Drag Panning without runtime overhead.
*   **ECS Integration:** Dynamically fetches map boundaries from the `WanderArea` component via a bridging service (`SystemBridgeService.cs`).
*   **Isometric Symmetry:** Ensures panning feels consistent across all screen edges despite the isometric angle.

### 6. Addressables Integration
Managing memory through chunking while bridging into unmanaged ECS environments.

*   **Streaming:** Uses `EntityPrefabReference` and `RequestEntityPrefabLoaded` to stream memory directly into the physics engine without GameObject overhead.
*   **Critical Rules:** Requires SubScene setup, explicit grouping of `.unity` files in Addressables, and careful handling of `PrefabLoadResult` to prevent runtime crashes.

### 7. Exclusive Team Selection & Handshake
High-authority team assignment system ensuring exclusivity and player-specific corner binding.

*   **Handshake Protocol:** 
    *   Client sends a `GoInGameRequest` RPC containing a `RequestedTeamIndex`.
    *   `ServerAcceptGameSystem` validates the request using a centralized `MatchState` containing a `DynamicBuffer<TeamStatusElement>`.
*   **MatchState Initialization:** 
    *   Automated via `MatchStateBootstrapSystem`, which creates the canonical authority entity in the Server world at runtime, removing the need for manual scene setup.
*   **Team Centralization:** 
    *   `TeamNumber` and `IsActive` are stored exclusively on `PlayerBase`. Spawners resolve their team identity by traversing their baked `PlayerBaseEntity` parent link.
*   **Direct Entity Binding:** 
    *   Approved connections carry `PendingBaseAllocation` with the `ApprovedTeam`.
    *   `BaseAllocationSystem` performs a direct lookup to bind the player's `NetworkId` to the correct `PlayerBase` and `Spawner` quadrant entities.
*   **Technical Stability:** 
    *   Uses `EndSimulationEntityCommandBufferSystem` for all structural changes (accept, allocation, spawn requests) to prevent mid-frame structural changes from invalidating parallel job handles.

---

## Development Changelog

### 12/09 - 13/09
- Project creation and task organization via GitHub Projects.
- Established folder structure and asmdefs.
- Initial ECS implementation: Spawner, Wandering system, and basic unit tests.

### 14/09 - 15/09
- Implemented Collision systems and Memory Analyser.
- Refactored collisions from Physics Events to better handle NativeStreams.
- Divided Spawner into separate systems and optimized job buffers.

### 16/09
- Finalized full game flow: Spawn -> Wander -> Collide -> Recycle.
- Added GC memory allocator reporter.
- Enhanced wandering logic and prefab creation exponential growth fixes.

### 17/09 - 18/09
- Created core architecture: Dependency Injection (GameServices), SceneManager, and Loader.
- Implemented and refined the Object Pooling system.
- Refactored Unit Tests to reflect pool and collision changes.

### 19/08
- Finalized PoolSystem and Particle systems.
- Improved camera controls and fixed spawning rate logic.
- Added final memory reporting and settings.

### 15/02
- Jobified `SpawnerSystem`, `ReturnToPoolSystem`, and `PoolSpawningSystem`.
- Restructured paths: paths are now defined via a Path System utilizing spawner origin rather than character authoring directly.

### 16/02
- Extended jobification to `PathfollowSystem` and `ParticleSystem` preventing main-thread bottlenecks.
- Revamped spawner structures to hold arrays of prefabs with individual internal seeds.
- Imported and implemented visual characters models.

### 17/02
- Reflowed pooling so all spawners share the same pool; reused source pool for destroyed entities.
- Jobified `WanderSystem` and improved spawner structure handling.
- Bumped Unity version to 6.3.8f1.

### 19/02
- Added base framework for camera control.

### 20/02
- Adapted camera initialization to project architecture. Created `SystemBridgeService` to bridge pure ECS data.
- Optimized physics/ECS performance: changed NativeList to HashSet, bypassed root evaluations.
- Improved spawning to operate as minion waves, creating logic for organic noise movement.
- Fixed entities lingering on scene using `LinkedEntityGroupAuthoring`.
- Jobified `PoolPrewarmSystem`.

### 25/02
- Uploaded new environment assets, objects, and unit models to the scene.

### 27/02
- Constructed `AddressablesService` and integrated environmental prefabs.

### 28/02
- Fixed Addressables error loading entityPrefabReference and corrected asmdefs.
- Refactored config and built Addressables, creating preload jobs for `LoadingScene`.

### 02/03
- Upgraded camera controller to an event-driven setup.
- Fully transitioned camera handling to a Manager-based DI pattern applying SOLID principles.
- Safely separated Mobile Drag Panning from Desktop Edge Panning using preprocessor directives without runtime overhead.

### 05/03 - 06/03
- Integrated Unity Relay and direct connectivity hub via `MultiplayerService`.
- Created `ConnectionScreenUI` and `MultiplayerTestUI` for managing host/join network flows.
- Expanded input handling to support simultaneous desktop edge-panning and mobile drag-panning.

### 15/03 - 25/03
- Refactored Spawner system for rigorous server-client wave synchronization.
- Updated environment prefabs and unified scene loading triggers for better network consistency.
- Resolved ASMDEF and dependency errors for automated Netcode entity replication.

### 03/04
- Executed project-wide namespace refactoring (ElementLogicFail -> FourCorners) and Minion migration.

### 05/04
- Implemented Exclusive Team Selection Handshake and Team Availability validation.
- Created `MatchState` singleton pattern using `DynamicBuffer<TeamStatusElement>` with automated `MatchStateBootstrapSystem` initialization.
- Centralized `TeamNumber` authority on `PlayerBase` entities, leveraging baked `Parent` links in Spawners.
- Refactored `ServerAcceptGameSystem`, `BaseAllocationSystem`, and RPC systems to use `EndSimulationEntityCommandBufferSystem`, resolving `ObjectDisposedException` caused by synchronous structural changes.
- Deprecated `SpawnerRegistry` and `MatchStateAuthoring` in favor of hierarchical lookups and automated bootstrapping.
