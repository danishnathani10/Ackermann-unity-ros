using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Manages to control car with the given inputs")]
    [Space]

    //New Input Actions
    //private VehicleControls inputActions;

    [Header("Wheels")]
    [SerializeField]
    private Wheel frontLeft;
    [SerializeField]
    private Wheel frontRight;
    [SerializeField]
    private Wheel rearLeft;
    [SerializeField]
    private Wheel rearRight;

    [Header("Vehicle")]
    [SerializeField]
    private Transform centerOfMass;

    //Car Mode
    [SerializeField] 
    private CarInput carInput;
    private CarMode carMode = CarMode.Parking;
    public CarMode CurrentCarMode => carMode;

    [Header("Engine")]
    [SerializeField, Min(0f)] 
    private float motorTorque = 1500f;
    [SerializeField, Min(1f)] 
    private float maxSpeed = 120f;

    [Header("Steering")]
    [SerializeField] 
    private SteeringMode steeringMode = SteeringMode.Ackermann;

    [SerializeField, Range(1f, 60f)]
    private float maxSteeringAngle = 30f;

    [SerializeField, Min(0f)]
    private float steeringResponse = 8f;

    [Tooltip("Steering tension at maximum speed.")]
    [SerializeField, Range(0.1f, 1f)]
    private float highSpeedSteeringMultiplier = 0.4f;

    [Header("Brakes")]
    [SerializeField, Min(0f)] 
    private float brakeTorque = 3000f;

    [Header("Stability")]
    [SerializeField, Min(0.01f)]
    private float restSpeedThreshold = 0.5f;

    [SerializeField, Min(0f)]
    private float angularDampingAtRest = 5f;

    [Header("Cruise Control")]
    [SerializeField]
    private bool cruiseControlEnabled = false;
    [SerializeField]
    private bool debugCruiseSpeed = true;
    [Tooltip("Target speed m/s")]
    [SerializeField, Min(0f)] 
    private float cruiseTargetSpeed = 8f;
    [Tooltip("How aggressively cruise control corrects speed error.")]
    [SerializeField, Min(0f)] 
    private float cruiseControlGain = 20f;

    private Rigidbody rb;
    private Wheel[] wheels;

    //Inputs from CarInput class
    private float throttleInput;
    private float steeringInput;
    private float brakeInput;
    private float currentSteeringAngle;

    // Used only when Ackermann steering is enabled
    private float wheelBase;
    private float trackWidth;

    private float SpeedKmh => rb.linearVelocity.magnitude * 3.6f;
    private float SpeedMs => rb.linearVelocity.magnitude;

    public bool CruiseControlEnabled
    {
        get => cruiseControlEnabled;
        set => cruiseControlEnabled = value;
    }

    public float CruiseTargetSpeed
    {
        get => cruiseTargetSpeed;
        set => cruiseTargetSpeed = Mathf.Max(0f, value);
    }

    #region Ros Output Reads

    public bool IsMoving => rb.linearVelocity.magnitude > 0.1f;
    public float LinearVelocity => rb.linearVelocity.magnitude;
    public float AngularVelocity => rb.angularVelocity.y;
    public float FrontLeftSteeringAngle => frontLeft.collider.steerAngle * Mathf.Deg2Rad;
    public float FrontRightSteeringAngle => frontRight.collider.steerAngle * Mathf.Deg2Rad;

    #endregion Ros Output Reads

    private void Awake()
    {
        carInput = FindAnyObjectByType<CarInput>(); //For package setttins

        rb = GetComponent<Rigidbody>();

        wheels = new[] {frontLeft, frontRight, rearLeft, rearRight};

        //inputActions = new VehicleControls();

        SetupVehicle();
    }


    // private void Update()
    // {
    //     ReadInput();
    // }


    private void FixedUpdate()
    {
        HandleSteering();
        HandleMotor();
        HandleBrakes();
        StabilizeAtRest();
        UpdateWheelVisuals();
        UpdateCarMode();
    }


    #region Setup

    private void SetupVehicle()
    {
        if (centerOfMass != null) // Config from the Inspector
            rb.centerOfMass = transform.InverseTransformPoint(centerOfMass.position);

        ValidateWheels();
        CalculateVehicleDimensions();
    }

    private void ValidateWheels()
    {
        foreach (Wheel wheel in wheels)
            if (wheel == null || wheel.collider == null)
                Debug.LogError($"{wheel.visual.gameObject.name}: A wheel collider reference is missing.", this);
    }

    private void CalculateVehicleDimensions()
    {
        if (frontLeft?.collider == null || frontRight?.collider == null || rearLeft?.collider == null || rearRight?.collider == null)
            return;

        Vector3 frontAxle = (frontLeft.collider.transform.position + frontRight.collider.transform.position) * 0.5f;
        Vector3 rearAxle = (rearLeft.collider.transform.position + rearRight.collider.transform.position) * 0.5f;

        wheelBase = Vector3.Distance(frontAxle, rearAxle);
        trackWidth = Vector3.Distance(frontLeft.collider.transform.position, frontRight.collider.transform.position);
    }

    #endregion

    #region Input

    //private void ReadInput()
    //{
    // throttleInput = Input.GetAxisRaw("Vertical");
    // steeringInput = Input.GetAxisRaw("Horizontal");

    // brakeInput = Input.GetKey(KeyCode.Space);

    // throttleInput = inputActions.Vehicle.Drive.ReadValue<float>();
    // steeringInput = inputActions.Vehicle.Steer.ReadValue<float>();

    // brakeInput = inputActions.Vehicle.Brake.IsPressed();
    //}

    public void SetInput(VehicleCommand command)// float throttle, float steering, bool brake, bool handbrake)
    {
        // Match the ranges defined in the assignment.
        throttleInput = command.throttle;
        brakeInput = command.brake;
        steeringInput = command.steering_angle;
    }

    #endregion Input


    private void HandleSteering()
    {
        float speedRatio = Mathf.Clamp01(SpeedKmh / maxSpeed);

        // Steering gets stiff as vehicle gets faster
        float steeringMultiplier = Mathf.Lerp(1f, highSpeedSteeringMultiplier, speedRatio);
        float targetAngle = steeringInput * Mathf.Rad2Deg;

        targetAngle = Mathf.Clamp(targetAngle, -maxSteeringAngle, maxSteeringAngle);
        targetAngle *= steeringMultiplier;

        currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetAngle, steeringResponse * Time.fixedDeltaTime);

        if (steeringMode == SteeringMode.Ackermann)
            ApplyAckermannSteering();
        else
            ApplyNormalSteering();
    }


    private void ApplyNormalSteering()
    {
        foreach (Wheel wheel in wheels)
        {
            if (wheel?.collider == null || !wheel.steering)
                continue;

            wheel.collider.steerAngle = currentSteeringAngle;
        }
    }


    private void ApplyAckermannSteering()
    {
        if (frontLeft?.collider == null || frontRight?.collider == null)
            return;

        if (Mathf.Abs(currentSteeringAngle) < 0.01f)
        {
            frontLeft.collider.steerAngle = 0f;
            frontRight.collider.steerAngle = 0f;
            return;
        }

        float steeringRadians = Mathf.Abs(currentSteeringAngle) * Mathf.Deg2Rad;
        float turnRadius = wheelBase / Mathf.Tan(steeringRadians);

        // Inner and outer wheels need different angles during a turn
        float innerAngle = Mathf.Atan(wheelBase / (turnRadius - trackWidth * 0.5f)) * Mathf.Rad2Deg;
        float outerAngle = Mathf.Atan(wheelBase / (turnRadius + trackWidth * 0.5f)) * Mathf.Rad2Deg;

        float leftAngle, rightAngle;

        if (currentSteeringAngle > 0f)
        {
            // Right wheel becomes the inner wheel
            leftAngle = outerAngle;
            rightAngle = innerAngle;
        }
        else
        {
            // Left wheel becomes the inner wheel
            leftAngle = innerAngle;
            rightAngle = outerAngle;
        }

        float direction = Mathf.Sign(currentSteeringAngle);

        frontLeft.collider.steerAngle = leftAngle * direction;
        frontRight.collider.steerAngle = rightAngle * direction;
    }


    private void HandleMotor()
    {
        float torque = 0f;

        if (cruiseControlEnabled)
        {
            float speedError = cruiseTargetSpeed - SpeedMs;

            if (debugCruiseSpeed)
                Debug.Log(
                $"[Cruise Control] Current Speed: {SpeedMs:F2} m/s | " +
                $"Target: {cruiseTargetSpeed:F2} m/s | " +
                $"Error: {speedError:F2} m/s");

            torque = speedError * cruiseControlGain;
            torque = Mathf.Clamp(torque, 0f, motorTorque);

            // At or above target speed, stop applying torque
            if (SpeedMs >= cruiseTargetSpeed)
                torque = 0f;

        }
        else
            if (SpeedKmh < maxSpeed)
            torque = (throttleInput / 100f) * motorTorque;

        foreach (Wheel wheel in wheels)
        {
            if (wheel?.collider == null || !wheel.drive)
                continue;

            wheel.collider.motorTorque = torque;
        }
    }

    private void HandleBrakes()
    {
        float appliedBrake = (brakeInput / 100f) * brakeTorque;

        foreach (Wheel wheel in wheels)
        {
            if (wheel?.collider == null)
                continue;

            wheel.collider.brakeTorque = appliedBrake;
        }
    }

    private void StabilizeAtRest()
    {
        if (rb.linearVelocity.magnitude > restSpeedThreshold)
            return;

        if (Mathf.Abs(throttleInput) > 0.01f)
            return;

        if (Mathf.Abs(steeringInput) > 0.01f)
            return;

        //Getting vehicle to StabilizeAtRest
        rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, angularDampingAtRest * Time.fixedDeltaTime);
    }

    private void UpdateWheelVisuals()
    {
        foreach (Wheel wheel in wheels)
        {
            if (wheel?.collider == null || wheel.visual == null)
                continue;

            wheel.collider.GetWorldPose(out Vector3 position, out Quaternion rotation);
            wheel.visual.SetPositionAndRotation(position, rotation);
        }
    }

    private void UpdateCarMode()
    {
        carMode = rb.linearVelocity.magnitude > 0.1f 
        ? (carInput.CurrentInputSource == CarInputSource.Keyboard) ? CarMode.Manual : CarMode.Auto
        : CarMode.Parking; 
    }
}


[System.Serializable]
public class Wheel
{
    public WheelCollider collider;
    public Transform visual;
    [Tooltip("Should this wheel take in steering")]
    public bool steering;
    [Tooltip("Can this wheel take in drive")]
    public bool drive;
}

public enum SteeringMode
{
    Normal,
    Ackermann
}

public enum CarMode
{
    Parking,
    Manual,
    Auto
}