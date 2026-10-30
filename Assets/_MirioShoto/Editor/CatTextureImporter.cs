using UnityEditor;
using UnityEngine;

namespace MirioShoto.EditorTools
{
    /// <summary>
    /// Por que: pixel art precisa de filtro "Point", sem compressão nem mipmaps; senão fica borrada.
    /// Aplica isso sozinho a toda imagem em Assets/Resources/Cats ao importar.
    /// </summary>
    public class CatTextureImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("Resources/Cats/")) return;
            var t = (TextureImporter)assetImporter;
            t.textureType = TextureImporterType.Default;
            t.alphaIsTransparency = true;
            t.mipmapEnabled = false;
            t.filterMode = FilterMode.Point;
            t.textureCompression = TextureImporterCompression.Uncompressed;
            t.npotScale = TextureImporterNPOTScale.None;
            t.isReadable = false;
        }
    }
}
