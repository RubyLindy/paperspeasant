using System.Collections;
using UnityEngine;

namespace PP
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class Walker : MonoBehaviour
    {
        SpriteRenderer _sr;
        Sprite[] _frames;

        const float WalkSpeed = 3.5f;
        const float WalkFPS   = 0.10f;

        void Awake() => _sr = GetComponent<SpriteRenderer>();

        public void Init(Sprite[] frames)
        {
            _frames = frames;
            if (_frames != null && _frames.Length > 0) _sr.sprite = _frames[0];
            // Flip sprite so NPC faces RIGHT (toward the gate) when walking left→right
            _sr.flipX = true;
        }

        public void WalkTo(float tx, System.Action done = null)
        {
            StopAllCoroutines();
            StartCoroutine(MoveRoutine(tx, true, done));   // face right toward gate
        }

        public void WalkOff(float tx, System.Action done = null)
        {
            StopAllCoroutines();
            bool goingLeft = tx < transform.position.x;
            StartCoroutine(MoveRoutine(tx, !goingLeft, done)); // face direction of travel
        }

        public void WalkThroughGate(float gateX, float offscreenX, System.Action done = null)
        {
            StopAllCoroutines();
            StartCoroutine(ThroughGateRoutine(gateX, done));
        }

        public void Idle()
        {
            StopAllCoroutines();
            if (_frames != null && _frames.Length > 0) _sr.sprite = _frames[0];
        }

        public void Arrest(System.Action done = null)
        {
            StopAllCoroutines();
            StartCoroutine(ArrestRoutine(done));
        }

        IEnumerator MoveRoutine(float targetX, bool faceRight, System.Action done)
        {
            _sr.flipX = faceRight;
            float lockedY   = transform.position.y;
            float animTimer = 0f;
            int   frameIdx  = 1;

            while (Mathf.Abs(transform.position.x - targetX) > 0.04f)
            {
                float newX = Mathf.MoveTowards(transform.position.x, targetX, WalkSpeed * Time.deltaTime);
                transform.position = new Vector3(newX, lockedY, 0f);

                if (_frames != null && _frames.Length > 1)
                {
                    animTimer += Time.deltaTime;
                    if (animTimer >= WalkFPS)
                    {
                        animTimer -= WalkFPS;
                        frameIdx++;
                        if (frameIdx >= _frames.Length) frameIdx = 1;
                        _sr.sprite = _frames[frameIdx];
                    }
                }
                yield return null;
            }

            transform.position = new Vector3(targetX, lockedY, 0f);
            if (_frames != null && _frames.Length > 0) _sr.sprite = _frames[0];
            done?.Invoke();
        }

        IEnumerator ThroughGateRoutine(float gateX, System.Action done)
        {
            float lockedY   = transform.position.y;
            float animTimer = 0f;
            int   frameIdx  = 1;
            Vector3 origScale = transform.localScale;
            _sr.flipX = true; // face gate

            while (Mathf.Abs(transform.position.x - gateX) > 0.05f)
            {
                float newX = Mathf.MoveTowards(transform.position.x, gateX, WalkSpeed * Time.deltaTime);
                transform.position = new Vector3(newX, lockedY, 0f);
                if (_frames != null && _frames.Length > 1)
                {
                    animTimer += Time.deltaTime;
                    if (animTimer >= WalkFPS)
                    { animTimer -= WalkFPS; frameIdx++; if(frameIdx>=_frames.Length)frameIdx=1; _sr.sprite=_frames[frameIdx]; }
                }
                yield return null;
            }

            // Shrink + fade into the gate
            float t = 0f;
            while (t < 0.7f)
            {
                t += Time.deltaTime * 1.5f;
                transform.localScale = origScale * Mathf.Lerp(1f, 0.05f, t);
                _sr.color = new Color(1, 1, 1, Mathf.Lerp(1f, 0f, t));
                yield return null;
            }
            _sr.color = Color.white;
            transform.localScale = origScale;
            gameObject.SetActive(false);
            done?.Invoke();
        }

        IEnumerator ArrestRoutine(System.Action done)
        {
            float lockedY  = transform.position.y;
            Vector3 origin = transform.position;
            for (int i = 0; i < 8; i++)
            {
                transform.position = new Vector3(origin.x + Random.Range(-0.12f, 0.12f), lockedY, 0f);
                yield return new WaitForSeconds(0.05f);
            }
            transform.position = new Vector3(origin.x, lockedY, 0f);
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime * 2f;
                _sr.color = new Color(1, 1, 1, Mathf.Clamp01(1f - t));
                yield return null;
            }
            _sr.color = Color.white;
            gameObject.SetActive(false);
            done?.Invoke();
        }
    }
}
