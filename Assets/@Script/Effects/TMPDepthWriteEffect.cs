using TMPro;
using UnityEngine;

public class TMPDepthWriteEffect : MonoBehaviour
{
    [SerializeField] private int renderQueue = 2450;
    [SerializeField] private bool forceDepthWrite = true;

    private TextMeshPro _textMeshPro;
    private Material _material;

    void Awake()
    {
        Apply();
    }

    void Apply()
    {
        _textMeshPro = GetComponent<TextMeshPro>();
        if (_textMeshPro == null) return;

        // fontSharedMaterial evita duplicar material por instância
        // mas CUIDADO: afeta todos que usam o mesmo material
        // Use fontMaterial se cada objeto precisar de configuração independente

        if(_textMeshPro.fontMaterial == null) return;

        _material = _textMeshPro.fontMaterial; // cópia local

        if (forceDepthWrite)
        {
            _material.SetFloat("_ZWrite", 1f);

            // Desativa o ZTest padrão "always" que o TMP usa em alguns shaders
            _material.SetInt("_ZTestMode", (int)UnityEngine.Rendering.CompareFunction.LessEqual);

            // Coloca na fila do opaco/alpha test, antes dos transparentes
            _material.renderQueue = renderQueue;
        }
    }

    private void OnValidate()
    {
        Apply();
    }

    private void OnDestroy()
    {
        // Limpa a cópia do material para evitar memory leak
        if (_material != null)
            Destroy(_material);
    }
}