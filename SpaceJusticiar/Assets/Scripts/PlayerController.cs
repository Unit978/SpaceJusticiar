﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerController : MonoBehaviour
{

    private Rigidbody2D _rigidBody;
    private SpriteRenderer _spriteRenderer;

    public float acceleration = 1f;
    public float maxVelocity = 10f;

    // How much to wait in seconds in order to boost.
    private float _boostInterval = 2f;
    private float _boostTimer = 0f;
    private float _boostScalar = 40f;

    private float _colorTimer = 0;

    // How often to change the player color in seconds.
    public float colorChangeRate = 0.5f;

    private float _minColor = 0.5f;
    private float _maxColor = 1f;

    public GameObject planet;
    public float gravityScale = 1f;

    public Transform camTransform = null;
    private Vector3 _prevPlayerPos;

    private ParticleSystem _thrustParticles;

    private Vector3 _prevThrustDir = new Vector3();

    public enum FrameOfReference { GLOBAL, PLANET };
    public FrameOfReference currentFrameOfRef = FrameOfReference.PLANET;

    private float _health = 1f;
    private float _healthRegenRate = 0.04f;

    private EnergyCell _energyCell;

    private float _energySlowTimeDrainRate = 0.5f;
    private bool _bInSlowMotion = false;

    public Text healthText = null;
    public Text energyText = null;

    public float Energy
    {
        get { return _energyCell.Charge; }
    }

    public EnergyCell EnergyCell
    {
        get { return _energyCell; }
    }

    // Use this for initialization
    void Start()
    {
        _energyCell = new EnergyCell();

        _rigidBody = gameObject.GetComponent<Rigidbody2D>();
        _spriteRenderer = gameObject.GetComponent<SpriteRenderer>();

        Vector2 newPos = transform.position;
        newPos.y = planet.transform.position.y + planet.transform.localScale.x * planet.GetComponent<CircleCollider2D>().radius + 1;
        transform.position = newPos;

        _thrustParticles = GameObject.Find("Player/Thrust").GetComponent<ParticleSystem>();

        _prevPlayerPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {

        Vector2 thrustForce = thrust();

        // Cap velocity if accelerating.
        if (thrustForce.sqrMagnitude != 0 && _rigidBody.velocity.SqrMagnitude() > maxVelocity * maxVelocity) {
            _rigidBody.velocity = Vector2.ClampMagnitude(_rigidBody.velocity, maxVelocity);
        }

        _rigidBody.AddForce(-up() * gravityScale);

        if (Input.GetKeyDown(KeyCode.Space) && Energy > 0) {
            Time.timeScale = 0.5f;
            _bInSlowMotion = true;
        }
        else if (Input.GetKeyUp(KeyCode.Space)) {
            Time.timeScale = 1f;
            _bInSlowMotion = false;
        }

        if (_bInSlowMotion && Energy > 0) {
            _energyCell.UseEnergy(_energySlowTimeDrainRate * Time.deltaTime);
        }

        // Set to normal time scale if we ran out of energy.
        // Set other values when energy runs out too.
        if (Energy == 0) {
            _bInSlowMotion = false;
            Time.timeScale = 1f;
            energyText.text = "0";
        }

        _energyCell.Update();

        // Energy regen.
        if (Energy < EnergyCell.MAX_ENERGY) {
            energyText.text = _energyCell.GetPercentage().ToString();
        }

        // Health regen.
        if (_health < 1) {
            _health += _healthRegenRate * Time.deltaTime;
            if (_health > 1) {
                _health = 1;
            }

            int healthPercent = (int)(_health * 100);
            healthText.text = healthPercent.ToString();
        }

        if (_health < 0) {
            _health = 0;
        }

    }

    void LateUpdate()
    {
        // Make the camera align to the planet's tangent.
        if (_prevPlayerPos != transform.position) {

            Vector2 upVector = up();
            float z = Mathf.Acos(upVector.y) * Mathf.Rad2Deg;

            if (upVector.x > 0)
                z *= -1;

            camTransform.eulerAngles = new Vector3(0, 0, z);
            _prevPlayerPos = transform.position;
        }
    }

    void FixedUpdate()
    {
        setColor();
    }


    Vector3 thrust()
    {
        Vector2 upDir = new Vector2(0, 0);
        Vector2 rightDir = new Vector2(0, 0);

        // Up thrust.
        if (Input.GetKey("w")) {
            upDir = up();
        }

        // Down thrust.
        else if (Input.GetKey("s")) {
            upDir = -up();
        }

        // Left thrust.
        if (Input.GetKey("a")) {
            rightDir = -right();
        }

        // Right thrust.
        else if (Input.GetKey("d")) {
            rightDir = right();
        }

        float accel = boost();

        Vector2 thrustDir = (upDir + rightDir).normalized;
        Vector2 thrust = thrustDir * accel;

        generateThrustParticles(thrust, thrustDir);
        _rigidBody.AddForce(thrust);

        return thrust;
    }

    // Boost the acceleration if ready.
    private float boost()
    {
        // Boost if ready.
        if (_boostTimer >= _boostInterval && Input.GetKeyDown(KeyCode.LeftShift)) {
            _boostTimer = 0f;
            return acceleration * _boostScalar;
        }

        // Tick boost timer.
        if (_boostTimer < _boostInterval) {
            _boostTimer += Time.deltaTime;
        }

        return acceleration;
    }

    void generateThrustParticles(Vector3 thrust, Vector3 thrustDir)
    {
        // Play the thrust particles.
        if (thrust.sqrMagnitude > 0f) {
            if (_thrustParticles.isStopped)
                _thrustParticles.Play();

            if (_prevThrustDir != thrustDir) {

                // Set the direction of the particles opposite to the player's motion.
                //_thrustParticles.gameObject.transform.LookAt(transform.position - thrustDir);

                float rotationZ = Mathf.Atan2(thrustDir.y, thrustDir.x) * Mathf.Rad2Deg;
                _thrustParticles.transform.rotation = Quaternion.Euler(0, 0, rotationZ);
                _prevThrustDir = thrustDir;
            }
        }
        else {

            //_thrustParticles.gameObject.transform.LookAt(transform.position + Vector3.forward);

            if (_thrustParticles.isPlaying)
                _thrustParticles.Stop();
        }
    }

    // Calculate the up vector relative to the planet's surface.
    public Vector2 up()
    {
        if (currentFrameOfRef == FrameOfReference.PLANET)
            return (transform.position - planet.transform.position).normalized;

        // Global up
        return Vector2.up;
    }

    // Calculate the right vector relative to the planet's surface.
    public Vector2 right()
    {
        Vector2 upDir = up();
        return new Vector2(upDir.y, -upDir.x);
    }

    void setColor()
    {
        // Change color based on the timer.
        _colorTimer += Time.deltaTime;
        if (_colorTimer >= colorChangeRate) {
            _colorTimer = 0;

            // Generate a random color.
            float r = Random.Range(_minColor, _maxColor);
            float g = Random.Range(_minColor, _maxColor);
            float b = Random.Range(_minColor, _maxColor);
            Color c = new Color(r, g, b);

            _spriteRenderer.material.color = c;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Projectile") {

            // Projectile is meant to hit enemy
            if (other.gameObject.GetComponent<ProjectileBehavior>().targetTag == gameObject.tag) {

                _health -= .2f;

                if (_health <= 0)
                    Destroy(gameObject, 0.1f);
            }
        }
    }
}
