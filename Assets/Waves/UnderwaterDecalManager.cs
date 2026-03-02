using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages underwater decals. Collects all UnderwaterDecalEmitter components
/// and uploads their data (including full inverse transform matrix) as shader globals.
///
/// Shader globals:
///   _UnderwaterDecalCount
///   _UnderwaterDecalPositions[8]  // xyz = world pos, w = depth below water
///   _UnderwaterDecalParams[8]     // x = opacity, y = edgeFade, z = distortion, w = distortSpeed
///   _UnderwaterDecalColors[8]     // rgb = tint, a = waterTintStrength
///   _UnderwaterDecalExtra[8]      // x = darken, y = desaturate, z/w = unused
///   _UnderwaterDecalMatRow0[8]    // inverse transform matrix row 0
///   _UnderwaterDecalMatRow1[8]    // inverse transform matrix row 1
///   _UnderwaterDecalMatRow2[8]    // inverse transform matrix row 2
/// </summary>
[ExecuteInEditMode]
public class UnderwaterDecalManager : MonoBehaviour
{
    public static UnderwaterDecalManager Instance { get; private set; }

    [Tooltip("Y position of the water surface (for auto depth calculation)")]
    public float waterSurfaceY = 0f;

    const int MAX_DECALS = 8;

    private List<UnderwaterDecalEmitter> emitters = new List<UnderwaterDecalEmitter>();

    // Shader arrays
    private Vector4[] positions = new Vector4[MAX_DECALS];
    private Vector4[] paramArray = new Vector4[MAX_DECALS];
    private Vector4[] colors = new Vector4[MAX_DECALS];
    private Vector4[] extras = new Vector4[MAX_DECALS];
    private Vector4[] matRow0 = new Vector4[MAX_DECALS];
    private Vector4[] matRow1 = new Vector4[MAX_DECALS];
    private Vector4[] matRow2 = new Vector4[MAX_DECALS];

    // Shader IDs
    static readonly int ID_Count     = Shader.PropertyToID("_UnderwaterDecalCount");
    static readonly int ID_Positions = Shader.PropertyToID("_UnderwaterDecalPositions");
    static readonly int ID_Params    = Shader.PropertyToID("_UnderwaterDecalParams");
    static readonly int ID_Colors    = Shader.PropertyToID("_UnderwaterDecalColors");
    static readonly int ID_Extra     = Shader.PropertyToID("_UnderwaterDecalExtra");
    static readonly int ID_MatRow0   = Shader.PropertyToID("_UnderwaterDecalMatRow0");
    static readonly int ID_MatRow1   = Shader.PropertyToID("_UnderwaterDecalMatRow1");
    static readonly int ID_MatRow2   = Shader.PropertyToID("_UnderwaterDecalMatRow2");
    static readonly int ID_TexArray  = Shader.PropertyToID("_UnderwaterDecalTextures");

    // Texture array
    private Texture2DArray texArray;
    private Texture2D[] lastTextures = new Texture2D[MAX_DECALS];
    private bool texArrayDirty = true;

