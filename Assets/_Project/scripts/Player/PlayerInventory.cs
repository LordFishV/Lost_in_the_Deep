using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private int _currentScrap;
    [SerializeField] private int _maxScrap = 10;

    public int CurrentScrap => _currentScrap;

    public void AddScrap()
    {
        if (_currentScrap < _maxScrap)
            _currentScrap++;
        else
            Debug.Log("Max Scrap");
    }

    public void RemoveScrap()
    {
        if (_currentScrap > 0)
            _currentScrap--;
        else
            Debug.Log("No Scrap");
    }
}
