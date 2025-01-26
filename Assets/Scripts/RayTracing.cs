using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class RayTracing : MonoBehaviour
{
    [Header("Ray Tracing Parameters")]
    public float rayLength = 50f;
    public int numberOfRays = 500;  // Increased for more accurate reflections
    public int maxReflections = 35; // Allow more bounces for room acoustics
    public float speedOfSound = 343f;  // Speed of sound in air (m/s)
    public float initialEnergy = 2.0f;
    public bool logToFile = true;

    private List<string> rayData = new List<string>();

    void Start()
    {
        EmitRaysFromSources();
        if (logToFile)
            SaveDataToFile();
    }

    void EmitRaysFromSources()
    {
        GameObject[] sources = GameObject.FindGameObjectsWithTag("SoundSource");
        GameObject[] receivers = GameObject.FindGameObjectsWithTag("Receiver");

        Debug.Log($"🔍 Found {sources.Length} Sound Sources and {receivers.Length} Receivers.");

        foreach (GameObject source in sources)
        {
            foreach (GameObject receiver in receivers)
            {
                string roomType = GetRoomType(source.transform.position, receiver.transform.position);
                Debug.Log($"📌 Room Classification: {roomType} for Ray {source.name} → {receiver.name}");

                EmitRaysFromSourceToReceiver(source.transform.position, receiver.transform.position, roomType);
            }
        }
    }

    void EmitRaysFromSourceToReceiver(Vector3 sourcePosition, Vector3 receiverPosition, string roomType)
    {
        if (roomType == "Unknown Room")
        {
            Debug.LogWarning($"⚠️ Skipping Unclassified Room - Source: {sourcePosition}, Receiver: {receiverPosition}");
            return;
        }

        for (int i = 0; i < numberOfRays; i++)
        {
            Vector3 randomDirection = Random.onUnitSphere;
            Ray ray = new Ray(sourcePosition, randomDirection);
            RaycastHit hit;

            float totalTravelTime = 0f;
            float travelDistance = 0f;
            float energy = initialEnergy;
            bool rayHitSomething = false;

            for (int reflection = 0; reflection < maxReflections; reflection++)
            {
                if (Physics.Raycast(ray, out hit, rayLength))
                {
                    rayHitSomething = true;
                    float segmentDistance = Vector3.Distance(ray.origin, hit.point);
                    travelDistance += segmentDistance;
                    totalTravelTime = travelDistance / speedOfSound;

                    if (hit.collider.CompareTag("Receiver"))
                    {
                        SaveRayData(roomType, sourcePosition, receiverPosition, reflection, hit.point, totalTravelTime, energy);
                        break;
                    }

                    AcousticMaterialHolder materialHolder = hit.collider.GetComponent<AcousticMaterialHolder>();
                    if (materialHolder != null && materialHolder.acousticMaterial != null)
                    {
                        AcousticMaterial material = materialHolder.acousticMaterial;
                        float absorption = GetFrequencyAbsorption(material, reflection);

                        // 📌 Improved exponential energy decay model
                        float absorptionFactor = Mathf.Lerp(1.0f, 4.0f, absorption);
                        energy *= Mathf.Exp(-absorptionFactor * (reflection + 1) * 0.8f);

                        if (energy < 0.002f) break; // Stop if energy is too low

                        SaveRayData(roomType, sourcePosition, receiverPosition, reflection, hit.point, totalTravelTime, energy);

                        if (Random.value < material.scatteringCoefficient)
                        {
                            ray = new Ray(hit.point, Random.onUnitSphere);
                        }
                        else if (material.isReflective)
                        {
                            ray = new Ray(hit.point, Vector3.Reflect(ray.direction, hit.normal));
                        }
                        else break;
                    }
                    else break;
                }
            }
        }
    }

    float GetFrequencyAbsorption(AcousticMaterial material, int reflection)
    {
        if (reflection < 5) return material.lowFrequencyAbsorption;
        if (reflection < 15) return material.midFrequencyAbsorption;
        return material.highFrequencyAbsorption;
    }

    void SaveRayData(string roomType, Vector3 sourcePos, Vector3 receiverPos, int reflection, Vector3 hitPoint, float travelTime, float energy)
    {
        rayData.Add($"{roomType}, {sourcePos.x}, {sourcePos.y}, {sourcePos.z}, " +
                    $"{receiverPos.x}, {receiverPos.y}, {receiverPos.z}, " +
                    $"{reflection}, {hitPoint.x}, {hitPoint.y}, {hitPoint.z}, {travelTime:F4}, {energy:F4}");
    }

    string GetRoomType(Vector3 source, Vector3 receiver)
    {
        if (source.x < 5 && receiver.x < 5) return "Empty Room";
        if (source.x > 10 && receiver.x > 10) return "Furnished Room";
        if (source.z < -5 && receiver.z < -5) return "Treated Room";
        if (source.x < 10 && receiver.z > 0) return "Large Untreated Room";

        Debug.LogWarning($"⚠️ UNKNOWN ROOM DETECTED - Source: {source}, Receiver: {receiver}");
        return "Unknown Room";
    }

    void SaveDataToFile()
    {
        string filePath = Application.dataPath + "/RayData.txt";
        File.WriteAllLines(filePath, rayData);
        Debug.Log($"📁 Ray data saved to: {filePath}");
    }
}
