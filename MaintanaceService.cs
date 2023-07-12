using BepInEx.Bootstrap;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ServerRestart
{
    class MaintanaceService : MonoBehaviour
    {
        private string _maintanaceFilePath;

        private void Awake()
        {
            var serverCharacters = Chainloader.PluginInfos.Values
                .FirstOrDefault(p => p.Metadata.GUID == "org.bepinex.plugins.servercharacters");

            if (serverCharacters == null)
            {
                Log.Error("Cannot enable maintanace mod. ServerCharacters mod not found!");
                enabled = false;
                return;
            }

            var serverCharactersFolder = Path.GetDirectoryName(serverCharacters.Location);
            Log.Debug($"ServerCharacters mod found at {serverCharactersFolder}");
            _maintanaceFilePath = Path.Combine(serverCharactersFolder, "maintenance");
            RemoveMaintanace();
        }

        private void OnEnable()
        {
            RestartService.OnScheduledRestartChanged += ScheduleMaintanace;
        }

        private void OnDisable()
        {
            RestartService.OnScheduledRestartChanged -= ScheduleMaintanace;
        }

        private void ScheduleMaintanace(DateTime date)
        {
            if (!Plugin.EnableMaintenance.Value) return;

            StopAllCoroutines();
            RemoveMaintanace();
            if (date == default) return;

            StartCoroutine(ScheduleMaintenance(date.Subtract(TimeSpan.FromMinutes(Plugin.MaintenanceMinutes.Value))));
        }

        private void RemoveMaintanace()
        {
            if (!File.Exists(_maintanaceFilePath)) return;

            File.Delete(_maintanaceFilePath);
            Log.Info("Maintenance disabled");
        }

        private IEnumerator ScheduleMaintenance(DateTime date)
        {
            yield return new WaitUntil(() => DateTime.UtcNow >= date);
            using (File.Create(_maintanaceFilePath)) { }
            Log.Info("Maintenance started");
        }
    }
}
