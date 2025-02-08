using UnityEngine;

[System.Serializable]
public class AcousticMaterial : ScriptableObject
{
    public string materialName;
    
    // Absorption coefficients for octave bands (125Hz, 250Hz, 500Hz, 1000Hz, 2000Hz, 4000Hz)
    public float[] absorptionCoefficients = new float[6];
    
    // Scattering coefficient (0 = no scattering, 1 = full diffusion)
    public float scatteringCoefficient = 0.5f;
    
    // Reflectivity flag for highly reflective surfaces
    public bool isReflective = false;
}
