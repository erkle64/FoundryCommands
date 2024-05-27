using C3.ModKit;
using HarmonyLib;
using System.Reflection;
using Unfoundry;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System;
using Expressive;
using Expressive.Exceptions;
using static BuildingModeHelpers;
using C3;

namespace FoundryCommands
{
    [UnfoundryMod(GUID)]
    public class Plugin : UnfoundryPlugin
    {
        public const string
            MODNAME = "FoundryCommands",
            AUTHOR = "erkle64",
            GUID = AUTHOR + "." + MODNAME,
            VERSION = "1.6.5";

        public static LogSource log;

        public static TypedConfigEntry<int> maxDragBuffer;

        private static Vector3 _lastPositionAtTeleport = Vector3.zero;
        private static bool _hasTeleported = false;

        public Plugin()
        {
            log = new LogSource(MODNAME);

            new Config(GUID)
                .Group("Drag")
                    .Entry(out maxDragBuffer, "maxDragBuffer", 2046,
                        "WARNING: Experimental feature!",
                        "May cause crashing if used incorrectly.",
                        "The maximum number of blocks that can be dragged at once.",
                        "Will be rounded up to the next multiple of 1023.")
                .EndGroup()
                .Load()
                .Save();
        }

        public override void Load(Mod mod)
        {
            log.Log($"Loading {MODNAME}");

            dataFolder = Application.persistentDataPath;
            dumpFolder = Path.Combine(dataFolder, MODNAME);
        }

        public static string dataFolder;
        public static string dumpFolder;

        private static float _dragPlanScaleModifier = 1.0f;

        private static FieldInfo timeInTicks = typeof(GameRoot).GetField("timeInTicks", BindingFlags.NonPublic | BindingFlags.Instance);

        private const ulong TICKS_PER_DAY = GameRoot.TIME_SYSTEM_TICKS_PER_DAY;
        private const ulong TICKS_PER_HOUR = GameRoot.TIME_SYSTEM_TICKS_PER_DAY / 24UL;
        private const ulong TICKS_PER_MINUTE = TICKS_PER_HOUR / 60UL;
        private static Vector2Int TicksToTime(ulong ticks)
        {
            var hours = ticks / TICKS_PER_HOUR % 24UL;
            var minutes = ticks / TICKS_PER_MINUTE % 60UL;
            return new Vector2Int((int)hours, (int)minutes);
        }

        private static ulong TimeToTicks(int hours, int minutes)
        {
            return (ulong)hours * TICKS_PER_HOUR + (ulong)minutes * TICKS_PER_MINUTE;
        }

