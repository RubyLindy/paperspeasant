using System.Collections.Generic;
using UnityEngine;

namespace PP
{
    public class Sheets : MonoBehaviour
    {
        public static Sheets I { get; private set; }

        static readonly Dictionary<ActorType, string> Paths = new Dictionary<ActorType, string>
        {
            { ActorType.Farmer,   "NPCs/farmer_01"  },
            { ActorType.Merchant, "NPCs/merchant"    },
            { ActorType.ShadyGuy, "NPCs/shady_guy"  },
            { ActorType.Villager, "NPCs/villager_01" },
            { ActorType.Stranger, "NPCs/stranger"    },
            { ActorType.Beggar,   "NPCs/beggar"      },
        };

        static readonly Dictionary<ActorType, int> FrameCounts = new Dictionary<ActorType, int>
        {
            { ActorType.Farmer,   5 },
            { ActorType.Merchant, 5 },
            { ActorType.ShadyGuy, 5 },
            { ActorType.Villager, 5 },
            { ActorType.Stranger, 4 },
            { ActorType.Beggar,   4 },
        };

        readonly Dictionary<ActorType, Sprite[]> _cache = new Dictionary<ActorType, Sprite[]>();

        void Awake()
        {
            if (I != null) { Destroy(gameObject); return; }
            I = this;
        }

        public Sprite[] Get(ActorType t)
        {
            if (_cache.TryGetValue(t, out var cached)) return cached;

            if (!Paths.TryGetValue(t, out var path)) return null;
            var tex = Resources.Load<Texture2D>(path);
            if (tex == null) { Debug.LogWarning("Sheets: texture not found: " + path); return null; }

            tex.filterMode = FilterMode.Point;
            int fc = FrameCounts.ContainsKey(t) ? FrameCounts[t] : 4;
            int fw = tex.width / fc;
            var frames = new Sprite[fc];
            for (int i = 0; i < fc; i++)
                frames[i] = Sprite.Create(tex, new Rect(i * fw, 0, fw, tex.height),
                                          new Vector2(0.5f, 0f), 32f);
            _cache[t] = frames;
            return frames;
        }

        public Sprite[] GetGuard()
        {
            var tex = Resources.Load<Texture2D>("NPCs/guard");
            if (tex == null) return null;
            tex.filterMode = FilterMode.Point;
            int fc = 4, fw = tex.width / fc;
            var f = new Sprite[fc];
            for (int i = 0; i < fc; i++)
                f[i] = Sprite.Create(tex, new Rect(i * fw, 0, fw, tex.height), new Vector2(0.5f, 0f), 32f);
            return f;
        }
    }
}
