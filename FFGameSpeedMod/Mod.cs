﻿using MelonLoader;
using System.Linq;
using UnhollowerBaseLib;
using UnityEngine;
using UnityEngine.UI;

namespace FFGameSpeedMod
{
    public class Mod : MelonMod
    {
        /// <summary>
        /// Internal name for the <see cref="TimeManager.timeScales"/> property.
        /// </summary>
        private const string _timeScalesPropertyName = "timeScales";

        /// <summary>
        /// Internal name for the <see cref="TimeManager.highestVisibleTimeScaleIndex"/> property.
        /// </summary>
        private const string _highestVisibleTimeScaleIndex = "highestVisibleTimeScaleIndex";

        /// <summary>
        /// Timescales to be either added or unlocked.
        /// </summary>
        private readonly float[] _customTimeScales = new float[]
        {
            5,  // Default but locked
            10, // Default but locked
            15, // Custom
            20, // Custom
            30, // Custom
            50  // Custom
        };

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
        /// <see cref="Color"/> values relative to the time scales defined in <see cref="_customTimeScales"/>.
        /// Keys from 1 to 4 cover the default cases bundled with vanilla game.
        /// </summary>
        private readonly Color[] _customTimeScalesColors = new Color[] {
            Color.yellow,   // Default but locked
            Color.yellow,   // Default but locked
            Color.magenta,  // Custom
            Color.magenta,  // Custom
            Color.red,      // Custom
            Color.red       // Custom
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
        private TimeManager TimeManager;

        /// <summary>
        /// Reference to the <see cref="Text"/> instance containing current game speed.
        /// </summary>
        private Text TimeScaleText;

        /// <summary>
        /// Reference to the default <see cref="Color"/> for the current <see cref="TimeScaleText"/>.
        /// </summary>
        private Color TimeScaleColor;

        /// <summary>
        /// Reference to the <see cref="TimeManager.timeScales"/> array which contains all the available multipliers.
        /// </summary>
        private float[] TimeScales;

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
                .FirstOrDefault(p => p.Name == _timeScalesPropertyName)
                .GetValue(TimeManager);

            // Update the allowed time scales by adding custom ones
            TimeManager
                .GetType()
                .GetProperties()
                .FirstOrDefault(p => p.Name == _timeScalesPropertyName)
                .SetValue(TimeManager, (Il2CppStructArray<float>)TimeScales.Union(_customTimeScales).ToArray());

            // Update with the added scales for logging purposes
            TimeScales = (Il2CppStructArray<float>)TimeManager
                .GetType()
                .GetProperties()
                .FirstOrDefault(p => p.Name == _timeScalesPropertyName)
                .GetValue(TimeManager);

            // Setting this means that all the time scales are available
            // This defaults to 3, which sets up to 4 different speeds
            TimeManager
                .GetType()
                .GetProperties()
                .FirstOrDefault(p => p.Name == _highestVisibleTimeScaleIndex)
                .SetValue(TimeManager, (uint)TimeScales.Length - 1);

            LoggerInstance.Msg($"Updated available tims scales with following values: {string.Join(", ", TimeScales)}");

            // Retrieve the UI references
            var gameSpeedText = GameObject.Find("Game Speed Text");
            if (gameSpeedText != null)
            {
                TimeScaleText = gameSpeedText.GetComponent<Text>();
                TimeScaleColor = TimeScaleText.color;
            }

            // Set this to true to prevent reloading everytime a button is pressed
            // TODO: set this to false and destroy everything if we're back to main menu
            _isLoaded = true;
        }

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

                    TimeManager.SetTimeScale((uint)timeScaleIndex, true);
                    LoggerInstance.Msg($"New value for time scale is {TimeManager.GetTimeScale()}");

                    // Update text color also
                    if (TimeScaleText != null)
                    {
                        // TODO: scale with yellow (first 2), orange (second 2), red (last 2) custom colors
                        // Array di colori e si prende il colore in base al valore di i
                        TimeScaleText.color = _customTimeScalesColors[i];
                    }
                }
            }
        }
    }
}