        public static CommandHandler[] commandHandlers = new CommandHandler[]
        {
            new CommandHandler(@"^\/drag\s*?(?:\s+(\d+(?:\.\d*)?))?$", (string[] arguments) => {
                switch(arguments.Length)
                {
                    case 1:
                        var range = float.Parse(arguments[0]);
                        if(range < 38.0f) range = 38.0f;
                        _dragPlanScaleModifier = (range - 0.5f) / 37.5f;
                        ChatFrame.addMessage($"Drag scale set to {range}.", 0);
                        break;

                    default:
                        ChatFrame.addMessage("Usage: <b>/drag</b> <i>range</i>", 0);
                        return;
                }
            }),
            new CommandHandler(@"^\/(?:(?:tp)|(?:teleport))(?:\s+([\s\w\d]*?)\s*)?$", (string[] arguments) => {
                if (arguments.Length == 0 || arguments[0].Length == 0)
                {
                    ChatFrame.addMessage("Usage: <b>/tp</b> <i>waypoint-name</i>", 0);
                    return;
                }

                var wpName = arguments[0];

                var character = GameRoot.getClientCharacter();
                if (character == null)
                {
                    ChatFrame.addMessage("Client character not found.", 0);
                    return;
                }

                foreach (var wp in character.getWaypointDict().Values)
                {
                    if (wp.description.ToLower() == wpName.ToLower())
                    {
                        ulong cidx;
                        uint tidx;
                        ChunkManager.getChunkIdxAndTerrainArrayIdxFromWorldCoords(wp.waypointPosition.x,wp.waypointPosition.y,wp.waypointPosition.z,out cidx,out tidx);
                        var chunk = ChunkManager.getChunkByWorldCoords(wp.waypointPosition.x, wp.waypointPosition.z);
                        if(chunk != null)
                        {
                            _lastPositionAtTeleport = character.position;
                            _hasTeleported = true;
                            GameRoot.addLockstepEvent(new GameRoot.ChatMessageEvent(character.usernameHash, string.Format("Teleporting to '{0}' at {1}, {2}, {3}", wp.description, wp.waypointPosition.x.ToString(), wp.waypointPosition.y.ToString(), wp.waypointPosition.z.ToString()), 0, false));
                            GameRoot.addLockstepEvent(new Character.CharacterRelocateEvent(character.usernameHash, wp.waypointPosition.x, wp.waypointPosition.y + 0.5f, wp.waypointPosition.z));
                        }
                        else
                        {
                            GameRoot.addLockstepEvent(new GameRoot.ChatMessageEvent(character.usernameHash, "Ungenerated chunk.", 0, false));
                            ChunkManager.generateNewChunksBasedOnPosition(wp.waypointPosition, ChunkManager._getChunkLoadDistance());
                        }
                        return;
                    }
                }

                ChatFrame.addMessage("Waypoint not found.", 0);
            }),
            new CommandHandler(@"^\/(?:(?:tpr)|(?:ret)|(?:return))$", (string[] arguments) => {
                if (!_hasTeleported)
                {
                    ChatFrame.addMessage("No return point found.", 0);
                    return;
                }

                var character = GameRoot.getClientCharacter();
                if (character == null)
                {
                    ChatFrame.addMessage("Client character not found.", 0);
                    return;
                }

                var targetCube = new Vector3Int(
                    Mathf.FloorToInt(_lastPositionAtTeleport.x),
                    Mathf.FloorToInt(_lastPositionAtTeleport.y),
                    Mathf.FloorToInt(_lastPositionAtTeleport.z)
                    );

                ulong cidx;
                uint tidx;
                ChunkManager.getChunkIdxAndTerrainArrayIdxFromWorldCoords(targetCube.x,targetCube.y,targetCube.z,out cidx,out tidx);
                var chunk = ChunkManager.getChunkByWorldCoords(targetCube.x, targetCube.z);
                if(chunk != null)
                {
                    GameRoot.addLockstepEvent(new GameRoot.ChatMessageEvent(character.usernameHash, $"Returning to {targetCube.x}, {targetCube.y}, {targetCube.z}", 0, false));
                    GameRoot.addLockstepEvent(new Character.CharacterRelocateEvent(character.usernameHash, _lastPositionAtTeleport.x, _lastPositionAtTeleport.y, _lastPositionAtTeleport.z));
                    _lastPositionAtTeleport = character.position;
                }
                else
                {
                    ChatFrame.addMessage("Ungenerated chunk.", 0);
                    ChunkManager.generateNewChunksBasedOnPosition(_lastPositionAtTeleport, ChunkManager._getChunkLoadDistance());
                }
            }),
            new CommandHandler(@"^\/time$", (string[] arguments) => {
                var gameRoot = GameRoot.getSingleton();
                if (gameRoot == null) return;

                var time = TicksToTime((ulong)timeInTicks.GetValue(gameRoot));

                ChatFrame.addMessage($"Current time is {time.x}:{time.y:00}.", 0);
            }),
            new CommandHandler(@"^\/time\s+([012]?\d)(?:\:(\d\d))?$", (string[] arguments) => {
                var gameRoot = GameRoot.getSingleton();
                if (gameRoot == null) return;

                var hours = Convert.ToInt32(arguments[0]);
                var minutes = arguments.Length > 1 && !string.IsNullOrWhiteSpace(arguments[1]) ? Convert.ToInt32(arguments[1]) : 0;
                var targetTicks = TimeToTicks(hours, minutes);
                var currentTicks = (ulong)timeInTicks.GetValue(gameRoot);
                var deltaTicks = (targetTicks + TICKS_PER_DAY - (currentTicks % TICKS_PER_DAY)) % TICKS_PER_DAY;
                GameRoot.addLockstepEvent(new GameRoot.DebugAdvanceTimeEvent(deltaTicks));


                var message = $"Setting time to {hours}:{minutes:00}.";
                var character = GameRoot.getClientCharacter();
                if (character == null)
                {
                    ChatFrame.addMessage(message, 0);
                }
                else
                {
                    GameRoot.addLockstepEvent(new GameRoot.ChatMessageEvent(character.usernameHash, message, 0, false));
                }
            }),
            new CommandHandler(@"^\/(?:(?:c)|(?:calc)|(?:calculate))\s+(.+)$", (string[] arguments) => {
                try {
                    var expression = new Expression(arguments[0].Trim(), ExpressiveOptions.IgnoreCaseForParsing | ExpressiveOptions.NoCache);
                    var result = expression.Evaluate();
                    ChatFrame.addMessage($"{arguments[0]} = {result}", 0);
                }
                catch(ExpressiveException e)
                {
                    ChatFrame.addMessage($"Error: {e.Message}", 0);
                }
            }),
            new CommandHandler(@"^\/count$", (string[] arguments) => {
                if (!Directory.Exists(dumpFolder)) Directory.CreateDirectory(dumpFolder);

                var buildings = StreamingSystem.getBuildableObjectTable();
                var counts = new Dictionary<ItemTemplate, int>();
                foreach(var building in buildings)
                {
                    if (building.Value != null
                        && building.Value.template != null
                        && building.Value.template.type != BuildableObjectTemplate.BuildableObjectType.WorldDecorMineAble
                        && building.Value.template.parentItemTemplate != null)
                    {
                        var template = building.Value.template.parentItemTemplate;
                        counts.TryGetValue(template, out var currentCount);
                        counts[template] = currentCount + 1;
                    }
                }

                var f = new StreamWriter(Path.Combine(dumpFolder, "count.txt"), false);
                foreach (var kv in counts.OrderBy(x => x.Key.name))
                {
                    f.WriteLine($"{kv.Key.name}: {kv.Value}");
                }
                f.Close();

                ChatFrame.addMessage($"Counts saved to {dumpFolder}\\count.txt", 0);
                ChatFrame.addMessage($"Total: {buildings.Count}", 0);
            }),
            new CommandHandler(@"^\/give(?:\s+([\s\w\d]*?)(?:\s+(\d+))?)?$", (string[] arguments) => {
                void GiveItem(ItemTemplate item, uint amount)
                {
                    if (amount == 0) amount = item.stackSize;
                    else if(GameRoot.IsMultiplayerEnabled)
                    {
                        ChatFrame.addMessage("<b>WARNING:</b> Count parameter not supported in multiplayer. Giving 1 stack.", 0);
                    }

                    var character = GameRoot.getClientCharacter();
                    if(character == null)
                    {
                        ChatFrame.addMessage("<b>ERROR:</b> Client character not found!", 0);
                        return;
                    }
                    if (GameRoot.IsMultiplayerEnabled)
                    {
                        GameRoot.addLockstepEvent(new GameRoot.ChatMessageEvent(character.usernameHash, string.Format("Spawning {0} of {1}", item.stackSize, item.name), 0, false));
                        GameRoot.addLockstepEvent(new DebugItemSpawnEvent(character.usernameHash, item.id));
                    }
                    else
                    {
                        GameRoot.addLockstepEvent(new GameRoot.ChatMessageEvent(character.usernameHash, string.Format("Spawning {0} of {1}", amount, item.name), 0, false));
                        InventoryManager.inventoryManager_tryAddItemAtAnyPosition(character.inventoryId, item.id, amount, IOBool.iofalse);
                    }
                }

                uint count = 0;
                switch(arguments.Length)
                {
                    case 1:
                        var name = arguments[0].ToLower();
                        List<ItemTemplate> foundItems = new List<ItemTemplate>();
                        foreach(var item in ItemTemplateManager.getAllItemTemplates().Values)
                        {
                            if(item.identifier.ToLower() == name || item.name.ToLower() == name)
                            {
                                GiveItem(item, count);
                                break;
                            }
                            else if(item.identifier.ToLower().Contains(name) || item.name.ToLower().Contains(name))
                            {
                                foundItems.Add(item);
                            }
                        }
                        switch(foundItems.Count)
                        {
                            case 0: ChatFrame.addMessage("Found no matching item template", 0); break;
                            case 1: GiveItem(foundItems[0], count); break;
                            default:
                                ChatFrame.addMessage("Found multiple matches:", 0);
                                foreach(var item in foundItems)
                                {
                                    ChatFrame.addMessage($"name: {item.name}    ident: {item.identifier}", 0);
                                }
                                break;
                        }
                        break;

                    case 2:
                        count = uint.Parse(arguments[1]);
                        goto case 1;

                    default:
                        ChatFrame.addMessage("Usage: <b>/give</b> <i>name</i> <i>amount</i>", 0);
                        break;
                }
            }),
            new CommandHandler(@"^\/dumpData(?:\s(minify))?", (string[] arguments) => {
                bool minify = (arguments.Length >= 1 && arguments[0].ToLower() == "minify");

                if (!Directory.Exists(dumpFolder)) Directory.CreateDirectory(dumpFolder);
                var f = new StreamWriter(Path.Combine(dumpFolder, "idmap.json"), false);
                void dumpEntry(string indent, string entry, params object[] args)
                {
                    if(minify) f.Write(string.Format(entry, args));
                    else f.WriteLine(indent+entry, args);
                }
                dumpEntry("", "{{");
                void dumpDictionary<T>(string label, Dictionary<ulong, T> dict, bool last = false)
                {
                    if (dict == null)
                    {
                        Debug.Log($"Missing data for '{label}'");
                        return;
                    }

                    var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
                    dumpEntry("  ", "\"{0}\": [", label);
                    var keys = dict.Keys;
                    int index = 0;
                    foreach(var key in keys)
                    {
                        var value = dict[key];
                        dumpEntry("    ", "{{");
                        dumpEntry("      ", "\"id\": {0},", key);

                        foreach (var field in fields)
                        {
                            if (field.FieldType == typeof(string))
                            {
                                dumpEntry("      ", "\"{0}\": \"{1}\",", field.Name, (string)field.GetValue(value));
                            }
                            else if (field.FieldType == typeof(short))
                            {
                                dumpEntry("      ", "\"{0}\": \"{1}\",", field.Name, (short)field.GetValue(value));
                            }
                            else if (field.FieldType == typeof(ushort))
                            {
                                dumpEntry("      ", "\"{0}\": \"{1}\",", field.Name, (ushort)field.GetValue(value));
                            }
                            else if (field.FieldType == typeof(int))
                            {
                                dumpEntry("      ", "\"{0}\": \"{1}\",", field.Name, (int)field.GetValue(value));
                            }
                            else if (field.FieldType == typeof(uint))
                            {
                                dumpEntry("      ", "\"{0}\": \"{1}\",", field.Name, (uint)field.GetValue(value));
                            }
                            else if (field.FieldType == typeof(long))
                            {
                                dumpEntry("      ", "\"{0}\": \"{1}\",", field.Name, (long)field.GetValue(value));
                            }
                            else if (field.FieldType == typeof(ulong))
                            {
                                dumpEntry("      ", "\"{0}\": \"{1}\",", field.Name, (ulong)field.GetValue(value));
                            }
                            else if (field.FieldType == typeof(float))
                            {
                                dumpEntry("      ", "\"{0}\": \"{1}\",", field.Name, (float)field.GetValue(value));
                            }
                        }
                        dumpEntry("    ", (index == keys.Count - 1) ? "}}" : "}},");
                        ++index;
                    }
                    dumpEntry("  ", last ? "]" : "],");
                }
                dumpDictionary<ItemTemplate>("items", ItemTemplateManager.getAllItemTemplates());
                dumpDictionary<ElementTemplate>("elements", ItemTemplateManager.getAllElementTemplates());
                dumpDictionary<CraftingRecipe>("recipes", ItemTemplateManager.getAllCraftingRecipes());
                dumpDictionary<BuildableObjectTemplate>("buildings", ItemTemplateManager.getAllBuildableObjectTemplates());
                dumpDictionary<TerrainBlockType>("terrain", ItemTemplateManager.getAllTerrainTemplates());
                dumpDictionary<CraftingRecipeCategory>("recipe_categories", ItemTemplateManager.getCraftingRecipeCategoryDictionary());
                dumpEntry("", "}}");
                f.Close();
                ChatFrame.addMessage($"Data saved to {dataFolder}\\{MODNAME}\\idmap.json", 0);
            }),
            new CommandHandler(@"^\/tweakItems\s+([\w\-.]+)(?:\s+(\w+)=((?:\""[^\""]*\"")|(?:[0-9]*(?:\.[0-9]*)?)))+$", (string[] arguments) => {
                Debug.Log(string.Join(", ", arguments));
                var regexNumber = new Regex(@"[0-9]*(?:\.[0-9]*)?", RegexOptions.Singleline);
                var targetPath = $"{arguments[0]}.json";
                var tweakCount = (arguments.Length - 1)/2;
                var tweakProperties = new string[tweakCount];
                var tweakValues = new string[tweakCount];
                for(int i = 0; i < tweakCount; ++i)
                {
                    tweakProperties[i] = arguments[i*2+1];
                    tweakValues[i] = arguments[i*2+2];
                }

