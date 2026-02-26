using UnityEngine;

/// <summary>
/// Expands the mesh bounding box to account for vertex displacement
/// from the vortex shader. Without this, Unity's frustum culling
/// uses the original flat plane bounds and hides the object when
/// the camera looks into the displaced funnel.
/// 
/// Attach to the same GameObject that has the OceanVortex material.
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
public class ExpandMeshBounds : MonoBehaviour
{
    [Tooltip("How much to expand the bounds downward (should match or exceed Vortex Depth)")]
    public float expandDown = 30f;

    [Tooltip("How much to expand horizontally (for pull displacement)")]
    public float expandHorizontal = 5f;

    void Start()
    {
        Apply();
    }

    void OnValidate()
    {
        Apply();
    }

    void Apply()
    {
        var mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;

        // We need to modify bounds on the mesh instance, not the shared asset
        Mesh mesh = mf.sharedMesh;
        Bounds b = mesh.bounds;

        // Expand the bounds to encompass the deepest point of the vortex
        b.Expand(new Vector3(expandHorizontal * 2f, expandDown * 2f, expandHorizontal * 2f));

        // Shift center down so the expanded box covers below the plane
        b.center = new Vector3(b.center.x, b.center.y - expandDown * 0.5f, b.center.z);

        mesh.bounds = b;
    }
}
