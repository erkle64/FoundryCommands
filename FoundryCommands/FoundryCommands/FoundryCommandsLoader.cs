using BepInEx;
using BepInEx.Configuration;
using UnhollowerRuntimeLib;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Collections.Generic;

namespace FoundryCommands
{
    [BepInPlugin(GUID, MODNAME, VERSION)]
    public class FoundryCommandsLoader : BepInEx.IL2CPP.BasePlugin
    {
        public const string
            MODNAME = "FoundryCommands",
            AUTHOR = "erkle64",
            GUID = "com." + AUTHOR + "." + MODNAME,
            VERSION = "1.0.0";

        public static BepInEx.Logging.ManualLogSource log;

        public static ConfigEntry<float> config_flight_speed;
        public static ConfigEntry<float> config_flight_verticalSpeed;
        public static ConfigEntry<float> config_flight_jumpInterval;
        public const float walkingSpeed = 6.0f;

        public FoundryCommandsLoader()
        {
            log = Log;
        }

        public override void Load()
        {
            log.LogMessage("Registering PluginComponent in Il2Cpp");

            //walkingSpeed = (float)AccessTools.Field(typeof(RenderCharacter), "SPEED_WALKING").GetValue(null);
            config_flight_speed = Config.Bind("Flight", "speed", walkingSpeed * 4.0f, "Speed for horizontal flight in meters per second.");
            config_flight_verticalSpeed = Config.Bind("Flight", "verticalSpeed", walkingSpeed, "Speed for vertical flight in meters per second.");
            config_flight_jumpInterval = Config.Bind("Flight", "jumpInterval", 0.5f, "Maximum time for double tapping jump to toggle flight.");

            PluginComponent.flightSpeedScale = config_flight_speed.Value/walkingSpeed;
            PluginComponent.flightSpeedVertical = config_flight_verticalSpeed.Value;
            PluginComponent.flightJumpInterval = config_flight_jumpInterval.Value;

            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<PluginComponent>();

                var go = new GameObject("Erkle64_FoundryCommands_PluginObject");
                go.AddComponent<PluginComponent>();
                Object.DontDestroyOnLoad(go);
            }
            catch
            {
                log.LogError("FAILED to Register Il2Cpp Type: PluginComponent!");
            }

            try
            {
                var harmony = new Harmony(GUID);

                var original = AccessTools.Method(typeof(ChatFrame), "onReturnCB");
                var pre = AccessTools.Method(typeof(PluginComponent), "processChatEvent");
                harmony.Patch(original, prefix: new HarmonyMethod(pre));

                original = AccessTools.Method(typeof(UnityEngine.CharacterController), "Move");
                pre = AccessTools.Method(typeof(PluginComponent), "characterMove");
                harmony.Patch(original, prefix: new HarmonyMethod(pre));

                original = AccessTools.Property(typeof(UnityEngine.CharacterController), "isGrounded").GetGetMethod();
                var post = AccessTools.Method(typeof(PluginComponent), "characterIsGrounded");
                harmony.Patch(original, postfix: new HarmonyMethod(post));

                original = AccessTools.Method(typeof(RenderCharacter), "getMovementSoundPackBasedOnPosition");
                post = AccessTools.Method(typeof(PluginComponent), "getMovementSoundPackBasedOnPosition");
                harmony.Patch(original, postfix: new HarmonyMethod(post));

                original = AccessTools.Method(typeof(GameRoot), "initInputRelay");
                pre = AccessTools.Method(typeof(PluginComponent), "initInputRelay");
                harmony.Patch(original, prefix: new HarmonyMethod(pre));
            }
            catch
            {
                log.LogError("Harmony - FAILED to Apply Patch's!");
            }
        }

        public static CommandHandler[] commandHandlers = new CommandHandler[]
        {
            new CommandHandler(@"^\/fly$", (string[] arguments) => {
                PluginComponent.isFlying = !PluginComponent.isFlying;
            }),
            new CommandHandler(@"^\/flySpeed\s*?(?:\s+(\d+(?:\.\d*)?)(?:\s+(\d+(?:\.\d*)?))?\s*)?$", (string[] arguments) => {
                log.LogMessage(string.Join(", ", arguments));

                switch(arguments.Length)
                {
                    case 1:
                        config_flight_speed.Value = float.Parse(arguments[0]);
                        PluginComponent.flightSpeedScale = config_flight_speed.Value/walkingSpeed;
                        PluginComponent.flightSpeedVertical = config_flight_verticalSpeed.Value;
                        break;
                    case 2:
                        config_flight_verticalSpeed.Value = float.Parse(arguments[1]);
                        PluginComponent.flightSpeedVertical = config_flight_verticalSpeed.Value;
                        goto case 1;
                    default:
                        ChatFrame.addMessage(PoMgr._po("Usage: <b>/flySpeed</b> <i>speed</i> <i>vertical-speed</i>"));
                        ChatFrame.addMessage(PoMgr._po("Current flight speed is {0}m/s horizontal and {1}m/s vertical.", config_flight_speed.Value.ToString(), config_flight_verticalSpeed.Value.ToString()));
                        return;
                }

                ChatFrame.addMessage(PoMgr._po("Set flight speed to {0}m/s horizontal and {1}m/s vertical.", config_flight_speed.Value.ToString(), config_flight_verticalSpeed.Value.ToString()));
            }),
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
                    if (wp.description.ToLower() == wpName)
                    {
                        GameRoot.addLockstepEvent(new GameRoot.ChatMessageEvent(character.usernameHash, string.Format("Teleporting to '{0}' at {1}, {2}, {3}", wp.description, wp.waypointPosition.x.ToString(), wp.waypointPosition.y.ToString(), wp.waypointPosition.z.ToString()), 0));
                        GameRoot.addLockstepEvent(new Character.CharacterRelocateEvent(character.usernameHash, wp.waypointPosition.x, wp.waypointPosition.y, wp.waypointPosition.z));
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

                ChatFrame.addMessage(PoMgr._po("arguments.Length: {0}", arguments.Length.ToString()));
                for(int i = 0; i < arguments.Length; ++i)
                {
                    ChatFrame.addMessage(PoMgr._po("arguments[{0}] = '{1}'", i.ToString(), arguments[i]));
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
                            else if(item.identifier.Contains(name) || item.name.Contains(name))
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
            })
        };
    }
}