using UnityEngine;

public class Health : MonoBehaviour, IHealth
{
    [Header("Health")]
    [SerializeField] private int _maxHealth = 3;
  
    private int _health;

    public int CurrentHealth => _health;

    public void Initilise()
    {
        _health = _maxHealth;
    }

    public void ApplyDamage(int damageAmount)
    {
        if (damageAmount > 0f && _health > 0f)
            _health = Mathf.Min(_health - damageAmount, _maxHealth);
        else
            Debug.Log("Player Dead");
    }

    public void ApplyHeal(int healAmount)
    {
        if (healAmount > 0 && _health < _maxHealth)
            _health = Mathf.Min(_health + healAmount, _maxHealth);
        else
            Debug.Log("Max Health");
    }
}
