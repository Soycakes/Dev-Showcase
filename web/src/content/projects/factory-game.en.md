Built a 2D multiplayer sandbox factory game from the ground up targeting tens of thousands of concurrent active machines, choosing Unity DOTS as the only viable path to that scale, with dedicated server support managing chunked world simulation and active chunk loading around each player.

Engineered server authoritative inventory with full client side prediction using shadow buffer reconciliation, achieving zero latency feel for all inventory actions while keeping the server as the only source of truth, with shift click routing and stack split logic fully decoupled from networking state.

Implemented a Burst compiled procedural chunk mesh system that builds vertex and index streams entirely off the main thread and streams geometry to GPU buffers using Unity's writable mesh descriptor API, sustaining 60 FPS across stress tests rendering over 4 million blocks simultaneously.

Wrote a custom binary world save system with Run Length Encoding compression across 5 independent tile layers, snapshotting ECS chunk state synchronously and flushing to disk on a background worker, with machines persisting independently of chunk lifetime to support always-on factory simulation.

Built a custom 2D AABB substepped physics and collision solver for player movement running client side to keep co-op feel smooth, using lightweight server correction RPCs for position reconciliation with a planned migration to full Netcode for Entities server prediction and rollback.
