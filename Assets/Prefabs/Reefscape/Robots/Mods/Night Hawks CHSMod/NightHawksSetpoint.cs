using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.TestingMod._614
{
    [CreateAssetMenu(fileName = "Setpoint", menuName = "Robot/Night Hawks Setpoint", order = 0)]
    public class NightHawksSetpoint : ScriptableObject
    {
        [Tooltip("Inches")] public float elevatorHeight;
        [Tooltip("Degrees")] public float armAngle;
        [Tooltip("Degrees")] public float intakeAngle;
    }
}