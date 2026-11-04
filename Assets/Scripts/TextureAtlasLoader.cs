using UnityEngine;

/// <summary>
/// Texture Atlas loader and integrator for Nightmare Hollow
/// This script loads the Halloween Texture Atlas and provides access to its sub-textures.
/// </summary>
public class TextureAtlasLoader : MonoBehaviour
{
    [Header("Texture Atlas Reference")]
    [Tooltip("The texture atlas containing multiple sub-textures arranged in a grid")]
    public Texture2D halloweenTextureAtlas;

    [Header("Atlas Settings")]
    [Tooltip("Number of columns/tiles across the atlas")]
    public int atlasColumns = 16;

    [Tooltip("Number of rows/tiles down the atlas")]
    public int atlasRows = 16;

    [Tooltip("Size of each sub-texture in pixels (assuming square tiles)")]
    public int tileSize = 128;

    // Cached sub-textures
    private Dictionary<string, Texture2D> subTextures = new Dictionary<string, Texture2D>();

    void Awake()
    {
        if (halloweenTextureAtlas == null)
        {
            // Try to find the atlas automatically if not assigned
            halloweenTextureAtlas = Resources.Load<Texture2D>("NightmareHollow_TextureAtlas");
        }

        if (halloweenTextureAtlas != null)
        {
            Debug.Log($"[TextureAtlasLoader] Loaded texture atlas: {halloweenTextureAtlas.width}x{halloweenTextureAtlas.height}");
            ParseAtlas();
        }
        else
        {
            Debug.LogWarning("[TextureAtlasLoader] No texture atlas found! Assign one in the inspector or place it in Resources folder.");
        }
    }

    /// <summary>
    /// Parses the texture atlas into individual sub-textures.
    /// </summary>
    private void ParseAtlas()
    {
        int totalTiles = atlasColumns * atlasRows;
        int atlasWidth = halloweenTextureAtlas.width;
        int atlasHeight = halloweenTextureAtlas.height;

        Debug.Log($"[TextureAtlasLoader] Atlas size: {atlasWidth}x{atlasHeight}, tiles: {totalTiles}, tile size: {tileSize}x{tileSize}");

        for (int i = 0; i < totalTiles; i++)
        {
            int col = i % atlasColumns;
            int row = i / atlasColumns;

            // Calculate sub-rectangle
            Rect rect = new Rect(
                col * tileSize,
                row * tileSize,
                tileSize,
                tileSize
            );

            // Ensure rect is within atlas bounds
            rect.x = Mathf.Clamp01(rect.x / atlasWidth) * atlasWidth;
            rect.y = Mathf.Clamp01(rect.y / atlasHeight) * atlasHeight;
            rect.width = Mathf.Min(tileSize, atlasWidth - (int)rect.x);
            rect.height = Mathf.Min(tileSize, atlasHeight - (int)rect.y);

            // Create a new texture from the sub-rectangle
            Texture2D subTex = new Texture2D(tileSize, tileSize, TextureFormat.RGB24, false);
            subTex.wrapMode = TextureWrapMode.Repeat;
            subTex.filterMode = FilterMode.Bilinear;

            // Read pixels from the atlas
            Color[] pixels = halloweenTextureAtlas.GetPixels(
                (int)rect.x,
                (int)rect.y,
                (int)rect.width,
                (int)rect.height
            );

            // Resize to exact tileSize x tileSize if needed
            if (pixels.Length != tileSize * tileSize)
            {
                Color[] resized = new Color[tileSize * tileSize];
                float scaleX = (float)tileSize / rect.width;
                float scaleY = (float)tileSize / rect.height;

                for (int y = 0; y < tileSize; y++)
                {
                    for (int x = 0; x < tileSize; x++)
                    {
                        int srcX = Mathf.FloorToInt(x / scaleX);
                        int srcY = Mathf.FloorToInt(y / scaleY);
                        int srcIdx = srcY * (int)rect.width + srcX;
                        resized[y * tileSize + x] = pixels[srcIdx];
                    }
                }
                pixels = resized;
            }

            subTex.SetPixels(pixels);
            subTex.Apply();

            // Store with a key based on position
            string key = $"atlas_{col}_{row}";
            subTextures[key] = subTex;
        }

        Debug.Log($"[TextureAtlasLoader] Parsed {subTextures.Count} sub-textures from atlas");
    }

    /// <summary>
    /// Gets a sub-texture from the atlas by grid position.
    /// </summary>
    /// <param name="col">Column index</param>
    /// <param name="row">Row index</param>
    /// <returns>The sub-texture, or null if not found</returns>
    public Texture2D GetSubTexture(int col, int row)
    {
        string key = $"atlas_{col}_{row}";
        if (subTextures.TryGetValue(key, out Texture2D tex))
        {
            return tex;
        }
        Debug.LogWarning($"[TextureAtlasLoader] Sub-texture not found at ({col}, {row})");
        return null;
    }

    /// <summary>
    /// Gets a sub-texture by name/ID.
    /// </summary>
    /// <param name="name">Name identifier (e.g., "eye", "mouth", "skin")</param>
    /// <returns>The sub-texture, or null if not found</returns>
    public Texture2D GetSubTextureByName(string name)
    {
        // Try common name mappings
        foreach (var kvp in subTextures)
        {
            if (kvp.Key.Contains(name.ToLower()) || kvp.Key.EndsWith(name))
            {
                return kvp.Value;
            }
        }
        return null;
    }

    /// <summary>
    /// Applies a sub-texture to a renderer's material as the main texture.
    /// </summary>
    /// <param name="renderer">The renderer to apply the texture to</param>
    /// <param name="col">Column index in the atlas</param>
    /// <param name="row">Row index in the atlas</param>
    public void ApplyToRenderer(Renderer renderer, int col, int row)
    {
        Texture2D subTex = GetSubTexture(col, row);
        if (subTex != null && renderer != null)
        {
            Material mat = renderer.sharedMaterial;
            if (mat != null)
            {
                // Use the NH helper method if available, otherwise set directly
                // Try to find NH class
                NH nh = FindObjectOfType<NH>();
                if (nh != null && mat.HasProperty("_BaseMap"))
                {
                    nh.MatTex(mat.color, Color.clear, 0f, subTex, 1f);
                }
                else
                {
                    // Direct approach
                    mat.SetTexture("_MainTex", subTex);
                    mat.SetTextureScale("_MainTex", new Vector2(1f, 1f));
                }
                Debug.Log($"[TextureAtlasApplied] Applied atlas sub-texture ({col},{row}) to {renderer.name}");
            }
        }
    }

    /// <summary>
    /// Draws gizmos showing the atlas layout and selected sub-texture.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (halloweenTextureAtlas == null) return;

        Giz.color = Color.yellow;
        Giz.matrix = transform.localToWorldMatrix;

        int atlasWidth = halloweenTextureAtlas.width;
        int atlasHeight = halloweenTextureAtlas.height;

        // Draw atlas outline
        Giz.DrawWireCube(new Vector3(atlasWidth / 2f, atlasHeight / 2f, 0), new Vector3(atlasWidth, atlasHeight, 1));

        // Draw grid cells
        for (int y = 0; y < atlasRows; y++)
        {
            for (int x = 0; x < atlasColumns; x++)
            {
                Rect rect = new Rect(x * tileSize, y * tileSize, tileSize, tileSize);
                Giz.color = Color.green;
                Giz.DrawWireCube(new Vector3(rect.x + rect.width / 2f, rect.y + rect.height / 2f, 0), new Vector3(rect.width, rect.height, 1));
            }
        }
    }
}