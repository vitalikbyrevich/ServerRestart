using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ServerRestart
{
    public class RestartService : MonoBehaviour
    {
        public DateTime NextRestartDate { get; private set; }

        public static event Action<string> OnRestartMessageSent;

        public bool RestartStarted { get; private set; }

        private void Awake()
        {
            InvokeRepeating(nameof(PrintScheduledRestartTime), 60, 60);
        }

        private void Start()
        {
            ScheduleNextRestart();
        }

        private void PrintScheduledRestartTime()
        {
            var currentTime = DateTime.UtcNow;
            var timeLeft = NextRestartDate - currentTime;
            if (NextRestartDate.Ticks > 0)
                Log.Message($"Next restart {NextRestartDate}. Time left: {timeLeft}");
            else
                Log.Message("No scheduled restarts");
        }

        public void ScheduleNextRestart()
        {
            StopAllCoroutines();
            RestartStarted = false;

            var schedule = Plugin.RestartTimes.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (schedule.Length == 0)
            {
                NextRestartDate = DateTime.MinValue;
                Log.Message("Restart schedule is empty");
                return;
            }
            NextRestartDate = GetNextRestartDate(schedule);
            Log.Message($"Next restart scheduled at {NextRestartDate}. Time left {NextRestartDate - DateTime.UtcNow}");

            StartCoroutine(ScheduleRestart(NextRestartDate));
            StartCoroutine(ScheduleMessage(NextRestartDate.Subtract(TimeSpan.FromMinutes(1)), Plugin.Message1Min.Value));
            StartCoroutine(ScheduleMessage(NextRestartDate.Subtract(TimeSpan.FromMinutes(10)), Plugin.Message10Mins.Value));
            StartCoroutine(ScheduleMessage(NextRestartDate.Subtract(TimeSpan.FromHours(1)), Plugin.Message1Hour.Value));
        }

        private IEnumerator ScheduleRestart(DateTime date)
        {
            yield return new WaitUntil(() => DateTime.UtcNow >= date);

            Log.Message("Starting restart. Disconnecting players");
            ZNet.instance.SendDisconnect();
            RestartStarted = true;
            yield return new WaitWhile(() => ZNet.instance.GetPeers().Count > 0);
            yield return new WaitForSeconds(5);

            Log.Message("Shutting down server");
            Game.instance.Shutdown();

            yield return new WaitForSeconds(10);

            Log.Message("Stopping server");
            Application.Quit();
        }

        private IEnumerator ScheduleMessage(DateTime date, string message)
        {
            if (DateTime.UtcNow > date)
                yield break;

            Log.Debug($"Schedule message '{message}' at {date}");
            yield return new WaitUntil(() => DateTime.UtcNow >= date);
            Log.Debug($"Sending message '{message}'");
            SendMessageToAll(message);
            OnRestartMessageSent?.Invoke(message);
        }

        private void SendMessageToAll(string message)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ShowMessage", new object[]
            {
                (int)MessageHud.MessageType.Center,
                message
            });
        }

        private DateTime GetNextRestartDate(IEnumerable<string> schedule)
        {
            var nowDate = DateTime.UtcNow;
            var restartSchedule = schedule.Select(timeText =>
            {
                var time = TimeSpan.Parse(timeText);
                var date = new DateTime(nowDate.Year, nowDate.Month, nowDate.Day).Add(time);
                if (date < nowDate)
                {
                    date = date.AddDays(1);
                }
                return date;
            });
            var ordered = restartSchedule.OrderBy(date => date - nowDate);
            return ordered.FirstOrDefault();
        }
    }
}
