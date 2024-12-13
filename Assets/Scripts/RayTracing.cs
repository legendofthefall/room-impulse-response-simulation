using UnityEngine;
using System.Collections.Generic;

public class RayTracing : MonoBehaviour
{
    public float rayLength = 10f; // Maximum length of each ray
    public int numberOfRays = 50; // Number of rays to emit per source
    public int maxReflections = 5; // Maximum number of reflections
    public float speedOfSound = 343f; // Speed of sound in m/s

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

            for (int reflection = 0; reflection < maxReflections; reflection++)
            {
                if (Physics.Raycast(ray, out hit, rayLength))
                {
                    // Calculate travel time and store data
                    float travelDistance = Vector3.Distance(ray.origin, hit.point);
                    float travelTime = travelDistance / speedOfSound;
                    totalTravelTime += travelTime;

                    rayData.Add($"Source: {sourcePosition}, Receiver: {receiverPosition}, Reflection: {reflection}, Hit: {hit.point}, Time: {totalTravelTime:F4}s");

                    // Visualize the ray in Scene View
                    Debug.DrawLine(ray.origin, hit.point, Color.red, 5.0f);

                    // Reflect the ray
                    ray = new Ray(hit.point, Vector3.Reflect(ray.direction, hit.normal));
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

    void SaveDataToFile()
    {
        // Save ray data to a file in the Assets folder
        string filePath = Application.dataPath + "/RayData.txt";
        System.IO.File.WriteAllLines(filePath, rayData);
        Debug.Log($"Ray data saved to: {filePath}");
    }
}
