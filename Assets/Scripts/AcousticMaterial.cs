using UnityEngine;

[CreateAssetMenu(menuName = "Acoustics/AcousticMaterial")]
public class AcousticMaterial : ScriptableObject
{
    [Header("Frequency Absorption Coefficients")]
    [Range(0, 1)] public float lowFrequencyAbsorption = 0.1f;
    [Range(0, 1)] public float midFrequencyAbsorption = 0.3f;
    [Range(0, 1)] public float highFrequencyAbsorption = 0.5f;

    [Header("Scattering Coefficient")]
    [Range(0, 1)] public float scatteringCoefficient = 0.1f;

    [Header("Reflectivity")]
    public bool isReflective; // If true, the surface strongly reflects sound.
}
