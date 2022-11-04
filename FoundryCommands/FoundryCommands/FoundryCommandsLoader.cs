using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine.UI;
using TMPro;
using UnhollowerRuntimeLib;

namespace FoundryCommands
{
    [BepInPlugin(GUID, MODNAME, VERSION)]
    public class FoundryCommandsLoader : BepInEx.IL2CPP.BasePlugin
    {
        public const string
            MODNAME = "FoundryCommands",
            AUTHOR = "erkle64",
            GUID = "com." + AUTHOR + "." + MODNAME,
            VERSION = "1.4.0";

        public static BepInEx.Logging.ManualLogSource log;

        public static ConfigEntry<bool> config_jetpackForceUnlocked;
        //public static ConfigEntry<float> config_flight_speed;
        //public static ConfigEntry<float> config_flight_verticalSpeed;
        //public static ConfigEntry<float> config_flight_jumpInterval;
        //public const float walkingSpeed = 6.0f;

        public static string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string dumpFolder = Path.Combine(assemblyFolder, MODNAME);

        //public static Hook hook_CharacterJetpack_handleJetpack = null;
        //public delegate bool orig_handleJetpack(CharacterJetpack self, float lastJumpTime, InputProxy inputProxy, ref Vector3 moveDirection);

        public FoundryCommandsLoader()
        {
            log = Log;
        }

