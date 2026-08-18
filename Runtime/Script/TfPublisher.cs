using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using RosMessageTypes.Tf2;

public class TfPublisher : MonoBehaviour
{
    [Header("Transform Library publishes transform data of the sensors")]
    [Space]

    [Header("ROS")]
    [SerializeField]
    private string tfTopic = "/tf";

    [Header("Vehicle")]
    [SerializeField]
    private Transform baseLink;

    [Header("Sensors")]
    [SerializeField]
    private Transform gps;
    [SerializeField]
    private Transform imu;
    [SerializeField]
    private Transform cameraSensor;
    [SerializeField]
    private Transform lidar;

    [Header("Publish Settings")]
    [Tooltip("Every that milli sec the data will be published")]
    [SerializeField]
    private float publishRate = 10f;

    private ROSConnection ros;

    internal void Begin(ROSConnection connection)
    {
        ros = connection;
        ros.RegisterPublisher<TFMessageMsg>(tfTopic);

        StartPublishing();
    }

    internal void Stop()
    {
        CancelInvoke();
    }

    private void StartPublishing()
    {
        if (publishRate <= 0f)
        {
            Debug.LogError("TfPublisher: Publish Rate must be greater than 0.");
            return;
        }

        InvokeRepeating(nameof(PublishTransforms), 0f, 1f / publishRate);
    }


    private void PublishTransforms()
    {
        if (baseLink == null)
            return;

        List<TransformStampedMsg> transforms = new List<TransformStampedMsg>();

        // Vehicle base frame
        transforms.Add(CreateTransform("world", "base_link", baseLink.position, baseLink.rotation));

        // GPS
        if (gps != null)
            transforms.Add(CreateRelativeTransform(baseLink, gps, "base_link", "gps"));

        // IMU
        if (imu != null)
            transforms.Add(CreateRelativeTransform(baseLink, imu, "base_link", "imu"));

        // Camera
        if (cameraSensor != null)
            transforms.Add(CreateRelativeTransform(baseLink, cameraSensor, "base_link", "camera"));

        // LiDAR
        if (lidar != null)
            transforms.Add(CreateRelativeTransform(baseLink, lidar, "base_link", "lidar"));

        TFMessageMsg message = new TFMessageMsg(transforms.ToArray());

        ros.Publish(tfTopic, message);
    }


    private TransformStampedMsg CreateTransform(string parentFrame, string childFrame, Vector3 position, Quaternion rotation)
    {
        return new TransformStampedMsg
        {
            header = new HeaderMsg
            {
                stamp = GetCurrentTime(),
                frame_id = parentFrame
            },
            child_frame_id = childFrame,
            transform = new TransformMsg
            {
                translation = new Vector3Msg(position.z, -position.x, position.y),
                rotation = new QuaternionMsg(rotation.z, -rotation.x, rotation.y, rotation.w)
            }
        };
    }


    private TransformStampedMsg CreateRelativeTransform(Transform parent, Transform child, string parentFrame, string childFrame)
    {
        Vector3 localPosition = parent.InverseTransformPoint(child.position);
        Quaternion localRotation = Quaternion.Inverse(parent.rotation) * child.rotation;

        return new TransformStampedMsg
        {
            header = new HeaderMsg
            {
                stamp = GetCurrentTime(),
                frame_id = parentFrame
            },
            child_frame_id = childFrame,
            transform = new TransformMsg
            {
                translation = new Vector3Msg(localPosition.z, -localPosition.x, localPosition.y),
                rotation = new QuaternionMsg(localRotation.z, -localRotation.x, localRotation.y, localRotation.w)
            }
        };
    }


    private TimeMsg GetCurrentTime()
    {
        float currentTime = Time.realtimeSinceStartup;
        int seconds = Mathf.FloorToInt(currentTime);
        uint nanoseconds = (uint)((currentTime - seconds) * 1000000000);

        return new TimeMsg((uint)seconds, nanoseconds);
    }


    private void OnDestroy()
    {
        CancelInvoke();
    }
}