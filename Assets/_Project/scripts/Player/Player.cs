using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private PlayerCharacter _playerCharacter;
    [SerializeField] private PlayerCamera _playerCamera;
    [SerializeField] private bool _inDiveGear = true;

    private PlayerInputAction _inputActions;
    private Camera _mainCamera;

    private void Start()
    {
        _inputActions = new PlayerInputAction();
        _inputActions.Enable();

        _mainCamera = Camera.main;

        _playerCharacter.Initilise();
        _playerCamera.Initilise(_playerCharacter.CameraAnchor);
    }

    private void OnDestroy()
    {
        _inputActions?.Dispose();
    }

    private void Update()
    {
        PlayerInputAction.PlayerActions input = _inputActions.Player;
        PlayerInput playerInput = new PlayerInput
        {
            Move                = input.Move.ReadValue<Vector2>(),
            MouseScreenPosition = input.MouseScreenPosition.ReadValue<Vector2>(),
            Click               = input.Click.WasPressedThisFrame(),
            Interact            = input.Interact.WasPressedThisFrame(),
            Run                 = input.Run.IsPressed()
        };

        _playerCharacter.UpdateInput(playerInput);

        // Only raycast when there's actually a click to process
        bool hasMouse = false;
        Vector3 mouseWorld = default;
        if (playerInput.Click)
            hasMouse = _playerCharacter.TryGetMouseWorldPosition(playerInput, _mainCamera, out mouseWorld);

        _playerCharacter.UpdateVelocity(_mainCamera.transform, hasMouse, mouseWorld, _inDiveGear);
        _playerCharacter.UpdateBody();

        // Inventory
    }

    private void LateUpdate()
    {
        // After the character has moved, so the camera never lags a frame behind
        _playerCamera.UpdatePosition(_playerCharacter.CameraAnchor);
    }
}