# Factory Game - Unity DOTS Multiplayer Factory Game

Core Systems from a 2D multiplayer automation game built using Unity ECS, C# Job System, Burst compiler, and Netcode for Entities.
  
The game is aimed to handle tens of thousands of machines, which made Unity DOTS/ECS the ONLY viable choice as we wanted to explore more into Unity's "hyper optimized" side,  
and includes support for dedicated servers running to handle chunkloading of gameplay areas.  
    
Currently multiplayer tests have been successful, including tests of rendering over 4 million blocks, and various server authority & client prediction tests for inventory!

## Tech Stack
* **Engine & Systems:** Unity DOTS (ECS, Job System, Burst Compiler)
  * The "Hyper Optimization" mainly for the factory machine logics
* **Networking:** Server authoritative inventory with client prediction
  * Movement is simulated client side to prioritize smooth perfect gameplay in a co op setting (similar to Minecraft & Terraria). Because movement is client side, a light RPC is done for current stage of the game. Moving on, these will be refactored to use Netcode for Entities for server prediction & rollback as needed.
* **Serialization:** Custom binary format with Run Length Encoding (RLE)

## Showcased Scripts
### [SaveWorldSerializer.cs](./SaveWorldSerializer.cs)
* Handles world state saving and loading (terrain chunks, inventory, machines).
* Snapshots ECS state synchronously on main thread and writes to disk asynchronously on a background worker.
* Uses Run Length Encoding to compress 32x32 chunk layers. Additional optimization is done to allow players to load up to 4 million blocks at once with 60 FPS.
* At the moment, player IDs are on network ID, but is planned to transition to proper player ID system in near future.
* **The 5 layers are done separately like so on purpose to handle ECS & Burst Compiler better.**

### [ChunkMeshSystem.cs](./ChunkMeshSystem.cs)
* Dynamically generates procedural 2D tile meshes for active chunk rendering.
* Uses Burst compiled jobs to construct vertex/index streams off the main thread.
* Streams geometry to GPU buffers using Unity's writable mesh descriptor API.

### [ClientInventoryPredictionSystems.cs](./ClientInventoryPredictionSystems.cs)
* Provides zero latency client prediction for inventory actions.
* Maintains local predicted "shadow" buffers, reducing acknowledged server transactions and replaying pending inputs.

### [PlayerMovementSystem.cs](./PlayerMovementSystem.cs)
* Custom 2D substepped platformer physics and AABB collision solver.
* Substeps physics integration steps to prevent collision at high speeds.
* Resolves collisions independently on X and Y axes against tilemaps.

### [InventoryClickSolver.cs](./InventoryClickSolver.cs)
* Logic solver for QoL inventory actions (stack splitting, item swapping, drag placement) similar to that of Minecraft.
* Decoupled from ECS or networking state & uses validator interfaces to enforce game rules.

Note: These scripts reference from the full project,  
`ItemDatabase`, `ChunkLookup`, `RegistryBlob`, `WorldSaveInfo`, `InventoryShiftClickRouter`  
are not included in this showcase for now!  
