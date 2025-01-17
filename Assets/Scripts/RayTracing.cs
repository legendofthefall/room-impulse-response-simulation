using UnityEngine;
using System.Collections.Generic;

public class RayTracing : MonoBehaviour
{
    [Header("Ray Tracing Parameters")]
    public float rayLength = 50f;
    public int numberOfRays = 50;
    public int maxReflections = 5;
    public float speedOfSound = 343f;
    public float initialEnergy = 1.0f;
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

        foreach (GameObject source in sources)
        {
            foreach (GameObject receiver in receivers)
            {
                Debug.Log($"Tracing rays from {source.name} to {receiver.name}");
                EmitRaysFromSourceToReceiver(source.transform.position, receiver.transform.position);
            }
        }
    }

    void EmitRaysFromSourceToReceiver(Vector3 sourcePosition, Vector3 receiverPosition)
    {
        string roomType = GetRoomType(sourcePosition, receiverPosition);

        // ✅ Skip saving unknown or extra room categories
        if (roomType == "Unknown Room" || roomType.Contains("Extended") || roomType.Contains("Intermediate"))
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
            float energy = initialEnergy;

            for (int reflection = 0; reflection < maxReflections; reflection++)
            {
                if (Physics.Raycast(ray, out hit, rayLength))
                {
                    float travelDistance = Vector3.Distance(ray.origin, hit.point);
                    float travelTime = travelDistance / speedOfSound;
                    totalTravelTime += travelTime;

                    if (hit.collider.CompareTag("Receiver"))
                    {
                        Debug.DrawLine(ray.origin, hit.point, Color.green, 5.0f);
                        rayData.Add($"{roomType}, {sourcePosition.x}, {sourcePosition.y}, {sourcePosition.z}, {receiverPosition.x}, {receiverPosition.y}, {receiverPosition.z}, {reflection}, {hit.point.x}, {hit.point.y}, {hit.point.z}, {totalTravelTime:F4}, {energy:F2}");
                        break;
                    }

                    AcousticMaterialHolder materialHolder = hit.collider.GetComponent<AcousticMaterialHolder>();
                    if (materialHolder != null && materialHolder.acousticMaterial != null)
                    {
                        AcousticMaterial material = materialHolder.acousticMaterial;
                        float absorption = GetFrequencyAbsorption(material);
                        energy *= (1 - absorption);

                        if (energy < 0.01f)
                        {
                            Debug.Log("Ray energy too low, stopping reflection.");
                            break;
                        }

                        rayData.Add($"{roomType}, {sourcePosition.x}, {sourcePosition.y}, {sourcePosition.z}, {receiverPosition.x}, {receiverPosition.y}, {receiverPosition.z}, {reflection}, {hit.point.x}, {hit.point.y}, {hit.point.z}, {totalTravelTime:F4}, {energy:F2}");
                        Color rayColor = Color.Lerp(Color.yellow, Color.red, energy);
                        Debug.DrawLine(ray.origin, hit.point, rayColor, 5.0f);

                        if (Random.value < material.scatteringCoefficient)
                        {
                            Vector3 scatterDirection = Random.onUnitSphere;
                            ray = new Ray(hit.point, scatterDirection);
                        }
                        else if (material.isReflective)
                        {
                            Vector3 reflectionDirection = Vector3.Reflect(ray.direction, hit.normal);
                            ray = new Ray(hit.point, reflectionDirection);
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
                else
                {
                    Debug.Log("Ray escaped the room boundary.");
                    break;
                }
            }
        }
    }

    string GetRoomType(Vector3 source, Vector3 receiver)
    {
        // ✅ STRICTLY CLASSIFYING ONLY EXISTING ROOMS
        if (source.x < 5 && receiver.x < 5) return "Empty Room";
        if (source.x > 15 && receiver.x > 15) return "Furnished Room";
        if (source.z < -10 && receiver.z < -10) return "Treated Room";
        if (source.x < 10 && receiver.z > 0) return "Large Untreated Room";

        // ✅ Discard Unclassified Rooms
        Debug.LogWarning($"⚠️ UNKNOWN ROOM DETECTED - Source: {source}, Receiver: {receiver}");
        return "Unknown Room";
    }

    float GetFrequencyAbsorption(AcousticMaterial material)
    {
        return material.midFrequencyAbsorption;
    }

    void SaveDataToFile()
    {
        string filePath = Application.dataPath + "/RayData.txt";
        System.IO.File.WriteAllLines(filePath, rayData);
        Debug.Log($"Ray data saved to: {filePath}");
    }
}
