using UnityEngine;

namespace Markyu.LastKernel
{
    [RequireComponent(typeof(ParticleSystem))]
    public class PuffParticle : MonoBehaviour
    {
        private void Awake()
        {
            var main = GetComponent<ParticleSystem>().main;
            AudioManager.Instance?.PlaySFX(AudioId.Puff);
            Destroy(gameObject, main.duration + 0.1f);
        }
    }
}

