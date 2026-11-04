using System.Collections.Generic;
using UnityEngine;

namespace MirioShoto
{
    /// <summary>
    /// Gato em pixel art 2D que gira entre 8 direções conforme o movimento na TELA.
    /// Por que: a câmera é isométrica (girada 45°), então a direção do sprite precisa ser
    /// calculada em coordenadas de tela, não de mundo. O sprite sempre encara a câmera (billboard).
    /// Os PNGs ficam em Assets/Resources/Cats/&lt;Nome&gt;/&lt;direção&gt;_idle.png e &lt;direção&gt;_walkN.png.
    /// </summary>
    public class CatSprite : MonoBehaviour
    {
        // setores de 45° na tela, começando em 0° (direita) no sentido anti-horário
        static readonly string[] Dirs = { "right", "backright", "back", "backleft", "left", "frontleft", "front", "frontright" };
        // Altura do corpo em metros (de frente). Mirio é um pouco maior que Shoto, mas não muito.
        public static float TargetHeight(string cat) => cat == "Shoto" ? .62f : .72f;
        float ppu = 100f, pivotY = .04f;
        readonly System.Collections.Generic.HashSet<string> mirrored = new();
        const float WalkFps = 9f;

        public SpriteRenderer sr;
        readonly Dictionary<string, Sprite> idle = new();
        readonly Dictionary<string, List<Sprite>> walk = new();
        int dir = 6; float animT;

        public static bool Available(string catName) => Resources.Load<Texture2D>($"Cats/{catName}/front_idle") != null;

        public void Init(string catName)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            Measure(catName);
            foreach (var d in Dirs)
            {
                // Por que espelhar: nas cartelas, os quadros "esquerda" misturavam gatos virados para os dois lados.
                // Os quadros "direita" são consistentes, então a esquerda usa a direita espelhada.
                bool esq = d.EndsWith("left"); string src = esq ? d.Replace("left", "right") : d;
                if (esq) mirrored.Add(d);
                var i = Load(catName, src + "_idle"); if (i != null) idle[d] = i;
                var list = new List<Sprite>();
                for (int n = 0; n < 12; n++) { var s = Load(catName, $"{src}_walk{n}"); if (s == null) break; list.Add(s); }
                if (list.Count > 0) walk[d] = list;
            }
            sr.sprite = idle.TryGetValue(Dirs[dir], out var first) ? first : null;
            sr.sortingOrder = 0;
        }

        /// <summary>Mede a área desenhada (sem a margem transparente) para que o tamanho do gato não dependa do tamanho da imagem.</summary>
        public static void Metrics(string cat, out float ppu, out float pivotY)
        {
            ppu = 100f; pivotY = .04f;
            var t = Resources.Load<Texture2D>($"Cats/{cat}/front_idle"); if (t == null || !t.isReadable) return;
            var px = t.GetPixels32(); int minY = t.height, maxY = -1;
            for (int y = 0; y < t.height; y++) for (int x = 0; x < t.width; x++) if (px[y * t.width + x].a > 20) { if (y < minY) minY = y; if (y > maxY) maxY = y; }
            if (maxY < minY) return;
            ppu = (maxY - minY + 1) / TargetHeight(cat);
            pivotY = minY / (float)t.height;   // pés exatamente no chão
        }

        void Measure(string cat) => Metrics(cat, out ppu, out pivotY);

        Sprite Load(string cat, string file)
        {
            var tex = Resources.Load<Texture2D>($"Cats/{cat}/{file}");
            if (tex == null) return null;
            tex.filterMode = FilterMode.Point; tex.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, pivotY), ppu);
        }

        /// <summary>Chamado todo quadro pelo gato. 'vel' é a velocidade no mundo; 'moving' vai de 0 a 1.</summary>
        public void Tick(Vector3 vel, float moving, float dt)
        {
            var cam = Camera.main;
            if (cam == null || sr == null) return;
            transform.rotation = cam.transform.rotation; // billboard: sempre de frente para a câmera

            if (moving > .12f && vel.sqrMagnitude > .01f)
            {
                Vector2 a = cam.WorldToScreenPoint(transform.position), b = cam.WorldToScreenPoint(transform.position + vel.normalized);
                Vector2 d = b - a;
                if (d.sqrMagnitude > .0001f)
                {
                    float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                    dir = ((Mathf.RoundToInt(ang / 45f) % 8) + 8) % 8;
                }
                animT += dt * WalkFps * Mathf.Lerp(.7f, 1.3f, moving);
            }
            else animT = 0;

            string key = Dirs[dir];
            sr.flipX = mirrored.Contains(key);
            if (moving > .12f && walk.TryGetValue(key, out var frames) && frames.Count > 0)
                sr.sprite = frames[Mathf.FloorToInt(animT) % frames.Count];
            else if (idle.TryGetValue(key, out var s)) sr.sprite = s;
        }
    }
}
