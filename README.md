# FoundryCommands
Adds various commands to the chat box in the game Foundry.

#### Installation

1. **Download**  
Download [FoundryCommands](https://github.com/erkle64/FoundryCommands/releases).

2. **Install**  
Extract everything inside the zip into your Foundry folder.

#### Usage

Open the chat box, defaults to Return/Enter, and type your commands in there.
All commands are case insensitve.

#### Commands

- **/monitor** or **/mon**
   Monitors a tank, modular tank or logistics container's contents once per second.  Use while not looking at a valid building to stop.

- **/monitor** _interval_ or **/mon** _interval_
   Monitors a tank, modular tank or logistics container's contents with a custom interval.  Use while not looking at a valid building to stop.

- **/time**
   Displays the current time of day.

- **/time** _HH_
   Set the time of day to _HH_:00

- **/time** _HH_:_MM_
  Set the time of day to _HH_:_MM_

- **/calculate** _expression_ or **/calc** _expression_ or **/c** _expression_
   Calculate the result of a mathematical expression. See [Expressive wiki](https://github.com/bijington/expressive/wiki/Functions) for available functions.

- **/drag** _range_
   Change the maximum range for drag building.  Use /drag 0 to restore default.

- **/tp** _waypoint-name_ or **/teleport** _waypoint-name_
   Teleport to the named waypoint.

- **/tpr** or **/ret** or **/return**
   Teleport back to position at last teleport.

- **/give** _item_
   Spawn single item into your inventory.

- **/give** _item_ _amount_
   Spawn multiple items into your inventory.

- **/count**
   Dump counts for all buildings within loading distance of the player.  Saves to `%AppData%\\..\\LocalLow\\Channel 3 Entertainment\\Foundry\\FoundryCommands\\count.txt`  

- **/dumpData**
   Dump data for use with Foundry Save Editor.  Saves to `%AppData%\\..\\LocalLow\\Channel 3 Entertainment\\Foundry\\FoundryCommands\\idmap.json`  

- **/dumpData minify**
   Same as above but leaving out extra whitespaces.  

- **/tweakItems** _tweak-name_ _identifier_=_value_...
   Generate a tweak file for use with [Tweakificator](https://github.com/erkle64/Tweakificator).  Saves to `%AppData%\\..\\LocalLow\\Channel 3 Entertainment\\Foundry\\FoundryCommands\\_tweak-name_.json`  
   Example: `/tweakItems Stack2000 stackSize=2000`

#### Compatibility

Probably only works in single player.
