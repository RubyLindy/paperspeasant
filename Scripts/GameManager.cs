using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PP
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public enum State { Title, Intro, WaitBell, Approaching, Checking, Dismissing, End }
        public State Current { get; private set; } = State.Title;

        public int Coins    { get; private set; } = 20;
        public int Mistakes { get; private set; } = 0;
        public int Approved { get; private set; } = 0;
        public int Arrested { get; private set; } = 0;
        public int Day      { get; private set; } = 1;

        public Visitor CurrentVisitor { get; private set; }

        // Events
        public event System.Action<State>       OnState;
        public event System.Action<DialogueLine> OnLine;
        public event System.Action<Visitor>      OnVisitorArrived;
        public event System.Action<Action, bool> OnDecision;
        public event System.Action               OnDayEnd;

        List<Visitor>     _queue;
        List<DialogueLine> _intro;
        int _qi;
        bool _waitAdv;

        void Awake()
        {
            if (I != null) { Destroy(gameObject); return; }
            I = this;
            BuildData();
        }

        void BuildData()
        {
            _intro = new List<DialogueLine>
            {
                new DialogueLine("HEAD GUARD", "Welcome to the gatepost. First day, right? Don't worry — simple job."),
                new DialogueLine("HEAD GUARD", "Everyone needs a Letter of Passage. Only Emmeloord seals are valid. Forgeries are rampant."),
                new DialogueLine("HEAD GUARD", "Documents check out — ring the bell, let them in. Something's wrong — call me."),
            };

            _queue = new List<Visitor>
            {
                new Visitor {
                    name = "Willem of Stonebridge", type = ActorType.Farmer, correct = Correct.Accept,
                    greeting    = "Good morning. Here are my papers. I seek work inside the walls.",
                    doc = new Document { issuedBy = "Kingdom of Emmeloord", bearer = "Willem of Stonebridge",
                        purpose = "Seeking employment", validUntil = "Spring's End, 1042", seal = SealType.Valid }
                },
                new Visitor {
                    name = "Garrett Sallow", type = ActorType.ShadyGuy, correct = Correct.CallGuard,
                    greeting    = "Aye... papers, yes. Here. Just passing through.",
                    arrestLine  = "Get your hands off me! I have rights!",
                    doc = new Document { issuedBy = "Princedom of Vorn", bearer = "Garrett Sallow",
                        purpose = "Trade and commerce", validUntil = "Midsummer, 1042", seal = SealType.WrongKingdom }
                },
                new Visitor {
                    name = "Marta the Elder", type = ActorType.Villager, correct = Correct.Accept,
                    greeting    = "These old bones have walked far. My passage, as required.",
                    doc = new Document { issuedBy = "Kingdom of Emmeloord", bearer = "Marta of Millfield",
                        purpose = "Family visit", validUntil = "Autumn, 1042", seal = SealType.Valid }
                },
                new Visitor {
                    name = "Rodrik the Merchant", type = ActorType.Merchant, correct = Correct.Deny,
                    greeting    = "Fine goods from the east! Surely a merchant warrants swift entry?",
                    doc = new Document { issuedBy = "Kingdom of Emmeloord", bearer = "Rodrik of Ashford",
                        purpose = "Trade", validUntil = "Winter's End, 1041", seal = SealType.Expired }
                },
            };
        }

        // ── Public API ──────────────────────────────────────────────────

        public void StartGame()
        {
            Coins = 20; Mistakes = 0; Approved = 0; Arrested = 0;
            _qi = 0; 
            SetState(State.Intro);
            StartCoroutine(RunIntro());
        }

        public void Advance()
        {
            if (_waitAdv) _waitAdv = false;
        }

        public void RingBell()
        {
            if (Current != State.WaitBell) return;
            if (_qi >= _queue.Count) { StartCoroutine(EndDay()); return; }
            StartCoroutine(RunVisitor());
        }

        public void MakeDecision(Action a)
        {
            if (Current != State.Checking) return;
            bool ok = Evaluate(a);
            if (ok) { if (a == Action.Accept) { Coins += 5; Approved++; } else { Coins += 8; Arrested++; } }
            else    { Coins = Mathf.Max(0, Coins - 10); Mistakes++; }
            OnDecision?.Invoke(a, ok);
            SetState(State.Dismissing);
            StartCoroutine(RunDismiss(a, ok));
        }

        // ── Coroutines ──────────────────────────────────────────────────

        IEnumerator RunIntro()
        {
            yield return new WaitForSeconds(0.3f);
            foreach (var line in _intro)
            {
                Emit(line);
                _waitAdv = true;
                yield return new WaitUntil(() => !_waitAdv);
                yield return new WaitForSeconds(0.1f);
            }
            SetState(State.WaitBell);
            Emit(new DialogueLine("HEAD GUARD", "Ring the bell when you are ready."));
        }

        IEnumerator RunVisitor()
        {
            CurrentVisitor = _queue[_qi++];
            SetState(State.Approaching);
            OnVisitorArrived?.Invoke(CurrentVisitor);
            yield return new WaitForSeconds(1.2f);
            Emit(new DialogueLine("YOU", "\"Papers, Peasant!\""));
            yield return new WaitForSeconds(0.6f);
            Emit(new DialogueLine(CurrentVisitor.name.ToUpper(), "\"" + CurrentVisitor.greeting + "\""));
            yield return new WaitForSeconds(0.8f);
            SetState(State.Checking);
        }

        IEnumerator RunDismiss(Action a, bool ok)
        {
            if (ok)
            {
                if (a == Action.Accept)
                    Emit(new DialogueLine("HEAD GUARD", "Bell rings. Passage granted. +5 coins."));
                else if (a == Action.Deny)
                    Emit(new DialogueLine("YOU", "\"Your papers are not in order. Move along.\""));
                else
                {
                    Emit(new DialogueLine(CurrentVisitor.name.ToUpper(),
                        CurrentVisitor.arrestLine ?? "\"Unhand me!\""));
                    yield return new WaitForSeconds(1.2f);
                    Emit(new DialogueLine("HEAD GUARD", "Good eye. That seal is invalid. +8 coins."));
                }
            }
            else
            {
                if (a == Action.Accept)
                    Emit(new DialogueLine("HEAD GUARD", "That seal was invalid! You let them through. -10 coins."));
                else if (a == Action.Deny)
                    Emit(new DialogueLine("HEAD GUARD", "That was a valid pass! You turned them away. -10 coins."));
                else
                    Emit(new DialogueLine("HEAD GUARD", "That person had valid papers! -10 coins."));
            }

            yield return new WaitForSeconds(1.8f);

            if (_qi < _queue.Count)
            {
                SetState(State.WaitBell);
                Emit(new DialogueLine("HEAD GUARD", "Next one is waiting. Ring the bell."));
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
                StartCoroutine(EndDay());
            }
        }

        IEnumerator EndDay()
        {
            yield return new WaitForSeconds(0.8f);
            SetState(State.End);
            OnDayEnd?.Invoke();
        }

        // ── Helpers ─────────────────────────────────────────────────────

        bool Evaluate(Action a)
        {
            if (CurrentVisitor.correct == Correct.Accept    && a == Action.Accept)    return true;
            if (CurrentVisitor.correct == Correct.Deny      && a == Action.Deny)      return true;
            if (CurrentVisitor.correct == Correct.CallGuard && a == Action.CallGuard) return true;
            return false;
        }

        void SetState(State s) { Current = s; OnState?.Invoke(s); }
        void Emit(DialogueLine l) => OnLine?.Invoke(l);
    }
}
