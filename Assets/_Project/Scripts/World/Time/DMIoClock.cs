using System;
using System.Text;
using Project.Core;
using UnityEngine;

namespace Project.World.Clock
{
    /// <summary>
    /// 30-hour Io day. One real hour = one Io day. Labels paint every real minute.
    /// </summary>
    public static class DMIoClock
    {
        public static int Day { get; private set; } = 1;
        public static int Hour { get; private set; } = 6;
        public static int Minute { get; private set; }
        public static int Second { get; private set; }

        public static bool IsAfternoon => Hour >= 15;
        public static string DisplayText { get; private set; } = "Io Day 1 · 06:00:00 AM";

        public static event Action OnPainted;
        public static event Action OnDayRolled;

        private static float paintAccum;
        private static readonly StringBuilder Builder = new StringBuilder(40);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Day = 1;
            Hour = 6;
            Minute = 0;
            Second = 0;
            paintAccum = 0f;
            DisplayText = "Io Day 1 · 06:00:00 AM";
            OnPainted = null;
            OnDayRolled = null;
            TimeRestored = false;
            StartHourApplied = false;
            StartHourOverride = -1f;
        }

        // 2026-10-24ao: Start Hour (DM_IoSkyProfile.startHour). Set by the sky controller at Play start;
        // a save restore (ApplySave) always wins over it.
        public static bool TimeRestored { get; private set; }
        public static bool StartHourApplied { get; private set; }
        public static float StartHourOverride = -1f;

        public static void SetHourOfDay(float hour)
        {
            int hpd = HoursPerDay;
            hour = Mathf.Repeat(hour, hpd);
            int totalSec = Mathf.Clamp(Mathf.RoundToInt(hour * 3600f), 0, hpd * 3600 - 1);
            Hour = totalSec / 3600;
            Minute = (totalSec / 60) % 60;
            Second = totalSec % 60;
            paintAccum = 0f;
            RebuildDisplay();
            OnPainted?.Invoke();
        }

        public static void ApplyStartHourOnce(float hour)
        {
            StartHourOverride = hour;
            if (StartHourApplied || TimeRestored) return;
            StartHourApplied = true;
            SetHourOfDay(hour);
        }

        public static void ResetToNewGame()
        {
            DMIoClockProfile profile = DMIoClockProfile.Live;
            Day = profile != null ? Mathf.Max(1, profile.startDay) : 1;
            Hour = profile != null ? Mathf.Clamp(profile.startHour, 0, HoursPerDay - 1) : 6;
            Minute = profile != null ? Mathf.Clamp(profile.startMinute, 0, 59) : 0;
            Second = 0;
            paintAccum = 0f;
            TimeRestored = false;
            if (StartHourOverride >= 0f) { SetHourOfDay(StartHourOverride); return; }
            RebuildDisplay();
            OnPainted?.Invoke();
        }

        public static void ApplySave(int day, int hour, int minute)
        {
            Day = Mathf.Max(1, day);
            Hour = Mathf.Clamp(hour, 0, HoursPerDay - 1);
            Minute = Mathf.Clamp(minute, 0, 59);
            Second = 0;
            TimeRestored = true;
            paintAccum = 0f;
            RebuildDisplay();
            OnPainted?.Invoke();
        }

        public static void CaptureSave(out int day, out int hour, out int minute)
        {
            day = Day;
            hour = Hour;
            minute = Minute;
        }

        public static void Tick(float deltaTime)
        {
            if (!GameSession.HasStarted || deltaTime <= 0f)
                return;

            paintAccum += deltaTime * NightTimeScaleNow; // 25q: Night Time Scale (exactly 1 at default)
            float step = PaintSeconds;
            if (paintAccum < step)
                return;

            int steps = 0;
            while (paintAccum >= step && steps < 8)
            {
                paintAccum -= step;
                steps++;
                AddIoMinutes(MinutesPerPaint);
            }

            RebuildDisplay();
            OnPainted?.Invoke();
        }

        // 2026-10-24ai: continuous (smooth) clock for the sky. Same time as the stepped labels, plus the
        // sub-paint fraction, so the sun / eclipse / moons move every frame on the 30 h Io day.
        public static int HoursPerDayValue => HoursPerDay;

        /// <summary>25q: 0 = sun up, 1 = sun down (set by the sky controller from sun elevation).</summary>
        public static float NightFactor;
        public static float NightTimeScaleNow
        {
            get
            {
                DMIoClockProfile profile = DMIoClockProfile.Live;
                float s = profile != null ? Mathf.Clamp(profile.nightTimeScale, 0.1f, 5f) : 1f;
                return s == 1f ? 1f : Mathf.Lerp(1f, s, Mathf.Clamp01(NightFactor));
            }
        }

        public static float HourOfDayContinuous
        {
            get
            {
                DMIoClockProfile profile = DMIoClockProfile.Live;
                float realPerHour = profile != null ? Mathf.Max(1f, profile.realSecondsPerGameHour) : 120f;
                float h = Hour + Minute / 60f + Second / 3600f + paintAccum / realPerHour;
                int hpd = HoursPerDay;
                return h >= hpd ? h - hpd : h;
            }
        }

        public static float DayFraction => HourOfDayContinuous / HoursPerDay;

        public static double TotalHoursContinuous => (Day - 1) * (double)HoursPerDay + HourOfDayContinuous;

        private static int HoursPerDay
        {
            get
            {
                DMIoClockProfile profile = DMIoClockProfile.Live;
                return profile != null ? Mathf.Max(1, profile.hoursPerDay) : 30;
            }
        }

        private static float PaintSeconds
        {
            get
            {
                DMIoClockProfile profile = DMIoClockProfile.Live;
                return profile != null ? Mathf.Max(1f, profile.uiPaintRealSeconds) : 60f;
            }
        }

        private static int MinutesPerPaint
        {
            get
            {
                DMIoClockProfile profile = DMIoClockProfile.Live;
                float realPerHour = profile != null ? Mathf.Max(1f, profile.realSecondsPerGameHour) : 120f;
                float paint = PaintSeconds;
                return Mathf.Max(1, Mathf.RoundToInt(60f * (paint / realPerHour)));
            }
        }

        private static void AddIoMinutes(int minutes)
        {
            if (minutes <= 0)
                return;

            Minute += minutes;
            int hoursPerDay = HoursPerDay;
            while (Minute >= 60)
            {
                Minute -= 60;
                Hour++;
            }

            while (Hour >= hoursPerDay)
            {
                Hour -= hoursPerDay;
                Day++;
                OnDayRolled?.Invoke();
            }
        }

        private static void RebuildDisplay()
        {
            Builder.Length = 0;
            Builder.Append("Io Day ");
            Builder.Append(Day);
            Builder.Append(" · ");
            if (Hour < 10)
                Builder.Append('0');
            Builder.Append(Hour);
            Builder.Append(':');
            if (Minute < 10)
                Builder.Append('0');
            Builder.Append(Minute);
            Builder.Append(':');
            if (Second < 10)
                Builder.Append('0');
            Builder.Append(Second);
            Builder.Append(IsAfternoon ? " PM" : " AM");
            DisplayText = Builder.ToString();
        }
    }
}
