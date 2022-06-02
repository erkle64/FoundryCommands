using System;
using UnhollowerBaseLib;
using HarmonyLib;
using UnityEngine;

namespace FoundryCommands
{
    public class PluginComponent : MonoBehaviour
    {
        public PluginComponent (IntPtr ptr) : base(ptr)
        {
        }

        [HarmonyPrefix]
        public static bool processChatEvent()
        {
            var message = ChatFrame.getMessage();
            if(message.StartsWith("/tp "))
            {
                var wpName = message.Substring(4).ToLower();
                if (wpName.Length > 0)
                {
                    var character = GameRoot.getClientCharacter();
                    if (character != null)
                    {
                        foreach (var wp in character.getWaypointDict().Values)
                        {
                            if (wp.description.ToLower() == wpName)
                            {
                                GameRoot.addLockstepEvent(new GameRoot.ChatMessageEvent(character.usernameHash, string.Format("Teleporting to '{0}' at {1}, {2}, {3}", wp.description, wp.waypointPosition.x.ToString(), wp.waypointPosition.y.ToString(), wp.waypointPosition.z.ToString()), 0));
                                GameRoot.addLockstepEvent(new Character.CharacterRelocateEvent(character.usernameHash, wp.waypointPosition.x, wp.waypointPosition.y, wp.waypointPosition.z));
                                ChatFrame.hideMessageBox();
                                return false;
                            }
                        }

                        ChatFrame.addMessage(PoMgr._po("Waypoint not found."));
                        ChatFrame.hideMessageBox();
                        return false;
                    }

                    ChatFrame.addMessage(PoMgr._po("Client character not found."));
                    ChatFrame.hideMessageBox();
                    return false;
                }

                ChatFrame.addMessage(PoMgr._po("Usage: /tp <i>waypoint-name</i>"));
                ChatFrame.hideMessageBox();
                return false;
            }

            return true;
        }
    }
}