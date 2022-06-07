using System;
using UnhollowerBaseLib;
using HarmonyLib;
using UnityEngine;
using System.Text.RegularExpressions;

namespace FoundryCommands
{
    public class PluginComponent : MonoBehaviour
    {
        public static bool isFlying = false;
        public static float flightSpeedScale = 2.0f;
        public static float flightSpeedVertical = 6.0f;
        public static float flightJumpInterval = 0.5f;

        private static float lastJumpTime = 0.0f;
        private static bool[] keyStates = new bool[6] { false, false, false, false, false, false };

        public enum KeyType
        {
            Forward,
            Back,
            Right,
            Left,
            Jump,
            Sprint
        }

        public PluginComponent (IntPtr ptr) : base(ptr)
        {
        }

        [HarmonyPrefix]
        public static void characterMove(UnityEngine.CharacterController __instance, ref Vector3 motion)
        {
            if (isFlying)
            {
                motion.y = keyStates[(int)KeyType.Jump] ? flightSpeedVertical * Time.fixedDeltaTime : keyStates[(int)KeyType.Sprint] ? -flightSpeedVertical * Time.fixedDeltaTime : 0.0f;
                
                motion.x = motion.x * flightSpeedScale;
                motion.z = motion.z * flightSpeedScale;
                var motionXZ = new Vector2(motion.x, motion.z)/Time.fixedDeltaTime;
                var mag = motionXZ.magnitude;
                if (mag > flightSpeedScale*FoundryCommandsLoader.walkingSpeed)
                {
                    motionXZ = motionXZ.normalized * (flightSpeedScale * FoundryCommandsLoader.walkingSpeed * Time.deltaTime);
                    motion.x = motionXZ.x;
                    motion.z = motionXZ.y;
                }
            }
        }

        [HarmonyPostfix]
        public static void characterIsGrounded(ref bool __result)
        {
            if (isFlying) __result = true;
        }

        [HarmonyPostfix]
        public static void getMovementSoundPackBasedOnPosition(ref MovementSoundPack __result)
        {
            if (isFlying) __result = null;
        }

        [HarmonyPrefix]
        public static void initInputRelay(ref bool isKeyDown, int inputKeyType, Character relatedCharacter)
        {
            if(relatedCharacter != null && relatedCharacter.sessionOnly_isClientCharacter && keyStates[inputKeyType] != isKeyDown)
            {
                keyStates[inputKeyType] = isKeyDown;

                if(inputKeyType == (int)KeyType.Jump && isKeyDown)
                {
                    if (Time.time - lastJumpTime < flightJumpInterval)
                    {
                        isFlying = !isFlying;
                        FoundryCommandsLoader.log.LogMessage(string.Format("Double jump detected. Switching to {0} mode", isFlying ? "flight" : "walk"));
                    }

                    lastJumpTime = Time.time;
                }
            }
            //FoundryCommandsLoader.log.LogMessage(string.Format("initInputRelay {0} {1} {2}", isKeyDown, inputKeyType, (relatedCharacter != null) ? relatedCharacter.username : "<nobody>"));
        }

        [HarmonyPrefix]
        public static bool processChatEvent()
        {
            var message = ChatFrame.getMessage();

            foreach (var handler in FoundryCommandsLoader.commandHandlers)
            {
                if (handler.TryProcessCommand(message))
                {
                    ChatFrame.hideMessageBox();
                    return false;
                }
            }

            return true;
        }
    }
}