        public override void Load()
        {
            log.LogMessage("Registering PluginComponent in Il2Cpp");

            config_jetpackForceUnlocked = Config.Bind("Flight", "jetpackForceUnlocked", false, "Treat jetpack as unlocked and fueled.");

            //walkingSpeed = (float)AccessTools.Field(typeof(RenderCharacter), "SPEED_WALKING").GetValue(null);
            //config_flight_speed = Config.Bind("Flight", "speed", walkingSpeed * 4.0f, "Speed for horizontal flight in meters per second.");
            //config_flight_verticalSpeed = Config.Bind("Flight", "verticalSpeed", walkingSpeed, "Speed for vertical flight in meters per second.");
            //config_flight_jumpInterval = Config.Bind("Flight", "jumpInterval", 0.5f, "Maximum time for double tapping jump to toggle flight.");

            //PluginComponent.flightSpeedScale = config_flight_speed.Value/walkingSpeed;
            //PluginComponent.flightSpeedVertical = config_flight_verticalSpeed.Value;
            //PluginComponent.flightJumpInterval = config_flight_jumpInterval.Value;

            RegisterDumper("spriteTexture", (Image component) => { return (component.sprite == null) ? "null" : (component.sprite.texture == null) ? "null" : component.sprite.texture.name; });
            RegisterDumper("spriteBorder", (Image component) => { return (component.sprite == null) ? "null" : component.sprite.border.ToString(); });
            RegisterDumper("imageType", (Image component) => { return component.type.ToString(); });
            RegisterDumper("color", (Image component) => { return component.color.ToString(); });
            RegisterDumper("minWidth", (LayoutElement component) => { return component.minWidth; });
            RegisterDumper("minHeight", (LayoutElement component) => { return component.minHeight; });
            RegisterDumper("preferredWidth", (LayoutElement component) => { return component.preferredWidth; });
            RegisterDumper("preferredHeight", (LayoutElement component) => { return component.preferredHeight; });
            RegisterDumper("flexibleWidth", (LayoutElement component) => { return component.flexibleWidth; });
            RegisterDumper("flexibleHeight", (LayoutElement component) => { return component.flexibleHeight; });
            RegisterDumper("text", (TextMeshProUGUI component) => { return component.text; });
            RegisterDumper("textFont", (TextMeshProUGUI component) => { return component.font.name; });
            RegisterDumper("textSize", (TextMeshProUGUI component) => { return component.fontSize; });
            RegisterDumper("textColor", (TextMeshProUGUI component) => { return component.color.ToString(); });
            RegisterDumper("textAlignment", (TextMeshProUGUI component) => { return component.alignment.ToString(); });
            RegisterDumper("transition_type", (Button component) => { return component.transition.ToString(); });
            RegisterDumper("transition_normalColor", (Button component) => { return component.colors.normalColor.ToString(); });
            RegisterDumper("transition_highlightedColor", (Button component) => { return component.colors.highlightedColor.ToString(); });
            RegisterDumper("transition_pressedColor", (Button component) => { return component.colors.pressedColor.ToString(); });
            RegisterDumper("transition_selectedColor", (Button component) => { return component.colors.selectedColor.ToString(); });
            RegisterDumper("transition_disabledColor", (Button component) => { return component.colors.disabledColor.ToString(); });
            RegisterDumper("transition_colorMultiplier", (Button component) => { return component.colors.colorMultiplier.ToString(); });
            RegisterDumper("transition_fadeDuration", (Button component) => { return component.colors.fadeDuration.ToString(); });
            RegisterDumper("verticalLayout_padding", (VerticalLayoutGroup component) => { return component.padding.ToString(); });
            RegisterDumper("verticalLayout_spacing", (VerticalLayoutGroup component) => { return component.spacing.ToString(); });
            RegisterDumper("verticalLayout_childAlignment", (VerticalLayoutGroup component) => { return component.childAlignment.ToString(); });
            RegisterDumper("verticalLayout_reverseArrangement", (VerticalLayoutGroup component) => { return component.reverseArrangement ? "true" : "false"; });
            RegisterDumper("verticalLayout_childControlWidth", (VerticalLayoutGroup component) => { return component.childControlWidth ? "true" : "false"; });
            RegisterDumper("verticalLayout_childControlHeight", (VerticalLayoutGroup component) => { return component.childControlHeight ? "true" : "false"; });
            RegisterDumper("verticalLayout_childForceExpandWidth", (VerticalLayoutGroup component) => { return component.childForceExpandWidth ? "true" : "false"; });
            RegisterDumper("verticalLayout_childForceExpandHeight", (VerticalLayoutGroup component) => { return component.childForceExpandHeight ? "true" : "false"; });
            RegisterDumper("verticalLayout_childScaleWidth", (VerticalLayoutGroup component) => { return component.childScaleWidth ? "true" : "false"; });
            RegisterDumper("verticalLayout_childScaleHeight", (VerticalLayoutGroup component) => { return component.childScaleHeight ? "true" : "false"; });
            RegisterDumper("horizontalFit", (ContentSizeFitter component) => { return component.horizontalFit.ToString(); });
            RegisterDumper("verticalFit", (ContentSizeFitter component) => { return component.verticalFit.ToString(); });

            try
            {
                var harmony = new Harmony(GUID);
                harmony.PatchAll(typeof(Patch));

                harmony.Patch(typeof(ResearchSystem).GetProperty("jetpackIsUnlocked").GetGetMethod(), postfix: new HarmonyMethod(typeof(Patch).GetMethod("ResearchSystem_jetpackIsUnlocked")));
            }
            catch
            {
                log.LogError("Harmony - FAILED to Apply Patch's!");
            }
        }

        ~FoundryCommandsLoader()
        {
            if (teleportTimer != null)
            {
                teleportTimer.Dispose();
                teleportTimer = null;
            }
        }

        private static Timer teleportTimer = null;

        static void timer_Teleport(object state)
        {
            var wp = (Waypoint)state;

            var character = GameRoot.getClientCharacter();
            if (character == null)
            {
                ChatFrame.addMessage(PoMgr._po("Client character not found."));
                return;
            }

            GameRoot.addLockstepEvent(new Character.CharacterRelocateEvent(character.usernameHash, wp.waypointPosition.x, wp.waypointPosition.y, wp.waypointPosition.z));
            teleportTimer.Dispose();
            teleportTimer = null;
        }

