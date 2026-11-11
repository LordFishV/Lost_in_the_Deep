using UnityEngine;

public enum Stance
{ Idle, Walk, Run, Crouch }

public enum MoveSource
{ None, Keyboard, Click}

public interface IHealable
{
    void ApplyHeal(float healAmount, GameObject affectingObject);
}

public interface IDamageable
{
    void ApplyDamage(float damageAmount, GameObject affectingObject);
}

public interface IInteractable
{
    void Interact(GameObject interactingObject);
}

public struct PlayerInput
{
    public Vector2 Move;
    public Vector2 MouseScreenPosition;
    public bool Click;
    public bool Interact;
    public bool Run;
}
