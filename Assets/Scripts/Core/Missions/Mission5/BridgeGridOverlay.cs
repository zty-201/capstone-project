using UnityEngine;

// Faint dot grid marking every point BridgeBuilderSystem.SnapToGrid can resolve to, shown only
// while building. The dot sprite is generated here rather than imported, so one tile always
// equals gridSpacing exactly — an imported sprite's PPU would have to be kept in sync with
// gridSpacing by hand. Dot color/opacity come from the SpriteRenderer's own Color in the Inspector.
[RequireComponent(typeof(SpriteRenderer))]
public class BridgeGridOverlay : MonoBehaviour
{
    private const int TilePixels = 16;
    private const int DotPixels = 2;

    private SpriteRenderer spriteRenderer;
    private float builtForSpacing;

    // Lazy, not Awake-cached — BridgeBuilderSystem.OnEnable calls Fit during the same container
    // activation that activates this object, and cross-object Awake/OnEnable order is undefined
    // (same reasoning as BridgeTestCart.Rb).
    private SpriteRenderer Renderer => spriteRenderer ??= GetComponent<SpriteRenderer>();

    // Called from BridgeBuilderSystem.ComputePlaygroundBounds. Grid points sit at
    // bounds.min + k * spacing, so each tile is centered on one point and the renderer's size is
    // an exact multiple of the tile — tiling alignment is then unambiguous.
    public void Fit(Rect bounds, float spacing)
    {
        if (spacing != builtForSpacing) BuildSprite(spacing);

        int countX = Mathf.FloorToInt(bounds.width / spacing) + 1;
        int countY = Mathf.FloorToInt(bounds.height / spacing) + 1;
        Vector2 size = new Vector2(countX * spacing, countY * spacing);

        Renderer.size = size;
        transform.position = new Vector3(
            bounds.xMin - spacing * 0.5f + size.x * 0.5f,
            bounds.yMin - spacing * 0.5f + size.y * 0.5f,
            transform.position.z);
    }

    public void SetVisible(bool visible) => Renderer.enabled = visible;

    private void BuildSprite(float spacing)
    {
        Texture2D texture = new Texture2D(TilePixels, TilePixels, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point
        };

        Color32[] pixels = new Color32[TilePixels * TilePixels];
        int dotStart = (TilePixels - DotPixels) / 2;
        for (int y = 0; y < TilePixels; y++)
            for (int x = 0; x < TilePixels; x++)
            {
                bool inDot = x >= dotStart && x < dotStart + DotPixels && y >= dotStart && y < dotStart + DotPixels;
                pixels[y * TilePixels + x] = inDot ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }
        texture.SetPixels32(pixels);
        texture.Apply();

        // FullRect mesh is required for SpriteDrawMode.Tiled.
        Renderer.sprite = Sprite.Create(texture, new Rect(0, 0, TilePixels, TilePixels),
            new Vector2(0.5f, 0.5f), TilePixels / spacing, 0, SpriteMeshType.FullRect);
        Renderer.drawMode = SpriteDrawMode.Tiled;
        builtForSpacing = spacing;
    }
}
