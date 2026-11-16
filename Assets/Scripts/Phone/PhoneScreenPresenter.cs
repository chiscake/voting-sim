using UnityEngine;

/// <summary>
/// Shows the phone RT on a copy of the Screen mesh with planar 0..1 UVs over the AABB.
/// Geometry keeps the phone's rounded silhouette so rectangular UI corners are clipped by the mesh.
/// UV space is shared with PhoneScreenInputBridge (origin bottom-left, V up).
/// </summary>
[DisallowMultipleComponent]
public class PhoneScreenPresenter : MonoBehaviour
{
    [SerializeField] private Renderer screenRenderer;
    [SerializeField] private MeshFilter screenMeshFilter;

    private Material _runtimeMaterial;
    private GameObject _displayQuad;
    private MeshCollider _quadCollider;
    private MeshFilter _quadFilter;
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    public Collider DisplayCollider => _quadCollider;
    public Transform DisplayTransform => _displayQuad != null ? _displayQuad.transform : transform;
    public Bounds DisplayLocalBounds { get; private set; }

    private void Awake()
    {
        EnsureDisplayReady();
    }

    /// <summary>
    /// Builds the display mesh if needed (safe to call before Awake order settles).
    /// </summary>
    public void EnsureDisplayReady()
    {
        if (screenRenderer == null)
            screenRenderer = GetComponent<Renderer>();
        if (screenMeshFilter == null)
            screenMeshFilter = GetComponent<MeshFilter>();

        if (_displayQuad == null)
            BuildDisplayMesh();
    }

    private void OnDestroy()
    {
        if (_runtimeMaterial != null)
            Destroy(_runtimeMaterial);
        if (_displayQuad != null)
            Destroy(_displayQuad);
    }

    public void SetRenderTexture(RenderTexture texture)
    {
        EnsureDisplayReady();
        if (_runtimeMaterial == null || texture == null)
            return;

        if (_runtimeMaterial.HasProperty(BaseColorId))
            _runtimeMaterial.SetColor(BaseColorId, Color.white);
        if (_runtimeMaterial.HasProperty(ColorId))
            _runtimeMaterial.SetColor(ColorId, Color.white);

        // No scale/offset flips — RT orientation must already be correct.
        if (_runtimeMaterial.HasProperty(BaseMapId))
        {
            _runtimeMaterial.SetTexture(BaseMapId, texture);
            _runtimeMaterial.SetTextureScale(BaseMapId, Vector2.one);
            _runtimeMaterial.SetTextureOffset(BaseMapId, Vector2.zero);
        }

        if (_runtimeMaterial.HasProperty(MainTexId))
        {
            _runtimeMaterial.SetTexture(MainTexId, texture);
            _runtimeMaterial.SetTextureScale(MainTexId, Vector2.one);
            _runtimeMaterial.SetTextureOffset(MainTexId, Vector2.zero);
        }
    }

    public bool TryWorldPointToUv(Vector3 worldPoint, out Vector2 uv)
    {
        uv = default;
        Bounds b = DisplayLocalBounds;
        Vector3 local = DisplayTransform.InverseTransformPoint(worldPoint);

        float u = Mathf.InverseLerp(b.min.x, b.max.x, local.x);
        float v = Mathf.InverseLerp(b.min.z, b.max.z, local.z);
        if (b.size.z < 1e-5f)
            v = Mathf.InverseLerp(b.min.y, b.max.y, local.y);

        uv = new Vector2(Mathf.Clamp01(u), Mathf.Clamp01(v));
        return true;
    }

    private void BuildDisplayMesh()
    {
        if (screenMeshFilter == null || screenMeshFilter.sharedMesh == null)
            return;

        Mesh source = screenMeshFilter.sharedMesh;
        Bounds b = source.bounds;
        DisplayLocalBounds = b;

        Vector3[] srcVerts = source.vertices;
        Vector3[] verts = new Vector3[srcVerts.Length];
        Vector2[] uvs = new Vector2[srcVerts.Length];

        // Slight forward bias avoids z-fighting with the disabled original Screen renderer.
        float forward = Mathf.Max(b.extents.y, 0.0005f) * 0.05f + 0.0005f;
        Vector3 bias = new Vector3(0f, forward, 0f);

        for (int i = 0; i < srcVerts.Length; i++)
        {
            Vector3 v = srcVerts[i] + bias;
            verts[i] = v;
            // Planar UVs over the full AABB — RT corners map past the rounded silhouette and are clipped.
            float u = Mathf.InverseLerp(b.min.x, b.max.x, srcVerts[i].x);
            float vv = Mathf.InverseLerp(b.min.z, b.max.z, srcVerts[i].z);
            if (b.size.z < 1e-5f)
                vv = Mathf.InverseLerp(b.min.y, b.max.y, srcVerts[i].y);
            uvs[i] = new Vector2(u, vv);
        }

        var mesh = new Mesh { name = "PhoneScreenDisplayMesh" };
        mesh.vertices = verts;
        mesh.triangles = source.triangles;
        mesh.uv = uvs;
        if (source.normals != null && source.normals.Length == srcVerts.Length)
        {
            mesh.normals = source.normals;
        }
        else
        {
            mesh.RecalculateNormals();
        }

        mesh.RecalculateBounds();

        if (_displayQuad != null)
            Destroy(_displayQuad);

        _displayQuad = new GameObject("PhoneScreenDisplayMesh");
        _displayQuad.transform.SetParent(transform, false);
        _displayQuad.layer = gameObject.layer;

        _quadFilter = _displayQuad.AddComponent<MeshFilter>();
        _quadFilter.sharedMesh = mesh;

        var quadRenderer = _displayQuad.AddComponent<MeshRenderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Texture");
        _runtimeMaterial = new Material(shader) { name = "PhoneScreenRT_Runtime" };
        if (_runtimeMaterial.HasProperty(BaseColorId))
            _runtimeMaterial.SetColor(BaseColorId, Color.white);
        quadRenderer.sharedMaterial = _runtimeMaterial;

        _quadCollider = _displayQuad.AddComponent<MeshCollider>();
        _quadCollider.sharedMesh = mesh;

        if (screenRenderer != null)
            screenRenderer.enabled = false;

        var originalCol = GetComponent<MeshCollider>();
        if (originalCol != null)
            originalCol.enabled = false;
    }
}
