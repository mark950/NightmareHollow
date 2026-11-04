using UnityEngine;

/// <summary>
/// Demo script showing how to use the Nightmare Hollow Halloween Texture Atlas
/// Attach this to any GameObject with a Renderer to apply a sub-texture from the atlas
/// </summary>
public class TextureAtlasDemo : MonoBehaviour
{
    [Header("Atlas Reference")]
    [Tooltip("Assign the NightmareHollow_TextureAtlas from the project")]
    public Texture2D textureAtlas;

    [Header("Tile Selection")]
    [Tooltip("Which column (0-based) to use from the atlas grid")]
    public int column = 0;

    [Tooltip("Which row (0-based) to use from the atlas grid")]
    public int row = 0;

    [Tooltip("Tile size in pixels (most atlas tiles are 128x128)")]
    public int tileSize = 128;

    void Start()
    {
        // Try to auto-find the atlas if not assigned
        if (textureAtlas == null)
        {
            textureAtlas = Resources.Load<Texture2D>("NightmareHollow_TextureAtlas");
            Debug.Log($"[AtlasDemo] Auto-loaded atlas: {textureAtlas != null ? "success" : "null"}");
        }

        if (textureAtlas != null)
        {
            ApplyAtlasTexture();
        }
        else
        {
            Debug.LogError("[AtlasDemo] No texture atlas found! Ensure NightmareHollow_TextureAtlas.png is in Assets or Resources folder.");
        }
    }

    /// <summary>
    /// Applies a sub-texture from the atlas to this object's renderer.
    /// </summary>
    public void ApplyAtlasTexture()
    {
        if (textureAtlas == null) return;

        Renderer rend = GetComponent<Renderer>();
        if (rend == null)
        {
            Debug.LogError("[AtlasDemo] No Renderer component found on this object!");
            return;
        }

        // Get the sub-texture from the atlas
        int totalColumns = Mathf.Max(1, Mathf.CeilToInt((float)textureAtlas.width / tileSize));
        int totalRows = Mathf.Max(1, Mathf.CeilToInt((float)textureAtlas.height / tileSize));

        // Clamp column/row to valid range
        int cols = Mathf.Clamp(column, 0, totalColumns - 1);
        int rows = Mathf.Clamp(row, 0, totalRows - 1);

        // Calculate the sub-rectangle
        int x = cols * tileSize;
        int y = rows * tileSize;

        // Ensure we don't exceed atlas bounds
        x = Mathf.Min(x, textureAtlas.width - tileSize);
        y = Mathf.Min(y, textureAtlas.height - tileSize);

        // Get pixels from the atlas sub-region
        Color[] pixels = textureAtlas.GetPixels(x, y, tileSize, tileSize);

        // Create a new texture
        Texture2D subTex = new Texture2D(tileSize, tileSize, TextureFormat.RGB24, false);
        subTex.wrapMode = TextureWrapMode.Repeat;
        subTex.filterMode = FilterMode.Bilinear;
        subTex.SetPixels(pixels);
        subTex.Apply();

        // Apply to the material using NH helper if available, otherwise directly
        Material mat = rend.sharedMaterial;

        // Try to find NH class for helper method
        NH nh = FindObjectOfType<NH>();

        if (nh != null && mat.HasProperty("_BaseMap"))
        {
            // Use NH's MatTex helper
            nh.MatTex(mat.color, Color.clear, 0f, subTex, 1f);
            Debug.Log($"[AtlasDemo] Applied atlas sub-texture ({cols},{rows}) to {name} using NH helper");
        }
        else if (mat.HasProperty("_MainTex"))
        {
            // Direct approach - set main texture
            mat.SetTexture("_MainTex", subTex);
            mat.SetTextureScale("_MainTex", new Vector2(1f, 1f));
            // Also set base map if available
            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", subTex);
                mat.SetTextureScale("_BaseMap", new Vector2(1f, 1f));
            }
            Debug.Log($"[AtlasDemo] Applied atlas sub-texture ({cols},{rows}) to {name} directly");
        }
        else
        {
            // Create a new material with the texture
            Material newMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            newMat.SetTexture("_MainTex", subTex);
            rend.sharedMaterial = newMat;
            Debug.Log($"[AtlasDemo] Created new material and applied atlas sub-texture ({cols},{rows}) to {name}");
        }

        // Log what we did
        Debug.Log($"[AtlasDemo] Atlas size: {textureAtlas.width}x{textureAtlas.height}");
        Debug.Log($"[AtlasDemo] Using tile ({cols},{rows}) at position ({x},{y})");
        Debug.Log($"[AtlasDemo] Sub-texture dimensions: {subTex.width}x{subTex.height}");
    }

    /// <summary>
    /// Draw gizmos to visualize the atlas tile selection
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (textureAtlas == null) return;

        Giz.color = Color.cyan;
        Giz.depthTest = false;
        Giz.depthWrite = false;

        int cols = Mathf.Clamp(column, 0, Mathf.CeilToInt((float)textureAtlas.width / tileSize) - 1);
        int rows = Mathf.Clamp(row, 0, Mathf.CeilToInt((float)textureAtlas.height / tileSize) - 1);

        int x = cols * tileSize;
        int y = rows * tileSize;

        // Draw the selected tile area
        Vector2 size = new Vector2(tileSize, tileSize);
        Rect selectionRect = new Rect(x, y, size.x, size.y);

        // Draw screen-space rectangle showing the selected area
        Vector3 center = new Vector3(x + tileSize / 2f, y + tileSize / 2f, 0);
        Giz.DrawIcon(transform.position + center, "Texture Atlas Selector", false);
    }
}