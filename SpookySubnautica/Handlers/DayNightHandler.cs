using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SpookySubnautica.Handlers
{
    internal class DayNightHandler
    {
        public static bool isDay = false;
        static bool isDayFirstSet = false;

        static float timeBetweenEffectsDay = 60 * 2;
        static float minTimeBetweenEffectsNight = 30f;

        static float sunsetTime = 0.84f;
        static float sunriseTime = 0.16f;

        static float daySpeed = 1f;
        static float nightSpeed = 0.5f;

        static FieldInfo _dayNightSpeedField = typeof(DayNightCycle)
            .GetField("_dayNightSpeed", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void Update()
        {
            if (DayNightCycle.main == null) { return; }

            bool dayBefore = isDay;
            float dayScalar = DayNightCycle.main.GetDayScalar();
            isDay = dayScalar >= sunriseTime && dayScalar <= sunsetTime;

            // Prevent the PDA from announcing the day on the loading screen
            if (!isDayFirstSet)
            {
                _dayNightSpeedField.SetValue(DayNightCycle.main, daySpeed);
                Mod.minTimeBetweenEffects = timeBetweenEffectsDay;
                isDayFirstSet = true;
                return;
            }

            if (dayBefore && !isDay)
            {
                Mod.minTimeBetweenEffects = minTimeBetweenEffectsNight;
                PDAHandler.PlayDuskApproaching();
                _dayNightSpeedField.SetValue(DayNightCycle.main, nightSpeed);
            }
            else if (!dayBefore && isDay)
            {
                Mod.minTimeBetweenEffects = timeBetweenEffectsDay;
                PDAHandler.PlayDawnApproaching();
                _dayNightSpeedField.SetValue(DayNightCycle.main, daySpeed);
            }
        }
    }
}
