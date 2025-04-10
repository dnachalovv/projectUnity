using UnityEngine;

public class LevelController : MonoBehaviour
{
    [Header("Grid Settings")]
    public float cellSize = 1f;

    void Start()
    {
        Debug.Log($"LevelController активен. Размер ячейки: {cellSize}");
    }
}
