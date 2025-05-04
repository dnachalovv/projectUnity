using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class EmptyController : MonoBehaviour
{
    [Header("Cell Info")]
    public int cellHashId = 0;

    [Header("Connections (manual or auto-filled)")]
    public List<EmptyController> connectedCells = new();
    public bool autoConnect = false;

    [Header("Gizmo Style")]
    public Color lineColor = Color.green;
    [Range(0.01f, 0.2f)] public float lineThickness = 0.05f;
    public bool draw90DegreeBend = true;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, Vector3.one);

        if (connectedCells == null || connectedCells.Count == 0)
            return;

        foreach (var target in connectedCells)
        {
            if (target == null) continue;

            Vector3 start = transform.position;
            Vector3 end = target.transform.position;

            Gizmos.color = lineColor;

            if (draw90DegreeBend)
            {
                Vector3 mid = new Vector3(end.x, start.y, 0);
                Gizmos.DrawLine(start, mid);
                Gizmos.DrawLine(mid, end);
            }
            else
            {
                Gizmos.DrawLine(start, end);
            }

#if UNITY_EDITOR
            UnityEditor.Handles.color = lineColor;
            UnityEditor.Handles.DrawAAPolyLine(lineThickness * 10f, new Vector3[] { start, end });
#endif
        }
    }
}