                if (!Directory.Exists(dumpFolder)) Directory.CreateDirectory(dumpFolder);
                var f = new StreamWriter(Path.Combine(dumpFolder, targetPath), false);
                void dumpEntry(string indent, string entry, params object[] args)
                {
                    f.WriteLine(indent+entry, args);
                }
                dumpEntry("", "{{");
                dumpEntry("  ", "\"changes\": {{");
                void dumpDictionary<T>(string label, Dictionary<ulong, T> dict, bool last = false)
                {
                    var identifier = typeof(T).GetField("identifier");
                    dumpEntry("    ", "\"{0}\": {{", label);
                    var keys = dict.Keys;
                    int index = 0;
                    foreach(var key in keys)
                    {
                        var value = dict[key];
                        dumpEntry("      ", "\"{0}\": {{", (string)identifier.GetValue(value));
                        for(int i = 0; i < tweakCount; ++i)
                        {
                            if(regexNumber.IsMatch(tweakValues[i]))
                            {
                                dumpEntry("        ", "\"{0}\": {1}{2}", tweakProperties[i], tweakValues[i], (i < tweakCount - 1) ? "," : "");
                            }
                            else
                            {
                                dumpEntry("        ", "\"{0}\": \"{1}\"{2}", tweakProperties[i], tweakValues[i].Replace("\"", "\\\""), (i < tweakCount - 1) ? "," : "");
                            }
                        }
                        dumpEntry("      ", (index == keys.Count - 1) ? "}}" : "}},");
                        ++index;
                    }
                    dumpEntry("    ", last ? "}}" : "}},");
                }
                dumpDictionary<ItemTemplate>("items", ItemTemplateManager.getAllItemTemplates(), last: true);
                dumpEntry("  ", "}}");
                dumpEntry("", "}}");
                f.Close();
                ChatFrame.addMessage($"Data saved to BepInEx\\plugins\\{MODNAME}\\{targetPath}", 0);
            })
        };

        [HarmonyPatch]
        public static class Patch
        {
            private static Vector3 scale_wall_x = Vector3.one;
            private static Vector3 scale_wall_z = Vector3.one;
            private static Vector3 scale_slope = Vector3.one;
            private static Material material_scaled = null;

            [HarmonyPatch(typeof(DragModeWorkingData), nameof(DragModeWorkingData.init))]
            [HarmonyPostfix]
            public static void DragModeWorkingData_init(DragModeWorkingData __instance)
            {
                var maxDragBuffer = Plugin.maxDragBuffer.Get();
                if (maxDragBuffer > 1023 * 2)
                {
                    var maxDragBufferCount = Mathf.CeilToInt(maxDragBuffer / 1023.0f);
                    var maxDragBufferSize = maxDragBufferCount * 1023;
                    __instance.dragPositions = new Vector3Int[maxDragBufferSize];
                    __instance.dragValidationArray = new bool[maxDragBufferSize];
                    __instance.dmrc_dragMatrices_green = new DrawMeshRenderingContainer(maxDragBufferCount, false);
                    __instance.dmrc_dragMatrices_red = new DrawMeshRenderingContainer(maxDragBufferCount, false);
                }
            }

            [HarmonyPatch(typeof(DragHelperGO), "Awake")]
            [HarmonyPostfix]
            public static void DragHelperGO_Awake(DragHelperGO __instance)
            {
                scale_wall_x = __instance.go_wall_x.transform.localScale;
                scale_wall_z = __instance.go_wall_z.transform.localScale;
                scale_slope = __instance.go_slope.transform.localScale;
                material_scaled = __instance.go_wall_x.GetComponent<MeshRenderer>().sharedMaterial;
            }

            [HarmonyPatch(typeof(DragHelperGO), nameof(DragHelperGO.setMode))]
            [HarmonyPrefix]
            public static void DragHelperGO_setMode(ref float dragPlanScaleModifier)
            {
                if (dragPlanScaleModifier < _dragPlanScaleModifier) dragPlanScaleModifier = _dragPlanScaleModifier;
            }

            [HarmonyPatch(typeof(DragHelperGO), nameof(DragHelperGO.setMode))]
            [HarmonyPostfix]
            public static void DragHelperGO_setMode(DragHelperGO __instance, BuildableObjectTemplate bot, BuildableObjectTemplate.DragBuildType dragBuildType, float dragPlanScaleModifier)
            {
                __instance.go_wall_x.transform.localScale = new Vector3(scale_wall_x.x * dragPlanScaleModifier, scale_wall_x.y * dragPlanScaleModifier, scale_wall_x.z);
                __instance.go_wall_z.transform.localScale = new Vector3(scale_wall_z.x, scale_wall_z.y * dragPlanScaleModifier, scale_wall_z.z * dragPlanScaleModifier);
                __instance.go_slope.transform.localScale = new Vector3(scale_slope.x * dragPlanScaleModifier, scale_slope.y, scale_slope.z * dragPlanScaleModifier);
                __instance.collider_wall_x.size = new Vector3(scale_wall_x.x * dragPlanScaleModifier + 0.5f, scale_wall_x.y * dragPlanScaleModifier + 0.5f, scale_wall_x.z);
                __instance.collider_wall_z.size = new Vector3(scale_wall_z.x, scale_wall_z.y * dragPlanScaleModifier + 0.5f, scale_wall_z.z * dragPlanScaleModifier + 0.5f);

                material_scaled.SetTextureScale("_TextureY", new Vector2(scale_wall_x.x * dragPlanScaleModifier, scale_wall_x.y * dragPlanScaleModifier));
            }

            [HarmonyPatch(typeof(ChatFrame), nameof(ChatFrame.onReturnCB))]
            [HarmonyPrefix]
            public static bool ChatFrame_onReturnCB()
            {
                Character clientCharacter = GameRoot.getClientCharacter();
                if (clientCharacter == null) return true;

                try
                {
                    var message = ChatFrame.getMessage();

                    foreach (var handler in commandHandlers)
                    {
                        if (handler.TryProcessCommand(message))
                        {
                            ChatFrame.hideMessageBox();
                            return false;
                        }
                    }
                }
                catch(System.Exception e)
                {
                    ChatFrame.addMessage(e.ToString(), 0);
                }

                return true;
            }
        }
    }
}


