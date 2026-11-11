using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CharacterController))]
public class PlayerCharacter : MonoBehaviour
{
    [SerializeField] private Transform _cameraAnchor;
    [SerializeField] private CharacterController _characterController;

    [Header("Movement")]
    [SerializeField] private float _walkSpeed = 10f;
    [SerializeField] private float _runSpeed = 15f;
    [SerializeField] private float _gearWalkSpeed = 5f;
    [SerializeField] private float _gearRunSpeed = 10f;
    [SerializeField] private float _stoppingDistance = 0.1f;

    [Header("Ground")]
    [SerializeField] private LayerMask _groundMask = ~0;
    [SerializeField] private float _groundCheckDistance = 0.3f;
    [SerializeField] private float _gravity = -25f;
    [SerializeField] private float _groundedStickForce = -2f;

    [Header("Pathfinding")]
    [Tooltip("How far from a click or from the player we search for the nearest NavMesh point")]
    [SerializeField] private float _navSampleDistance = 2f;
    [SerializeField] private float _cornerReachDistance = 0.3f;
    [Tooltip("Cancel click movement if we make no progress for this long (seconds)")]
    [SerializeField] private float _stuckTimeout = 0.5f;

    [Header("Rotation")]
    [Tooltip("How fast the character will rotate in the direction of the movement vector")]
    [SerializeField] private float _turnSmoothTime = 0.1f;

    // Movement state
    private MoveSource _moveSource = MoveSource.None;
    private Stance _stance;
    private Vector3 _keyboardInput;
    private Vector3 _lastKeyboardInput;
    private Vector3 _velocity;
    private float _turnSmoothVelocity;
    private float _stuckTimer;
    private bool _isMoving;
    private bool _isInGear;

    // Ground state
    private Vector3 _groundNormal = Vector3.up;
    private bool _isGrounded;
    private float _verticalVelocity;

    // Path state
    private NavMeshPath _path;
    private Vector3[] _corners = System.Array.Empty<Vector3>();
    private int _cornerIndex;

    // Requested by input
    private bool _requestedClick;
    private bool _requestedInteract;

    public Transform CameraAnchor => _cameraAnchor;

    private void Reset()
    {
        _characterController = GetComponent<CharacterController>();
    }

    public void Initilise()
    {
        _path = new NavMeshPath();
    }

    // ---------------------------------------------------------------- Input

    public void UpdateInput(PlayerInput input)
    {
        _keyboardInput = Vector3.ClampMagnitude(new Vector3(input.Move.x, 0f, input.Move.y), 1f);

        // WASD takes over when an axis that was idle last frame becomes active.
        // Releasing one of two held keys doesn't count, so it won't cancel a click.
        bool newKeyboardInput =
            (_keyboardInput.x != 0f && _lastKeyboardInput.x == 0f) ||
            (_keyboardInput.z != 0f && _lastKeyboardInput.z == 0f);
        _lastKeyboardInput = _keyboardInput;

        if (newKeyboardInput)
            _moveSource = MoveSource.Keyboard;

        _requestedClick = input.Click;
        _requestedInteract = input.Interact;
        _stance = input.Run ? Stance.Run : Stance.Walk;
    }

    // ------------------------------------------------------------- Movement

