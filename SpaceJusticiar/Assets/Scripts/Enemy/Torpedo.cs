﻿using UnityEngine;
using System.Collections;

public class Torpedo : MonoBehaviour {

    [SerializeField]
    private ObjectController _oc;

	// Use this for initialization
    void Start()
    {
        Vector2 down = -CelestialBody.GetUp(_oc.PlanetTarget, transform);
        GetComponent<Rigidbody2D>().velocity = down * 7f;

        float angle = Mathf.Atan2(down.y, down.x) * Mathf.Rad2Deg;
        transform.Rotate(0, 0, angle);

        _oc.Health.ReInit(0, 0.1f);
    }
	
	// Update is called once per frame
	void Update () {

	}

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Collidable") {

            GameObject explosion = Pools.Instance.Fetch("GreenEnergyExplosion");
            explosion.transform.position = transform.position;

            ParticleSystem effect = explosion.GetComponent<ParticleSystem>();
            effect.Play();

            Destroy(gameObject, 0.01f);
        }
    }
}
