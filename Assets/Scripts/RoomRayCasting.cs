using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class RoomRaycasting : MonoBehaviour
{
    [Header("Raycasting Parameters")]
    public float rayLength = 50f;
    public int numberOfRays = 500;
    public int maxReflections = 15;
    public float speedOfSound = 343f;
    public float initialEnergy = 1.0f;
    public bool logToFile = true;
    public float[] octaveFrequencies = {125, 250, 500, 1000, 2000, 4000};

    private List<string> rayData = new List<string>();
    private GameObject soundSource;
    private GameObject receiver;

    void Start()
    {
        soundSource = GameObject.FindGameObjectWithTag("SoundSource");
        receiver = GameObject.FindGameObjectWithTag("Receiver");

        if (soundSource != null && receiver != null)
        {
            Debug.Log("🔊 Sound Source and Receiver found. Starting Raycasting...");
            EmitRays();
            if (logToFile) SaveDataToFile();
        }
        else
        {
            Debug.LogError("❌ Sound Source or Receiver not assigned in the scene!");
        }
    }

    void EmitRays()
    {
        for (int i = 0; i < numberOfRays; i++)
        {
            Vector3 randomDirection = Random.onUnitSphere;
            Ray ray = new Ray(soundSource.transform.position, randomDirection);
            RaycastHit hit;

            float totalTravelTime = 0f;
            float travelDistance = 0f;
            float energy = initialEnergy;

            for (int reflection = 0; reflection < maxReflections; reflection++)
            {
                if (Physics.Raycast(ray, out hit, rayLength))
                {
                    Debug.DrawLine(ray.origin, hit.point, Color.yellow, 5.0f);

                    float segmentDistance = Vector3.Distance(ray.origin, hit.point);
                    travelDistance += segmentDistance;
                    totalTravelTime = travelDistance / speedOfSound;

                    if (hit.collider.CompareTag("Receiver"))
                    {
                        Debug.DrawLine(ray.origin, hit.point, Color.green, 5.0f);
                        SaveRayData(reflection, hit.point, totalTravelTime, energy);
                        break;
                    }

                    AcousticMaterialHolder materialHolder = hit.collider.GetComponent<AcousticMaterialHolder>();
                    if (materialHolder != null && materialHolder.acousticMaterial != null)
                    {
                        float frequency = octaveFrequencies[Random.Range(0, octaveFrequencies.Length)];
                        float absorption = materialHolder.acousticMaterial.absorptionCoefficients[GetOctaveBandIndex(frequency)];
                        energy *= Mathf.Exp(-absorption * (reflection + 1) * 0.8f);

                        Debug.Log($"🔄 Reflection {reflection} at {hit.collider.name} | Frequency: {frequency} Hz | Absorption: {absorption:F2} | Energy: {energy:F2}");

                        if (energy < 0.005f) break;

                        // Add realistic scattering to reflections
                        Vector3 reflectionDirection = Vector3.Reflect(ray.direction, hit.normal);
                        Vector3 scatter = Random.onUnitSphere * 0.3f; // Scattering effect
                        ray = new Ray(hit.point, (reflectionDirection + scatter).normalized);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }
    }

    int GetOctaveBandIndex(float frequency)
    {
        if (frequency <= 125) return 0;
        if (frequency <= 250) return 1;
        if (frequency <= 500) return 2;
        if (frequency <= 1000) return 3;
        if (frequency <= 2000) return 4;
        return 5; // 4000 Hz
    }

    void SaveRayData(int reflection, Vector3 hitPoint, float travelTime, float energy)
    {
        rayData.Add($"{reflection}, {hitPoint.x}, {hitPoint.y}, {hitPoint.z}, {travelTime:F4}, {energy:F4}");
    }

    void SaveDataToFile()
    {
        string filePath = Application.dataPath + "/RayData.txt";
        File.WriteAllLines(filePath, rayData);
        Debug.Log($"📁 Ray data saved to: {filePath}");
    }
}
