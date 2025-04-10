using UnityEngine;

public class EmptyController : MonoBehaviour
{
    [Header("Cell Info")]
    public int cellHashId = 0;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, Vector3.one);
    }
}
