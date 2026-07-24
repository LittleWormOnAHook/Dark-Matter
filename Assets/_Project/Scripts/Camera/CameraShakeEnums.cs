using UnityEngine;

namespace Project.CameraFx
{
    /// <summary>
    /// How an emitter feeds trauma into <see cref="CameraShakeService"/>.
    /// </summary>
    public enum CameraShakePattern
    {
        /// <summary>Single burst on Play / OnEnable / trigger.</summary>
        OneShot,
        /// <summary>While running, holds a sustained trauma floor (rumble / vibration).</summary>
        Continuous,
        /// <summary>Repeating bursts at an interval (pulse / aftershock).</summary>
        Pulse
    }

    public enum CameraShakeEmitterMode
    {
        Manual,
        OnEnable,
        OnTriggerEnter,
        /// <summary>Runs Continuous/Pulse while a player is inside a trigger volume.</summary>
        WhileInsideTrigger
    }
}
