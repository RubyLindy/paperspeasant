using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace PP
{
    /// <summary>
    /// The flip-able side panel on the left of the lower area.
    /// Page 0 = Rulebook (current rules)
    /// Page 1+ = Wanted posters
    /// Flip arrow buttons cycle between pages.
    /// </summary>
    public class SidePanel : MonoBehaviour
    {
        // Built at runtime by Game.cs via Setup()
        static GameObject  _panel;
        static Image       _pageImage;
        static TextMeshProUGUI _pageTitle;
        static TextMeshProUGUI _pageBody;
        static Button      _prevBtn, _nextBtn;
        static int         _currentPage = 0;

        // Pages: each is (title, body, sprite?)
        static readonly string[] Titles = new string[]
        {
            "RULEBOOK",
            "WANTED",
            "WANTED",
            "WANTED",
        };

        static readonly string[] Bodies = new string[]
        {
            "Day 1\n\nAll visitors must present a valid Letter of Passage.\n\nOnly the seal of the Kingdom of Emmeloord is accepted.\n\nExpired documents must be denied.",
            "GARRETT SALLOW\nAlias: 'The Fox'\n\nWanted for forgery and impersonation.\nLast seen near the eastern road.\n\nDo NOT allow entry. Call the guard.",
            "MIRA VOSS\nAlias: Unknown\n\nWanted for theft of royal goods.\nMedium height, dark cloak.\n\nDetain on sight.",
            "DORN ASHWICK\nKnown smuggler.\nMay carry false Emmeloord seals.\n\nInspect all documents carefully.",
        };

        static readonly Color[] PageColors = new Color[]
        {
            new Color(0.94f, 0.88f, 0.72f),  // rulebook: warm parchment
            new Color(0.92f, 0.82f, 0.72f),  // wanted: slightly reddish parchment
            new Color(0.92f, 0.82f, 0.72f),
            new Color(0.92f, 0.82f, 0.72f),
        };

        public static void Setup(GameObject panel, Image pageImage,
            TextMeshProUGUI title, TextMeshProUGUI body,
            Button prev, Button next)
        {
            _panel = panel; _pageImage = pageImage;
            _pageTitle = title; _pageBody = body;
            _prevBtn = prev; _nextBtn = next;
        }

        void Start()
        {
            if (_prevBtn) _prevBtn.onClick.AddListener(PrevPage);
            if (_nextBtn) _nextBtn.onClick.AddListener(NextPage);
            ShowPage(0);
        }

        static void ShowPage(int idx)
        {
            _currentPage = Mathf.Clamp(idx, 0, Titles.Length - 1);
            if (_pageTitle) _pageTitle.text = Titles[_currentPage];
            if (_pageBody)  _pageBody.text  = Bodies[_currentPage];
            if (_panel)     _panel.GetComponent<Image>().color = PageColors[_currentPage];
            // Update arrows
            if (_prevBtn) _prevBtn.interactable = _currentPage > 0;
            if (_nextBtn) _nextBtn.interactable = _currentPage < Titles.Length - 1;
            // Fade arrows that are at limits
            SetBtnAlpha(_prevBtn, _currentPage > 0 ? 1f : 0.3f);
            SetBtnAlpha(_nextBtn, _currentPage < Titles.Length-1 ? 1f : 0.3f);
        }

        static void PrevPage() => ShowPage(_currentPage - 1);
        static void NextPage() => ShowPage(_currentPage + 1);

        static void SetBtnAlpha(Button b, float a)
        {
            if (!b) return;
            var img = b.GetComponent<Image>();
            if (img) { var c = img.color; c.a = a; img.color = c; }
        }
    }
}
