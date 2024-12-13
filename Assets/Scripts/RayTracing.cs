using UnityEngine;
using System.Collections.Generic;

public class RayTracing : MonoBehaviour
{
    public Transform soundSource; // Reference to the sound source
    public int numberOfRays = 50; // Number of rays to emit
    public float rayLength = 10f; // Maximum length of each ray
    public int maxReflections = 5; // Maximum number of reflections
    public float energyLossPerReflection = 0.2f; // Energy lost per reflection (0.0 to 1.0)
    public float speedOfSound = 343f; // Speed of sound in m/s

    // Data storage for ray travel time, hit points, and energy
    private List<string> rayData = new List<string>();

    void Start()
    {
        EmitRays(); // Emit the rays
        SaveDataToFile(); // Save the recorded data
    }

    void EmitRays()
    {
        for (int i = 0; i < numberOfRays; i++)
        {
            // Generate a random direction for the ray
            Vector3 randomDirection = Random.onUnitSphere;

            // Start the ray from the sound source
            Vector3 rayOrigin = soundSource.position;
            Vector3 rayDirection = randomDirection;
            float remainingEnergy = 1.0f; // Initial energy of the ray
            float totalTravelTime = 0.0f; // Total time taken by the ray

            for (int reflection = 0; reflection < maxReflections; reflection++)
            {
                // Create a ray
                Ray ray = new Ray(rayOrigin, rayDirection);
                RaycastHit hit;

                // Perform the raycast
                if (Physics.Raycast(ray, out hit, rayLength))
                {
                    // Calculate travel time to this hit point
                    float travelDistance = Vector3.Distance(rayOrigin, hit.point);
                    float travelTime = travelDistance / speedOfSound;
                    totalTravelTime += travelTime;

                    // Save data: Reflection number, hit point, travel time, and energy
                    rayData.Add($"Ray {i}, Reflection {reflection}, Hit: {hit.point}, Time: {totalTravelTime:F4}s, Energy: {remainingEnergy:F2}");

                    // Visualize the ray (color intensity based on energy)
                    Debug.DrawLine(ray.origin, hit.point, new Color(remainingEnergy, 0, 0), 5.0f);

                    // Calculate reflection direction
                    rayDirection = Vector3.Reflect(rayDirection, hit.normal);

                    // Update the origin for the next ray
                    rayOrigin = hit.point;

                    // Reduce the energy of the ray
                    remainingEnergy -= energyLossPerReflection;

                    // Stop the ray if energy is too low
                    if (remainingEnergy <= 0.0f)
                        break;
                }
                else
                {
                    // If the ray doesn't hit anything, visualize it and stop
                    Debug.DrawLine(ray.origin, ray.origin + rayDirection * rayLength, Color.blue, 5.0f);
                    break;
                }
            }
        }
    }

    void SaveDataToFile()
    {
        // Save the ray data to a file in the project's Assets folder
        string filePath = Application.dataPath + "/RayData.txt";
        System.IO.File.WriteAllLines(filePath, rayData);
        Debug.Log($"Ray data saved to: {filePath}");
    }
}
