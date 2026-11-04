using UnityEngine;

/// <summary>
/// Jacko Texture Atlas Adapter
/// This script adapts the Nightmare Hollow Halloween Texture Atlas for use with the Jacko AI character.
/// Attach to the same GameObject as JackoAI or to a child with Renderers.
/// </summary>
public class JackoTextureAtlasAdapter : MonoBehaviour
{
    [Header("Atlas Reference")]
    [Tooltip("The Halloween texture atlas")]
    public Texture2D halloweenAtlas;

    [Header("Atlas Settings")]
    [Tooltip("Typical tile size for this atlas (adjust if needed)")]
    public int tileSize = 128;

    [Tooltip("Atlas grid columns")]
    public int atlasColumns = 16;

    [Tooltip("Atlas grid rows")]
    public int atlasRows = 16;

    // Cached sub-textures
    private Dictionary<string, Texture2D> subTextures = new Dictionary<string, Texture2D>();

    // References
    private JackoAI jacko;
    private Renderer bodyRenderer;
    private Renderer headRenderer;
    private Material originalBodyMat;
    private Material originalHeadMat;

    void Awake()
    {
        // Find JackoAI component
        jacko = GetComponent<JackoAI>();
        if (jacko == null)
        {
            jacko = GetComponentInParent<JackoAI>();
        }

        // Find renderers - body and head
        bodyRenderer = GetComponentInChildren<Renderer>();
        Transform headTransform = transform.Find("fej");
        if (headTransform != null)
        {
            headRenderer = headTransform.GetComponentInChildren<Renderer>();
        }

        // Try to auto-find atlas
        if (halloweenAtlas == null)
        {
            halloweenAtlas = Resources.Load<Texture2D>("NightmareHollow_TextureAtlas");
            Debug.Log($"[JackoAtlas] Auto-loaded atlas: {halloweenAtlas != null ? "success" : "null - assign manually"}");
        }

        if (halloweenAtlas != null)
        {
            ParseAtlas();
            ApplyAtlasToJacko();
        }
        else
        {
            Debug.LogWarning("[JackoAtlas] No texture atlas found! Assign 'Halloween Atlas' in inspector or place in Resources folder.");
        }
    }

    /// <summary>
    /// Parse the texture atlas into individual sub-textures.
    /// </summary>
    private void ParseAtlas()
    {
        int totalTiles = atlasColumns * atlasRows;
        int atlasWidth = halloweenAtlas.width;
        int atlasHeight = halloweenAtlas.height;

        Debug.Log($"[JackoAtlas] Parsing atlas: {atlasWidth}x{atlasHeight}, {totalTiles} tiles, {tileSize}x{tileSize} each");

        for (int i = 0; i < totalTiles; i++)
        {
            int col = i % atlasColumns;
            int row = i / atlasColumns;

            // Calculate sub-rectangle
            int x = col * tileSize;
            int y = row * tileSize;

            // Ensure within bounds
            x = Mathf.Min(x, atlasWidth - tileSize);
            y = Mathf.Min(y, atlasHeight - tileSize);

            // Get pixels from atlas region
            Color[] pixels = halloweenAtlas.GetPixels(x, y, tileSize, tileSize);

            // Create sub-texture
            Texture2D subTex = new Texture2D(tileSize, tileSize, TextureFormat.RGB24, false);
            subTex.wrapMode = TextureWrapMode.Repeat;
            subTex.filterMode = FilterMode.Bilinear;
            subTex.SetPixels(pixels);
            subTex.Apply();

            // Store with key
            string key = $"atlas_{col}_{row}";
            subTextures[key] = subTex;
        }

        Debug.Log($"[JackoAtlas] Parsed {subTextures.Count} sub-textures from atlas");
    }

    /// <summary>
    /// Apply atlas textures to the Jacko character's materials.
    /// </summary>
    private void ApplyAtlasToJacko()
    {
        if (halloweenAtlas == null || subTextures.Count == 0) return;

        // Apply body texture (suit texture - typically from atlas)
        if (bodyRenderer != null && subTextures.Count > 0)
        {
            // Get a body-related texture from the atlas (e.g., first few tiles)
            // Body typically uses striped/canvas textures - try tile (0,0) or (1,0)
            int bodyCol = 0;
            int bodyRow = 0;

            // Try to find a suitable body texture - look for skin-like textures
            // In a typical Halloween atlas, early tiles might be skin tones
            Texture2D bodyTex = GetSubTextureSafely(bodyCol, bodyRow);
            if (bodyTex != null)
            {
                ApplyBodyTexture(bodyTex);
                Debug.Log($"[JackoAtlas] Applied body texture from atlas ({bodyCol},{bodyRow})");
            }
        }

        // Apply head texture (pumpkin/face texture)
        if (headRenderer != null && subTextures.Count > 10)
        {
            // Head textures are typically later in the atlas or have specific indices
            // Try some common head texture positions
            int headCol = 8;  // middle-right area might have face textures
            int headRow = 4;  // lower half

            Texture2D headTex = GetSubTextureSafely(headCol, headRow);
            if (headTex != null)
            {
                ApplyHeadTexture(headTex);
                Debug.Log($"[JackoAtlas] Applied head texture from atlas ({headCol},{headRow})");
            }
            else
            {
                // Try alternative: search for a texture that looks like a face
                headTex = FindFaceTexture();
                if (headTex != null)
                {
                    ApplyHeadTexture(headTex);
                    Debug.Log("[JackoAtlas] Applied detected face texture from atlas");
                }
            }
        }

        Debug.Log("[JackoAtlas] Jacko texture atlas adaptation complete");
    }

