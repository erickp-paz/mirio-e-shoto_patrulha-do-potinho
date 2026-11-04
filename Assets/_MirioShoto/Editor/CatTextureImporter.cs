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
        public override uint GetVersion() => 3;   // sobe quando as regras mudam, para reimportar tudo

        void OnPreprocessTexture()
        {
            string ap = assetPath.Replace('\\', '/');
            bool cat = ap.Contains("Resources/Cats/");
            if (!cat && !ap.Contains("Resources/Insects/") && !ap.Contains("Resources/Plush/") && !ap.Contains("Resources/Items/")) return;
            var t = (TextureImporter)assetImporter;
            t.textureType = TextureImporterType.Default;
            t.alphaIsTransparency = true;
            t.mipmapEnabled = false;
            t.filterMode = FilterMode.Point;
            t.textureCompression = TextureImporterCompression.Uncompressed;
            t.npotScale = TextureImporterNPOTScale.None;
            t.isReadable = cat;   // gatos precisam ser legíveis para medir o tamanho do desenho
        }
    }
}
