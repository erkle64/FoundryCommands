using C3.ModKit;
using HarmonyLib;
using System.Reflection;
using Unfoundry;
using UnityEngine;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using C3;
using System.Linq;

namespace FoundryCommands
{
    [UnfoundryMod(Plugin.GUID)]
    public class Plugin : UnfoundryPlugin
    {
        public const string
            MODNAME = "FoundryCommands",
            AUTHOR = "erkle64",
            GUID = AUTHOR + "." + MODNAME,
            VERSION = "1.6.0";

        public static LogSource log;

        public Plugin()
        {
            log = new LogSource(MODNAME);
        }

        public override void Load(Mod mod)
        {
            log.Log($"Loading {MODNAME}");

            dataFolder = Application.persistentDataPath;
            dumpFolder = Path.Combine(dataFolder, MODNAME);
        }

        public static string dataFolder;
        public static string dumpFolder;

        private static Timer teleportTimer = null;

        static void timer_Teleport(object state)
        {
            var wp = (Waypoint)state;

            var character = GameRoot.getClientCharacter();
            if (character == null)
            {
                ChatFrame.addMessage("Client character not found.", 0);
                return;
            }

            GameRoot.addLockstepEvent(new Character.CharacterRelocateEvent(character.usernameHash, wp.waypointPosition.x, wp.waypointPosition.y, wp.waypointPosition.z));
            teleportTimer.Dispose();
            teleportTimer = null;
        }

