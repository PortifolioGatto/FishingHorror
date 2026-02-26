using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CircularGridOcean : MonoBehaviour
{
    public Transform follow;
    public int resolution = 128; // grid x grid
    public float radius = 500f;

    private void Update()
    {
        transform.position = new Vector3(follow.position.x, transform.position.y, follow.position.z);
    }

    [ContextMenu("Generate Mesh")]
    public void GenerateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Circular Grid Ocean";

        List<Vector3> vertices = new();
        List<int> triangles = new();
        List<Vector2> uvs = new();

        float size = radius * 2f;
        float step = size / resolution;
        float half = size * 0.5f;

        // ---------- VÉRTICES ----------
        for (int z = 0; z <= resolution; z++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                float px = x * step - half;
                float pz = z * step - half;

                vertices.Add(new Vector3(px, 0, pz));
                uvs.Add(new Vector2((float)x / resolution, (float)z / resolution));
            }
        }

        // ---------- TRIÂNGULOS ----------
        int vertCount = resolution + 1;

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i0 = z * vertCount + x;
                int i1 = i0 + 1;
                int i2 = i0 + vertCount;
                int i3 = i2 + 1;

                // Centro do quad
                Vector3 center =
                    (vertices[i0] + vertices[i1] + vertices[i2] + vertices[i3]) * 0.25f;

                // Se o quad estiver fora do círculo, ignora
                if (center.magnitude > radius)
                    continue;

                // Triângulo 1 (CCW)
                triangles.Add(i0);
                triangles.Add(i2);
                triangles.Add(i1);

                // Triângulo 2 (CCW)
                triangles.Add(i1);
                triangles.Add(i2);
                triangles.Add(i3);
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;
    }
}
