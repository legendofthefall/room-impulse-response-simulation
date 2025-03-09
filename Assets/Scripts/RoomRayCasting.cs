using UnityEngine;
using System.Collections.Generic;
using SteamAudio;

public class SteamAudioRIRVisualization : MonoBehaviour
{
    [Header("Visualization Settings")]
    public float rayLifetime = 2.0f;
    public int maxVisibleRays = 20;
    public int maxReflections = 6;
    public float movementThreshold = 0.05f;

    private SteamAudioSource steamAudioSource;
    private List<RayData> activeRays = new List<RayData>();
    private GameObject soundSource;
    private GameObject receiver;
    private UnityEngine.Vector3 lastReceiverPosition;
    private bool receiverMoved = false;

    void Start()
    {
        soundSource = GameObject.FindGameObjectWithTag("SoundSource");
        receiver = GameObject.FindGameObjectWithTag("Receiver");
        steamAudioSource = soundSource?.GetComponent<SteamAudioSource>();

        if (soundSource == null || receiver == null || steamAudioSource == null)
        {
            Debug.LogError("❌ Missing Sound Source, Receiver, or SteamAudioSource!");
            return;
        }

        lastReceiverPosition = receiver.transform.position;
    }

    void Update()
    {
        receiverMoved = (UnityEngine.Vector3.Distance(receiver.transform.position, lastReceiverPosition) > movementThreshold);

        if (receiverMoved)
        {
            VisualizeReflectionPaths();
            lastReceiverPosition = receiver.transform.position;
        }

        CleanupRays();
        DrawActiveRays();
    }

    void VisualizeReflectionPaths()
    {
        activeRays.Clear(); // Reset previous rays

        for (int i = 0; i < maxVisibleRays; i++)
        {
            UnityEngine.Vector3 randomDirection = UnityEngine.Random.onUnitSphere;
            UnityEngine.Ray ray = new UnityEngine.Ray(soundSource.transform.position, randomDirection);
            RaycastHit hit;
            List<UnityEngine.Vector3> rayPath = new List<UnityEngine.Vector3> { soundSource.transform.position };

            for (int reflection = 0; reflection < maxReflections; reflection++)
            {
                if (Physics.Raycast(ray, out hit, 50f))
                {
                    rayPath.Add(hit.point);
                    if (hit.collider.CompareTag("Receiver")) break;

                    SteamAudioGeometry steamGeometry = hit.collider.GetComponent<SteamAudioGeometry>();

                    if (steamGeometry != null && steamGeometry.material != null)
                    {
                        float absorption = GetAbsorptionForReflection(steamGeometry.material, reflection);
                        UnityEngine.Vector3 reflectionDirection = UnityEngine.Vector3.Reflect(ray.direction, hit.normal);
                        ray = new UnityEngine.Ray(hit.point, reflectionDirection);
                    }
                    else break;
                }
                else break;
            }

            activeRays.Add(new RayData(rayPath, Time.time));
        }
    }

    void DrawActiveRays()
    {
        if (!receiverMoved) return; // Only render rays when the receiver moves

        foreach (var rayData in activeRays)
        {
            float lifeTime = Time.time - rayData.startTime;
            float fadeFactor = Mathf.Clamp01(1f - (lifeTime / rayLifetime));

            // 🔥 Color shifts from yellow (new) to red (old)
            Color fadeColor = Color.Lerp(Color.red, Color.yellow, fadeFactor);

            for (int j = 0; j < rayData.points.Count - 1; j++)
            {
                Debug.DrawLine(rayData.points[j], rayData.points[j + 1], fadeColor, 2.0f);
            }
        }
    }

    void CleanupRays()
    {
        activeRays.RemoveAll(ray => Time.time - ray.startTime > rayLifetime);

        if (activeRays.Count > maxVisibleRays)
        {
            activeRays.RemoveRange(0, activeRays.Count - maxVisibleRays);
        }
    }

    float GetAbsorptionForReflection(SteamAudioMaterial material, int reflection)
    {
        if (reflection < 3) return material.lowFreqAbsorption;
        if (reflection < 6) return material.midFreqAbsorption;
        return material.highFreqAbsorption;
    }

    void OnApplicationQuit()
    {
        SteamAudioManager steamAudioManager = FindObjectOfType<SteamAudioManager>();

        if (steamAudioManager != null)
        {
            Debug.Log("🔻 Cleaning up Steam Audio...");
            SteamAudioManager.ShutDown();
        }
        else
        {
            Debug.LogWarning("⚠ SteamAudioManager is missing. Skipping ShutDown().");
        }
    }
}

// ✅ Stores Steam Audio's reflection paths
class RayData
{
    public List<UnityEngine.Vector3> points;
    public float startTime;

    public RayData(List<UnityEngine.Vector3> path, float time)
    {
        points = path;
        startTime = time;
    }
}