        public static CommandHandler[] commandHandlers = new CommandHandler[]
        {
            //new CommandHandler(@"^\/drag\s*?(?:\s+(\d+(?:\.\d*)?))?$", (string[] arguments) => {
            //    switch(arguments.Length)
            //    {
            //        case 1:
            //            var range = float.Parse(arguments[0]);
            //            if(range < 38) range = 38;
            //            range = ((int)range) - 0.5f;
            //            var range2 = range*2.0f;
            //            var gameRoot = GameRoot.getSingleton();
            //            var dragHelperGO = Traverse.Create(gameRoot).Field("dragHelperGO").GetValue() as DragHelperGO;
            //            var dragHelperGO_bulkDemolish = Traverse.Create(gameRoot).Field("dragHelperGO_bulkDemolish").GetValue() as DragHelperGO;
            //            dragHelperGO.collider_area_xz.size = new Vector3(range, 0.05f, range);
            //            dragHelperGO.collider_area_xz_elevated.size = new Vector3(range, 0.05f, range);
            //            dragHelperGO.collider_slope.transform.localScale = new Vector3(range2, 0.1f, range2);
            //            dragHelperGO.collider_wall_x.size = new Vector3(range, range, 0.05f);
            //            dragHelperGO.collider_wall_z.size = new Vector3(0.05f, range, range);
            //            dragHelperGO_bulkDemolish.collider_area_xz.size = new Vector3(range, 0.05f, range);
            //            dragHelperGO_bulkDemolish.collider_area_xz_elevated.size = new Vector3(range, 0.05f, range);
            //            dragHelperGO_bulkDemolish.collider_slope.transform.localScale = new Vector3(range2, 0.1f, range2);
            //            dragHelperGO_bulkDemolish.collider_wall_x.size = new Vector3(range, range, 0.05f);
            //            dragHelperGO_bulkDemolish.collider_wall_z.size = new Vector3(0.05f, range, range);

            //            dragHelperGO.go_area_xz.transform.localScale = new Vector3(range2, 0.1f, range2);
            //            dragHelperGO.go_area_xz.GetComponent<MeshRenderer>().material.SetTextureScale("_TextureY", new Vector2(range2, range2));
            //            dragHelperGO.go_area_xz_elevated.transform.localScale = new Vector3(range2, 0.1f, range2);
            //            dragHelperGO.go_area_xz_elevated.GetComponent<MeshRenderer>().material.SetTextureScale("_TextureY", new Vector2(range2, range2));
            //            dragHelperGO.go_slope.transform.localScale = new Vector3(range2, 0.1f, range2);
            //            dragHelperGO.go_slope.GetComponent<MeshRenderer>().material.SetTextureScale("_TextureY", new Vector2(range2, range2));
            //            dragHelperGO.go_wall_x.transform.localScale = new Vector3(range2, range2, 0.1f);
            //            dragHelperGO.go_wall_x.GetComponent<MeshRenderer>().material.SetTextureScale("_TextureY", new Vector2(range2, range2));
            //            dragHelperGO.go_wall_z.transform.localScale = new Vector3(0.1f, range2, range2);
            //            dragHelperGO.go_wall_z.GetComponent<MeshRenderer>().material.SetTextureScale("_TextureY", new Vector2(range2, range2));
            //            dragHelperGO_bulkDemolish.go_area_xz.transform.localScale = new Vector3(range2, 0.1f, range2);
            //            dragHelperGO_bulkDemolish.go_area_xz.GetComponent<MeshRenderer>().material.SetTextureScale("_TextureY", new Vector2(range2, range2));
            //            dragHelperGO_bulkDemolish.go_area_xz_elevated.transform.localScale = new Vector3(range2, 0.1f, range2);
            //            dragHelperGO_bulkDemolish.go_area_xz_elevated.GetComponent<MeshRenderer>().material.SetTextureScale("_TextureY", new Vector2(range2, range2));
            //            dragHelperGO_bulkDemolish.go_slope.transform.localScale = new Vector3(range2, 0.1f, range2);
            //            dragHelperGO_bulkDemolish.go_slope.GetComponent<MeshRenderer>().material.SetTextureScale("_TextureY", new Vector2(range2, range2));
            //            dragHelperGO_bulkDemolish.go_wall_x.transform.localScale = new Vector3(range2, range2, 0.1f);
            //            dragHelperGO_bulkDemolish.go_wall_x.GetComponent<MeshRenderer>().material.SetTextureScale("_TextureY", new Vector2(range2, range2));
            //            dragHelperGO_bulkDemolish.go_wall_z.transform.localScale = new Vector3(0.1f, range2, range2);
            //            dragHelperGO_bulkDemolish.go_wall_z.GetComponent<MeshRenderer>().material.SetTextureScale("_TextureY", new Vector2(range2, range2));
            //            break;

            //        default:
            //            ChatFrame.addMessage("Usage: <b>/drag</b> <i>range</i>");
            //            return;
            //    }
            //}),
            new CommandHandler(@"^\/tp(?:\s+([\s\w\d]*?)\s*)?$", (string[] arguments) => {
                if (arguments.Length == 0)
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
                        ChunkManager.getChunkIdxAndTerrainArrayIdxFromWorldCoords((int)wp.waypointPosition.x, (int)wp.waypointPosition.y, (int)wp.waypointPosition.z, out cidx, out tidx);
                        var chunk = ChunkManager.getChunkByWorldCoords((int)wp.waypointPosition.x, (int)wp.waypointPosition.z);
                        if(chunk != null)
                        {
                            GameRoot.addLockstepEvent(new GameRoot.ChatMessageEvent(character.usernameHash, string.Format("Teleporting to '{0}' at {1}, {2}, {3}", wp.description, wp.waypointPosition.x.ToString(), wp.waypointPosition.y.ToString(), wp.waypointPosition.z.ToString()), 0, false));
                            GameRoot.addLockstepEvent(new Character.CharacterRelocateEvent(character.usernameHash, wp.waypointPosition.x, wp.waypointPosition.y, wp.waypointPosition.z));
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
            new CommandHandler(@"^\/spawnOre(?:\s+([\s\w\d]*?)\s*)?$", (string[] arguments) => {
                if (arguments.Length == 0)
                {
                    ChatFrame.addMessage("Usage: <b>/spawnOre</b> <i>oreVeinType</i>", 0);
                    return;
                }

                var character = GameRoot.getClientCharacter();
                if (character == null)
                {
                    ChatFrame.addMessage("Client character not found.", 0);
                    return;
                }

                void SpawnOreVein(TerrainBlockType terrainBlockType)
                {
                    ChunkManager.convertWorldFloatCoordsToIntCoords(character.position, out var center);
                    var oreVeinSpawningSystem = GameRoot.World.Systems.Get<OreVeinSpawningSystem>();
                    if (GameRoot.World.Systems.Get<RaycastHelperSystem>().raycastFromCameraToTerrain(out Vector3 _, out var worldCellPos))
                    {
                        center = worldCellPos;
                    }

                    ChatFrame.addMessage($"Spawning {terrainBlockType.name} ore vein at {center.x} {center.y} {center.z}", 0);

                    if (terrainBlockType.parentBOT == null)
                    {
                        ChatFrame.addMessage($"Ore template not buildable", 0);
                        return;
                    }
                    if (terrainBlockType.parentBOT.parentItemTemplate == null)
                    {
                        ChatFrame.addMessage($"Ore template has no item", 0);
                        return;
                    }

                    var chunkIds = new HashSet<ulong>();
                    var from = center - new Vector3Int(10, 5, 10);
                    var to = center + new Vector3Int(10, 5, 10);
                    for (int wz = from.z; wz <= to.z; ++wz)
                    {
                        for (int wy = from.y; wy <= to.y; ++wy)
                        {
                            for (int wx = from.x; wx <= to.x; ++wx)
                            {
                                var dx = Mathf.Abs(wx - center.x);
                                var dy = Mathf.Abs(wy - center.y) * 2;
                                var dz = Mathf.Abs(wz - center.z);
                                var distanceSqr = dx*dx + dy*dy + dz*dz;
                                if (distanceSqr <= 100)
                                {
                                    ChunkManager.getChunkIdxAndTerrainArrayIdxFromWorldCoords(wx, wy, wz, out var chunkIndex, out var blockIndex);
                                    var terrainData = ChunkManager.chunks_getTerrainData(chunkIndex, blockIndex);

                                    chunkIds.Add(chunkIndex);

                                    if (terrainData > 0 && terrainData < GameRoot.BUILDING_PART_ARRAY_IDX_START)
                                    {
                                        var worldPos = new Vector3Int(wx, wy, wz);
                                        GameRoot.addLockstepEvent(new Character.RemoveTerrainEvent(character.usernameHash, worldPos, ulong.MaxValue));
                                        GameRoot.addLockstepEvent(new BuildEntityEvent(character.usernameHash, terrainBlockType.parentBOT.parentItemTemplate.id, 0, worldPos, 0, Quaternion.identity, 0, 0, false));
                                    }
                                }
                            }
                        }
                    }

                    foreach (var chunkIndex in chunkIds)
                    {
                        ChunkManager.getChunkCoordsFromChunkIdx(chunkIndex, out var chunkX, out var chunkZ);
                        var count = ChunkManager.chunks_getOrePatchCount(chunkIndex);
                        var orePatchData = new Chunk.OrePatchData[count];
                        ChunkManager.chunks_populateOrePatchData(chunkIndex, orePatchData, count);
                        ChatFrame.addMessage($"Found {count} ore patches in chunk {chunkIndex} {chunkX}x{chunkZ}", 0);
                    }
                }

                var oreVeinTemplateName = arguments[0].ToLower();
                List<TerrainBlockType> foundTemplates = new List<TerrainBlockType>();
                foreach(var template in ItemTemplateManager.getAllTerrainTemplates().Values)
                {
                    if (template.parentBOT != null && template.parentBOT.identifier.StartsWith("_erkle_terrain"))
                    {
                        ChatFrame.addMessage($"Found ore type: {template.identifier}", 0);

                        if(template.identifier.ToLower() == oreVeinTemplateName || template.name.ToLower() == oreVeinTemplateName)
                        {
                            SpawnOreVein(template);
                            break;
                        }
                        else if(template.identifier.ToLower().Contains(oreVeinTemplateName) || template.name.ToLower().Contains(oreVeinTemplateName))
                        {
                            foundTemplates.Add(template);
                        }
                    }
                }
                switch(foundTemplates.Count)
                {
                    case 0: ChatFrame.addMessage("Found no matching ore block template", 0); break;
                    case 1: SpawnOreVein(foundTemplates[0]); break;
                    default:
                        ChatFrame.addMessage("Found multiple matches:", 0);
                        foreach(var template in foundTemplates)
                        {
                            ChatFrame.addMessage($"name: {template.name}    ident: {template.identifier}", 0);
                        }
                        break;
                }
            }),
            new CommandHandler(@"^\/give(?:\s+([\s\w\d]*?)(?:\s+(\d+))?)?$", (string[] arguments) => {
                void GiveItem(ItemTemplate item, uint amount)
                {
                    if (amount == 0) amount = item.stackSize;

                    var character = GameRoot.getClientCharacter();
                    if(character == null)
                    {
                        ChatFrame.addMessage("<b>ERROR:</b> Client character not found!", 0);
                        return;
                    }
                    GameRoot.addLockstepEvent(new GameRoot.ChatMessageEvent(character.usernameHash, string.Format("Spawning {0} of {1}", amount, item.name), 0, false));
                    InventoryManager.inventoryManager_tryAddItemAtAnyPosition(character.inventoryId, item.id, amount, IOBool.iofalse);
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
            [HarmonyPatch(typeof(ChatFrame), nameof(ChatFrame.onReturnCB))]
            [HarmonyPrefix]
            public static bool ChatFrame_onReturnCB()
            {
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


