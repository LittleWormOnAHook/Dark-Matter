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
        }

        public static void ResetToNewGame()
        {
            DMIoClockProfile profile = DMIoClockProfile.Live;
            Day = profile != null ? Mathf.Max(1, profile.startDay) : 1;
            Hour = profile != null ? Mathf.Clamp(profile.startHour, 0, HoursPerDay - 1) : 6;
            Minute = profile != null ? Mathf.Clamp(profile.startMinute, 0, 59) : 0;
            Second = 0;
            paintAccum = 0f;
            RebuildDisplay();
            OnPainted?.Invoke();
        }

        public static void ApplySave(int day, int hour, int minute)
        {
            Day = Mathf.Max(1, day);
            Hour = Mathf.Clamp(hour, 0, HoursPerDay - 1);
            Minute = Mathf.Clamp(minute, 0, 59);
            Second = 0;
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

            paintAccum += deltaTime;
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
