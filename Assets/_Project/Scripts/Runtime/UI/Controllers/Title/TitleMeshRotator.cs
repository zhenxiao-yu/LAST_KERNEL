using UnityEngine;

namespace Markyu.LastKernel
{
    public sealed class TitleMeshRotator : MonoBehaviour
    {
        [SerializeField] private float _yawSpeed   = 28f;
        [SerializeField] private float _tiltSpeed  =  8f;
        [SerializeField] private float _tiltAmount = 12f;

        private float _time;

        private void Update()
        {
            _time += Time.deltaTime;
            float tilt = Mathf.Sin(_time * _tiltSpeed * Mathf.Deg2Rad) * _tiltAmount;
            transform.rotation = Quaternion.Euler(tilt, _time * _yawSpeed, 0f);
        }
    }
}
