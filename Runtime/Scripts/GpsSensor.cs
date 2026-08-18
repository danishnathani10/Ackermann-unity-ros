using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;

public class GpsSensor : MonoBehaviour
{
    [Header("Publishes GPS Coordinates of the car.")]
    [Space]

    [Header("ROS")]
    [SerializeField]
    private string gpsTopic = "/gps";

    [Header("Vehicle")]
    [SerializeField]
    private Transform vehicle;

    //using Abu Dhabhi GPS here
    [Header("GPS Origin - Any place's GPS for reference")]
    [SerializeField]
    private double originLatitude = 24.42;
    [SerializeField]
    private double originLongitude = 54.61;
    [SerializeField]
    private double originAltitude = 13.0;

    [Header("Sensor Settings")]
    [SerializeField]
    private float positionNoise = 0.000001f;
    [Tooltip("Every that milli sec the data will be published")]
    [SerializeField]
    private float publishRate = 10f;

    private ROSConnection ros;

    internal void Begin(ROSConnection connection)
    {
        ros = connection;
        
        ros.RegisterPublisher<NavSatFixMsg>(gpsTopic);

        InvokeRepeating(nameof(PublishGPS), 0f, 1/publishRate);
    }

    internal void Stop()
    {
        CancelInvoke();
    }

    private void PublishGPS()
    {
        if (vehicle == null)
            return;

        Vector3 position = vehicle.position;

        double latitude = originLatitude + (position.z / 111111.0);
        double longitude = originLongitude + (position.x / (111111.0 * Mathf.Cos((float)(originLatitude * Mathf.Deg2Rad))));
        double altitude = originAltitude + position.y;

        latitude += Random.Range(-positionNoise, positionNoise);
        longitude += Random.Range(-positionNoise, positionNoise);

        NavSatFixMsg gpsMessage = new NavSatFixMsg
        {
            latitude = latitude,
            longitude = longitude,
            altitude = altitude
        };

        ros.Publish(gpsTopic, gpsMessage);
    }
}