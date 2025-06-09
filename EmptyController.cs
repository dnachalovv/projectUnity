using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class EmptyController : MonoBehaviour
{
    #region Statics
    public static EmptyController[] GetEmpties()
    {
        return FindObjectsByType<EmptyController>(FindObjectsSortMode.None);
    }
    #endregion

    [Header("Cell Info")]
    public int cellHashId = 0;

    [Header("Connections")]
    public List<EmptyController> connectedCells = new List<EmptyController>();
    public bool autoConnect = true;
    public float autoConnectRadius = 1.1f;

    [Header("Visuals")]
    public Color lineColor = Color.green;
    [Range(0.01f, 0.2f)] public float lineThickness = 0.05f;
    public bool draw90DegreeBend = true;

    [Header("Grid")]
    public LevelController grid;
    public bool fitToGridPosition = true;
    public bool autoFitCollider = true;

    private BlockController placedBlock;
    public event Action<BlockController> OnBlockPlaced;

    public BlockController PlacedBlock
    {
        get
        {
            return placedBlock;
        }
        set
        {
            // maybe controll this . . .

            placedBlock = value;

            OnBlockPlaced?.Invoke(value);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, Vector3.one);

        if (connectedCells == null || connectedCells.Count == 0) return;

        foreach (var target in connectedCells)
        {
            if (target == null) continue;

            Vector3 start = transform.position;
            Vector3 end = target.transform.position;

            List<Vector3> points = new List<Vector3>();
            if (draw90DegreeBend)
            {
                Vector3 mid = new Vector3(end.x, start.y, 0);
                points.Add(start);
                points.Add(mid);
                points.Add(end);
            }
            else
            {
                points.Add(start);
                points.Add(end);
            }

#if UNITY_EDITOR
            Handles.color = lineColor;
            Handles.DrawAAPolyLine(lineThickness * 10f, points.ToArray());
#endif
        }
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        /*if (!Application.isPlaying) // legacy
        {
            EditorApplication.delayCall += () =>
            {
                if (this == null) return;

                if (autoConnect)
                    AutoConnectNearbyCells();

                if (fitToGridPosition)
                    FitToGridPositionOnly();

                if (autoFitCollider)
                    FitColliderToSprite();
            };
        }*/
#endif
    }

    [ContextMenu("Auto Connect")]
    public void AutoConnectNearbyCells()
    {
        connectedCells.Clear();

#if UNITY_2023_1_OR_NEWER
        var all = FindObjectsByType<EmptyController>(FindObjectsSortMode.None);
#else
        var all = FindObjectsOfType<EmptyController>();
#endif

        foreach (var other in all)
        {
            if (other == this) continue;

            float distance = Vector2.Distance(transform.position, other.transform.position);
            if (distance <= autoConnectRadius)
            {
                if (!connectedCells.Contains(other))
                    connectedCells.Add(other);
                if (!other.connectedCells.Contains(this))
                    other.connectedCells.Add(this);
            }
        }

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    [ContextMenu("Подогнать позицию под сетку")]
    public void FitToGridPositionOnly()
    {
        if (grid == null)
        {
#if UNITY_2023_1_OR_NEWER
            grid = FindFirstObjectByType<LevelController>();
#else
            grid = FindObjectOfType<LevelController>();
#endif
        }

        if (grid == null) return;

        Vector3 pos = transform.position;
        int col = Mathf.RoundToInt((pos.x - grid.GridOrigin.x) / grid.cellSize);
        int row = Mathf.RoundToInt((pos.y - grid.GridOrigin.y) / grid.cellSize);

        transform.position = grid.GetCellCenter(col, row, grid.GridOrigin);

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    [ContextMenu("Подогнать BoxCollider2D под спрайт")]
    public void FitColliderToSprite()
    {
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            Debug.LogWarning("Нет SpriteRenderer или спрайта");
            return;
        }

        var collider = GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider2D>();
        }

        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
        collider.size = spriteSize;
        collider.offset = spriteRenderer.sprite.bounds.center;

#if UNITY_EDITOR
        EditorUtility.SetDirty(collider);
#endif
    }

    public void ReturnPlacedBlock()
    {
        if (!PlacedBlock)
            return;

        PlacedBlock.ReturnBlock();
        PlacedBlock = null;
    }
}
