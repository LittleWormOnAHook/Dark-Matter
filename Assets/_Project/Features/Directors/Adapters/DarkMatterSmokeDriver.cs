using Project.Features.Directors;
using Project.Features.Validation;
using Project.Features.WorldState;
using Project.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Features.Directors.Adapters
{
    /// <summary>Play Mode smoke: F9 WorldState · F10 Directors · F11 cycle storm phase.</summary>
    public sealed class DarkMatterSmokeDriver : MonoBehaviour
    {
        private void Update()
        {
            if (!Application.isPlaying)
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.f9Key.wasPressedThisFrame)
                SmokeWorldState();
            else if (keyboard.f10Key.wasPressedThisFrame)
                SmokeDirectors();
            else if (keyboard.f11Key.wasPressedThisFrame)
                SmokeWeather();
        }

        private static void SmokeWorldState()
        {
            WorldStateService world = WorldStateService.Instance;
            if (world == null)
            {
                Debug.LogWarning("[WorldState] F9 smoke — service not bootstrapped.");
                return;
            }

            Debug.Log(world.GetSnapshot().ToOneLineSummary() + " key=" + DarkMatterSmokeKeys.WorldStateSummary);
        }

        private static void SmokeDirectors()
        {
            DirectorOrchestrator orch = DirectorOrchestrator.Instance;
            if (orch == null)
            {
                Debug.LogWarning("[Directors] F10 smoke — orchestrator not bootstrapped.");
                return;
            }

            orch.Evaluate(DirectorTrigger.ManualDebug);
        }

        private static void SmokeWeather()
        {
            WeatherCommandServiceAdapter weather = DirectorsBootstrap.WeatherCommands;
            if (weather == null)
            {
                Debug.LogWarning("[Directors] F11 smoke — weather commands not bootstrapped.");
                return;
            }

            StormPhase next = WeatherDirectorService.ResolveNextPhase(weather.CurrentPhase);
            weather.SetStormPhase(next);

            DirectorOrchestrator.Instance?.Evaluate(DirectorTrigger.StormPhaseChanged);
            Debug.Log("[Directors] F11 weather phase=" + next + " crisis=" + EnvironmentalCrisisHudMode.IsCrisisActive);
        }
    }
}
