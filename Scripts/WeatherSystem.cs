using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PP
{
    /// <summary>
    /// Spawns rain particles in the upper scene area.
    /// Rain starts randomly, lasts 8-15 seconds, then stops.
    /// </summary>
    public class WeatherSystem : MonoBehaviour
    {
        const int   MAX_DROPS   = 80;
        const float RAIN_SPEED  = 8f;
        const float DROP_W      = 0.04f;
        const float DROP_H      = 0.22f;

        readonly List<SpriteRenderer> _drops = new List<SpriteRenderer>();
        readonly List<Vector3>        _vel   = new List<Vector3>();

        bool  _raining   = false;
        float _intensity = 0f;   // 0..1

        void Awake()
        {
            // Keep weather running always, independent of game state
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            // Pre-create drop pool
            for (int i = 0; i < MAX_DROPS; i++)
            {
                var go = new GameObject("Rain");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 20;
                sr.color = new Color(0.6f, 0.7f, 0.9f, 0f);
                sr.sprite = MakeDropSprite();
                go.SetActive(false);
                _drops.Add(sr);
                _vel.Add(Vector3.zero);
            }
            StartCoroutine(WeatherLoop());
        }

        IEnumerator WeatherLoop()
        {
            while (true)
            {
                // Wait a random interval before next rain
                //yield return new WaitForSeconds(Random.Range(8f, 20f));

                // Fade in
                _raining = true;
                float dur = Random.Range(8f, 15f);
                _intensity = Random.Range(0.4f, 1.0f);

                // Activate drops
                int activeCount = Mathf.RoundToInt(MAX_DROPS * _intensity);
                for (int i = 0; i < MAX_DROPS; i++)
                {
                    bool on = i < activeCount;
                    _drops[i].gameObject.SetActive(on);
                    if (on) ResetDrop(i);
                }

                // Fade in alpha
                float t = 0;
                while (t < 0.5f) { t += Time.deltaTime; SetAlpha(t * 2f * _intensity * 0.85f); yield return null; }

                // Rain for duration
                yield return new WaitForSeconds(dur);

                // Fade out
                t = 0;
                while (t < 1f) { t += Time.deltaTime; SetAlpha((1f-t) * _intensity * 0.7f); yield return null; }

                // Deactivate
                foreach (var sr in _drops) sr.gameObject.SetActive(false);
                _raining = false;
            }
        }

        void Update()
        {
            if (!_raining) return;
            for (int i = 0; i < _drops.Count; i++)
            {
                if (!_drops[i].gameObject.activeSelf) continue;
                var pos = _drops[i].transform.position;
                pos.x += _vel[i].x * Time.deltaTime;
                pos.y += _vel[i].y * Time.deltaTime;
                _drops[i].transform.position = pos;

                // Reset if below ground or off screen
                if (pos.y < -0.5f || pos.x < -12f || pos.x > 12f)
                    ResetDrop(i);
            }
        }

        void ResetDrop(int i)
        {
            float x = Random.Range(-12f, 12f);
            float y = Random.Range(0.5f, 10f);  // full height of scene
            _drops[i].transform.position = new Vector3(x, y, 0);
            // Slight diagonal wind
            _vel[i] = new Vector3(Random.Range(-0.5f, 0.0f), -RAIN_SPEED * Random.Range(0.8f, 1.2f), 0);
        }

        void SetAlpha(float a)
        {
            foreach (var sr in _drops)
            {
                var c = sr.color; c.a = a; sr.color = c;
            }
        }

        static Sprite MakeDropSprite()
        {
            int pw = Mathf.Max(1, Mathf.RoundToInt(DROP_W * 32));
            int ph = Mathf.Max(1, Mathf.RoundToInt(DROP_H * 32));
            var tex = new Texture2D(pw, ph, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pix = new Color[pw * ph];
            for (int i = 0; i < pix.Length; i++) pix[i] = Color.white;
            tex.SetPixels(pix); tex.Apply();
            return Sprite.Create(tex, new Rect(0,0,pw,ph), new Vector2(0.5f, 0f), 32f);
        }
    }
}