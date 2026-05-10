using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PP
{
    /// <summary>
    /// Attach this to ONE empty GameObject in the scene.
    /// It builds the camera, background, canvas and wires everything up.
    /// No other scripts need to be attached manually.
    /// </summary>
    public class Game : MonoBehaviour
    {
        // ── UI refs (built in Start) ─────────────────────────────────────
        GameObject   _titleScreen, _dayBanner, _endScreen, _docPanel;
        TextMeshProUGUI _bannerTxt, _speakerTxt, _dlgTxt, _coinsTxt;
        TextMeshProUGUI _docTitle, _docIssued, _docBearer, _docPurpose, _docValid, _docSeal;
        TextMeshProUGUI _nameLabel, _descLabel, _statsTxt;
        Image        _portraitImg;
        GameObject   _stampOK, _stampNO;
        Button       _acceptBtn, _denyBtn, _guardBtn, _bellBtn, _advBtn;

        // ── Scene refs ───────────────────────────────────────────────────
        GameObject _visitorGO;
        Walker     _walker;
        SpriteRenderer _g1, _g2;
        Sprite[]   _guardFrames;
        int        _gf;

        // ── Managers ─────────────────────────────────────────────────────
        GameManager _gm;
        Sheets      _sheets;

        // ════════════════════════════════════════════════════════════════
        void Start()
        {
            // Order matters: managers first, then scene, then UI
            BuildManagers();
            BuildCamera();
            BuildBackground();
            BuildUI();
            HookEvents();

            // Show title
            SetActive(_titleScreen, true);
            SetActive(_dayBanner, false);
            SetActive(_endScreen, false);
            SetActive(_docPanel, false);
            SetActive(_stampOK, false);
            SetActive(_stampNO, false);
            SetButtons(false);
            ShowBell(false);

            StartCoroutine(GuardAnim());
        }

        // ════════════════════════════════════════════════════════════════
        //  BUILD
        // ════════════════════════════════════════════════════════════════

        void BuildManagers()
        {
            var go = new GameObject("_Managers");
            _gm     = go.AddComponent<GameManager>();
            _sheets = go.AddComponent<Sheets>();
        }

        void BuildCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.orthographic     = true;
            cam.orthographicSize = 5.625f;   // 360/2/32
            cam.backgroundColor  = new Color(0.08f, 0.05f, 0.15f);
            cam.clearFlags       = CameraClearFlags.SolidColor;
            go.transform.position = new Vector3(0, 2.5f, -10f);
        }

        void BuildBackground()
        {
            // Sky
            MakeQuad("Sky", 0, 4f, 22, 10, new Color(0.11f,0.09f,0.22f), -10);
            for (int i = 0; i < 28; i++)
                MakeQuad("*", Random.Range(-10f,10f), Random.Range(3f,8f), 0.07f, 0.07f, Color.white, -9);

            // Ground
            var groundTex = Resources.Load<Texture2D>("Tileset/TX Tileset Ground");
            if (groundTex != null)
            {
                groundTex.filterMode = FilterMode.Point;
                int cols = groundTex.width/32, rows = groundTex.height/32;
                for (int x = -11; x <= 11; x++) Tile(groundTex, x, 0, 0, 0, cols, rows, -5);
                for (int x = -11; x <= 11; x++)
                for (int y = -3;  y < 0;  y++) Tile(groundTex, x, y, 0, 1, cols, rows, -6);
            }
            else
            {
                MakeQuad("Ground", 0, 0, 22, 0.5f, new Color(0.3f,0.5f,0.2f), -5);
                MakeQuad("Sub",    0,-2, 22, 4,    new Color(0.2f,0.3f,0.1f), -6);
            }

            // Castle wall
            MakeQuad("Wall",    0, 4.5f, 22, 8,   new Color(0.29f,0.25f,0.25f), -8);
            for (int x = -10; x <= 10; x+=2)
                MakeQuad("M",  x, 8.3f, 0.9f, 1.3f, new Color(0.33f,0.28f,0.28f), -7);
            MakeQuad("Gate",    0, 2.5f, 2.8f, 4.2f, new Color(0.05f,0.02f,0.02f), -6);
            MakeQuad("GArch",   0, 4.6f, 3.2f, 0.7f, new Color(0.29f,0.25f,0.25f), -5);
            MakeQuad("GBL", -1.6f,2.8f, 0.18f, 3.4f, new Color(0.42f,0.35f,0.22f), -4);
            MakeQuad("GBR",  1.6f,2.8f, 0.18f, 3.4f, new Color(0.42f,0.35f,0.22f), -4);
            for (int i=-2;i<=2;i++) MakeQuad("PV", i*0.55f, 2.6f, 0.10f, 3.6f, new Color(0.42f,0.35f,0.22f), -5);
            for (int j= 0;j<=3;j++) MakeQuad("PH", 0, 1f+j*0.85f, 2.4f, 0.09f, new Color(0.42f,0.35f,0.22f), -5);
            MakeQuad("BanL",-3.5f,6f, 0.7f,1.5f, new Color(0.55f,0.1f,0.1f), -3);
            MakeQuad("BanR", 3.5f,6f, 0.7f,1.5f, new Color(0.55f,0.1f,0.1f), -3);

            // Torches
            Torch(-2.5f, 3.2f);
            Torch( 2.5f, 3.2f);

            // Guards
            var gf = _sheets.GetGuard();
            _guardFrames = gf;

            var g1go = new GameObject("Guard1");
            g1go.transform.position   = new Vector3(2.2f, 0.1f, 0);
            g1go.transform.localScale = Vector3.one * 2f;
            _g1 = g1go.AddComponent<SpriteRenderer>();
            _g1.sortingOrder = 4;
            if (gf != null && gf.Length > 0) _g1.sprite = gf[0];

            var g2go = new GameObject("Guard2");
            g2go.transform.position   = new Vector3(-2.2f, 0.1f, 0);
            g2go.transform.localScale = Vector3.one * 2f;
            _g2 = g2go.AddComponent<SpriteRenderer>();
            _g2.sortingOrder = 4;
            _g2.flipX = true;
            if (gf != null && gf.Length > 0) _g2.sprite = gf[0];
        }

        void BuildUI()
        {
            // Canvas
            var cvGO = new GameObject("Canvas");
            var cv   = cvGO.AddComponent<Canvas>();
            cv.renderMode    = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder  = 10;
            var scaler = cvGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution  = new Vector2(640, 360);
            scaler.matchWidthOrHeight   = 0.5f;
            cvGO.AddComponent<GraphicRaycaster>();
            var C = cvGO.transform;

            // ── Title screen ──────────────────────────────────────────
            _titleScreen = Pnl(C, "Title", Hex(0.05f,0.03f,0.1f), V(0,0), V(1,1));
            BigTxt(_titleScreen.transform, "PAPERS, PEASANT!", 28, V(0.5f,0.65f), Hex(1f,0.85f,0.2f));
            SmTxt(_titleScreen.transform,
                "Kingdom of Emmeloord  -  Year 1042\n\nYou are the gate keeper of Castle Ede.\nCheck every document. Uphold the King's law.",
                12, V(0.5f,0.44f), Hex(0.7f,0.65f,0.55f), new Vector2(420,90));
            var startBtn = MakeBtn(_titleScreen.transform, "BEGIN DUTY", V(0.5f,0.17f), new Vector2(170,40),
                Hex(0.85f,0.7f,0.1f), Hex(0.05f,0.02f,0f));
            startBtn.onClick.AddListener(() => { SetActive(_titleScreen,false); _gm.StartGame(); });

            // ── Day banner ────────────────────────────────────────────
            _dayBanner = Pnl(C, "Banner", Hex(0.04f,0.02f,0.1f,0.92f), V(0.25f,0.35f), V(0.75f,0.65f));
            _bannerTxt = BigTxt(_dayBanner.transform, "DAY 1", 32, V(0.5f,0.5f), Hex(1f,0.85f,0.2f));

            // ── Lower bg ──────────────────────────────────────────────
            Pnl(C, "LoBG", Hex(0.05f,0.04f,0.02f), V(0,0), V(1,0.44f));

            // ── Portrait ──────────────────────────────────────────────
            var pPnl = Pnl(C, "PPanel", Hex(0.09f,0.07f,0.02f), V(0,0), V(0.33f,0.44f));
            _portraitImg = MakeImg(pPnl.transform, V(0.5f,0.68f), new Vector2(72,72));
            _nameLabel   = SmTxt(pPnl.transform, "...", 11, V(0.5f,0.44f), Hex(1f,0.85f,0.2f), new Vector2(200,26), true);
            _descLabel   = SmTxt(pPnl.transform, "", 9,  V(0.5f,0.25f), Hex(0.65f,0.58f,0.45f), new Vector2(200,56));
            _descLabel.enableWordWrapping = true;

            // ── Document panel ────────────────────────────────────────
            _docPanel  = Pnl(C, "Doc", Hex(0.96f,0.9f,0.78f), V(0.335f,0.05f), V(0.98f,0.43f));
            _docTitle  = SmTxt(_docPanel.transform, "Letter of Passage", 11, V(0.5f,0.88f), Hex(0.15f,0.08f,0f), new Vector2(300,22), true, TextAlignmentOptions.Center);
            _docIssued = SmTxt(_docPanel.transform, "", 9, V(0.06f,0.73f), Hex(0.2f,0.12f,0.03f), new Vector2(280,20));
            _docBearer = SmTxt(_docPanel.transform, "", 9, V(0.06f,0.60f), Hex(0.2f,0.12f,0.03f), new Vector2(280,20));
            _docPurpose= SmTxt(_docPanel.transform, "", 9, V(0.06f,0.47f), Hex(0.2f,0.12f,0.03f), new Vector2(280,20));
            _docValid  = SmTxt(_docPanel.transform, "", 9, V(0.06f,0.34f), Hex(0.2f,0.12f,0.03f), new Vector2(280,20));
            _docSeal   = SmTxt(_docPanel.transform, "", 10, V(0.06f,0.18f), Hex(0.1f,0.5f,0.1f), new Vector2(280,22), true);

            _stampOK = Pnl(_docPanel.transform, "SOK", Color.clear, V(0.05f,0.2f), V(0.95f,0.8f));
            SmTxt(_stampOK.transform, "APPROVED", 22, V(0.5f,0.5f), Hex(0.1f,0.55f,0.1f,0.88f), new Vector2(280,48), true, TextAlignmentOptions.Center);
            _stampNO = Pnl(_docPanel.transform, "SNO", Color.clear, V(0.05f,0.2f), V(0.95f,0.8f));
            SmTxt(_stampNO.transform, "DENIED", 22, V(0.5f,0.5f), Hex(0.7f,0.1f,0.1f,0.88f), new Vector2(280,48), true, TextAlignmentOptions.Center);

            // ── Buttons ───────────────────────────────────────────────
            var bPnl = Pnl(C, "Btns", Color.clear, V(0.335f,0f), V(0.98f,0.068f));
            _acceptBtn = MakeBtn(bPnl.transform, "Let Through", V(0.17f,0.5f), new Vector2(128,32), Hex(0.1f,0.55f,0.1f), Color.white);
            _denyBtn   = MakeBtn(bPnl.transform, "Deny Entry",  V(0.5f, 0.5f), new Vector2(128,32), Hex(0.55f,0.1f,0.1f), Color.white);
            _guardBtn  = MakeBtn(bPnl.transform, "Call Guard",  V(0.83f,0.5f), new Vector2(128,32), Hex(0.55f,0.4f,0.1f), Color.white);
            _bellBtn   = MakeBtn(bPnl.transform, "Ring Bell",   V(0.5f, 0.5f), new Vector2(160,32), Hex(0.85f,0.7f,0.1f), Hex(0.05f,0.02f,0f));

            _acceptBtn.onClick.AddListener(() => _gm.MakeDecision(Action.Accept));
            _denyBtn.onClick.AddListener(()   => _gm.MakeDecision(Action.Deny));
            _guardBtn.onClick.AddListener(()  => _gm.MakeDecision(Action.CallGuard));
            _bellBtn.onClick.AddListener(()   => _gm.RingBell());

            // ── Dialogue box ──────────────────────────────────────────
            var dlgPnl  = Pnl(C, "Dlg", Hex(0.04f,0.03f,0.09f,0.97f), V(0,0), V(1,0.135f));
            _speakerTxt = SmTxt(dlgPnl.transform, "...", 10, V(0.012f,0.78f), Hex(1f,0.85f,0.2f), new Vector2(280,20), true);
            _dlgTxt     = SmTxt(dlgPnl.transform, "",   11, V(0.5f, 0.36f),  Hex(0.9f,0.85f,0.72f), new Vector2(600,52));
            _dlgTxt.enableWordWrapping = true;
            var advGO = new GameObject("AdvBtn");
            advGO.transform.SetParent(dlgPnl.transform, false);
            var advRT = advGO.AddComponent<RectTransform>();
            advRT.anchorMin = advRT.anchorMax = V(0.975f, 0.35f);
            advRT.sizeDelta = new Vector2(26,26);
            advGO.AddComponent<Image>().color = Hex(0.85f,0.7f,0.1f);
            _advBtn = advGO.AddComponent<Button>();
            var advLbl = new GameObject("L"); advLbl.transform.SetParent(advGO.transform, false);
            var alvRT  = advLbl.AddComponent<RectTransform>();
            alvRT.anchorMin = Vector2.zero; alvRT.anchorMax = Vector2.one; alvRT.offsetMin = alvRT.offsetMax = Vector2.zero;
            var alvTMP = advLbl.AddComponent<TextMeshProUGUI>();
            alvTMP.text = ">"; alvTMP.fontSize = 12; alvTMP.color = Hex(0.05f,0.02f,0f);
            alvTMP.alignment = TextAlignmentOptions.Center; alvTMP.fontStyle = FontStyles.Bold;
            _advBtn.onClick.AddListener(() => _gm.Advance());

            // Coins HUD
            _coinsTxt = SmTxt(C, "Coins: 20", 11, V(0.97f,0.97f), Hex(1f,0.85f,0.2f), new Vector2(120,22), true, TextAlignmentOptions.Right);

            // ── End screen ────────────────────────────────────────────
            _endScreen = Pnl(C, "End", Hex(0.04f,0.03f,0.09f,0.95f), V(0,0), V(1,1));
            BigTxt(_endScreen.transform, "DAY COMPLETE", 26, V(0.5f,0.72f), Hex(1f,0.85f,0.2f));
            _statsTxt = SmTxt(_endScreen.transform, "", 13, V(0.5f,0.44f), Hex(0.85f,0.8f,0.65f), new Vector2(420,180));
            _statsTxt.enableWordWrapping = true;
            _statsTxt.alignment = TextAlignmentOptions.Center;
            var agBtn = MakeBtn(_endScreen.transform, "SERVE AGAIN", V(0.5f,0.14f), new Vector2(180,40),
                Hex(0.85f,0.7f,0.1f), Hex(0.05f,0.02f,0f));
            agBtn.onClick.AddListener(() => { SetActive(_endScreen,false); SetActive(_titleScreen,true); });
        }

        void HookEvents()
        {
            _gm.OnState         += OnState;
            _gm.OnLine          += OnLine;
            _gm.OnVisitorArrived += OnVisitor;
            _gm.OnDecision      += OnDecision;
            _gm.OnDayEnd        += OnDayEnd;
        }

        // ════════════════════════════════════════════════════════════════
        //  EVENT HANDLERS
        // ════════════════════════════════════════════════════════════════

        void OnState(GameManager.State s)
        {
            bool check = s == GameManager.State.Checking;
            bool bell  = s == GameManager.State.WaitBell;
            SetButtons(check);
            ShowBell(bell);
            if (!check) { SetActive(_stampOK, false); SetActive(_stampNO, false); }
            if (s == GameManager.State.Intro) StartCoroutine(ShowBanner());
        }

        void OnLine(DialogueLine l)
        {
            if (_speakerTxt) _speakerTxt.text = l.speaker;
            if (_dlgTxt)     StopAllCoroutines_Safe(() => StartCoroutine(Type(l.text)));
        }

        bool _typing;
        string _full;
        void StopAllCoroutines_Safe(System.Action then)
        {
            // Just start a new type coroutine; old one overwrites gracefully
            then?.Invoke();
        }

        IEnumerator Type(string msg)
        {
            _full = msg; _typing = true;
            if (_dlgTxt) _dlgTxt.text = "";
            float acc = 0; int idx = 0;
            while (idx < msg.Length)
            {
                acc += Time.deltaTime * 42f;
                int n = Mathf.FloorToInt(acc);
                if (n > 0) { acc -= n; idx = Mathf.Min(idx + n, msg.Length); if (_dlgTxt) _dlgTxt.text = msg.Substring(0, idx); }
                yield return null;
            }
            _typing = false;
        }

        void OnVisitor(Visitor v)
        {
            // Spawn walker
            if (_visitorGO) Destroy(_visitorGO);
            _visitorGO = new GameObject("Visitor");
            _visitorGO.transform.position   = new Vector3(-12f, 0.1f, 0);
            _visitorGO.transform.localScale = Vector3.one * 2f;
            var sr = _visitorGO.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;
            _walker = _visitorGO.AddComponent<Walker>();
            var frames = _sheets.Get(v.type);
            _walker.Init(frames);

            // Portrait
            if (frames != null && frames.Length > 0 && _portraitImg)
            {
                _portraitImg.sprite = frames[0];
                _portraitImg.preserveAspect = true;
            }
            if (_nameLabel) _nameLabel.text = v.name;
            if (_descLabel) _descLabel.text = DescFor(v.type);

            // Show document
            SetActive(_docPanel, true);
            var d = v.doc;
            bool bad = !d.IsValid();
            _docPanel.GetComponent<Image>().color = bad ? Hex(0.96f,0.84f,0.78f) : Hex(0.96f,0.9f,0.78f);
            if (_docTitle)   _docTitle.text   = d.title;
            if (_docIssued)  _docIssued.text  = "Issued by: " + d.issuedBy;
            if (_docBearer)  _docBearer.text  = "Bearer: " + d.bearer;
            if (_docPurpose) _docPurpose.text = "Purpose: " + d.purpose;
            if (_docValid)   _docValid.text   = "Valid until: " + d.validUntil;
            if (_docSeal)
            {
                _docSeal.text  = "Seal: " + d.SealLabel();
                _docSeal.color = d.IsValid() ? Hex(0.1f,0.5f,0.1f) : Hex(0.7f,0.1f,0.1f);
            }

            _walker.WalkTo(-1.5f, () => _walker.Idle());
        }

        void OnDecision(Action a, bool ok)
        {
            SetActive(_stampOK, a == Action.Accept);
            SetActive(_stampNO, a != Action.Accept);
            if (_walker == null) return;
            if (a == Action.Accept)    _walker.WalkOff(12f);
            else if (a == Action.Deny) _walker.WalkOff(-12f);
            else                       _walker.Arrest();
        }

        void OnDayEnd()
        {
            StartCoroutine(ShowEnd());
        }

        IEnumerator ShowBanner()
        {
            if (_bannerTxt) _bannerTxt.text = "Day " + _gm.Day;
            SetActive(_dayBanner, true);
            yield return new WaitForSeconds(2f);
            SetActive(_dayBanner, false);
        }

        IEnumerator ShowEnd()
        {
            yield return new WaitForSeconds(1.2f);
            if (_statsTxt) _statsTxt.text =
                $"Approved: {_gm.Approved}    Arrested: {_gm.Arrested}    Mistakes: {_gm.Mistakes}\n\n" +
                $"Coins Earned: {_gm.Coins}\n\n" +
                (_gm.Mistakes == 0 ? "Perfect duty! The King is pleased." : "Errors were made. The Head Guard is watching.");
            SetActive(_endScreen, true);
        }

        // ════════════════════════════════════════════════════════════════
        //  UPDATE
        // ════════════════════════════════════════════════════════════════

        void Update()
        {
            if (_coinsTxt && _gm) _coinsTxt.text = "Coins: " + _gm.Coins;
        }

        IEnumerator GuardAnim()
        {
            while (true)
            {
                if (_guardFrames != null && _guardFrames.Length > 0)
                {
                    var f = _guardFrames[_gf % _guardFrames.Length];
                    if (_g1) _g1.sprite = f;
                    if (_g2) _g2.sprite = f;
                    _gf++;
                }
                yield return new WaitForSeconds(0.35f);
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  HELPERS
        // ════════════════════════════════════════════════════════════════

        void SetButtons(bool on)
        {
            SetBtn(_acceptBtn, on); SetBtn(_denyBtn, on); SetBtn(_guardBtn, on);
        }

        void ShowBell(bool on) { if (_bellBtn) _bellBtn.gameObject.SetActive(on); }

        void SetBtn(Button b, bool on)
        {
            if (!b) return;
            b.interactable = on;
            var img = b.GetComponent<Image>(); if (!img) return;
            var c = img.color; c.a = on ? 1f : 0.35f; img.color = c;
        }

        static void SetActive(GameObject g, bool v) { if (g) g.SetActive(v); }

        static string DescFor(ActorType t)
        {
            switch(t)
            {
                case ActorType.Farmer:   return "A farmer from the outlying villages.";
                case ActorType.Merchant: return "A traveling merchant.";
                case ActorType.ShadyGuy: return "A shifty individual. Beady eyes.";
                case ActorType.Stranger: return "A stranger of unknown origin.";
                case ActorType.Beggar:   return "A beggar seeking shelter.";
                default:                 return "A common villager.";
            }
        }

        static Color Hex(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
        static Vector2 V(float x, float y) => new Vector2(x, y);

        // ── UI factory ────────────────────────────────────────────────

        static GameObject Pnl(Transform parent, string name, Color color, Vector2 amin, Vector2 amax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = amin; rt.anchorMax = amax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.AddComponent<Image>().color = color;
            return go;
        }

        static TextMeshProUGUI BigTxt(Transform p, string txt, int sz, Vector2 anchor, Color col)
        {
            var go = new GameObject("T"); go.transform.SetParent(p, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor; rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(500, 40);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = txt; t.fontSize = sz; t.color = col;
            t.fontStyle = FontStyles.Bold; t.alignment = TextAlignmentOptions.Center;
            return t;
        }

        static TextMeshProUGUI SmTxt(Transform p, string txt, int sz, Vector2 anchor, Color col,
            Vector2 sd = default, bool bold = false, TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            var go = new GameObject("T"); go.transform.SetParent(p, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor; rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = sd == default ? new Vector2(300, 26) : sd;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = txt; t.fontSize = sz; t.color = col; t.alignment = align;
            if (bold) t.fontStyle = FontStyles.Bold;
            return t;
        }

        static Image MakeImg(Transform p, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject("Img"); go.transform.SetParent(p, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor; rt.anchoredPosition = Vector2.zero; rt.sizeDelta = size;
            return go.AddComponent<Image>();
        }

        static Button MakeBtn(Transform p, string lbl, Vector2 anchor, Vector2 size, Color bg, Color tc)
        {
            var go = new GameObject("Btn_" + lbl); go.transform.SetParent(p, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor; rt.anchoredPosition = Vector2.zero; rt.sizeDelta = size;
            go.AddComponent<Image>().color = bg;
            var btn = go.AddComponent<Button>();
            var cb = ColorBlock.defaultColorBlock;
            cb.normalColor = bg; cb.highlightedColor = bg * 1.25f;
            cb.pressedColor = bg * 0.7f; cb.disabledColor = new Color(bg.r,bg.g,bg.b,0.35f);
            btn.colors = cb;
            var lgo = new GameObject("L"); lgo.transform.SetParent(go.transform, false);
            var lrt = lgo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            var t = lgo.AddComponent<TextMeshProUGUI>();
            t.text = lbl; t.fontSize = 11; t.color = tc;
            t.alignment = TextAlignmentOptions.Center; t.fontStyle = FontStyles.Bold;
            return btn;
        }

        void MakeQuad(string name, float cx, float cy, float w, float h, Color col, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(cx, cy, 0);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = order;
            int pw = Mathf.Max(1, Mathf.RoundToInt(w*32));
            int ph = Mathf.Max(1, Mathf.RoundToInt(h*32));
            var tex = new Texture2D(pw, ph, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pix = new Color[pw*ph]; for (int i=0;i<pix.Length;i++) pix[i]=Color.white;
            tex.SetPixels(pix); tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0,0,pw,ph), new Vector2(0.5f,0f), 32f);
            sr.color = col;
        }

        void Tile(Texture2D tex, int gx, int gy, int col, int row, int tcols, int trows, int order)
        {
            int tw=tex.width/tcols, th=tex.height/trows;
            var go = new GameObject("Tile");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(gx, gy, 0);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = order;
            sr.sprite = Sprite.Create(tex, new Rect(col*tw, tex.height-(row+1)*th, tw, th),
                                      new Vector2(0.5f,0f), 32f);
        }

        void Torch(float wx, float wy)
        {
            MakeQuad("Holder", wx, wy, 0.2f, 0.7f, Hex(0.42f,0.35f,0.22f), -2);
            var go = new GameObject("Flame");
            go.transform.SetParent(transform, false);
            go.transform.position   = new Vector3(wx, wy+0.5f, 0);
            go.transform.localScale = Vector3.one * 1.5f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = -1;
            var t = Resources.Load<Texture2D>("Props/TX FX Torch Flame");
            if (t != null)
            {
                t.filterMode = FilterMode.Point;
                int fw = t.width/4;
                sr.sprite = Sprite.Create(t, new Rect(0,0,fw,t.height), new Vector2(0.5f,0f), 32f);
            }
            sr.color = Hex(1f,0.7f,0.3f);
            go.AddComponent<TorchFlick>();
        }
    }

    public class TorchFlick : MonoBehaviour
    {
        SpriteRenderer _sr; float _t, _iv;
        void Start() { _sr = GetComponent<SpriteRenderer>(); _iv = Random.Range(0.05f,0.12f); }
        void Update()
        {
            _t += Time.deltaTime;
            if (_t < _iv) return;
            _t = 0; _iv = Random.Range(0.05f,0.12f);
            if (_sr) _sr.color = new Color(1f, Random.Range(0.55f,1f), Random.Range(0.1f,0.4f));
        }
    }
}
