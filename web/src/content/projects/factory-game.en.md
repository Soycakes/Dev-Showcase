This is a 2D multiplayer sandbox factory game built in Unity DOTS. I made it with three other developers. One of them wrote the procedural world generation, and I built the rest of the game's systems: the world and chunk structure, rendering, networking, inventory, machines, saving and hosting. The design target is Factorio-like scale, with thousands of machines running at the same time while players walk around and build. That target is why the whole thing is built on the Entity Component System, the C# Job System and the Burst compiler. Normal MonoBehaviour objects would have hit a wall long before we got there.

## The world

The world is split into 32x32 tile chunks, and every chunk carries five independent layers: Wall, Logistics, Main, Fluid and Overlay. Each layer has its own version counter. When a tile changes, only that layer's counter moves, so mesh rebuilds and save snapshots can tell exactly what is dirty without scanning the world every frame.

Almost everything in the game needs to answer "what is at this world position?", so I built ChunkLookup, a job-safe structure that goes from world coordinates to a chunk entity and then to a single tile in constant time. Block placement, block breaking, collision, mesh building and the minimap all use it, and none of them allocate to do so.

## Rendering

ChunkMeshSystem builds the visible tile geometry in Burst jobs, off the main thread. It writes vertex and index data straight into GPU-side mesh buffers through Unity's writable mesh descriptor API, so the managed Mesh API never gets involved. The mesh state is a cleanup component, which lets the system release GPU memory and job handles on its own schedule after a chunk entity is destroyed. Without that, a chunk despawning would stall the frame.

In an early prototype stress test of render distance, the renderer held 60 FPS with over 4 million blocks loaded across active chunks. It was a prototype test of rendering and chunk loading, not a full gameplay session.

## Networking

We use Netcode for Entities and split authority by what each action costs when it lags.

Inventory and block placement are server authoritative. The server validates every action and either confirms or rejects it. A round trip on every click would feel awful, though, so the client predicts inventory actions locally with a shadow buffer and reconciles when the server answers. Shift clicking, stack splitting and opening containers all respond instantly while the server stays the only source of truth.

The inventory logic itself is a pure static solver that knows nothing about ECS or networking. It takes an inventory buffer and a held item and returns the new state, and one decision tree handles left click, right click, shift click, stack splitting and drag placement. Machine-specific rules, like what a furnace accepts or what a chest allows, plug in as validators, so the solver never needs to know what kind of container it is working on. It uses only blittable data, which lets the exact same code run on the client for prediction and on the server as the authority.

Movement goes the other way. A frame of latency on a jump or a direction change ruins co-op feel in a way inventory latency never does, so movement runs on the client. The server corrects positions with lightweight RPCs and checks interaction range. The plan is to move to full Netcode for Entities prediction and rollback later.

The player physics is a custom 2D solver written in Burst. It does AABB collision against the tile grid and splits fast movement into substeps so a falling or dashing player cannot tunnel through a block. It doesn't use the Unity physics engine.

To keep network cost from growing with the world, a server system called ServerGhostRelevancySystem decides per connection which ghosts get replicated, using the same chunk distance that controls terrain loading. A dropped item far from a player costs that player nothing. Block placement and mining broadcasts follow the same rule and go only to connections that have the chunk loaded.

## Machines

Machines are deliberately not owned by chunks. A furnace or conveyor has to keep working whether or not a player is nearby, so machines live in their own global spatial hash index. Their footprints can cover several cells and straddle chunk boundaries, and placement, saving and loading all register machines through the same path.

The machine simulation runs on the server in a fixed 20 Hz system group. If a frame runs long, the group runs extra steps to catch up, so a hitch never makes a furnace smelt slower. Idle machines carry an enableable MachineActive component, so the Burst jobs skip them without touching their data, and an idle machine costs essentially nothing no matter how many exist. Adding a new machine type takes one component and one system.

What exists today is the infrastructure for all of this. The actual machine recipes are still early.

## Saving

I wrote the save format from scratch with BinaryWriter and BinaryReader. Each chunk layer is compressed with run length encoding, where a span is stored as a count, a block ID and a metadata byte. The file has terrain chunks, machines (a flat list that doesn't depend on which chunks are loaded) and per-player position and inventory. The header carries a version, and the loader rejects older versions outright instead of guessing at bytes.

The serializer captures all the ECS state synchronously on the main thread so the data is consistent, then hands the disk write to a background thread. Save results come back through a ConcurrentQueue and get broadcast in chat. Two details protect against lost work. A WorldSaveCache catches chunks that were modified and then despawned before a save, and merges them back in when the save flushes. And when a player disconnects, their state is snapshotted into the save cache before the entity is destroyed, so inventory and position survive an abrupt drop.

## Content and hosting

Items, blocks and machines are authored as ScriptableObjects in the editor. At bake time a compiler turns them into a blob asset registry, so runtime lookups never allocate.

Hosting goes through Unity Relay. One player starts a world and friends join with a join code, with no port forwarding. When the host leaves, a migration screen follows the relay reconnection and the new join code until play resumes. I integrated Unity's Relay and host migration packages rather than writing them, and the migration screen is adapted from Unity's sample.

## Making four people's code fit together

Development was fast while I was working alone. Once other people joined, it got harder. A change in one feature broke an unrelated one, and PR merges conflicted constantly. I patched around it with temporary fixes for a while, which only made errors harder to trace. In the end I rebuilt the structure around three assemblies: Mixed for code shared by client and server, Client for rendering and client-only logic, and Server for the authoritative simulation and saving. That split is also what let me keep the server-authoritative inventory apart from the client's prediction code, and the machine spatial index apart from chunk loading.

DOTS was new to the rest of the team, so I wrote spec documents covering how data structures live and die and how they sync. Once there was a written rule for who owns what, teammates could add their own features without touching the synchronization code.

Unity regenerates project files whenever the editor opens, so the three assemblies use explicit file lists. One build command runs CSharpier formatting and then builds the three assemblies with fail-fast, short error output. For debugging, the game has an F3 info overlay, a pooled F9 chunk border renderer, chat slash commands like /save, /give and /ping, and a minimap drawn by a parallel Burst job.