    /// <summary>
    /// Safely get a sub-texture, returning null if not available.
    /// </summary>
    private Texture2D GetSubTextureSafely(int col, int row)
    {
        string key = $"atlas_{col}_{row}";
        if (subTextures.TryGetValue(key, out Texture2D tex))
        {
            return tex;
        }
        return null;
    }

    /// <summary>
    /// Apply a body texture from the atlas to the Jacko's body material.
    /// </bodyTexture>
    private void ApplyBodyTexture(Texture2D atlasTex)
    {
        if (bodyRenderer == null || atlasTex == null) return;

        Material bodyMat = bodyRenderer.sharedMaterial;
        if (bodyMat == null) return;

        // Use NH helper if available, otherwise direct approach
        NH nh = FindObjectOfType<NH>();

        if (nh != null && bodyMat.HasProperty("_BaseMap"))
        {
            nh.MatTex(bodyMat.color, Color.clear, 0f, atlasTex, 1f);
        }
        else
        {
            // Direct approach - set as main texture
            bodyMat.SetTexture("_MainTex", atlasTex);
            bodyMat.SetTextureScale("_MainTex", new Vector2(1f, 1f));
            if (bodyMat.HasProperty("_BaseMap"))
            {
                bodyMat.SetTexture("_BaseMap", atlasTex);
                bodyMat.SetTextureScale("_BaseMap", new Vector2(1f, 1f));
            }
        }

        // Also try to emit some color for the dark suit
        if (bodyMat.HasProperty("_EmissionColor"))
        {
            bodyMat.SetColor("_EmissionColor", new Color(0.1f, 0.05f, 0.1f) * 0.5f);
        }
    }

    /// <summary>
    /// Apply a head/face texture from the atlas to the Jacko's head material.
    /// </summary>
    private void ApplyHeadTexture(Texture2D atlasTex)
    {
        if (headRenderer == null || atlasTex == null) return;

        Material headMat = headRenderer.sharedMaterial;
        if (headMat == null) return;

        // Use NH helper if available
        NH nh = FindObjectOfType<NH>();

        if (nh != null && headMat.HasProperty("_EmissionMap"))
        {
            nh.MatEmis(Color.clear, Color.clear, 0f, atlasTex, atlasTex, 1f);
        }
        else
        {
            // Direct approach
            headMat.SetTexture("_EmissionMap", atlasTex);
            headMat.SetTextureScale("_EmissionMap", new Vector2(1f, 1f));
            if (headMat.HasProperty("_MainTex"))
            {
                headMat.SetTexture("_MainTex", atlasTex);
                headMat.SetTextureScale("_MainTex", new Vector2(1f, 1f));
            }
        }

        // Set emission for the pumpkin/face glow
        if (headMat.HasProperty("_EmissionColor"))
        {
            headMat.SetColor("_EmissionColor", new Color(1f, 0.4f, 0.1f) * 0.8f);
        }
    }

    /// <summary>
    /// Try to find a face/texture in the atlas that looks like a face/pumpkin.
    /// </summary>
    private Texture2D FindFaceTexture()
    {
        // Search through atlas tiles looking for suitable face textures
        // Heuristic: look for tiles with specific color patterns
        for (int row = atlasRows - 1; row >= 0; row--)
        {
            for (int col = 0; col < atlasColumns; col++)
            {
                string key = $"atlas_{col}_{row}";
                if (subTextures.TryGetValue(key, out Texture2D tex))
                {
                    // Simple heuristic: check if texture has orange/red tones (pumpkin colors)
                    Color[] pixels = tex.GetPixels();
                    int orangeCount = 0;
                    foreach (Color c in pixels)
                    {
                        if (c.r > 0.5f && c.g < 0.3f && c.b < 0.3f) orangeCount++;
                    }
                    // If more than 10% of pixels are orange-ish, it might be a face texture
                    if (pixels.Length > 0 && orangeCount > pixels.Length * 0.1f)
                    {
                        return tex;
                    }
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Draw gizmos to visualize atlas tile selection.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (halloweenAtlas == null) return;

        Giz.color = Color.magenta;
        Giz.matrix = transform.localToWorldMatrix;

        int atlasWidth = halloweenAtlas.width;
        int atlasHeight = halloweenAtlas.height;

        // Draw atlas outline
        Giz.DrawWireCube(new Vector3(atlasWidth / 2f, atlasHeight / 2f, 0), new Vector3(atlasWidth, atlasHeight, 1));

        // Draw selected tile indicator
        int cols = Mathf.Clamp(0, 0, atlasColumns - 1); // default
        int rows = Mathf.Clamp(0, 0, atlasRows - 1);

        int x = cols * tileSize;
        int y = rows * tileSize;

        Giz.color = Color.cyan;
        Giz.DrawWireCube(new Vector3(x + tileSize / 2f, y + tileSize / 2f, 0), new Vector3(tileSize, tileSize, 1));

        // Draw grid
        Giz.color = Color.gray;
        for (int r = 0; r < atlasRows; r++)
        {
            for (int c = 0; c < atlasColumns; c++)
            {
                int cx = c * tileSize;
                int cy = r * tileSize;
                Giz.DrawWireCube(new Vector3(cx + tileSize / 2f, cy + tileSize / 2f, 0), new Vector3(tileSize, tileSize, 1));
            }
        }
    }
}