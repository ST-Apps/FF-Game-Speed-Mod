using HarmonyLib;
using MelonLoader;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnhollowerBaseLib;
using UnityEngine;
using UnityEngine.UI;

namespace FFGameSpeedMod
{
    public class Mod : MelonMod
    {
        #region Private Fields

        /// <summary>
        /// <see cref="KeyCode"/> values relative to the time scales defined in <see cref="_customTimeScales"/>.
        /// Keys from 1 to 4 cover the default cases bundled with vanilla game.
        /// </summary>
        private readonly KeyCode[] _customTimeScalesKeyCodes = new KeyCode[] {
            KeyCode.Alpha5, // Default but locked
            KeyCode.Alpha6, // Default but locked
            KeyCode.Alpha7, // Custom
            KeyCode.Alpha8, // Custom
            KeyCode.Alpha9, // Custom
            KeyCode.Alpha0  // Custom
        };

        /// <summary>
        /// Methods to be patched in <see cref="TimeManager"/>.
        /// </summary>
        private readonly string[] _timeManagerPatchedMethods = new[]
        {
            nameof(TimeManager.SetTimeScale),
            nameof(TimeManager.IncreaseTimeScale),
            nameof(TimeManager.DecreaseTimeScale)
        };

        /// <summary>
        /// Reference to the <see cref="TimeManager.timeScales"/> array which contains all the available multipliers.
        /// </summary>
        private float[] TimeScales;

        #endregion

        #region Static Fields

        /// <summary>
        /// Timescales to be either added or unlocked, alongside their relative <see cref="Color"/> value.
        /// Keys from 1 to 4 cover the default cases bundled with vanilla game.
        /// 
        /// Palette generated at <see href="https://coolors.co/gradient-palette/ffea00-ff4d00?number=6">
        /// </summary>
        private static readonly SortedDictionary<float, Color> _customTimeScales = new SortedDictionary<float, Color>
        {
            { 5, new Color32(255, 234, 0, 255) },  // Default but locked
            { 10, new Color32(255, 203, 0, 255) }, // Default but locked
            { 15, new Color32(255, 171, 0, 255) }, // Custom
            { 20, new Color32(255, 140, 0, 255) }, // Custom
            { 30, new Color32(255, 108, 0, 255) }, // Custom
            { 50, new Color32(255, 77, 0, 255) }   // Custom
        };

        /// <summary>
        /// Prevent reloading custom time scales multiple times.
        /// 
        /// TODO: Find a way to run <see cref="InitTimeScales"/> only on game load using <see cref="MelonLoader"/>.
        /// </summary>
        private static bool _isLoaded;

        /// <summary>
        /// Reference to the <see cref="TimerManager"/> instance for the current game.
        /// </summary>
        private static TimeManager TimeManager;

        /// <summary>
        /// Reference to the <see cref="Text"/> instance containing current game speed.
        /// </summary>
        private static Text TimeScaleText;

        /// <summary>
        /// Reference to the default <see cref="Color"/> for the current <see cref="TimeScaleText"/>.
        /// </summary>
        private static Color TimeScaleColor;

        #endregion


        #region Initializers

        /// <summary>
        /// Adds the custom time scales defined in <see cref="_customTimeScales"/>.
        /// </summary>
        private void InitTimeScales()
        {
            if (_isLoaded) return;

            LoggerInstance.Msg($"Setting up extended timescales...");

            TimeManager = UnitySingleton<GameManager>.Instance?.timeManager;
            if (TimeManager == null)
            {
                LoggerInstance.Msg("TimeManager is null, aborting...");
                return;
            }

            // Retrieve current values for time scales
            TimeScales = (Il2CppStructArray<float>)TimeManager
                .GetType()
                .GetProperties()
                .FirstOrDefault(p => p.Name == nameof(TimeManager.timeScales))
                .GetValue(TimeManager);

            // Update the allowed time scales by adding custom ones
            TimeManager
                .GetType()
                .GetProperties()
                .FirstOrDefault(p => p.Name == nameof(TimeManager.timeScales))
                .SetValue(TimeManager, (Il2CppStructArray<float>)TimeScales.Union(_customTimeScales.Keys).ToArray());

            // Update with the added scales for logging purposes
            TimeScales = (Il2CppStructArray<float>)TimeManager
                .GetType()
                .GetProperties()
                .FirstOrDefault(p => p.Name == nameof(TimeManager.timeScales))
                .GetValue(TimeManager);

            // Setting this means that all the time scales are available
            // This defaults to 3, which sets up to 4 different speeds
            TimeManager
                .GetType()
                .GetProperties()
                .FirstOrDefault(p => p.Name == nameof(TimeManager.highestVisibleTimeScaleIndex))
                .SetValue(TimeManager, (uint)TimeScales.Length - 1);

            LoggerInstance.Msg($"Updated available tims scales with following values: {string.Join(", ", TimeScales)}");

            // Retrieve the UI references
            var gameSpeedText = GameObject.Find("Game Speed Text");
            if (gameSpeedText != null)
            {
                TimeScaleText = gameSpeedText.GetComponent<Text>();
                TimeScaleColor = TimeScaleText.color;
            }

            // Finally we can patch the SetTimeScale method
            LoggerInstance.Msg($"Patching TimeManager methods...");
            PatchTimeManager();
            LoggerInstance.Msg($"Patched TimeManager methods");

            // Set this to true to prevent reloading everytime a button is pressed
            // TODO: set this to false and destroy everything if we're back to main menu
            _isLoaded = true;
        }