    void Awake() { Instance = this; }
    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (texArray != null) DestroyImmediate(texArray);
    }

    public void Register(UnderwaterDecalEmitter emitter)
    {
        if (!emitters.Contains(emitter))
        {
            emitters.Add(emitter);
            texArrayDirty = true;
        }
    }

    public void Unregister(UnderwaterDecalEmitter emitter)
    {
        emitters.Remove(emitter);
        texArrayDirty = true;
    }

    void LateUpdate()
    {
        emitters.RemoveAll(e => e == null || !e.isActiveAndEnabled);

        int count = Mathf.Min(emitters.Count, MAX_DECALS);
        bool needTexRebuild = texArrayDirty;

        for (int i = 0; i < count; i++)
        {
            var e = emitters[i];
            Vector3 pos = e.transform.position;
            float depthBelow = e.GetDepthBelow(waterSurfaceY);

            // Auto depth effect
            float depthNorm = Mathf.Clamp01(depthBelow / 20f); // normalize: 20 units = max
            float autoDarken = depthNorm * 0.5f * e.autoDepthEffect;
            float autoDesat = depthNorm * 0.4f * e.autoDepthEffect;

            positions[i] = new Vector4(pos.x, pos.y, pos.z, depthBelow);
            paramArray[i] = new Vector4(e.opacity, e.edgeFade, e.waveDistortion, e.distortionSpeed);
            colors[i] = new Vector4(e.tintColor.r, e.tintColor.g, e.tintColor.b, e.waterTintStrength);
            extras[i] = new Vector4(
                Mathf.Clamp01(e.extraDarken + autoDarken),
                Mathf.Clamp01(e.extraDesaturate + autoDesat),
                0, 0
            );

            // Inverse transform matrix (world → local)
            // This encodes position, rotation, AND scale all at once.
            // In the shader: localPos = invMatrix * (worldPos - decalPos)
            // If localPos.xz is in [-0.5, 0.5] → pixel is inside the decal
            Matrix4x4 worldToLocal = e.transform.worldToLocalMatrix;
            matRow0[i] = new Vector4(worldToLocal.m00, worldToLocal.m01, worldToLocal.m02, worldToLocal.m03);
            matRow1[i] = new Vector4(worldToLocal.m10, worldToLocal.m11, worldToLocal.m12, worldToLocal.m13);
            matRow2[i] = new Vector4(worldToLocal.m20, worldToLocal.m21, worldToLocal.m22, worldToLocal.m23);

            if (lastTextures[i] != e.texture)
            {
                lastTextures[i] = e.texture;
                needTexRebuild = true;
            }
        }

        // Zero unused
        for (int i = count; i < MAX_DECALS; i++)
        {
            positions[i] = Vector4.zero;
            paramArray[i] = Vector4.zero;
            colors[i] = Vector4.zero;
            extras[i] = Vector4.zero;
            matRow0[i] = Vector4.zero;
            matRow1[i] = Vector4.zero;
            matRow2[i] = Vector4.zero;
            if (lastTextures[i] != null) { lastTextures[i] = null; needTexRebuild = true; }
        }

        if (needTexRebuild)
            RebuildTextureArray(count);

        // Upload
        Shader.SetGlobalFloat(ID_Count, count);
        Shader.SetGlobalVectorArray(ID_Positions, positions);
        Shader.SetGlobalVectorArray(ID_Params, paramArray);
        Shader.SetGlobalVectorArray(ID_Colors, colors);
        Shader.SetGlobalVectorArray(ID_Extra, extras);
        Shader.SetGlobalVectorArray(ID_MatRow0, matRow0);
        Shader.SetGlobalVectorArray(ID_MatRow1, matRow1);
        Shader.SetGlobalVectorArray(ID_MatRow2, matRow2);

        if (texArray != null)
            Shader.SetGlobalTexture(ID_TexArray, texArray);
    }

    void RebuildTextureArray(int count)
    {
        texArrayDirty = false;
        if (texArray != null) Destroy(texArray);

        int resolution = 256;
        texArray = new Texture2DArray(resolution, resolution, Mathf.Max(count, 1), TextureFormat.RGBA32, true);
        texArray.filterMode = FilterMode.Bilinear;
        texArray.wrapMode = TextureWrapMode.Clamp;

        for (int i = 0; i < Mathf.Max(count, 1); i++)
        {
            Texture2D src = (i < count && lastTextures[i] != null) ? lastTextures[i] : Texture2D.whiteTexture;

            RenderTexture rt = RenderTexture.GetTemporary(resolution, resolution, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(src, rt);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D resized = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            resized.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
            resized.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            texArray.SetPixels(resized.GetPixels(), i);
            DestroyImmediate(resized);
        }
        texArray.Apply();
    }
}
