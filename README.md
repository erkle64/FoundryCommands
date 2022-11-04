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

- **/drag** _range_  
   Change the maximum range for drag building.  Use /drag 0 to restore default.

- **/tp** _waypoint-name_  
   Teleport to the named waypoint.

- **/give** _item_  
   Spawn single item into your inventory.

- **/give** _item_ _amount_  
   Spawn multiple items into your inventory.

- **/jet**  
   Toggle infinite jetpack mode.  It lets you use the jetpack without the research and gives infinite fuel.  It doesn't unlock the research.

- **/dumpData**  
   Dump data for use with Foundry Save Editor.  Saves to BepInEx\\plugins\\FoundryCommands\\idmap.json  

- **/dumpData minify**  
   Same as above but leaving out extra whitespaces.  

- **/tweakItems** _tweak-name_ _identifier_=_value_...  
   Generate a tweak file for use with [Tweakificator](https://github.com/erkle64/Tweakificator).  Saves to BepInEx\\plugins\\FoundryCommands\\_tweak-name_.json  
   Example: `/tweakItems Stack2000 stackSize=2000`

#### Compatibility

Probably only works in single player.