        public static CommandHandler[] commandHandlers = new CommandHandler[]
        {
            new CommandHandler(@"^\/drag\s*?(?:\s+(\d+(?:\.\d*)?))?$", (string[] arguments) => {
                switch(arguments.Length)
                {
                    case 1:
                        var range = float.Parse(arguments[0]);
                        if(range < 38) range = 38;
                        //if(range < 200) range = 200;
                        range = ((int)range) - 0.5f;
                        var range2 = range*2.0f;
                        GameRoot.singleton.dragHelperGO.collider_area_xz.extents = new Vector3(range, 0.05f, range);
                        GameRoot.singleton.dragHelperGO.collider_area_xz_elevated.extents = new Vector3(range, 0.05f, range);
                        GameRoot.singleton.dragHelperGO.collider_slope.transform.localScale = new Vector3(range2, 0.1f, range2);
                        GameRoot.singleton.dragHelperGO.collider_wall_x.extents = new Vector3(range, range, 0.05f);
                        GameRoot.singleton.dragHelperGO.collider_wall_z.extents = new Vector3(0.05f, range, range);
                        GameRoot.singleton.dragHelperGO_bulkDemolish.collider_area_xz.extents = new Vector3(range, 0.05f, range);
                        GameRoot.singleton.dragHelperGO_bulkDemolish.collider_area_xz_elevated.extents = new Vector3(range, 0.05f, range);
                        GameRoot.singleton.dragHelperGO_bulkDemolish.collider_slope.transform.localScale = new Vector3(range2, 0.1f, range2);
                        GameRoot.singleton.dragHelperGO_bulkDemolish.collider_wall_x.extents = new Vector3(range, range, 0.05f);
                        GameRoot.singleton.dragHelperGO_bulkDemolish.collider_wall_z.extents = new Vector3(0.05f, range, range);

                        GameRoot.singleton.dragHelperGO.go_area_xz.transform.localScale = new Vector3(range2, 0.1f, range2);
                        GameRoot.singleton.dragHelperGO.go_area_xz.GetComponent<MeshRenderer>().material.SetTextureScale("_TextureY", new Vector2(range2, range2));
                        GameRoot.singleton.dragHelperGO.go_area_xz_elevated.transform.localScale = new Vector3(range2, 0.1f, range2);
                        GameRoot.singleton.dragHelperGO.go_area_xz_elevated.GetComponent<MeshRenderer>().material.SetTextureScale("_TextureY", new Vector2(range2, range2));
                        GameRoot.singleton.dragHelperGO.go_slope.transform.localScale = new Vector3(range2, 0.1f, range2);
                        GameRoot.singleton.dragHelperGO.go_slope.GetComponent<MeshRenderer>().material.SetTextureScale("_TextureY", new Vector2(range2, range2));
                        GameRoot.singleton.dragHelperGO.go_wall_x.transform.localScale = new Vector3(range2, range2, 0.1f);
                        GameRoot.singleton.dragHelperGO.go_wall_x.GetComponent<MeshRenderer>().material.SetTextureScale("_TextureY", new Vector2(range2, range2));
                        GameRoot.singleton.dragHelperGO.go_wall_z.transform.localScale = new Vector3(0.1f, range2, range2);
                        GameRoot.singleton.dragHelperGO.go_wall_z.GetComponent<MeshRenderer>().material.SetTextureScale("_TextureY", new Vector2(range2, range2));
                        GameRoot.singleton.dragHelperGO_bulkDemolish.go_area_xz.transform.localScale = new Vector3(range2, 0.1f, range2);
                        GameRoot.singleton.dragHelperGO_bulkDemolish.go_area_xz.GetComponent<MeshRenderer>().material.SetTextureScale("_TextureY", new Vector2(range2, range2));
                        GameRoot.singleton.dragHelperGO_bulkDemolish.go_area_xz_elevated.transform.localScale = new Vector3(range2, 0.1f, range2);
                        GameRoot.singleton.dragHelperGO_bulkDemolish.go_area_xz_elevated.GetComponent<MeshRenderer>().material.SetTextureScale("_TextureY", new Vector2(range2, range2));
                        GameRoot.singleton.dragHelperGO_bulkDemolish.go_slope.transform.localScale = new Vector3(range2, 0.1f, range2);
                        GameRoot.singleton.dragHelperGO_bulkDemolish.go_slope.GetComponent<MeshRenderer>().material.SetTextureScale("_TextureY", new Vector2(range2, range2));
                        GameRoot.singleton.dragHelperGO_bulkDemolish.go_wall_x.transform.localScale = new Vector3(range2, range2, 0.1f);
                        GameRoot.singleton.dragHelperGO_bulkDemolish.go_wall_x.GetComponent<MeshRenderer>().material.SetTextureScale("_TextureY", new Vector2(range2, range2));
                        GameRoot.singleton.dragHelperGO_bulkDemolish.go_wall_z.transform.localScale = new Vector3(0.1f, range2, range2);
                        GameRoot.singleton.dragHelperGO_bulkDemolish.go_wall_z.GetComponent<MeshRenderer>().material.SetTextureScale("_TextureY", new Vector2(range2, range2));
                        break;

                    default:
                        ChatFrame.addMessage(PoMgr._po("Usage: <b>/drag</b> <i>range</i>"));
                        return;
                }
            }),
            new CommandHandler(@"^\/jet", (string[] arguments) => {
                if (config_jetpackForceUnlocked.Value)
                {
                    ChatFrame.addMessage(PoMgr._po("Disabling infinite jetpack mode."));

                    config_jetpackForceUnlocked.Value = false;

                    var template = ItemTemplateManager.getResearchTemplateById(ResearchTemplate.generateStringHash("_base_jetpack"));
                    if (template != null)
                    {
                        if (!ResearchSystem.isFinished(template, -1)) ResearchSystem.singleton.jetpackIsUnlocked = false;
                    }
                    else
                    {
                        ChatFrame.addMessage(PoMgr._po("<color=red>Failed to find jetpack research item.</color>"));
                    }
                }
                else
                {
                    ChatFrame.addMessage(PoMgr._po("Enabling infinite jetpack mode."));

                    config_jetpackForceUnlocked.Value = true;
                    ResearchSystem.singleton.jetpackIsUnlocked = true;
                }
            }),
            //new CommandHandler(@"^\/fly$", (string[] arguments) => {
            //    PluginComponent.isFlying = !PluginComponent.isFlying;
            //}),
            //new CommandHandler(@"^\/flySpeed\s*?(?:\s+(\d+(?:\.\d*)?)(?:\s+(\d+(?:\.\d*)?))?\s*)?$", (string[] arguments) => {
            //    switch(arguments.Length)
            //    {
            //        case 1:
            //            config_flight_speed.Value = float.Parse(arguments[0]);
            //            PluginComponent.flightSpeedScale = config_flight_speed.Value/walkingSpeed;
            //            PluginComponent.flightSpeedVertical = config_flight_verticalSpeed.Value;
            //            break;
            //        case 2:
            //            config_flight_verticalSpeed.Value = float.Parse(arguments[1]);
            //            PluginComponent.flightSpeedVertical = config_flight_verticalSpeed.Value;
            //            goto case 1;
            //        default:
            //            ChatFrame.addMessage(PoMgr._po("Usage: <b>/flySpeed</b> <i>speed</i> <i>vertical-speed</i>"));
            //            ChatFrame.addMessage(PoMgr._po("Current flight speed is {0}m/s horizontal and {1}m/s vertical.", config_flight_speed.Value.ToString(), config_flight_verticalSpeed.Value.ToString()));
            //            return;
            //    }

            //    ChatFrame.addMessage(PoMgr._po("Set flight speed to {0}m/s horizontal and {1}m/s vertical.", config_flight_speed.Value.ToString(), config_flight_verticalSpeed.Value.ToString()));
            //}),
            new CommandHandler(@"^\/tp(?:\s+([\s\w\d]*?)\s*)?$", (string[] arguments) => {
                if (arguments.Length == 0 && arguments[0].Length == 0)
                {
                    ChatFrame.addMessage(PoMgr._po("Usage: <b>/tp</b> <i>waypoint-name</i>"));
                    return;
                }

                var wpName = arguments[0];
                var character = GameRoot.getClientCharacter();
                if (character == null)
                {
                    ChatFrame.addMessage(PoMgr._po("Client character not found."));
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
                            GameRoot.addLockstepEvent(new GameRoot.ChatMessageEvent(character.usernameHash, string.Format("Teleporting to '{0}' at {1}, {2}, {3}", wp.description, wp.waypointPosition.x.ToString(), wp.waypointPosition.y.ToString(), wp.waypointPosition.z.ToString()), 0));
                            GameRoot.addLockstepEvent(new Character.CharacterRelocateEvent(character.usernameHash, wp.waypointPosition.x, wp.waypointPosition.y, wp.waypointPosition.z));
                        }
                        else
                        {
                            GameRoot.addLockstepEvent(new GameRoot.ChatMessageEvent(character.usernameHash, "Ungenerated chunk.", 0));
                            ChunkManager.generateNewChunksBasedOnPosition(wp.waypointPosition, ChunkManager._getChunkLoadDistance());
                        }
                        //else
                        //{
                        //    ChunkManager.generateNewChunksBasedOnPosition(wp.waypointPosition, 15);
                        //    ChunkManager.generateNewChunksForGameStart(wp.waypointPosition);
                        //    teleportTimer = new Timer(timer_TeleportWait, wp, 1000, 1000);
                        //}
                        return;
                    }
                }

                ChatFrame.addMessage(PoMgr._po("Waypoint not found."));
            }),
            new CommandHandler(@"^\/give(?:\s+([\s\w\d]*?)(?:\s+(\d+))?)?$", (string[] arguments) => {
                void GiveItem(ItemTemplate item, uint amount)
                {
                    if (amount == 0) amount = item.stackSize;

                    var character = GameRoot.getClientCharacter();
                    if(character == null)
                    {
                        ChatFrame.addMessage(PoMgr._po("<b>ERROR:</b> Client character not found!"));
                        return;
                    }
                    GameRoot.addLockstepEvent(new GameRoot.ChatMessageEvent(character.usernameHash, string.Format("Spawning {0} of {1}", amount, item.name), 0));
                    InventoryManager.inventoryManagerPtr_tryAddItemAtAnyPosition(character.inventoryPtr, item.id, amount, IOBool.iofalse);
                    //GameRoot.addLockstepEvent(new DebugItemSpawnEvent(character, item.id));
                }

                uint count = 0;
                switch(arguments.Length)
                {
                    case 1:
                        var name = arguments[0].ToLower();
                        List<ItemTemplate> foundItems = new List<ItemTemplate>();
                        foreach(var item in ItemTemplateManager.dict_itemTemplates.Values)
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
                            case 0: ChatFrame.addMessage(PoMgr._po("Found no matching item template")); break;
                            case 1: GiveItem(foundItems[0], count); break;
                            default:
                                ChatFrame.addMessage(PoMgr._po("Found multiple matches:"));
                                foreach(var item in foundItems)
                                {
                                    ChatFrame.addMessage(PoMgr._po("name: {0}    ident: {1}", item.name, item.identifier));
                                }
                                break;
                        }
                        break;

                    case 2:
                        count = uint.Parse(arguments[1]);
                        goto case 1;

                    default:
                        ChatFrame.addMessage(PoMgr._po("Usage: <b>/give</b> <i>name</i> <i>amount</i>"));
                        break;
                }
            }),
            new CommandHandler(@"^\/dumpData(?:\s(minify))?", (string[] arguments) => {
                bool minify = (arguments.Length >= 1 && arguments[0].ToLower() == "minify");

                if (!Directory.Exists(dumpFolder)) Directory.CreateDirectory(dumpFolder);
                var f = new StreamWriter(Path.Combine(dumpFolder, "idmap.json"), false);
                void dumpEntry(string indent, string entry, params object[] args)
                {
                    if(minify) f.Write(entry, args);
                    else f.WriteLine(indent+entry, args);
                }
                dumpEntry("", "{{");
                void dumpDictionary<T>(string label, Il2CppSystem.Collections.Generic.Dictionary<ulong, T> dict, bool last = false)
                {
                    var identifier = typeof(T).GetProperty("identifier");
                    var icon_identifier = typeof(T).GetProperty("icon_identifier");
                    var name = typeof(T).GetProperty("name");
                    var stackSize = typeof(T).GetProperty("stackSize");
                    var category_identifier = typeof(T).GetProperty("category_identifier");
                    var rowGroup_identifier = typeof(T).GetProperty("rowGroup_identifier");
                    var sortingOrderWithinRowGroup = typeof(T).GetProperty("sortingOrderWithinRowGroup");
                    var identifier_category = typeof(T).GetProperty("identifier_category");
                    var sortingOrder = typeof(T).GetProperty("sortingOrder");
                    var relatedItemTemplateIdentifier = typeof(T).GetProperty("relatedItemTemplateIdentifier");
                    var output_data = typeof(T).GetProperty("output_data");
                    dumpEntry("  ", "\"{0}\": [", label);
                    var keys = dict.Keys;
                    int index = 0;
                    foreach(var key in keys)
                    {
                        var value = dict[key];
                        dumpEntry("    ", "{{");
                        dumpEntry("      ", "\"id\": {0},", key);
                        dumpEntry("      ", "\"name\": \"{0}\",", (string)name.GetValue(value));
                        if(stackSize != null) dumpEntry("      ", "\"stackSize\": {0},", (uint)stackSize.GetValue(value));
                        if(icon_identifier != null) dumpEntry("", "      \"icon\": \"{0}\",", (string)icon_identifier.GetValue(value));
                        if(relatedItemTemplateIdentifier != null || output_data != null)
                        {
                            if(relatedItemTemplateIdentifier != null && !((string)relatedItemTemplateIdentifier.GetValue(value)).IsNullOrWhiteSpace())
                            {
                                dumpEntry("", "      \"item_identifier\": \"{0}\",", (string)relatedItemTemplateIdentifier.GetValue(value));
                            }
                            else
                            {
                                var outputs = (UnhollowerBaseLib.Il2CppReferenceArray<CraftingRecipe.CraftingRecipeItemInput>)output_data.GetValue(value);
                                if(outputs != null && outputs.Count > 0)
                                {
                                    dumpEntry("", "      \"item_identifier\": \"{0}\",", (string)outputs[0].identifier);
                                }
                            }
                        }
                        if(category_identifier != null) dumpEntry("", "      \"category_identifier\": \"{0}\",", (string)category_identifier.GetValue(value));
                        if(identifier_category != null) dumpEntry("", "      \"identifier_category\": \"{0}\",", (string)identifier_category.GetValue(value));
                        if(rowGroup_identifier != null) dumpEntry("", "      \"rowGroup_identifier\": \"{0}\",", (string)rowGroup_identifier.GetValue(value));
                        if(sortingOrderWithinRowGroup != null) dumpEntry("", "      \"sortingOrderWithinRowGroup\": {0},", (int)sortingOrderWithinRowGroup.GetValue(value));
                        if(sortingOrder != null) dumpEntry("", "      \"sortingOrder\": {0},", (int)sortingOrder.GetValue(value));
                        dumpEntry("      ", "\"identifier\": \"{0}\"", (string)identifier.GetValue(value));
                        dumpEntry("    ", (index == keys.Count - 1) ? "}}" : "}},");
                        ++index;
                    }
                    dumpEntry("  ", last ? "]" : "],");
                }
                dumpDictionary<ItemTemplate>("items", ItemTemplateManager.dict_itemTemplates);
                dumpDictionary<ElementTemplate>("elements", ItemTemplateManager.dict_elementTemplates);
                dumpDictionary<CraftingRecipe>("recipes", ItemTemplateManager.dict_craftingRecipes);
                dumpDictionary<BuildableObjectTemplate>("buildings", ItemTemplateManager.dict_buildableObjectTemplates);
                dumpDictionary<TerrainBlockType>("terrain", ItemTemplateManager.dict_terrainBlockTemplates);
                dumpDictionary<CraftingRecipeCategory>("recipe_categories", ItemTemplateManager.dict_craftingRecipeCategories);
                dumpDictionary<CraftingRecipeRowGroup>("recipe_row_groups", ItemTemplateManager.dict_craftingRecipeRowGroups, true);
                dumpEntry("", "}}");
                f.Close();
                ChatFrame.addMessage(PoMgr._po("Data saved to BepInEx\\plugins\\{0}\\idmap.json", MODNAME));
            }),
            new CommandHandler(@"^\/tweakItems\s+([\w\-.]+)(?:\s+(\w+)=((?:\""[^\""]*\"")|(?:[0-9]*(?:\.[0-9]*)?)))+$", (string[] arguments) => {
                log.LogMessage(string.Join(", ", arguments));
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
                void dumpDictionary<T>(string label, Il2CppSystem.Collections.Generic.Dictionary<ulong, T> dict, bool last = false)
                {
                    var identifier = typeof(T).GetProperty("identifier");
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
                dumpDictionary<ItemTemplate>("items", ItemTemplateManager.dict_itemTemplates, last: true);
                dumpEntry("  ", "}}");
                dumpEntry("", "}}");
                f.Close();
                ChatFrame.addMessage(PoMgr._po("Data saved to BepInEx\\plugins\\{0}\\{1}", MODNAME, targetPath));
            }),
            new CommandHandler(@"^\/dumpGO(?:\s+([\w\/]+))?$", (string[] arguments) => {
                log.LogInfo(string.Join(", ", arguments));
                var targetPath = "GameObject.json";

                if (arguments.Length < 1)
                {
                    ChatFrame.addMessage(PoMgr._po("Usage: <b>/dumpGO</b> <i>name/path</i>"));
                    return;
                }

                var go = GameObject.Find(arguments[0]);
                if (go == null)
                {
                    ChatFrame.addMessage(PoMgr._po("<b>ERROR:</b> Game object not found!"));
                    return;
                }

                var dump = new DumpGO(go);
                var json = JsonConvert.SerializeObject(dump, Formatting.Indented);
                File.WriteAllText(Path.Combine(dumpFolder, targetPath), json);

                ChatFrame.addMessage(PoMgr._po("Data saved to BepInEx\\plugins\\{0}\\{1}", MODNAME, targetPath));
            })
        };


        private delegate object DumperDg<A>(A argument);
        private static Dictionary<string, Dictionary<string, DumperDg<MonoBehaviour>>> componentDumpers = new Dictionary<string, Dictionary<string, DumperDg<MonoBehaviour>>>();
        private static void RegisterDumper<T>(string name, DumperDg<T> dumper) where T : MonoBehaviour
        {
            var typeName = UnhollowerRuntimeLib.Il2CppType.Of<T>().FullName;
            Dictionary<string, DumperDg<MonoBehaviour>> typeDumpers;
            if(!componentDumpers.TryGetValue(typeName, out typeDumpers))
            {
                componentDumpers[typeName] = typeDumpers = new Dictionary<string, DumperDg<MonoBehaviour>>();
            }

            typeDumpers[name] = (MonoBehaviour component) =>
            {
                return dumper.Invoke(component.Cast<T>());
            };
        }


        private class DumpGO
        {
            public string name;
            public float offsetMinX;
            public float offsetMinY;
            public float offsetMaxX;
            public float offsetMaxY;
            public float pivotX;
            public float pivotY;
            public float anchorMinX;
            public float anchorMinY;
            public float anchorMaxX;
            public float anchorMaxY;
            public DumpMB[] components;
            public DumpGO[] children;

            public DumpGO(GameObject go)
            {
                name = go.name;

                var transform = go.transform.Cast<RectTransform>();
                offsetMinX = transform.offsetMin.x;
                offsetMinY = transform.offsetMin.y;
                offsetMaxX = transform.offsetMax.x;
                offsetMaxY = transform.offsetMax.y;
                pivotX = transform.pivot.x;
                pivotY = transform.pivot.y;
                anchorMinX = transform.anchorMin.x;
                anchorMinY = transform.anchorMin.y;
                anchorMaxX = transform.anchorMax.x;
                anchorMaxY = transform.anchorMax.y;

                var components = go.GetComponents<MonoBehaviour>();
                this.components = new DumpMB[components.Length];
                for(int i = 0; i < components.Length; i++) this.components[i] = new DumpMB(components[i]);

                var childCount = go.transform.GetChildCount();
                children = new DumpGO[childCount];
                for (int i = 0; i < childCount; ++i) children[i] = new DumpGO(go.transform.GetChild(i).gameObject);
            }
        }


        private class DumpMB
        {
            public string typeName;
            public Dictionary<string, object> values = new Dictionary<string, object>();

            public DumpMB(MonoBehaviour component)
            {
                typeName = component.GetIl2CppType().FullName;

                Dictionary<string, DumperDg<MonoBehaviour>> typeDumpers;
                if (componentDumpers.TryGetValue(typeName, out typeDumpers))
                {
                    foreach(var kv in typeDumpers)
                    {
                        values[kv.Key] = kv.Value.Invoke(component);
                    }
                }
           }
        }

        [HarmonyPatch]
        public static class Patch
        {
            [HarmonyPatch(typeof(Character.SaveSync_JetpackPreconsumedFuel), nameof(Character.SaveSync_JetpackPreconsumedFuel.processEvent))]
            [HarmonyPrefix]
            public static bool SaveSync_JetpackPreconsumedFuel_processEvent(Character.SaveSync_JetpackPreconsumedFuel __instance)
            {
                if (!config_jetpackForceUnlocked.Value) return true;

                var character = CharacterManager.getByUsernameHash(__instance.characterHash);
                if (character == null) return true;

                character.saveSyncData.preConsumedFuel_kj = character.clientData.preConsumedFuel_kj = 25000.0f;

                return false;
            }

            [HarmonyPatch(typeof(Character.ConsumeFuelForJetpackEvent), nameof(Character.ConsumeFuelForJetpackEvent.processEvent))]
            [HarmonyPrefix]
            public static bool ConsumeFuelForJetpackEvent_processEvent(Character.ConsumeFuelForJetpackEvent __instance)
            {
                if (!config_jetpackForceUnlocked.Value) return true;

                var character = CharacterManager.getByUsernameHash(__instance.characterHash);
                if (character == null) return true;

                character.clientData.preConsumedFuel_kj = 25000.0f;

                return false;
            }

            [HarmonyPatch(typeof(ResearchSystem), nameof(ResearchSystem.Init))]
            [HarmonyPostfix]
            public static void ResearchSystem_Init(ResearchSystem __instance)
            {
                log.LogInfo("============================================================= ResearchSystem_Init");
                if (!config_jetpackForceUnlocked.Value) return;

                ResearchSystem.singleton.jetpackIsUnlocked = true;
            }

            [HarmonyPatch(typeof(ChatFrame), nameof(ChatFrame.onReturnCB))]
            [HarmonyPrefix]
            public static bool ChatFrame_onReturnCB()
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

                return true;
            }

            [HarmonyPatch(typeof(InteractableObject), nameof(InteractableObject.onClick))]
            [HarmonyPrefix]
            public static bool InteractableObject_onClick(InteractableObject __instance)
            {
                if (GameRoot.getClientRenderCharacter().inputProxy.isKeyPressed[(int)InputProxy.eKey.SPRINT] == 0) return true;

                var bogo = StreamingSystem.getBuildableObjectGOByEntityId(__instance.relatedEntityId);
                if (bogo == null || bogo.template == null || bogo.template.type != BuildableObjectTemplate.BuildableObjectType.ConveyorBalancer) return true;

                var balancer = bogo.Cast<ConveyorBalancerGO>();
                var leverState = __instance.interactableObjectIdx == 0 ? balancer.getInputPriority() : balancer.getOutputPriority();
                int pulseCount = 2 + leverState / 2;

                var character = GameRoot.getClientCharacter();
                if (character == null)
                {
                    log.LogError("<b>ERROR:</b> Client character not found!");
                    return true;
                }

                for (int i = 0; i < pulseCount; i++)
                {
                    GameRoot.addLockstepEvent(new BuildableObjectInteraction(character.usernameHash, __instance.relatedEntityId, __instance.interactableObjectIdx));
                }

                return false;
            }
        }
    }
}