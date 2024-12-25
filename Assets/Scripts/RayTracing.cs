using UnityEngine;
using System.Collections.Generic;

public class RayTracing : MonoBehaviour
{
    [Header("Ray Tracing Parameters")]
    public float rayLength = 50f; // Maximum length of each ray
    public int numberOfRays = 50; // Number of rays emitted per source
    public int maxReflections = 5; // Maximum reflections
    public float speedOfSound = 343f; // Speed of sound in m/s
    public float initialEnergy = 1.0f; // Starting energy of each ray

    private List<string> rayData = new List<string>(); // Store ray interaction data

    void Start()
    {
        EmitRaysFromSources(); // Emit rays from all sound sources
        SaveDataToFile(); // Save ray data to file
    }

    void EmitRaysFromSources()
    {
        // Find all sound sources and receivers dynamically
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
        for (int i = 0; i < numberOfRays; i++)
        {
            // Generate a random direction for the ray
            Vector3 randomDirection = Random.onUnitSphere;

            // Create a ray
            Ray ray = new Ray(sourcePosition, randomDirection);
            RaycastHit hit;

            float totalTravelTime = 0f;
            float energy = initialEnergy;

            for (int reflection = 0; reflection < maxReflections; reflection++)
            {
                if (Physics.Raycast(ray, out hit, rayLength))
                {
                    // Check if the ray hit a receiver
                    if (hit.collider.CompareTag("Receiver"))
                    {
                        // Visualize the ray as green when it hits a receiver
                        Debug.DrawLine(ray.origin, hit.point, Color.green, 5.0f);
                        Debug.Log($"Ray hit Receiver at {hit.point}");
                        rayData.Add($"Source: {sourcePosition}, Receiver: {receiverPosition}, Hit Receiver: {hit.point}, Time: {totalTravelTime:F4}s, Energy: {energy:F2}");
                        break; // Stop further reflections
                    }

                    // Check for AcousticMaterial on the hit surface
                    AcousticMaterialHolder materialHolder = hit.collider.GetComponent<AcousticMaterialHolder>();
                    if (materialHolder != null && materialHolder.acousticMaterial != null)
                    {
                        AcousticMaterial material = materialHolder.acousticMaterial;

                        // Calculate absorption and reduce energy
                        float absorption = GetFrequencyAbsorption(material);
                        energy *= (1 - absorption);

                        // Stop tracing if energy is too low
                        if (energy < 0.01f)
                        {
                            Debug.Log("Ray energy too low, stopping reflection.");
                            break;
                        }

                        // Calculate travel time
                        float travelDistance = Vector3.Distance(ray.origin, hit.point);
                        float travelTime = travelDistance / speedOfSound;
                        totalTravelTime += travelTime;

                        // Store ray interaction data
                        rayData.Add($"Source: {sourcePosition}, Receiver: {receiverPosition}, Reflection: {reflection}, Hit: {hit.point}, Time: {totalTravelTime:F4}s, Energy: {energy:F2}");

                        // Visualize the ray (color based on energy level)
                        Color rayColor = Color.Lerp(Color.yellow, Color.red, energy);
                        Debug.DrawLine(ray.origin, hit.point, rayColor, 5.0f);

                        // Handle scattering or reflection
                        if (Random.value < material.scatteringCoefficient)
                        {
                            // Scatter the ray
                            Vector3 scatterDirection = Random.onUnitSphere;
                            ray = new Ray(hit.point, scatterDirection);
                            Debug.Log($"Ray scattered at {hit.point}");
                        }
                        else if (material.isReflective)
                        {
                            // Reflect the ray
                            Vector3 reflectionDirection = Vector3.Reflect(ray.direction, hit.normal);
                            ray = new Ray(hit.point, reflectionDirection);
                            Debug.Log($"Ray reflected at {hit.point}");
                        }
                        else
                        {
                            Debug.Log("Ray absorbed, stopping reflection.");
                            break;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"No AcousticMaterial found on object: {hit.collider.name}");
                        break;
                    }
                }
                else
                {
                    // If no hit, draw the ray as blue
                    Debug.DrawLine(ray.origin, ray.origin + ray.direction * rayLength, Color.blue, 5.0f);
                    break;
                }
            }
        }
    }

    float GetFrequencyAbsorption(AcousticMaterial material)
    {
        // Example: Use mid-frequency absorption for simplicity
        return material.midFrequencyAbsorption;
    }

    void SaveDataToFile()
    {
        // Save ray data to a file in the Assets folder
        string filePath = Application.dataPath + "/RayData.txt";
        System.IO.File.WriteAllLines(filePath, rayData);
        Debug.Log($"Ray data saved to: {filePath}");
    }
}
