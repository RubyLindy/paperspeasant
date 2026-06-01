using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PP
{
    // Current in-game season  -  shown on title screen, used to judge expired docs
    public enum Season { Spring, Summer, Autumn, Winter }

    public class Game : MonoBehaviour
    {
        public static Season CurrentSeason = Season.Summer;  // Year 1042, Midsummer

        GameObject      _titleScreen, _dayBanner, _endScreen, _docPanel, _dlgPanel;
        TextMeshProUGUI _bannerTxt, _speakerTxt, _dlgTxt, _coinsTxt;
        TextMeshProUGUI _docTitle, _docIssued, _docBearer, _docPurpose, _docValid, _docSeal, _docFlavour;
        TextMeshProUGUI _nameLabel, _descLabel, _statsTxt;
        Image           _portraitImg;
        GameObject      _stampOK, _stampNO;
        Button          _acceptBtn, _denyBtn, _guardBtn, _bellBtn, _advBtn;
        GameObject      _visitorGO;
        Walker          _walker;
        SpriteRenderer  _g1, _g2;
        Sprite[]        _guardFrames;
        int             _gf;
        GameManager     _gm;
        Sheets          _sheets;
        AudioManager    _audio;
        WeatherSystem   _weather;

        // Gate entrance world X  -  NPCs walk here when accepted
        const float GATE_X     = 0f;
        const float OFF_RIGHT   = 12f;
        const float OFF_LEFT    = -12f;

        void Start()
        {
            BuildManagers();
            BuildCamera();
            BuildBackground();
            BuildUI();
            HookEvents();

            Show(_dayBanner,          false);
            Show(_endScreen,          false);
            Show(_docPanel,           false);
            var wpObj = GameObject.Find("WantedPanel");
            if (wpObj != null) wpObj.SetActive(false);
            Show(_stampOK,            false);
            Show(_stampNO,            false);
            Show(_advBtn.gameObject,  false);
            Show(_bellBtn.gameObject, false);
            Show(_dlgPanel,           false);
            SetButtons(false);
            Show(_titleScreen, true);

            StartCoroutine(GuardAnim());
        }

        void BuildManagers()
        {
            var go  = new GameObject("_Managers");
            _gm     = go.AddComponent<GameManager>();
            _sheets = go.AddComponent<Sheets>();
            _audio  = go.AddComponent<AudioManager>();

            // Weather lives on its own GO so it can parent rain drops
            var wGO  = new GameObject("Weather");
            _weather = wGO.AddComponent<WeatherSystem>();
        }

        void BuildCamera()
        {
            var go  = new GameObject("Main Camera");
            go.tag  = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.orthographic     = true;
            cam.orthographicSize = 5.625f;
            cam.backgroundColor  = new Color(0.06f, 0.04f, 0.12f);
            cam.clearFlags       = CameraClearFlags.SolidColor;
            go.transform.position = new Vector3(0, 1.5f, -10f);
        }

        void BuildBackground()
        {
            // ── Castle background sprite ──────────────────────────────
            var bgTex = Resources.Load<Texture2D>("Props/CastleBG");
            if (bgTex != null)
            {
                bgTex.filterMode = FilterMode.Point;
                var bgGO = new GameObject("CastleBG");
                bgGO.transform.SetParent(transform, false);
                // Position so the 640x220 image fills the upper scene area
                // At 32 PPU: 640px = 20 units wide, 220px = 6.875 units tall
                bgGO.transform.position = new Vector3(0, 2.8f, 0);
                var sr = bgGO.AddComponent<SpriteRenderer>();
                sr.sortingOrder = -10;
                sr.sprite = Sprite.Create(bgTex,
                    new Rect(0, 0, bgTex.width, bgTex.height),
                    new Vector2(0.5f, 0.5f), 32f);
            }

            // ── Ground ────────────────────────────────────────────────
            var gt = Resources.Load<Texture2D>("Tileset/TX Tileset Ground");
            if (gt != null)
            {
                gt.filterMode = FilterMode.Point;
                int gc = gt.width/32, gr = gt.height/32;
                for (int x=-12;x<=12;x++) Tile(gt,x, 0,0,0,gc,gr,-4);
                for (int x=-12;x<=12;x++)
                for (int y=-5;y<0;y++)    Tile(gt,x, y,0,1,gc,gr,-5);
            }
            else
            {
                Quad("Gnd", 0,  0,   22, 0.6f, new Color(0.35f, 0.28f, 0.18f), -4);
                Quad("Sub", 0, -2.5f,22, 5f,   new Color(0.25f, 0.20f, 0.13f), -5);
            }

            // ── Guards ────────────────────────────────────────────────
            var gf = _sheets.GetGuard();
            _guardFrames = gf;

            var g1go = new GameObject("Guard1");
            g1go.transform.position   = new Vector3(2.5f, 0.05f, 0);
            g1go.transform.localScale = Vector3.one * 2.5f;
            _g1 = g1go.AddComponent<SpriteRenderer>(); _g1.sortingOrder = 6;
            if (gf!=null&&gf.Length>0) _g1.sprite = gf[0];

            var g2go = new GameObject("Guard2");
            g2go.transform.position   = new Vector3(-2.5f, 0.05f, 0);
            g2go.transform.localScale = Vector3.one * 2.5f;
            _g2 = g2go.AddComponent<SpriteRenderer>(); _g2.sortingOrder = 6; _g2.flipX = true;
            if (gf!=null&&gf.Length>0) _g2.sprite = gf[0];
        }


        void BuildTorch(float wx, float wy)
        {
            // Wall mount bracket
            Quad("TBkt", wx, wy-0.05f, 0.28f, 0.55f, new Color(0.42f, 0.35f, 0.22f), -1);
            // Handle
            Quad("THdl", wx, wy+0.18f, 0.10f, 0.44f, new Color(0.30f, 0.18f, 0.08f), 0);
            // Flame base bowl
            Quad("TBwl", wx, wy+0.42f, 0.22f, 0.12f, new Color(0.42f, 0.35f, 0.22f), 1);
            // Glow ring (large, soft)
            Quad("TGlw", wx, wy+0.6f, 0.80f, 0.80f, new Color(1.0f, 0.5f, 0.05f, 0.18f), 1);
            // Inner glow
            Quad("TGlI", wx, wy+0.6f, 0.40f, 0.40f, new Color(1.0f, 0.7f, 0.15f, 0.30f), 2);

            // Flame sprite
            var fGO = new GameObject("Flame");
            fGO.transform.SetParent(transform, false);
            fGO.transform.position   = new Vector3(wx, wy+0.44f, 0);
            fGO.transform.localScale = new Vector3(1.5f, 2.0f, 1f);
            var sr = fGO.AddComponent<SpriteRenderer>(); sr.sortingOrder = 3;
            var ft = Resources.Load<Texture2D>("Props/TX FX Torch Flame");
            if (ft != null)
            {
                ft.filterMode = FilterMode.Point;
                int fw = ft.width / 4;
                sr.sprite = Sprite.Create(ft, new Rect(0,0,fw,ft.height),
                    new Vector2(0.5f,0f), 32f);
            }
            sr.color = new Color(1f, 0.80f, 0.25f);
            fGO.AddComponent<TorchFlick>();
        }

        void BuildUI()
        {
            var cvGO = new GameObject("Canvas");
            var cv   = cvGO.AddComponent<Canvas>();
            cv.renderMode   = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 10;
            var sc = cvGO.AddComponent<CanvasScaler>();
            sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(640, 360);
            sc.matchWidthOrHeight  = 0.5f;
            cvGO.AddComponent<GraphicRaycaster>();
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Transform C = cvGO.transform;

            // Lower bg
            Pnl(C,"LoBG",RGB(0.05f,0.04f,0.02f),V(0,0),V(1,0.44f));

            // Portrait panel (NPC display)
            var pPnl = Pnl(C,"Portrait",RGB(0.09f,0.07f,0.02f),V(0,0),V(0.34f,0.44f));
            _portraitImg = Img(pPnl.transform,V(0.5f,0.65f),new Vector2(110,110));
            _portraitImg.color = Color.clear;
            _nameLabel = RTMP(pPnl.transform,"Awaiting...",11,
                V(0,0),V(1,0),V(4,-42),V(-4,-26),RGB(1f,0.85f,0.2f),true,TextAlignmentOptions.Center);
            _descLabel = RTMP(pPnl.transform,"",8,
                V(0,0),V(1,0),V(4,-64),V(-4,-44),RGB(0.65f,0.58f,0.45f),false,TextAlignmentOptions.Center);
            _descLabel.textWrappingMode = TextWrappingModes.Normal;

            // Document panel
            _docPanel = Pnl(C,"Doc",RGB(0.96f,0.9f,0.78f),V(0.35f,0.04f),V(0.99f,0.43f));
            _docTitle = RTMP(_docPanel.transform,"LETTER OF PASSAGE",9,
                V(0,1),V(1,1),V(4,-14),V(-4,-2),RGB(0.15f,0.08f,0f),true,TextAlignmentOptions.Center);
            DivLine(_docPanel.transform,-16f,-15f);
            _docSeal = RTMP(_docPanel.transform,"",8,
                V(0,1),V(1,1),V(4,-28),V(-4,-17),RGB(0.1f,0.45f,0.1f),true,TextAlignmentOptions.Center);
            _docBearer  = RTMP(_docPanel.transform,"",8,V(0,1),V(1,1),V(6,-40), V(-4,-28),RGB(0.2f,0.12f,0.03f),false,TextAlignmentOptions.Left);
            _docPurpose = RTMP(_docPanel.transform,"",8,V(0,1),V(1,1),V(6,-52), V(-4,-40),RGB(0.2f,0.12f,0.03f),false,TextAlignmentOptions.Left);
            _docIssued  = RTMP(_docPanel.transform,"",8,V(0,1),V(1,1),V(6,-64), V(-4,-52),RGB(0.2f,0.12f,0.03f),false,TextAlignmentOptions.Left);
            _docValid   = RTMP(_docPanel.transform,"",8,V(0,1),V(1,1),V(6,-76), V(-4,-64),RGB(0.2f,0.12f,0.03f),false,TextAlignmentOptions.Left);
            DivLine(_docPanel.transform,-78f,-77f);
            _docFlavour = RTMP(_docPanel.transform,"",7,
                V(0,1),V(1,1),V(6,-100),V(-4,-79),RGB(0.4f,0.28f,0.1f),false,TextAlignmentOptions.Center);
            _docFlavour.textWrappingMode = TextWrappingModes.Normal;
            _docFlavour.fontStyle = FontStyles.Italic;

            // ── Flip arrow: cycles between Letter of Passage and Wanted Posters ──
            // Transparent button, just a ">" arrow, sits on right edge of doc panel
            var flipGO = new GameObject("FlipBtn");
            flipGO.transform.SetParent(_docPanel.transform, false);
            var flipRT = flipGO.AddComponent<RectTransform>();
            flipRT.anchorMin = new Vector2(1f, 1f);
            flipRT.anchorMax = new Vector2(1f, 1f);
            flipRT.anchoredPosition = new Vector2(-8f, -8f);
            flipRT.sizeDelta = new Vector2(28f, 28f);
            // No background image  -  just transparent
            var flipImg = flipGO.AddComponent<Image>();
            flipImg.color = new Color(1f, 1f, 1f, 0.01f);
            flipImg.raycastTarget = true;
            var flipBtn = flipGO.AddComponent<Button>();
            var flipCB = ColorBlock.defaultColorBlock;
            flipCB.normalColor      = new Color(1f, 1f, 1f, 0.01f);
            flipCB.highlightedColor = new Color(1f, 1f, 1f, 0.01f);
            flipCB.pressedColor     = new Color(1f, 1f, 1f, 0.01f);
            flipCB.disabledColor    = new Color(1f, 1f, 1f, 0.01f);
            flipBtn.colors = flipCB;
            var flipLbl = new GameObject("Lbl");
            flipLbl.transform.SetParent(flipGO.transform, false);
            var flipLblRT = flipLbl.AddComponent<RectTransform>();
            flipLblRT.anchorMin = Vector2.zero; flipLblRT.anchorMax = Vector2.one;
            flipLblRT.offsetMin = flipLblRT.offsetMax = Vector2.zero;
            var flipTxt = flipLbl.AddComponent<TextMeshProUGUI>();
            flipTxt.text = ">"; flipTxt.fontSize = 16;
            flipTxt.color = new Color(0.55f, 0.30f, 0.05f);
            flipTxt.alignment = TextAlignmentOptions.Center;
            flipTxt.fontStyle = FontStyles.Bold;

            // Wanted poster panel  -  same bounds as doc panel, hidden by default
            var wantedPanel = Pnl(C,"WantedPanel",RGB(0.92f,0.82f,0.72f),V(0.35f,0.04f),V(0.99f,0.43f));
            wantedPanel.SetActive(false);
            var wTitle = RTMP(wantedPanel.transform,"WANTED",13,V(0,1),V(1,1),V(4,-18),V(-4,-2),new Color(0.65f,0.08f,0.08f),true,TextAlignmentOptions.Center);
            var wDivL  = new GameObject("WDL"); wDivL.transform.SetParent(wantedPanel.transform,false);
            var wDLRT  = wDivL.AddComponent<RectTransform>();
            wDLRT.anchorMin=new Vector2(0,1); wDLRT.anchorMax=new Vector2(1,1);
            wDLRT.offsetMin=new Vector2(4,-20); wDLRT.offsetMax=new Vector2(-4,-19);
            wDivL.AddComponent<Image>().color=new Color(0.6f,0.1f,0.1f);
            var wName  = RTMP(wantedPanel.transform,"",11,V(0,1),V(1,1),V(4,-36),V(-4,-20),new Color(0.15f,0.05f,0.05f),true,TextAlignmentOptions.Center);
            var wAlias = RTMP(wantedPanel.transform,"",8, V(0,1),V(1,1),V(4,-46),V(-4,-36),new Color(0.4f,0.15f,0.1f),false,TextAlignmentOptions.Center);
            var wDesc  = RTMP(wantedPanel.transform,"",8, V(0,1),V(1,1),V(8,-80),V(-4,-48),new Color(0.25f,0.1f,0.05f),false,TextAlignmentOptions.Left);
            wDesc.textWrappingMode = TextWrappingModes.Normal;
            var wWarn  = RTMP(wantedPanel.transform,"",8, V(0,1),V(1,1),V(4,-100),V(-4,-82),new Color(0.65f,0.08f,0.08f),true,TextAlignmentOptions.Center);
            wWarn.textWrappingMode = TextWrappingModes.Normal;
            // Back arrow on wanted panel (same style)
            var backGO = new GameObject("BackBtn");
            backGO.transform.SetParent(wantedPanel.transform, false);
            var backRT = backGO.AddComponent<RectTransform>();
            backRT.anchorMin = new Vector2(0f, 1f);
            backRT.anchorMax = new Vector2(0f, 1f);
            backRT.anchoredPosition = new Vector2(16f, -8f);
            backRT.sizeDelta = new Vector2(28f, 28f);
            var backImg = backGO.AddComponent<Image>();
            backImg.color = new Color(1f, 1f, 1f, 0.01f);
            backImg.raycastTarget = true;
            var backBtn = backGO.AddComponent<Button>();
            backBtn.colors = flipCB;
            var backLbl = new GameObject("Lbl");
            backLbl.transform.SetParent(backGO.transform, false);
            var backLblRT = backLbl.AddComponent<RectTransform>();
            backLblRT.anchorMin = Vector2.zero; backLblRT.anchorMax = Vector2.one;
            backLblRT.offsetMin = backLblRT.offsetMax = Vector2.zero;
            var backTxt = backLbl.AddComponent<TextMeshProUGUI>();
            backTxt.text = "<"; backTxt.fontSize = 16;
            backTxt.color = new Color(0.55f, 0.30f, 0.05f);
            backTxt.alignment = TextAlignmentOptions.Center;
            backTxt.fontStyle = FontStyles.Bold;

            // Wanted data
            var wantedNames   = new string[]{"GARRETT SALLOW","MIRA VOSS","DORN ASHWICK"};
            var wantedAliases = new string[]{"Alias: 'The Fox'","Alias: Unknown","Known Smuggler"};
            var wantedDescs   = new string[]{
                "Wanted for forgery and impersonation of royal officials.\nLast seen near the eastern road.\nApproaches gates with false Emmeloord seals.",
                "Wanted for theft of royal goods.\nMedium height, dark cloak, nervous manner.\nMay attempt bribery.",
                "Known to carry false Emmeloord seals.\nOften poses as a merchant.\nInspect cargo permits carefully."
            };
            var wantedWarns = new string[]{
                "DETAIN ON SIGHT  -  Call the guard immediately.",
                "DO NOT ALLOW ENTRY  -  Detain if seen.",
                "INSPECT ALL DOCUMENTS  -  Call guard if suspicious."
            };
            int wantedIdx = 0;
            int wantedCount = wantedNames.Length;

            // Use arrays captured by lambdas directly
            System.Action refreshWanted = () => {
                int i = wantedIdx;
                wName.text  = wantedNames[i];
                wAlias.text = wantedAliases[i];
                wDesc.text  = wantedDescs[i];
                wWarn.text  = wantedWarns[i];
                backBtn.interactable = true;
                wTitle.text = "WANTED  (" + (i+1) + "/" + wantedCount + ")";
            };

            // Next wanted poster arrow on wanted panel
            var wNextGO = new GameObject("WNextBtn");
            wNextGO.transform.SetParent(wantedPanel.transform, false);
            var wNextRT = wNextGO.AddComponent<RectTransform>();
            wNextRT.anchorMin = new Vector2(1f,1f); wNextRT.anchorMax = new Vector2(1f,1f);
            wNextRT.anchoredPosition = new Vector2(-8f,-8f); wNextRT.sizeDelta = new Vector2(28f,28f);
            var wNextImg = wNextGO.AddComponent<Image>();
            wNextImg.color = new Color(1f, 1f, 1f, 0.01f);
            wNextImg.raycastTarget = true;
            var wNextBtn = wNextGO.AddComponent<Button>(); wNextBtn.colors = flipCB;
            var wNextLbl = new GameObject("L"); wNextLbl.transform.SetParent(wNextGO.transform,false);
            var wNLRT = wNextLbl.AddComponent<RectTransform>();
            wNLRT.anchorMin=Vector2.zero; wNLRT.anchorMax=Vector2.one;
            wNLRT.offsetMin=wNLRT.offsetMax=Vector2.zero;
            var wNextT = wNextLbl.AddComponent<TextMeshProUGUI>();
            wNextT.text=">"; wNextT.fontSize=16; wNextT.color=new Color(0.55f,0.30f,0.05f);
            wNextT.alignment=TextAlignmentOptions.Center; wNextT.fontStyle=FontStyles.Bold;

            // Wire flip/back/next
            flipBtn.onClick.AddListener(() => {
                Show(_docPanel, false);
                wantedIdx = 0;
                refreshWanted();
                Show(wantedPanel, true);
            });
            backBtn.onClick.AddListener(() => {
                Show(wantedPanel, false);
                Show(_docPanel, true);
            });
            wNextBtn.onClick.AddListener(() => {
                wantedIdx = (wantedIdx + 1) % wantedCount;
                refreshWanted();
            });

            _stampOK = Pnl(_docPanel.transform,"SOK",Color.clear,V(0.05f,0.2f),V(0.95f,0.8f));
            RTMP(_stampOK.transform,"APPROVED",20,V(0,0),V(1,1),V(0,0),V(0,0),RGB(0.1f,0.55f,0.1f,0.88f),true,TextAlignmentOptions.Center);
            _stampNO = Pnl(_docPanel.transform,"SNO",Color.clear,V(0.05f,0.2f),V(0.95f,0.8f));
            RTMP(_stampNO.transform,"DENIED",20,V(0,0),V(1,1),V(0,0),V(0,0),RGB(0.7f,0.1f,0.1f,0.88f),true,TextAlignmentOptions.Center);

            // Buttons
            var bPnl = Pnl(C,"Btns",Color.clear,V(0.35f,0.005f),V(0.99f,0.12f));
            _acceptBtn = Btn(bPnl.transform,"Let Through",V(0.17f,0.5f),new Vector2(124,32),RGB(0.1f,0.55f,0.1f),Color.white);
            _denyBtn   = Btn(bPnl.transform,"Deny Entry", V(0.5f, 0.5f),new Vector2(124,32),RGB(0.55f,0.1f,0.1f),Color.white);
            _guardBtn  = Btn(bPnl.transform,"Call Guard", V(0.83f,0.5f),new Vector2(124,32),RGB(0.55f,0.4f,0.1f),Color.white);
            _bellBtn   = Btn(bPnl.transform,"Ring Bell",  V(0.5f, 0.5f),new Vector2(170,32),RGB(0.85f,0.7f,0.1f),Color.black);
            _acceptBtn.onClick.AddListener(()=>_gm.MakeDecision(Action.Accept));
            _denyBtn.onClick.AddListener(()=>_gm.MakeDecision(Action.Deny));
            _guardBtn.onClick.AddListener(()=>_gm.MakeDecision(Action.CallGuard));
            _bellBtn.onClick.AddListener(()=>{ if(_audio)_audio.PlayMurmur(); _gm.RingBell(); });

            // Dialogue
            _dlgPanel   = Pnl(C,"Dlg",RGB(0.04f,0.03f,0.09f,0.97f),V(0,0.14f),V(1,0.28f));
            _speakerTxt = RTMP(_dlgPanel.transform,"",10,V(0,1),V(1,1),V(8,-20),V(-40,-4),RGB(1f,0.85f,0.2f),true,TextAlignmentOptions.Left);
            _dlgTxt     = RTMP(_dlgPanel.transform,"",10,V(0,0),V(1,1),V(8,4),V(-40,-22),RGB(0.9f,0.85f,0.72f),false,TextAlignmentOptions.Left);
            _dlgTxt.textWrappingMode = TextWrappingModes.Normal;

            var advGO = new GameObject("AdvBtn");
            advGO.transform.SetParent(_dlgPanel.transform,false);
            var advRT = advGO.AddComponent<RectTransform>();
            advRT.anchorMin=new Vector2(1,0); advRT.anchorMax=new Vector2(1,1);
            advRT.offsetMin=new Vector2(-36,4); advRT.offsetMax=new Vector2(-4,-4);
            advGO.AddComponent<Image>().color=RGB(0.85f,0.7f,0.1f);
            _advBtn = advGO.AddComponent<Button>();
            var advL = new GameObject("L"); advL.transform.SetParent(advGO.transform,false);
            var alvRT = advL.AddComponent<RectTransform>();
            alvRT.anchorMin=Vector2.zero; alvRT.anchorMax=Vector2.one;
            alvRT.offsetMin=alvRT.offsetMax=Vector2.zero;
            var alvT = advL.AddComponent<TextMeshProUGUI>();
            alvT.text=">"; alvT.fontSize=14; alvT.color=Color.black;
            alvT.alignment=TextAlignmentOptions.Center; alvT.fontStyle=FontStyles.Bold;
            _advBtn.onClick.AddListener(()=>{ Show(_advBtn.gameObject,false); Show(_dlgPanel,false); _gm.Advance(); });

            _coinsTxt = Lbl(C,"Coins: 20",12,V(0.82f,0.97f),RGB(1f,0.85f,0.2f),new Vector2(200,24),true,TextAlignmentOptions.Right);

            _dayBanner = Pnl(C,"Banner",RGB(0.04f,0.02f,0.1f,0.92f),V(0.25f,0.35f),V(0.75f,0.65f));
            _bannerTxt = Lbl(_dayBanner.transform,"DAY 1",32,V(0.5f,0.5f),RGB(1f,0.85f,0.2f),new Vector2(300,44),true,TextAlignmentOptions.Center);

            _endScreen = Pnl(C,"End",RGB(0.04f,0.03f,0.09f,0.95f),V(0,0),V(1,1));
            Lbl(_endScreen.transform,"DAY COMPLETE",26,V(0.5f,0.72f),RGB(1f,0.85f,0.2f),new Vector2(400,40),true,TextAlignmentOptions.Center);
            _statsTxt = Lbl(_endScreen.transform,"",13,V(0.5f,0.44f),RGB(0.85f,0.8f,0.65f),new Vector2(420,180),false,TextAlignmentOptions.Center);
            _statsTxt.textWrappingMode = TextWrappingModes.Normal;
            var agBtn = Btn(_endScreen.transform,"SERVE AGAIN",V(0.5f,0.14f),new Vector2(180,40),RGB(0.85f,0.7f,0.1f),Color.black);
            agBtn.onClick.AddListener(()=>{ Show(_endScreen,false); Show(_titleScreen,true); });

            // ── Title screen LAST ─────────────────────────────────────
            _titleScreen = Pnl(C,"Title",RGB(0.05f,0.03f,0.1f),V(0,0),V(1,1));
            Lbl(_titleScreen.transform,"PAPERS, PEASANT!",28,V(0.5f,0.67f),
                RGB(1f,0.85f,0.2f),new Vector2(500,44),true,TextAlignmentOptions.Center);

            // Season display
            string seasonStr = SeasonString();
            Lbl(_titleScreen.transform, seasonStr, 11, V(0.5f,0.56f),
                SeasonColor(), new Vector2(400,22), false, TextAlignmentOptions.Center);

            Lbl(_titleScreen.transform,
                "You are the gate keeper of Castle Ede.\nCheck all documents. Uphold the King's law.",
                11,V(0.5f,0.43f),RGB(0.7f,0.65f,0.55f),new Vector2(420,52),false,TextAlignmentOptions.Center);
            var sb = Btn(_titleScreen.transform,"BEGIN DUTY",V(0.5f,0.17f),
                new Vector2(170,40),RGB(0.85f,0.7f,0.1f),Color.black);
            sb.onClick.AddListener(()=>{ Show(_titleScreen,false); _gm.StartGame(); });
        }

        // ── Season helpers ─────────────────────────────────────────────
        static string SeasonString()
        {
            switch (CurrentSeason)
            {
                case Season.Spring: return "Kingdom of Emmeloord   -   Spring, Year 1042";
                case Season.Summer: return "Kingdom of Emmeloord   -   Midsummer, Year 1042";
                case Season.Autumn: return "Kingdom of Emmeloord   -   Autumn, Year 1042";
                case Season.Winter: return "Kingdom of Emmeloord   -   Winter, Year 1042";
                default: return "Kingdom of Emmeloord   -   Year 1042";
            }
        }

        static Color SeasonColor()
        {
            switch (CurrentSeason)
            {
                case Season.Spring: return new Color(0.5f, 0.9f, 0.4f);
                case Season.Summer: return new Color(1.0f, 0.85f, 0.3f);
                case Season.Autumn: return new Color(0.9f, 0.55f, 0.2f);
                case Season.Winter: return new Color(0.7f, 0.85f, 1.0f);
                default: return Color.white;
            }
        }

        void DivLine(Transform p, float yMin, float yMax)
        {
            var go=new GameObject("Div"); go.transform.SetParent(p,false);
            var rt=go.AddComponent<RectTransform>();
            rt.anchorMin=new Vector2(0,1); rt.anchorMax=new Vector2(1,1);
            rt.offsetMin=new Vector2(4,yMin); rt.offsetMax=new Vector2(-4,yMax);
            go.AddComponent<Image>().color=RGB(0.5f,0.35f,0.1f);
        }

        void HookEvents()
        {
            _gm.OnState          += OnState;
            _gm.OnLine           += OnLine;
            _gm.OnVisitorArrived += OnVisitor;
            _gm.OnDecision       += OnDecision;
            _gm.OnDayEnd         += OnDayEnd;
        }

        void OnState(GameManager.State s)
        {
            SetButtons(s==GameManager.State.Checking);
            Show(_bellBtn.gameObject,s==GameManager.State.WaitBell);
            if(s!=GameManager.State.Checking){Show(_stampOK,false);Show(_stampNO,false);}
            if(s==GameManager.State.Intro) StartCoroutine(ShowBanner());
        }

        void OnLine(DialogueLine l)
        {
            Show(_dlgPanel,true);
            if(_speakerTxt)_speakerTxt.text=l.speaker;
            StopCoroutine("TypeText");
            StartCoroutine(TypeText(l.text));
            Show(_advBtn.gameObject,false);
        }

        IEnumerator TypeText(string msg)
        {
            if(_dlgTxt)_dlgTxt.text="";
            float acc=0; int idx=0;
            while(idx<msg.Length)
            {
                acc+=Time.deltaTime*42f; int n=Mathf.FloorToInt(acc);
                if(n>0){acc-=n;idx=Mathf.Min(idx+n,msg.Length);if(_dlgTxt)_dlgTxt.text=msg.Substring(0,idx);}
                yield return null;
            }
            Show(_advBtn.gameObject,true);
        }

        void OnVisitor(Visitor v)
        {
            if(_visitorGO)Destroy(_visitorGO);
            _visitorGO=new GameObject("Visitor");
            _visitorGO.transform.position  =new Vector3(-12f,0f,0);
            _visitorGO.transform.localScale =Vector3.one*2.5f;
            var sr=_visitorGO.AddComponent<SpriteRenderer>(); sr.sortingOrder=5;
            _walker=_visitorGO.AddComponent<Walker>();
            var frames=_sheets.Get(v.type);
            _walker.Init(frames);

            if(frames!=null&&frames.Length>0&&_portraitImg!=null)
            {_portraitImg.sprite=frames[0];_portraitImg.color=Color.white;_portraitImg.preserveAspect=true;}
            if(_nameLabel)_nameLabel.text=v.name;
            if(_descLabel)_descLabel.text=DescFor(v.type);

            Show(_docPanel,true);
            // Always return to Letter of Passage when new visitor arrives
            var wantedPanelObj = GameObject.Find("WantedPanel");
            if (wantedPanelObj != null) wantedPanelObj.SetActive(false);
            var d=v.doc; bool valid=d.IsValid();
            _docPanel.GetComponent<Image>().color=RGB(0.96f,0.9f,0.78f);
            if(_docTitle)  _docTitle.text ="LETTER OF PASSAGE";
            if(_docSeal)  { _docSeal.text = "Seal: " + d.issuedBy; _docSeal.color = RGB(0.2f, 0.12f, 0.03f); }
            if(_docBearer)  _docBearer.text  ="Bearer:      "+d.bearer;
            if(_docPurpose) _docPurpose.text ="Purpose:   "+d.purpose;
            if(_docIssued)  _docIssued.text  ="Origin:       "+d.issuedBy;
            if(_docValid)  { _docValid.text = "Valid until: " + d.validUntil; _docValid.color = RGB(0.2f, 0.12f, 0.03f); }
            if(_docFlavour) _docFlavour.text =FlavourFor(v.type,valid);
            _walker.WalkTo(-1.8f,()=>_walker.Idle());
            StartCoroutine(SlideDocIn());
        }

        void OnDecision(Action a, bool ok)
        {
            Show(_stampOK,a==Action.Accept);
            Show(_stampNO,a!=Action.Accept);
            // Stamp sound + screen shake
            if (_audio) _audio.PlayStamp();
            StartCoroutine(ScreenShake(0.08f, 0.18f));
            if(_walker==null)return;

            if (a==Action.Accept)
            {
                // Walk INTO the castle gate, then disappear
                _walker.WalkThroughGate(GATE_X, OFF_RIGHT);
            }
            else if(a==Action.Deny)
            {
                _walker.WalkOff(OFF_LEFT);
            }
            else
            {
                _walker.Arrest();
            }
        }

        void OnDayEnd()=>StartCoroutine(ShowEnd());

        IEnumerator ShowBanner()
        {if(_bannerTxt)_bannerTxt.text="Day "+_gm.Day; Show(_dayBanner,true);
         yield return new WaitForSeconds(2f); Show(_dayBanner,false);}

        IEnumerator ShowEnd()
        {
            yield return new WaitForSeconds(1.2f);
            if(_statsTxt)_statsTxt.text=
                "Approved: "+_gm.Approved+"    Arrested: "+_gm.Arrested+"    Mistakes: "+_gm.Mistakes+
                "\n\nCoins Earned: "+_gm.Coins+"\n\n"+
                (_gm.Mistakes==0?"Perfect duty! The King is pleased.":"Errors were made. The Head Guard is watching.");
            Show(_endScreen,true);
        }

        IEnumerator ScreenShake(float magnitude, float duration)
        {
            var cam = Camera.main;
            if (cam == null) yield break;
            Vector3 orig = cam.transform.position;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float remaining = 1f - (t / duration);
                cam.transform.position = orig + new Vector3(
                    UnityEngine.Random.Range(-magnitude, magnitude) * remaining,
                    UnityEngine.Random.Range(-magnitude, magnitude) * remaining, 0);
                yield return null;
            }
            cam.transform.position = orig;
        }

        IEnumerator SlideDocIn()
        {
            if (_docPanel == null) yield break;
            var rt = _docPanel.GetComponent<RectTransform>();
            if (rt == null) yield break;
            // Slide from right edge
            Vector2 target = rt.anchoredPosition;
            rt.anchoredPosition = target + new Vector2(300f, 0);
            float t = 0;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                float ease = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / 0.25f), 3f);
                rt.anchoredPosition = Vector2.Lerp(target + new Vector2(300f, 0), target, ease);
                yield return null;
            }
            rt.anchoredPosition = target;
        }

        void Update(){if(_coinsTxt&&_gm)_coinsTxt.text="Coins: "+_gm.Coins;}

        IEnumerator GuardAnim()
        {
            while(true)
            {
                if(_guardFrames!=null&&_guardFrames.Length>0)
                {var f=_guardFrames[_gf%_guardFrames.Length];if(_g1)_g1.sprite=f;if(_g2)_g2.sprite=f;_gf++;}
                yield return new WaitForSeconds(0.35f);
            }
        }

        void SetButtons(bool on){SetBtn(_acceptBtn,on);SetBtn(_denyBtn,on);SetBtn(_guardBtn,on);}
        void SetBtn(Button b,bool on)
        {if(!b)return;b.interactable=on;var img=b.GetComponent<Image>();
         if(img){var c=img.color;c.a=on?1f:0.35f;img.color=c;}}
        static void Show(GameObject g,bool v){if(g)g.SetActive(v);}

        static string DescFor(ActorType t)
        {
            switch(t){
                case ActorType.Farmer:   return "A farmer from the outlying villages.";
                case ActorType.Merchant: return "A traveling merchant.";
                case ActorType.ShadyGuy: return "A shifty individual. Beady eyes.";
                case ActorType.Stranger: return "A stranger of unknown origin.";
                case ActorType.Beggar:   return "A beggar seeking shelter.";
                default:                 return "A common villager.";}
        }

        static string FlavourFor(ActorType t,bool valid)
        {
            if(!valid) return "WARNING: Seal does not match the Royal Registry. Entry must be denied.";
            switch(t){
                case ActorType.Farmer:   return "By order of His Majesty, this bearer is permitted entry to Castle Ede.";
                case ActorType.Merchant: return "Granted for trade. Bearer must declare all goods at the inner gate.";
                case ActorType.Villager: return "Issued by the Royal Scribe. Valid for a single entry only.";
                case ActorType.Stranger: return "Granted under review. Bearer must report to the steward.";
                case ActorType.Beggar:   return "Charitable passage by royal decree.";
                default:                 return "By order of the Crown, this passage is duly authorised.";}
        }

        static Color   RGB(float r,float g,float b,float a=1f)=>new Color(r,g,b,a);
        static Vector2 V(float x,float y)=>new Vector2(x,y);

        static TextMeshProUGUI RTMP(Transform p,string txt,int sz,
            Vector2 ancMin,Vector2 ancMax,Vector2 offMin,Vector2 offMax,
            Color col,bool bold,TextAlignmentOptions align)
        {
            var go=new GameObject("T"); go.transform.SetParent(p,false);
            var rt=go.AddComponent<RectTransform>();
            rt.anchorMin=ancMin; rt.anchorMax=ancMax;
            rt.offsetMin=offMin; rt.offsetMax=offMax;
            var t=go.AddComponent<TextMeshProUGUI>();
            t.text=txt; t.fontSize=sz; t.color=col; t.alignment=align;
            if(bold)t.fontStyle=FontStyles.Bold; return t;
        }

        static GameObject Pnl(Transform p,string n,Color c,Vector2 amin,Vector2 amax)
        {
            var go=new GameObject(n); go.transform.SetParent(p,false);
            var rt=go.AddComponent<RectTransform>();
            rt.anchorMin=amin; rt.anchorMax=amax; rt.offsetMin=rt.offsetMax=Vector2.zero;
            go.AddComponent<Image>().color=c; return go;
        }

        static TextMeshProUGUI Lbl(Transform p,string txt,int sz,Vector2 anchor,Color col,
            Vector2 sd=default,bool bold=false,TextAlignmentOptions align=TextAlignmentOptions.Left)
        {
            var go=new GameObject("T"); go.transform.SetParent(p,false);
            var rt=go.AddComponent<RectTransform>();
            rt.anchorMin=rt.anchorMax=anchor; rt.anchoredPosition=Vector2.zero;
            rt.sizeDelta=sd==default?new Vector2(300,26):sd;
            var t=go.AddComponent<TextMeshProUGUI>();
            t.text=txt; t.fontSize=sz; t.color=col; t.alignment=align;
            if(bold)t.fontStyle=FontStyles.Bold; return t;
        }

        static Image Img(Transform p,Vector2 anchor,Vector2 size)
        {
            var go=new GameObject("Img"); go.transform.SetParent(p,false);
            var rt=go.AddComponent<RectTransform>();
            rt.anchorMin=rt.anchorMax=anchor; rt.anchoredPosition=Vector2.zero; rt.sizeDelta=size;
            return go.AddComponent<Image>();
        }

        static Button Btn(Transform p,string lbl,Vector2 anchor,Vector2 size,Color bg,Color tc)
        {
            var go=new GameObject("Btn_"+lbl); go.transform.SetParent(p,false);
            var rt=go.AddComponent<RectTransform>();
            rt.anchorMin=rt.anchorMax=anchor; rt.anchoredPosition=Vector2.zero; rt.sizeDelta=size;
            go.AddComponent<Image>().color=bg;
            var btn=go.AddComponent<Button>();
            var cb=ColorBlock.defaultColorBlock;
            cb.normalColor=bg; cb.highlightedColor=bg*1.25f;
            cb.pressedColor=bg*0.7f; cb.disabledColor=new Color(bg.r,bg.g,bg.b,0.35f);
            btn.colors=cb;
            var lgo=new GameObject("L"); lgo.transform.SetParent(go.transform,false);
            var lrt=lgo.AddComponent<RectTransform>();
            lrt.anchorMin=Vector2.zero; lrt.anchorMax=Vector2.one;
            lrt.offsetMin=lrt.offsetMax=Vector2.zero;
            var t=lgo.AddComponent<TextMeshProUGUI>();
            t.text=lbl; t.fontSize=11; t.color=tc;
            t.alignment=TextAlignmentOptions.Center; t.fontStyle=FontStyles.Bold;
            return btn;
        }

        void Quad(string n,float cx,float cy,float w,float h,Color col,int order)
        {
            var go=new GameObject(n); go.transform.SetParent(transform,false);
            go.transform.position=new Vector3(cx,cy,0);
            var sr=go.AddComponent<SpriteRenderer>(); sr.sortingOrder=order;
            int pw=Mathf.Max(1,Mathf.RoundToInt(w*32)),ph=Mathf.Max(1,Mathf.RoundToInt(h*32));
            var tex=new Texture2D(pw,ph,TextureFormat.RGBA32,false); tex.filterMode=FilterMode.Point;
            var pix=new Color[pw*ph]; for(int i=0;i<pix.Length;i++) pix[i]=Color.white;
            tex.SetPixels(pix); tex.Apply();
            sr.sprite=Sprite.Create(tex,new Rect(0,0,pw,ph),new Vector2(0.5f,0f),32f); sr.color=col;
        }

        void Tile(Texture2D tex,int gx,int gy,int col,int row,int tc,int tr,int order)
        {
            int tw=tex.width/tc,th=tex.height/tr;
            var go=new GameObject("T"); go.transform.SetParent(transform,false);
            go.transform.position=new Vector3(gx,gy,0);
            var sr=go.AddComponent<SpriteRenderer>(); sr.sortingOrder=order;
            sr.sprite=Sprite.Create(tex,new Rect(col*tw,tex.height-(row+1)*th,tw,th),new Vector2(0.5f,0f),32f);
        }
    }

    public class TorchFlick : MonoBehaviour
    {
        SpriteRenderer _sr; float _t,_iv;
        void Start(){_sr=GetComponent<SpriteRenderer>();_iv=Random.Range(0.04f,0.10f);}
        void Update()
        {
            _t+=Time.deltaTime; if(_t<_iv)return;
            _t=0; _iv=Random.Range(0.04f,0.10f);
            if(_sr)_sr.color=new Color(1f,Random.Range(0.6f,1f),Random.Range(0.05f,0.35f));
        }
    }
}