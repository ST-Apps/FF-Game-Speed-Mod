using MelonLoader;
using System.Collections.Generic;
using System.Linq;
using UnhollowerBaseLib;
using UnityEngine;

namespace FFGameSpeedMod
{
    public class Mod : MelonMod
    {
        /// <summary>
        /// Internal name for the <see cref="TimeManager.timeScales"/> property.
        /// </summary>
        private const string _timeScalesPropertyName = "timeScales";

        /// <summary>
        /// Timescales to be either added or unlocked, alongside their respective <see cref="KeyCode"/> value.
        /// Keys from 1 to 4 cover the default cases bundled with vanilla game.
        /// </summary>
        private readonly Dictionary<KeyCode, float> _customTimeScales = new Dictionary<KeyCode, float> {
            { KeyCode.Alpha5, 5 },  // Default but locked
            { KeyCode.Alpha6, 10 }, // Default but locked
            { KeyCode.Alpha7, 15 }, // Custom
            { KeyCode.Alpha8, 20 }, // Custom
            { KeyCode.Alpha9, 30 }, // Custom
            { KeyCode.Alpha0, 50 }  // Custom
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
        private TimeManager TimeManager => GameObject.Find("GameManager")?.GetComponent<GameManager>()?.timeManager;

        /// <summary>
        /// Reference to the <see cref="TimeManager.timeScales"/> array which contains all the available multipliers.
        /// </summary>
        private float[] TimeScales
        {
            get => (Il2CppStructArray<float>)TimeManager
                .GetType()
                .GetProperties()
                .FirstOrDefault(p => p.Name == _timeScalesPropertyName)
                .GetValue(TimeManager);

            set => TimeManager
                .GetType()
                .GetProperties()
                .FirstOrDefault(p => p.Name == _timeScalesPropertyName)
                .SetValue(TimeManager, (Il2CppStructArray<float>)value);
        }

        /// <summary>
        /// Adds the custom time scales defined in <see cref="_customTimeScales"/>.
        /// </summary>
        private void InitTimeScales()
        {
            if (_isLoaded) return;

            LoggerInstance.Msg($"Setting up extended timescales...");

            if (TimeManager == null)
            {
                LoggerInstance.Msg("TimeManager is null, aborting...");
                return;
            }

            TimeScales = TimeScales.Union(_customTimeScales.Values).ToArray();
            _isLoaded = true;
        }

        /// <summary>
        /// Dynamically deal with custom time scales by setting the one associated to the pressed key.
        /// </summary>
        public override void OnUpdate()
        {
            // Check if any of the managed KeyCodes is being pressed
            for (var i=0; i<_customTimeScales.Count; i++)
            {
                var keyCode = _customTimeScales.Keys.ToArray()[i];
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
                }
            }
        }
    }
}
