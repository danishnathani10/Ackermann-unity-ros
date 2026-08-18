using UnityEngine;

public class LidarVisualizer : MonoBehaviour
{
    [Header("Ray Settings")]
    [SerializeField]
    private float rayWidth = 0.02f;

    [Header("Visual")]
    [SerializeField]
    private Material rayMaterial;

    [SerializeField]
    private string debugLayer = "LiDARDebug";

    private LineRenderer[] rays;

    //Called from  Lidar Sensor class
    internal void Begin(int rayCount)
    {
        CreateRays(rayCount);
    }

    private void CreateRays(int rayCount)
    {
        rays = new LineRenderer[rayCount];

        int layer = LayerMask.NameToLayer(debugLayer);

        for (int i = 0; i < rayCount; i++)
        {
            GameObject ray = new GameObject("LiDAR Ray " + i);

            ray.transform.SetParent(transform);
            ray.layer = layer;

            LineRenderer line = ray.AddComponent<LineRenderer>();

            line.material = rayMaterial;
            line.startWidth = rayWidth;
            line.endWidth = rayWidth;
            line.positionCount = 2;
            line.useWorldSpace = true;

            // Default colour
            line.startColor = Color.green;
            line.endColor = Color.green;

            rays[i] = line;
        }
    }

    public void UpdateRays(float[] ranges, bool[] detected, float angleMin, float angleIncrement)
    {
        if (rays == null || ranges == null || detected == null)
            return;

        for (int i = 0; i < ranges.Length; i++)
        {
            float angle = angleMin + i * angleIncrement;

            Vector3 direction = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            direction = transform.TransformDirection(direction);

            rays[i].SetPosition(0, transform.position); //multi vertex
            rays[i].SetPosition(1, transform.position + direction * ranges[i]);

            Color rayColor = detected[i] ? Color.red : Color.green;
            rays[i].material.color = rayColor;

            // rays[i].startColor = rayColor;
            // rays[i].endColor = rayColor;
        }
    }
}