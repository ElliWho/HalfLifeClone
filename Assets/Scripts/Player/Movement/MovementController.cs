using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class MovementController : MonoBehaviour
{
    [Header("User Settings")]
    [SerializeField] private float mouseSensitivity = 0.5f;

    [Header("Settings")]
    [SerializeField] private float groundAcceleration = 100f;
    [SerializeField] private float groundLimit = 12f;
    [SerializeField] private float friction = 6f;
    [SerializeField] private float slopeLimit = 60f;

    [Header("Jump")]
    [SerializeField] private bool additiveJump = true;
    [SerializeField] private bool autoJump = true;
    [SerializeField] private float jumpHeight = 5f;
    [SerializeField] private float gravity = 16f;
    private bool jumpPending;
    private bool canJump = true;

    [Header("AirMovement")]
    [SerializeField] private float airLimit = 1f;
    [SerializeField] private float airAcceleration = 100f;

    [Header("Surf / Extra")]
    [SerializeField] private float rampSlideLimit = 5f;
    [SerializeField] private bool clampGroundSpeed = false;
    [SerializeField] private bool disableBunnyHopping = false;

    [Header("Hands")]
    [SerializeField] private Transform handTransform;

    private Vector2 _moveInput;
    private Vector2 _lookInput;
    public Vector2 LookInput => _lookInput;

    private Vector3 _velocity;
    private Vector3 _inputDir;
    private Vector3 _inputRot;
    public Vector3 InputRot => _inputRot;

    private Rigidbody _rb;

    private Vector3 groundNormal;
    private bool _isGrounded;


    InputAction _lookAction;
    private float _yaw;
    private float _pitch;

    [SerializeField] private Transform cameraPivot;

    [SerializeField] private float StickDegPerSec = 240f;

    private void Awake()
    {
        SubscribeInputs();
    }

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();

        _rb.constraints = RigidbodyConstraints.FreezeRotationX
                        | RigidbodyConstraints.FreezeRotationY
                        | RigidbodyConstraints.FreezeRotationZ;

        _rb.constraints = RigidbodyConstraints.FreezeRotation;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        _rb.useGravity = false;

        GameServices.Cam.SetFollowTarget(cameraPivot);
    }

    #region Inputs
    private void SubscribeInputs()
    {
        GameServices.Input.Actions.Player.Move.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
        GameServices.Input.Actions.Player.Move.canceled += ctx => _moveInput = Vector2.zero;

        GameServices.Input.Actions.Player.Look.performed += ctx => _lookInput = ctx.ReadValue<Vector2>();
        GameServices.Input.Actions.Player.Look.canceled += ctx => _lookInput = Vector2.zero;

        _lookAction = GameServices.Input.Actions.Player.Look;

        GameServices.Input.Actions.Player.Jump.performed += ctx => jumpPending = true;
        GameServices.Input.Actions.Player.Jump.canceled += ctx => jumpPending = false;
    }

    private void GetMovementInput()
    {
        float x = _moveInput.x;
        float z = _moveInput.y;

        _inputDir = transform.rotation * new Vector3(x, 0, z).normalized;
    }
    #endregion

    private void Update()
    {
        GetLookInput();

        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        cameraPivot.localRotation = Quaternion.Euler(_pitch, 0, 0);
        handTransform.localRotation = Quaternion.Euler(_pitch, 0, 0);

        GetMovementInput();
    }

    private void GetLookInput()
    {
        Vector2 look = _lookAction.ReadValue<Vector2>();
        bool isMouse = _lookAction.activeControl?.device is Mouse;

        if (isMouse)
        {
            _yaw += look.x * mouseSensitivity;
            _pitch = Mathf.Clamp(_pitch - look.y * mouseSensitivity, -90f, 90f);
        }
        else
        {
            _yaw += look.x * StickDegPerSec * Time.deltaTime;
            _pitch = Mathf.Clamp(_pitch - look.y * StickDegPerSec * Time.deltaTime, -90f, 90f);
        }
    }

    private void FixedUpdate()
    {
        _velocity = _rb.linearVelocity;

        if (disableBunnyHopping && _isGrounded)
        {
            if (_velocity.magnitude > groundLimit)
                _velocity = _velocity.normalized * groundLimit;
        }

        if (jumpPending && _isGrounded)
        {
            Jump();
        }

        if (rampSlideLimit >= 0f && _velocity.y > rampSlideLimit)
            _isGrounded = false;

        if (_isGrounded)
        {
            _inputDir = Vector3.Cross(Vector3.Cross(groundNormal, _inputDir), groundNormal);

            GroundAccelerate();
            ApplyFriction();

            if (clampGroundSpeed)
            {
                if (_velocity.magnitude > groundLimit)
                    _velocity = _velocity.normalized * groundLimit;
            }
        }
        else
        {
            ApplyGravity();
            AirAccelerate();
        }
        _rb.linearVelocity = _velocity;
        //_rb.MoveRotation(Quaternion.Euler(0f, _yaw, 0f));

        _isGrounded = false;
        groundNormal = Vector3.zero;
    }

    private void Jump()
    {
        if (!canJump) return;

        if (_velocity.y < 0f || !additiveJump)
            _velocity.y = 0f;

        _velocity.y += jumpHeight;
        _isGrounded = false;

        if (!autoJump)
            jumpPending = false;

        StartCoroutine(JumpTimer());
    }

    private IEnumerator JumpTimer()
    {
        canJump = false;
        yield return new WaitForSeconds(0.1f);
        canJump = true;
    }

    private void GroundAccelerate()
    {
        float addSpeed = groundLimit - Vector3.Dot(_velocity, _inputDir);

        if (addSpeed <= 0)
            return;

        float accelSpeed = groundAcceleration * Time.deltaTime;

        if (accelSpeed > addSpeed)
            accelSpeed = addSpeed;

        _velocity += accelSpeed * _inputDir;
    }

    private void AirAccelerate()
    {
        Vector3 hVel = _velocity;
        hVel.y = 0;

        float dot = Vector3.Dot(hVel, _inputDir);
        float addSpeed = airLimit - dot;

        if (addSpeed <= 0)
            return;

        float accelSpeed = airAcceleration * Time.deltaTime;

        if (accelSpeed > addSpeed)
            accelSpeed = addSpeed;

        _velocity += accelSpeed * _inputDir;
    }

    private void ApplyFriction()
    {
        _velocity *= Mathf.Clamp01(1 - Time.deltaTime * friction);
    }

    private void ApplyGravity()
    {
        _velocity.y -= gravity * Time.deltaTime;
    }
    private void OnCollisionStay(Collision other)
    {
        foreach (ContactPoint contact in other.contacts)
        {
            if (contact.normal.y > Mathf.Sin(slopeLimit * (Mathf.PI / 180f) + Mathf.PI / 2f))
            {
                groundNormal = contact.normal;
                _isGrounded = true;
                return;
            }
        }
    }    
}