        /// <summary>
        /// Patches <see cref="TimeManager"/> methods to add our custom postfix <see cref="UpdateGameSpeedText"/>.
        /// </summary>
        private void PatchTimeManager()
        {
            // Find MethodInfo objects for all the methods that require patching
            var patchedMethodInfos = _timeManagerPatchedMethods.Select(m =>
                TimeManager
                    .GetType()
                    .GetMethod(m)
                );

            // Find the target postfix method for our patches
            var timeManagerPostfixMethod = typeof(Mod).GetMethod(nameof(UpdateGameSpeedText), BindingFlags.Static | BindingFlags.NonPublic);
            LoggerInstance.Msg($"Found postfix method for TimeManager: {timeManagerPostfixMethod}");

            // Apply the patch to all the methods
            foreach (var patchedMethodInfo in patchedMethodInfos)
            {
                LoggerInstance.Msg($"Patching method: {patchedMethodInfo.Name}");
                // Prefix and finalizer have values, however the postfix does not.
                HarmonyInstance.Patch(patchedMethodInfo, postfix: new HarmonyMethod(timeManagerPostfixMethod));
            }
        }

        #endregion

        #region Melon Mod

        /// <summary>
        /// Dynamically deal with custom time scales by setting the one associated to the pressed key.
        /// </summary>
        public override void OnUpdate()
        {
            // Check if any of the managed KeyCodes is being pressed
            for (var i = 0; i < _customTimeScalesKeyCodes.Length; i++)
            {
                var keyCode = _customTimeScalesKeyCodes[i];
                if (Input.GetKeyDown(keyCode))
                {
                    // We need to init our custom time scales
                    InitTimeScales();

                    // Now we're ready to set the time scale value based on the pressed keycode.
                    // It's important to remember that keys from 1 to 4 are already taken!
                    // This means that our array index will be current key + 4
                    var timeScaleIndex = i + 4;
                    LoggerInstance.Msg($"Setting time scale index to {timeScaleIndex} with value {TimeScales[timeScaleIndex]}");

                    TimeManager.SetTimeScale((uint)timeScaleIndex);
                    LoggerInstance.Msg($"New value for time scale is {TimeManager.GetTimeScale()}");
                }
            }
        }

        #endregion

        #region Harmony Patch

        /// <summary>
        /// This updates <see cref="TimeScaleText"/>'s color based on the selected time scale index.
        /// Time scales included with the base game will retain their original color, custom ones will be mapped using <see cref="_customTimeScalesColors"/>.
        /// </summary>
        static void UpdateGameSpeedText()
        {
            if (TimeScaleText != null)
            {
                // We need to use reflection for this because otherwise the field is not there, and I'm too tired to understand why
                // var timeScaleIndex = TimeManager.inde(uint) TimeManager.GetType().GetField(nameof(TimeManager.timeScaleIndex)).GetValue(TimeManager);

                // This is the case for the time scales included with the base game
                if (TimeManager.GetTimeScale() < _customTimeScales.Keys.First())
                {
                    TimeScaleText.color = TimeScaleColor;
                    return;
                }

                // For custom ones we'll use the provided mapping
                // Keep in mind that timeScaleIndex refers to an annray containing both the default and custom time scales.
                // This means that the real index will be timeScaleIndex - 4
                TimeScaleText.color = _customTimeScales[TimeManager.GetTimeScale()];
            }
        }

        #endregion
    }
}
