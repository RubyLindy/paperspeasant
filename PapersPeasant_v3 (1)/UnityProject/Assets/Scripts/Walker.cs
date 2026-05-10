using System.Collections;
using UnityEngine;

namespace PP
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class Walker : MonoBehaviour
    {
        SpriteRenderer _sr;
        Sprite[] _frames;

        void Awake() => _sr = GetComponent<SpriteRenderer>();

        public void Init(Sprite[] frames)
        {
            _frames = frames;
            if (_frames != null && _frames.Length > 0) _sr.sprite = _frames[0];
        }

        public void WalkTo(float tx, System.Action done = null)   => StartCoroutine(Move(tx, false, done));
        public void WalkOff(float tx, System.Action done = null)  => StartCoroutine(Move(tx, tx < 0, done));

        public void Arrest(System.Action done = null) => StartCoroutine(ArrestSeq(done));

        public void Idle() => StartCoroutine(IdleLoop());

        IEnumerator Move(float tx, bool flipX, System.Action done)
        {
            StopCoroutine("IdleLoop");
            _sr.flipX = flipX;
            int frame = 1;
            while (Mathf.Abs(transform.position.x - tx) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position,
                    new Vector3(tx, transform.position.y, 0), 2.5f * Time.deltaTime);
                if (_frames != null && _frames.Length > 1)
                    _sr.sprite = _frames[(frame / 3) % (_frames.Length - 1) + 1];
                frame++;
                yield return null;
            }
            _sr.flipX = false;
            done?.Invoke();
        }

        IEnumerator IdleLoop()
        {
            float by = transform.position.y;
            bool up = false;
            while (true)
            {
                if (_frames != null) _sr.sprite = _frames[0];
                var p = transform.position; p.y = by + (up ? 0.04f : 0f);
                transform.position = p; up = !up;
                yield return new WaitForSeconds(0.4f);
            }
        }

        IEnumerator ArrestSeq(System.Action done)
        {
            var orig = transform.position;
            for (int i = 0; i < 8; i++)
            {
                transform.position = orig + new Vector3(Random.Range(-0.09f, 0.09f), 0, 0);
                yield return new WaitForSeconds(0.05f);
            }
            transform.position = orig;
            float t = 0;
            while (t < 0.6f)
            {
                t += Time.deltaTime;
                _sr.color = new Color(1, 1, 1, 1 - t / 0.6f);
                yield return null;
            }
            _sr.color = Color.white;
            gameObject.SetActive(false);
            done?.Invoke();
        }
    }
}
