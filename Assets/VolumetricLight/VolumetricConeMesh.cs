using UnityEngine;

namespace VolumetricLight
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [ExecuteInEditMode]
    public class VolumetricConeMesh : MonoBehaviour
    {
        [Header("Cone Shape")]
        [Tooltip("Radius at the base of the cone (near the light source)")]
        [Range(0.01f, 10f)]
        public float baseRadius = 0.1f;

        [Tooltip("Radius at the tip/end of the cone")]
        [Range(0.01f, 20f)]
        public float tipRadius = 2f;

        [Tooltip("Length of the cone")]
        [Range(0.1f, 50f)]
        public float length = 8f;

        [Tooltip("Number of segments around the cone")]
        [Range(8, 64)]
        public int segments = 24;

        [Tooltip("Number of rings along the length")]
        [Range(2, 32)]
        public int rings = 10;

        [Header("Render Range")]
        [Tooltip("Start of visible area (0 = base, 1 = tip)")]
        [Range(0f, 1f)]
        public float renderStart = 0f;

        [Tooltip("End of visible area (0 = base, 1 = tip)")]
        [Range(0f, 1f)]
        public float renderEnd = 1f;

        [Header("Light Settings")]
        [ColorUsage(true, true)]
        public Color lightColor = new Color(1f, 0.95f, 0.8f, 0.5f);

        [Range(0f, 5f)]
        public float intensity = 1.0f;

        [Header("References")]
        public Material coneMaterial;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Mesh _mesh;

        // Cache para detectar mudanças
        private float _lastBaseRadius, _lastTipRadius, _lastLength;
        private int _lastSegments, _lastRings;
        private float _lastRenderStart, _lastRenderEnd;
        private Color _lastColor;
        private float _lastIntensity;

        private void OnEnable()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();

            if (coneMaterial != null)
                _meshRenderer.sharedMaterial = coneMaterial;

            GenerateMesh();
            UpdateMaterialProperties();
        }

        private void Update()
        {
            bool meshDirty = false;
            bool materialDirty = false;

            // Verifica se precisa regerar a mesh
            if (!Mathf.Approximately(_lastBaseRadius, baseRadius) ||
                !Mathf.Approximately(_lastTipRadius, tipRadius) ||
                !Mathf.Approximately(_lastLength, length) ||
                _lastSegments != segments ||
                _lastRings != rings)
            {
                meshDirty = true;
            }

            // Verifica se precisa atualizar o material
            if (!Mathf.Approximately(_lastRenderStart, renderStart) ||
                !Mathf.Approximately(_lastRenderEnd, renderEnd) ||
                _lastColor != lightColor ||
                !Mathf.Approximately(_lastIntensity, intensity))
            {
                materialDirty = true;
            }

            if (meshDirty) GenerateMesh();
            if (materialDirty) UpdateMaterialProperties();
        }

        public void GenerateMesh()
        {
            if (_mesh == null)
            {
                _mesh = new Mesh();
                _mesh.name = "VolumetricCone";
            }
            else
            {
                _mesh.Clear();
            }

            int vertCount = (rings + 1) * (segments + 1);
            int triCount = rings * segments * 6;

            Vector3[] vertices = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            Color[] colors = new Color[vertCount];
            int[] triangles = new int[triCount];

            int vi = 0;
            for (int r = 0; r <= rings; r++)
            {
                float t = (float)r / rings; // 0 = base, 1 = tip
                float radius = Mathf.Lerp(baseRadius, tipRadius, t);
                float z = t * length;

                for (int s = 0; s <= segments; s++)
                {
                    float angle = (float)s / segments * Mathf.PI * 2f;
                    float x = Mathf.Cos(angle) * radius;
                    float y = Mathf.Sin(angle) * radius;

                    vertices[vi] = new Vector3(x, y, z);
                    uvs[vi] = new Vector2((float)s / segments, t);

                    // Vertex color R = normalized height (0=base, 1=tip)
                    // Usado no shader para controlar o render range
                    colors[vi] = new Color(t, 0, 0, 1);

                    vi++;
                }
            }

            int ti = 0;
            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int current = r * (segments + 1) + s;
                    int next = current + segments + 1;

                    triangles[ti++] = current;
                    triangles[ti++] = next;
                    triangles[ti++] = current + 1;

                    triangles[ti++] = current + 1;
                    triangles[ti++] = next;
                    triangles[ti++] = next + 1;
                }
            }

            _mesh.vertices = vertices;
            _mesh.uv = uvs;
            _mesh.colors = colors;
            _mesh.triangles = triangles;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();

            _meshFilter.sharedMesh = _mesh;

            // Atualiza cache
            _lastBaseRadius = baseRadius;
            _lastTipRadius = tipRadius;
            _lastLength = length;
            _lastSegments = segments;
            _lastRings = rings;
        }

        private void UpdateMaterialProperties()
        {
            if (coneMaterial == null && _meshRenderer.sharedMaterial != null)
                coneMaterial = _meshRenderer.sharedMaterial;

            if (coneMaterial == null) return;

            // Garante renderStart < renderEnd
            float start = Mathf.Min(renderStart, renderEnd);
            float end = Mathf.Max(renderStart, renderEnd);

            coneMaterial.SetColor("_Color", lightColor);
            coneMaterial.SetFloat("_Intensity", intensity);
            coneMaterial.SetFloat("_RenderStart", start);
            coneMaterial.SetFloat("_RenderEnd", end);

            // Atualiza cache
            _lastRenderStart = renderStart;
            _lastRenderEnd = renderEnd;
            _lastColor = lightColor;
            _lastIntensity = intensity;
        }

        private void OnDestroy()
        {
            if (_mesh != null)
            {
                if (Application.isPlaying)
                    Destroy(_mesh);
                else
                    DestroyImmediate(_mesh);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(lightColor.r, lightColor.g, lightColor.b, 0.3f);

            // Desenha o cone no editor
            int gizmoSegments = 16;
            for (int i = 0; i < gizmoSegments; i++)
            {
                float a1 = (float)i / gizmoSegments * Mathf.PI * 2f;
                float a2 = (float)(i + 1) / gizmoSegments * Mathf.PI * 2f;

                // Base circle
                Vector3 b1 = new Vector3(Mathf.Cos(a1) * baseRadius, Mathf.Sin(a1) * baseRadius, 0);
                Vector3 b2 = new Vector3(Mathf.Cos(a2) * baseRadius, Mathf.Sin(a2) * baseRadius, 0);
                Gizmos.DrawLine(b1, b2);

                // Tip circle
                Vector3 t1 = new Vector3(Mathf.Cos(a1) * tipRadius, Mathf.Sin(a1) * tipRadius, length);
                Vector3 t2 = new Vector3(Mathf.Cos(a2) * tipRadius, Mathf.Sin(a2) * tipRadius, length);
                Gizmos.DrawLine(t1, t2);

                // Connecting lines
                if (i % 4 == 0)
                    Gizmos.DrawLine(b1, t1);
            }

            // Render range indicators
            Gizmos.color = Color.green;
            float startZ = renderStart * length;
            float endZ = renderEnd * length;
            float startR = Mathf.Lerp(baseRadius, tipRadius, renderStart);
            float endR = Mathf.Lerp(baseRadius, tipRadius, renderEnd);

            for (int i = 0; i < gizmoSegments; i++)
            {
                float a1 = (float)i / gizmoSegments * Mathf.PI * 2f;
                float a2 = (float)(i + 1) / gizmoSegments * Mathf.PI * 2f;

                Vector3 s1 = new Vector3(Mathf.Cos(a1) * startR, Mathf.Sin(a1) * startR, startZ);
                Vector3 s2 = new Vector3(Mathf.Cos(a2) * startR, Mathf.Sin(a2) * startR, startZ);
                Gizmos.DrawLine(s1, s2);

                Vector3 e1 = new Vector3(Mathf.Cos(a1) * endR, Mathf.Sin(a1) * endR, endZ);
                Vector3 e2 = new Vector3(Mathf.Cos(a2) * endR, Mathf.Sin(a2) * endR, endZ);
                Gizmos.DrawLine(e1, e2);
            }
        }
    }
}
