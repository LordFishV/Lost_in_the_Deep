using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Tooltip("0 = rigid follow. Higher values trail the player more smoothly.")]
    [SerializeField] private float _followSmoothTime = 0.08f;

    private Vector3 _followVelocity;

    public void Initilise(Transform anchor)
    {
        transform.position = anchor.position;
        _followVelocity = Vector3.zero;
    }

    public void UpdatePosition(Transform anchor)
    {
        transform.position = Vector3.SmoothDamp(transform.position, anchor.position, ref _followVelocity, _followSmoothTime);
    }
}