    public void UpdateVelocity(Transform cameraTransform, bool hasMouseWorld, Vector3 mouseWorldPosition, bool inDiveGear)
    {
        _isInGear = inDiveGear;
        UpdateGround();

        // A click replaces whatever we were doing, if it lands somewhere we can path to
        if (_requestedClick && hasMouseWorld && !IsPointerOverUI() && TrySetPath(mouseWorldPosition))
        {
            _moveSource = MoveSource.Click;
            _stuckTimer = 0f;
        }

        // Flat (XZ) direction for the active source
        Vector3 direction = Vector3.zero;
        float inputStrength = 1f;
        float distanceToTarget = float.MaxValue;
        bool clampToTarget = false;

        switch (_moveSource)
        {
            case MoveSource.Keyboard:
                if (_keyboardInput.sqrMagnitude > 0f)
                {
                    // Make WASD relative to the camera's yaw
                    float yaw = cameraTransform != null ? cameraTransform.eulerAngles.y : 0f;
                    Vector3 worldInput = Quaternion.Euler(0f, yaw, 0f) * _keyboardInput;
                    inputStrength = worldInput.magnitude;
                    direction = worldInput / inputStrength;
                }
                else
                {
                    _moveSource = MoveSource.None;
                }
                break;

            case MoveSource.Click:
                if (!FollowPath(out direction, out distanceToTarget, out clampToTarget))
                    _moveSource = MoveSource.None; // arrived
                break;
        }

        _isMoving = direction.sqrMagnitude > 0f;

        Vector3 slopeDirection = Vector3.zero;
        float step = 0f;
        float moveSpeed = 0f;

        if (_isMoving)
        {
            moveSpeed = (_isInGear, _stance) switch
            {
                (true, Stance.Run) => _gearRunSpeed,
                (true, _) => _gearWalkSpeed,
                (false, Stance.Run) => _runSpeed,
                (false, _) => _walkSpeed
            };

            // Project onto the ground plane so we follow the slope (walkable slopes only)
            bool walkable = _isGrounded && Vector3.Angle(Vector3.up, _groundNormal) <= _characterController.slopeLimit;
            slopeDirection = walkable
                ? Vector3.ProjectOnPlane(direction, _groundNormal).normalized
                : direction;

            step = moveSpeed * inputStrength * Time.deltaTime;

            // Don't overshoot the final destination. Only the last corner needs this,
            // otherwise we'd slow down at every bend.
            if (clampToTarget)
            {
                float horizontalRatio = new Vector2(slopeDirection.x, slopeDirection.z).magnitude;
                step = Mathf.Min(step, distanceToTarget / Mathf.Max(horizontalRatio, 0.01f));
            }
        }

        // Gravity runs every frame so we stay planted on slopes and fall off ledges
        if (_isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = _groundedStickForce;
        else
            _verticalVelocity += _gravity * Time.deltaTime;

        Vector3 motion = slopeDirection * step + Vector3.up * (_verticalVelocity * Time.deltaTime);
        _characterController.Move(motion);
        _velocity = _characterController.velocity;

        // Give up on a click path if we're pressed against something and not getting anywhere
        if (_moveSource == MoveSource.Click)
        {
            float flatSpeed = new Vector2(_velocity.x, _velocity.z).magnitude;
            _stuckTimer = flatSpeed < moveSpeed * 0.1f ? _stuckTimer + Time.deltaTime : 0f;

            if (_stuckTimer >= _stuckTimeout)
                _moveSource = MoveSource.None;
        }
    }

    public void UpdateBody()
    {
        if (!_isMoving) return;

        Vector3 flatVelocity = new Vector3(_velocity.x, 0f, _velocity.z);
        if (flatVelocity.sqrMagnitude < 0.0001f) return; // Blocked by an object

        float targetAngle = Mathf.Atan2(flatVelocity.x, flatVelocity.z) * Mathf.Rad2Deg;
        float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, _turnSmoothTime);
        transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
    }

    // ------------------------------------------------------------- Pathing

    private bool TrySetPath(Vector3 worldTarget)
    {
        // Snap both ends onto the NavMesh, since clicks can land on props or slightly off the mesh
        if (!NavMesh.SamplePosition(worldTarget, out NavMeshHit targetHit, _navSampleDistance, NavMesh.AllAreas))
            return false;
        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit startHit, _navSampleDistance, NavMesh.AllAreas))
            return false;

        if (!NavMesh.CalculatePath(startHit.position, targetHit.position, NavMesh.AllAreas, _path))
            return false;
        if (_path.status == NavMeshPathStatus.PathInvalid || _path.corners.Length < 2)
            return false;

        _corners = _path.corners;
        _cornerIndex = 1; // corner 0 is where we're standing
        return true;
    }

    /// <summary>Returns false once the final corner has been reached.</summary>
    private bool FollowPath(out Vector3 direction, out float distance, out bool onLastCorner)
    {
        direction = Vector3.zero;
        distance = 0f;
        onLastCorner = false;

        while (_cornerIndex < _corners.Length)
        {
            Vector3 toCorner = _corners[_cornerIndex] - transform.position;
            toCorner.y = 0f; // judge arrival on flat distance so height differences can't stop us arriving

            bool isLast = _cornerIndex == _corners.Length - 1;
            float reach = isLast ? _stoppingDistance : _cornerReachDistance;
            float dist = toCorner.magnitude;

            if (dist > reach)
            {
                direction = toCorner / dist;
                distance = dist;
                onLastCorner = isLast;
                return true;
            }

            _cornerIndex++;
        }

        return false;
    }

    // --------------------------------------------------------------- Ground

    private void UpdateGround()
    {
        float radius = _characterController.radius * 0.9f;

        // Start the cast inside the capsule's lower sphere so it still hits when we're on the ground
        Vector3 origin = transform.position + Vector3.up * (radius + _characterController.skinWidth + 0.05f);
        float castDistance = _characterController.skinWidth + 0.05f + _groundCheckDistance;

        if (Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit, castDistance, _groundMask,
                QueryTriggerInteraction.Ignore))
        {
            _isGrounded = true;
            _groundNormal = hit.normal;
        }
        else
        {
            _isGrounded = false;
            _groundNormal = Vector3.up;
        }
    }

    // ---------------------------------------------------------------- Mouse

    public bool TryGetMouseWorldPosition(PlayerInput input, Camera mainCamera, out Vector3 worldPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(input.MouseScreenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _groundMask, QueryTriggerInteraction.Ignore))
        {
            worldPosition = hit.point;
            return true;
        }

        worldPosition = default;
        return false;
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}