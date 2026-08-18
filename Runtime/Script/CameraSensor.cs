using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;

[RequireComponent(typeof(Camera))]
public class CameraSensor : MonoBehaviour
{
    [Header("Sends attached camera's image data")]
    [Space]

    [Header("ROS")]
    [SerializeField]
    private string cameraTopic = "/camera";

    [Header("Camera Settings")]
    [SerializeField]
    private int imageWidth = 640;
    [SerializeField]
    private int imageHeight = 480;

    [Tooltip("No. of images you want to send each second")]
    [SerializeField]
    private float publishRate = 10f;

    private ROSConnection ros;
    private Camera sensorCamera;

    private RenderTexture renderTexture;
    private Texture2D texture2D;


    internal void Begin(ROSConnection connection)
    {
        ros = connection;
        sensorCamera = GetComponent<Camera>();

        ros.RegisterPublisher<ImageMsg>(cameraTopic);

        SetupCamera();
        StartPublishing();
    }

    internal void Stop()
    {
        CancelInvoke();
    }

    private void SetupCamera()
    {
        renderTexture = new RenderTexture(imageWidth, imageHeight, 24, RenderTextureFormat.ARGB32);

        renderTexture.Create();
        sensorCamera.targetTexture = renderTexture;

        texture2D = new Texture2D(imageWidth, imageHeight, TextureFormat.RGB24, false);
    }


    private void StartPublishing()
    {
        if (publishRate <= 0f)
        {
            Debug.LogError("CameraSensor: Publish Rate must be greater than 0.");
            return;
        }

        InvokeRepeating(nameof(PublishCameraImage), 0f, 1f / publishRate);
    }


    private void PublishCameraImage()
    {
        RenderTexture previousTexture = RenderTexture.active;
        RenderTexture.active = renderTexture;

        sensorCamera.Render();

        texture2D.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 0, 0);
        texture2D.Apply();

        FlipImage();    //Current output in rViz was upside down

        RenderTexture.active = previousTexture;

        ImageMsg message = new ImageMsg
        {
            header = new HeaderMsg { frame_id = "camera" },
            height = (uint)imageHeight,
            width = (uint)imageWidth,
            encoding = "rgb8",
            is_bigendian = 0,
            step = (uint)(imageWidth * 3),
            data = texture2D.GetRawTextureData()
        };

        ros.Publish(cameraTopic, message);
    }

    private void FlipImage()
    {
        Color[] pixels = texture2D.GetPixels();

        for (int y = 0; y < imageHeight / 2; y++)
        {
            for (int x = 0; x < imageWidth; x++)
            {
                int top = y * imageWidth + x;
                int bottom = (imageHeight - 1 - y) * imageWidth + x;

                Color temp = pixels[top];
                pixels[top] = pixels[bottom];
                pixels[bottom] = temp;
            }
        }

        texture2D.SetPixels(pixels);
        texture2D.Apply();
    }

    private void OnDestroy()
    {
        CancelInvoke();

        if (renderTexture != null)
            renderTexture.Release();

        if (texture2D != null)
            Destroy(texture2D);
    }
}