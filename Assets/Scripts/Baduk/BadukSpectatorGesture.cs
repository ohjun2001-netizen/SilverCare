using UnityEngine;

namespace Baduk
{
    public class BadukSpectatorGesture : MonoBehaviour
    {
        Transform _leftArm;
        Transform _rightArm;
        Transform _head;
        Quaternion _leftBase;
        Quaternion _rightBase;
        Quaternion _headBase;
        float _phase;
        float _amplitude;

        public void Initialize(float seed)
        {
            _phase = seed;
            _amplitude = Random.Range(0.35f, 0.75f);
            _leftArm = FindDeep(transform, "SpectatorArmL") ?? FindDeep(transform, "arm_l") ?? FindDeep(transform, "left_arm");
            _rightArm = FindDeep(transform, "SpectatorArmR") ?? FindDeep(transform, "arm_r") ?? FindDeep(transform, "right_arm");
            _head = FindDeep(transform, "SpectatorHead") ?? FindDeep(transform, "human_head_mesh") ?? FindDeep(transform, "skin_head");

            if (_leftArm != null) _leftBase = _leftArm.localRotation;
            if (_rightArm != null) _rightBase = _rightArm.localRotation;
            if (_head != null) _headBase = _head.localRotation;
        }

        void Update()
        {
            _phase += Time.deltaTime;
            float talk = Mathf.Sin(_phase * 1.1f) * _amplitude;
            float nod = Mathf.Sin(_phase * 0.7f) * 2.2f;

            if (_leftArm != null)
                _leftArm.localRotation = _leftBase * Quaternion.Euler(0f, 0f, talk * 6f);
            if (_rightArm != null)
                _rightArm.localRotation = _rightBase * Quaternion.Euler(0f, 0f, -talk * 5f);
            if (_head != null)
                _head.localRotation = _headBase * Quaternion.Euler(nod, talk * 3f, 0f);
        }

        static Transform FindDeep(Transform root, string name)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                if (string.Equals(t.name, name, System.StringComparison.OrdinalIgnoreCase))
                    return t;
            }

            return null;
        }
    }
}
