using UnityEngine;

namespace MirioShoto
{
    /// <summary>
    /// Sprite 2D que sempre encara a câmera (billboard) e vira para a esquerda/direita conforme o movimento NA TELA.
    /// Por que: a câmera é isométrica, então "esquerda" precisa ser medida em coordenadas de tela, não de mundo.
    /// Os PNGs devem ser desenhados olhando para a DIREITA.
    /// </summary>
    public class SpriteActor : MonoBehaviour
    {
        public SpriteRenderer sr;
        public float roll;   // inclinação extra (graus) usada para dar vida ao passo

        public static Sprite LoadSprite(string path, float worldWidth, float pivotY = .1f)
        {
            var tex = Resources.Load<Texture2D>(path);
            if (tex == null) return null;
            tex.filterMode = FilterMode.Point; tex.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, pivotY), tex.width / worldWidth);
        }

        public static SpriteActor Create(Transform parent, string path, float worldWidth)
        {
            var sp = LoadSprite(path, worldWidth);
            if (sp == null) return null;
            var go = new GameObject("Sprite"); go.transform.SetParent(parent, false);
            var a = go.AddComponent<SpriteActor>();
            a.sr = go.AddComponent<SpriteRenderer>(); a.sr.sprite = sp;
            return a;
        }

        public void Face(Vector3 worldVel)
        {
            var cam = Camera.main; if (cam == null || worldVel.sqrMagnitude < .0001f) return;
            float dx = cam.WorldToScreenPoint(transform.position + worldVel.normalized).x - cam.WorldToScreenPoint(transform.position).x;
            if (dx > .5f) sr.flipX = false; else if (dx < -.5f) sr.flipX = true;
        }

        void LateUpdate()
        {
            var cam = Camera.main; if (cam == null) return;
            transform.rotation = cam.transform.rotation * Quaternion.Euler(0, 0, roll);
        }
    }
}
