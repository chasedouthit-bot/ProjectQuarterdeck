using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SaltySeaDogs
{
    public class Cannon : MonoBehaviour {
        public ShipSide GunLocation;
        public GunStatus Status;
        private ParticleSystem Particle; //instantiated particle for this cannon only
        public ParticleSystem ParticlePrefab;
        public float Cooldown = 10f;
        public float TimeUntilReady = 0f;

        public void FireGun()
        {
            if (Status == GunStatus.Ready || (Status == GunStatus.Reloading && TimeUntilReady == 0))
            {
                StartCoroutine(FireGun(Cooldown));
            }
  
        }

        IEnumerator FireGun(float _cooldown)
        {
            Particle.Play();
            TimeUntilReady = Cooldown;
            Status = GunStatus.Reloading;
            while (TimeUntilReady > 0f)
            {
                TimeUntilReady -= Time.deltaTime;
                yield return null;
            }
            Status = GunStatus.Ready;
        }

        private void Start()
        {
            Particle = Instantiate(ParticlePrefab, transform);
            Particle.transform.position = transform.position;
            Particle.transform.rotation = transform.rotation;
        }
    }
}

