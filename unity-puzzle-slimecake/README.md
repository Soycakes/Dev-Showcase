# Unity Puzzle Slimecake

Core gameplay systems and level editor tools from Slimecake, a 2D puzzle platformer built in Unity. 

The game features grid based puzzle mechanics, an in-game level editor, and a precise undo system that snapshots the state of the world during play.

You can play the game on [**itch.io**](https://soycakes.itch.io/slime-cake)  
or watch the [**trailer on YouTube**](https://youtu.be/vj3thLsvkqk?si=u7NFimiWjYYPobY8).

## Features

*   **Undo System:** Snapshots player & world during live gameplay to allow undo in realtime gameplay! Custom set of rules make this possible in a non turnbased puzzle game like this.
*   **Abilities:** A stack based food inventory where different foods grant unique effects and interact with player's inventory.
*   **Level Editor:** A tool that lets both developers and players build and test levels in real time, then share them via copy pasteable text codes. Level registry can be accessed from developer side editor to modify and add new levels!

## Showcased Scripts

### [SmartUndoController.cs](./GameplayUndo/SmartUndoController.cs)
Logic for realtime undo. Saves snapshots of player & world state before certain player actions (like eating or picking up items) and groups same cell movements together to keep the undo history clean.  
(There's actually quite a lot more that goes into this but that's the gist)

### [GameplaySnapshot.cs](./GameplayUndo/GameplaySnapshot.cs)
The data structure for undo snapshots, tracking player positions, inventory, and world items.

### [MoveTool.cs](./EditorTools/MoveTool.cs)
Editor tool for dragging selected tiles, one's typically seen in paint apps!

### [MarchingAntsRenderer.cs](./EditorTools/MarchingAntsRenderer.cs)
Renders the animated selection border, one's typically seen in paint apps!

### [LevelCodec.cs](./EditorTools/LevelCodec.cs)
Saves and loads level strings. Compresses JSON layout data using GZip and Base64 so levels can be shared as short text strings.

### [EditorHistory.cs](./EditorUndo/EditorHistory.cs)
Tracks level editor's undo history.

### [CompositeAction.cs](./EditorUndo/CompositeAction.cs)
Multiple level editor actions in one (for editor undo system)

### [AbilityController.cs](./Foods/AbilityController.cs)
Manages player food abilities.  
Input buffers during a dash (when food can't be used) to make player input keys get "skipped" less.

### [FoodData.cs](./Foods/FoodData.cs)
Base class for all foods, with events for picking up, using, or storing items.

### [FoodPickupController.cs](./Foods/FoodPickupController.cs)
Controller for when the player picks up food in the world.  
Handles the pickup rules and connects with other events.

### [Inventory.cs](./Foods/Inventory.cs)
A stack based inventory for food.  
Uses events from inventory operations to handle UI animations (for food gained/used/destroyed/etc...)

### [MultiUseJumpFood.cs](./Foods/MultiUseJumpFood.cs)
Generic scripts for foods with multiple uses.  
Clones itself to keep track of use counts.

### [OnionFood.cs](./Foods/OnionFood.cs) / [PineappleFood.cs](./Foods/PineappleFood.cs)
2 Food types that have more special cases with inventory interactions.  
Onion food consumes itself and the next food item touched in world.  
Pineapple pops itself and the next item in inventory.

#

Note: These scripts reference from the full project,  
physics controllers, level builders, and UI managers  
are not included in this showcase for now!  

