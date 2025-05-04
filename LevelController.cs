using UnityEngine;

[ExecuteInEditMode]
public class LevelController : MonoBehaviour
{
    [Header("Grid Size")]
    [Tooltip("Количество столбцов в сетке")]
    public int columns = 5;

    [Tooltip("Количество строк в сетке")]
    public int rows = 5;

    [Tooltip("Размер одной ячейки")]
    public float cellSize = 1f;

    [Header("Grid Origin (смещение)")]
    [Tooltip("Начальная точка сетки в мировых координатах")]
    public Vector2 gridOrigin = Vector2.zero;

    [Header("Grid Visual Appearance")]
    [Tooltip("Цвет линий сетки")]
    public Color lineColor = Color.gray;

    [Tooltip("Толщина линий сетки")]
    [Range(0.1f, 5f)]
    public float lineThickness = 1f;

    [Tooltip("Прозрачность заливки ячеек")]
    [Range(0f, 1f)]
    public float fillAlpha = 0.1f;

    private void OnDrawGizmos()
    {
        if (Application.isPlaying) return;

        Gizmos.color = new Color(lineColor.r, lineColor.g, lineColor.b, fillAlpha);

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector3 cellCenter = GetCellCenter(x, y);
                Vector3 size = new Vector3(cellSize, cellSize, 0f);
                Gizmos.DrawCube(cellCenter, size);
            }
        }

        Gizmos.color = lineColor;
        float thickness = lineThickness * 0.01f;

        for (int x = 0; x <= columns; x++)
        {
            Vector3 from = new Vector3(gridOrigin.x + x * cellSize, gridOrigin.y, 0f);
            Vector3 to = new Vector3(gridOrigin.x + x * cellSize, gridOrigin.y + rows * cellSize, 0f);
            Gizmos.DrawLine(from, to);
        }

        for (int y = 0; y <= rows; y++)
        {
            Vector3 from = new Vector3(gridOrigin.x, gridOrigin.y + y * cellSize, 0f);
            Vector3 to = new Vector3(gridOrigin.x + columns * cellSize, gridOrigin.y + y * cellSize, 0f);
            Gizmos.DrawLine(from, to);
        }

        DrawGridOutline();
    }

    private void DrawGridOutline()
    {
        Gizmos.color = Color.black;
        Vector3 bottomLeft = new Vector3(gridOrigin.x, gridOrigin.y, 0f);
        Vector3 topLeft = new Vector3(gridOrigin.x, gridOrigin.y + rows * cellSize, 0f);
        Vector3 topRight = new Vector3(gridOrigin.x + columns * cellSize, gridOrigin.y + rows * cellSize, 0f);
        Vector3 bottomRight = new Vector3(gridOrigin.x + columns * cellSize, gridOrigin.y, 0f);

        Gizmos.DrawLine(bottomLeft, topLeft);
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
    }

    public Vector3 GetCellCenter(int column, int row)
    {
        return new Vector3(
            gridOrigin.x + column * cellSize + cellSize / 2f,
            gridOrigin.y + row * cellSize + cellSize / 2f,
            0f
        );
    }

    private void OnValidate()
    {
        if (columns < 1) columns = 1;
        if (rows < 1) rows = 1;
        if (cellSize < 0.1f) cellSize = 0.1f;
    }
}
