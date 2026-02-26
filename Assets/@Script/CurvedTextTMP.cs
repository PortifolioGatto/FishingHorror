using UnityEngine;
using TMPro;

[ExecuteAlways]
public class CurvedTextTMP : MonoBehaviour
{
    public float curveRadius = 200f;
    public float curveAngle = 180f;

    public Vector3 curveCenter = Vector3.zero;

    private TMP_Text textComponent;

    void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
    }

    void Update()
    {
        if (!textComponent) return;

        textComponent.ForceMeshUpdate();
        var textInfo = textComponent.textInfo;

        int characterCount = textInfo.characterCount;
        if (characterCount == 0) return;

        float totalAngle = curveAngle;
        float anglePerChar = totalAngle / characterCount;

        for (int i = 0; i < characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible)
                continue;

            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;

            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            Vector3 charMidBaseline =
                (vertices[vertexIndex + 0] +
                 vertices[vertexIndex + 2]) / 2;

            float angle = -totalAngle / 2 + anglePerChar * i;
            float rad = angle * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(
                Mathf.Sin(rad) * curveRadius,
                Mathf.Cos(rad) * curveRadius,
                0);

            offset += curveCenter;

            Matrix4x4 matrix = Matrix4x4.TRS(
                offset,
                Quaternion.Euler(0, 0, -angle),
                Vector3.one);

            for (int j = 0; j < 4; j++)
            {
                vertices[vertexIndex + j] -= charMidBaseline;
                vertices[vertexIndex + j] = matrix.MultiplyPoint3x4(vertices[vertexIndex + j]);
            }
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            textComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }
}