# Just The Boss Fights

Unity 2.5D (with mix of 3D) Boss encounter game using inhouse coroutine state machines and dynamic skill systems, with 7 unique boss fights with nearly 100 patterns/mechanics.  

This game was developed at University of Utah's Capstone Games with a team of 30+ members.

The script showcase includes coroutine state machines, modular projectile/attack system, and a player ability framework.

Download & Play the game here!  https://store.steampowered.com/app/3572440/Just_the_Boss_Fights/


## Tech Stack
* **Engine:** Unity 3D (2.5D visuals, with mix of 2D and 3D boss mechanics)
* **Boss Architecture:** State Machine design pattern combined with Unity Coroutines to turn boss attack phases into classes
* **Attack Systems:** Strategy pattern combined with ScriptableObjects for data based projectile and melee attack behaviors


## Showcased Scripts

### [EnemyController.cs](./BossStateMachine/EnemyController.cs)
* Abstract brain for boss state machines.
* Manages state transitions, health event binding, phase checks, and sprite/animator lookups.
* Caches active state MonoBehaviours in a dictionary to prevent overhead during pattern switches.

### [EnemyState.cs](./BossStateMachine/EnemyState.cs)
* Abstract base class for all boss phase states.
* Contains state coroutine loops, enter/exit lifecycle hooks, and transition condition guards, CanEnter & CanExit.

### [FW_S_TeleportSlam.cs](./BossStateMachine/FW_S_TeleportSlam.cs)
* Boss attack pattern state (Frog Wizard teleport slam phase).
* Boss blinks to random air coordinates, falling into ground shockwaves, and scaling damage with boss phases and hardmode.

### [FW_S_FireballHop.cs](./BossStateMachine/FW_S_FireballHop.cs)
* Boss attack pattern state (Frog Wizard fireball jump and leap).
* Fast paced pattern with boss directly jumping towards player and firing projectile attacks midair to the player's position.

### [MF_S_BladeFan.cs](./BossStateMachine/MF_S_BladeFan.cs)
* Boss attack pattern state (Mysterious Figure spectral blade fan).
* Spawns projectiles in a fan pattern, pausing during a charge up phase, and then following a firing sequence.

### [Attack.cs](./AttackSystem/Attack.cs)
* Generic runtime physics container for attacks and projectiles.
* Binds parameter data `AttackData` with modular movement and collision strategy objects using `AttackBehavior`.

### [AttackData.cs](./AttackSystem/AttackData.cs)
* ScriptableObject parameter data for damage numbers, speed, lifespan, sprite visuals, and audio event references for attack prefabs.

### [AttackBehavior.cs](./AttackSystem/AttackBehavior.cs)
* Abstract attack behavior pattern interface with reusable movement named `Step` and collision logic `CollideWithTarget`.
* Modular attack behaviors `AB_ShieldBash`, `AB_BounceInBound`, `AB_FreezeEnemy` plug directly into generic attack prefabs to give effect.
* Examples may be AB_HomingAttack (not ported over), where a boss pattern may home in towards player with an adjustable angle value, then a developer can simply set an attack to have this attack behavior for quick implementation & testing!

### [PlayerAbilityBase.cs](./PlayerAbilityFramework/PlayerAbilityBase.cs)
* Abstract framework for player active skills.
* Manages cooldown timing, mana checks, hotkey bindings, and interrupt lifecycle hooks.

### [PlayerAbilityDash.cs](./PlayerAbilityFramework/PlayerAbilityDash.cs)
* Player ability script for dash.
* Handles iframes, 0 gravity during movement, velocity locking, and state cleanup on ability cancel.

### [SceneTriggerBox.cs](./GameManager/SceneTriggerBox.cs)
* Event driven trigger area that broadcasts player contact events and auto destroys.
* Allows cutscenes, camera shifts, and boss encounters to listen without direct coupling.

Note: These scripts reference from the full project,  
`PlayerController`, `Stats`, `FrogWizard`  
are not included in this showcase for now!  