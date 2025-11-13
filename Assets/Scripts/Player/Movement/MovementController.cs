using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(Rigidbody))]
public class MovementController : MonoBehaviour
{
    [Header("User Settings")]
    [SerializeField] private float mouseSensitivity = 1f;
    [Space]
    [Header("Settings")]
    [SerializeField] private float groundAcceleration = 100f;
    [SerializeField] private float groundLimit = 12f;
    [SerializeField] private float friction = 6f;
    [SerializeField] private float slopeLimit = 60f;
    [Space]
    [Header("Jump")]
    [SerializeField] private bool additiveJump = true;
    [SerializeField] private bool autoJump = true;
    [SerializeField] private float jumpHeight = 5f;
    [SerializeField] private float gravity = 16f;
    private bool jumpPending;
    private bool canJump = true;
    [Space]
    [Header("AirMovement")]
    [SerializeField] private float airLimit = 1f;
    [SerializeField] private float airAcceleration = 100f;
    [Header("Camera")]
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 1.75f, 0);
    [Space]
    [SerializeField] private float thirdPersonCameraDistance = 4f;
    [SerializeField] private float thirdPersonCameraHeight = 5f;
    [SerializeField] private float thirdPersonCameraLookAtHeight = 3f;

    [Header("Hands")]
    [SerializeField] private Transform handTransform;

    private bool isThirdPersonCamera = false;
    private bool overrideCamera = false;

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

    private Vector3 _lastPlatformPosition;

    InputAction _lookAction;
    private float _yaw;
    private float _pitch;
    private void Awake()
    {
        SubscribeInputs();
    }
    private void Start()
    {
        _rb = GetComponent<Rigidbody>();

        GameServices.Cam.SetFollowTarget(cameraPivot);
    }

    #region Inputs
    private void SubscribeInputs()
    {
        GameServices.Input.Actions.Player.Move.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
        GameServices.Input.Actions.Player.Move.canceled += ctx => _moveInput = Vector2.zero;

        GameServices.Input.Actions.Player.Look.performed += ctx => _lookInput = ctx.ReadValue<Vector2>();
        GameServices.Input.Actions.Player.Look.canceled += ctx => _lookInput = Vector3.zero;

        _lookAction = GameServices.Input.Actions.Player.Look;

        GameServices.Input.Actions.Player.Jump.performed += ctx => jumpPending = true;
        GameServices.Input.Actions.Player.Jump.canceled += ctx => jumpPending = false;

        //InputManager.Instance.Actions.Player.ToggleThirdPerson.canceled += ctx => isThirdPersonCamera = false;

        //InputManager.Instance.Actions.Player.Sprint.performed += ctx => dashPending = true;

        //GameServices.Input.Actions.Player.Crouch.performed += ctx => Crouch(true);
        //GameServices.Input.Actions.Player.Crouch.canceled += ctx => Crouch(false);
    }
    private void GetMovementInput()
    {
        float x = _moveInput.x;
        float z = _moveInput.y;

        _inputDir = transform.rotation * new Vector3(x, 0, z).normalized;
    }
    private void GetMouseInput()
    {
        _inputRot.y += _lookInput.x * mouseSensitivity;
        _inputRot.x -= _lookInput.y * mouseSensitivity;

        if (_inputRot.x > 90f)
            _inputRot.x = 90f;
        if (_inputRot.x < -90f)
            _inputRot.x = -90f;
    }
    #endregion
    #region Camera
    public void SetCameraOverride(bool value)
    {
        overrideCamera = value;
    }
    [SerializeField] private Transform cameraPivot;
    private void CameraFollow()
    {
        cameraPivot.rotation = Quaternion.Euler(0, _yaw, 0);
    }
    #endregion   
    private void Update()
    {
        GetMovementInput();
        //GetMouseInput();
        GetLookInput();
    }
    [SerializeField] private float StickDegPerSec = 240f;
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
        if (_isGrounded)
        {
            _inputDir = Vector3.Cross(Vector3.Cross(groundNormal, _inputDir), groundNormal);
            GroundAccelerate();
            ApplyFriction();

            if (jumpPending)
            {
                Jump();
            }
        }
        else if (!_isGrounded)
        {
            AirAccelerate();
            ApplyGravity();
        }        
        _rb.MoveRotation(Quaternion.Euler(0, _yaw, 0));

        _rb.linearVelocity = _velocity;
        _isGrounded = false;
        groundNormal = Vector3.zero;

        /*
        if(_currentPlatform != null) 
        {
            _currentPlatform.ConsumeDeltas(out Vector3 dPos, out Quaternion dRot);

            _rb.MovePosition(_rb.position + dPos);
            _rb.MoveRotation(_rb.rotation * dRot);
        }
        */
    }
    private void LateUpdate()
    {
        cameraPivot.localRotation = Quaternion.Euler(_pitch, 0, 0);
        handTransform.localRotation = Quaternion.Euler(_pitch, 0, 0);
    }
    public void Teleport(Vector3 pos)
    {
        StopAllCoroutines();
        StartCoroutine(iTeleport(pos));
    }
    private IEnumerator iTeleport(Vector3 pos)
    {
        yield return new WaitForFixedUpdate();
        _rb.linearVelocity = Vector3.zero;
        _velocity = Vector3.zero;
        transform.position = pos;

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
    private MovingPlatform _currentPlatform;
    private void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            /*
            if(_currentPlatform == null) 
            {
                MovingPlatform plat = contact.otherCollider.GetComponent<MovingPlatform>();
                if (plat)
                {
                    _currentPlatform = plat;
                }
            }
            */
            if (contact.normal.y > Mathf.Sin(slopeLimit * (Mathf.PI / 180) + Mathf.PI / 2f))
            {
                groundNormal = contact.normal;
                _isGrounded = true;
                return;
            }
        }
    }
    private void OnCollisionExit(Collision collision)
    {
        _currentPlatform = null;
    }
}
