using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

// Copies one texture's Sprite Editor slices (rect, pivot, alignment, border) onto every
// selected texture — the Sprite Editor has no copy/paste, and building tier variants share
// their original's exact layout. Slices are renamed after the target (e.g. townHallbroke_0),
// and a target's existing slice IDs are reused by index so sprites already referenced in the
// scene keep pointing at the same slice.
public class SpriteSliceCopyTool : EditorWindow
{
    private Texture2D source;

    [MenuItem("Tools/Copy Sprite Slicing")]
    private static void Open() => GetWindow<SpriteSliceCopyTool>("Copy Slicing");

    private void OnGUI()
    {
        source = (Texture2D)EditorGUILayout.ObjectField("Source", source, typeof(Texture2D), false);

        if (GUILayout.Button("Apply to Selected Textures") && source != null)
        {
            foreach (Texture2D target in Selection.GetFiltered<Texture2D>(SelectionMode.Assets))
            {
                if (target != source) CopySlicing(source, target);
            }
        }
    }

    private static void CopySlicing(Texture2D source, Texture2D target)
    {
        if (source.width != target.width || source.height != target.height)
            throw new System.InvalidOperationException(
                $"{target.name} is {target.width}x{target.height}, but {source.name} is {source.width}x{source.height}.");

        var targetImporter = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(target));
        if (targetImporter.spriteImportMode != SpriteImportMode.Multiple)
            throw new System.InvalidOperationException($"{target.name} must use Sprite Mode: Multiple.");

        SpriteRect[] sourceRects = GetProvider(source).GetSpriteRects();
        ISpriteEditorDataProvider targetProvider = GetProvider(target);
        SpriteRect[] existingRects = targetProvider.GetSpriteRects();

        SpriteRect[] newRects = sourceRects.Select((r, i) => new SpriteRect
        {
            name = $"{target.name}_{i}",
            rect = r.rect,
            pivot = r.pivot,
            alignment = r.alignment,
            border = r.border,
            spriteID = i < existingRects.Length ? existingRects[i].spriteID : GUID.Generate()
        }).ToArray();

        targetProvider.SetSpriteRects(newRects);
        targetProvider.GetDataProvider<ISpriteNameFileIdDataProvider>()
            .SetNameFileIdPairs(newRects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        targetProvider.Apply();
        targetImporter.SaveAndReimport();

        Debug.Log($"Copied {newRects.Length} slices from {source.name} to {target.name}.");
    }

    private static ISpriteEditorDataProvider GetProvider(Texture2D texture)
    {
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider provider =
            factory.GetSpriteEditorDataProviderFromObject(AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)));
        provider.InitSpriteEditorDataProvider();
        return provider;
    }
}
