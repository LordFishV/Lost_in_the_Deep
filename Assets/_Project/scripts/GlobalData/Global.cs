using UnityEngine;

// Enumerators
public enum Stance
{ Idle, Walk, Run, Crouch }

public enum MoveSource
{ None, Keyboard, Click }

public enum Action
{ None, Repair, PickUp }



// Interfaces
public interface IHealth
{
    void ApplyHeal(int healAmount);
    void ApplyDamage(int damageAmount);
}

public interface IInteractable
{
    void Interact(GameObject interactingObject);
    void HoverOverEnter(GameObject interactingObject);
    void HoverOverExit(GameObject interactingObject);
}



// Structs
public struct PlayerInput
{
    public Vector2 Move;
    public Vector2 MouseScreenPosition;
    public bool Click;
    public bool Interact;
    public bool Run;
}
