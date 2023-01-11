using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SMLHelper.V2.Json;
using SMLHelper.V2.Options.Attributes;
using SMLHelper.V2.Handlers;
using UnityEngine;
using System.Reflection;
using System.Collections;

namespace pppardo.CyclopPitchStrafe
{
    [BepInPlugin(myGUID, pluginName, versionString)]
    public class CyclopPitchStrafePlugin : BaseUnityPlugin
    {
        private const string myGUID = "pppardo.CyclopPitchStrafe";
        private const string pluginName = "Cyclop Pitch & Strafe Mod SN(BepInEx)";
        private const string pluginSort = "CyclopPitchStrafe";
        private const string versionString = "1.0.0";

        internal static Config MyConfig { get; } = OptionsPanelHandler.Main.RegisterModOptions<Config>();

        public static bool IsError{ get; set; }

        private static readonly Harmony harmony = new Harmony(myGUID);

        public static ManualLogSource logger;

        private void Awake() {
            //Logger.LogInfo(pluginName + " " + versionString + " " + "loaded.");
            logger = BepInEx.Logging.Logger.CreateLogSource(pluginSort);
            logger.LogInfo("Inicializando Plugin "+ pluginName + " " + versionString);
            harmony.PatchAll(typeof(SubControlPatch));
            harmony.PatchAll(typeof(PlayerPatch));
            harmony.PatchAll(typeof(CyclopsHelmHUDManagerPatch));
            harmony.PatchAll(typeof(StabilizerPatch));
            logger.LogInfo(string.Join(",", harmony.GetPatchedMethods()));
            logger.LogInfo("Parcheado.");

        }
    }

    /// <summary>
    /// Menu de teclas
    /// </summary>
    [Menu("Cyclop Pitch & Strafe")]
    public class Config : ConfigFile {

        [Slider("Strafe speed", Format = "{0:F2}", Min = 0.01f, Max = 2f, DefaultValue = 0.5f, Step = 0.01f)]
        public float StrafeSpeed = 0.5f;

        [Keybind("Modifier")]
        public KeyCode StrafeModifierKey = KeyCode.LeftShift;

        [Toggle("Use modifier?")]
        public bool UseModifier = true;

        [Toggle("Modifier cancel normal movement?")]
        public bool ModifierCancel = true;

        [Keybind("Left")]
        public KeyCode StrafeLeftKey = KeyCode.A; //KeyCode.LeftArrow;
        
        [Keybind("Right")]
        public KeyCode StrafeRightKey = KeyCode.D;  //KeyCode.RightArrow;

        [Keybind("Stabilizer")]
        public KeyCode Stabilizer =  KeyCode.PageDown;

        [Slider("Emergency Stabilizer Accelerator", Format = "{0:F2}", Min = 10f, Max = 1000f, DefaultValue = 100f, Step = 10f)]
        public float StabilizerSpeed = 100f;


        [Keybind("PitchUp")]
        public KeyCode PitchUpKey = KeyCode.S; //KeyCode.UpArrow;

        [Keybind("PitchDown")]
        public KeyCode PitchDowntKey = KeyCode.W;//KeyCode.DownArrow;
    }

    public static class KeyHandler {
        
        private static bool _Active => !CyclopPitchStrafePlugin.MyConfig.UseModifier || GameInput.GetKey(CyclopPitchStrafePlugin.MyConfig.StrafeModifierKey);

        private static bool _StrafeLeft => GameInput.GetKey(CyclopPitchStrafePlugin.MyConfig.StrafeLeftKey);
        private static bool _StrafeRight => GameInput.GetKey(CyclopPitchStrafePlugin.MyConfig.StrafeRightKey);
        private static bool _PitchUp => GameInput.GetKey(CyclopPitchStrafePlugin.MyConfig.PitchUpKey);
        private static bool _PitchDown => GameInput.GetKey(CyclopPitchStrafePlugin.MyConfig.PitchDowntKey);



        public static bool StrafeLeft => _Active && _StrafeLeft;
        public static bool StrafeRight => _Active && _StrafeRight;
        public static bool PitchUp => _Active && _PitchUp;
        public static bool PitchDown => _Active && _PitchDown;

        public static bool Stabilizer => GameInput.GetKey(CyclopPitchStrafePlugin.MyConfig.Stabilizer);

        public static bool CancelMovement => _Active && CyclopPitchStrafePlugin.MyConfig.ModifierCancel;

    }
}
