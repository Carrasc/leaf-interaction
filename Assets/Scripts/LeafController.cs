using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeafController : MonoBehaviour
{
    [Header("Physics simulation variables")]
    [SerializeField] private float gravityMultiplier = -9.81f;
    [SerializeField] private float airResistance = 0.1f; // Tune this value

    [Header("Leaf simulation variables")]
    [SerializeField] private float swayAmplitude = 1f;   // how wide it sways
    [SerializeField] private float swayFrequency = 2f;     // how fast it sways
    [SerializeField] private float flutterAngleAmplitude = 10f;  // degrees
    [SerializeField] private float flutterSpeed = 3f;

    [Header("Interaction variables")]
    [SerializeField] private float dragFollowSpeed = 10f; // Try 5–15 for soft drag
    [SerializeField] private float grabRadius = 0.5f; // adjust based on leaf size

    [Header("Audio")]
    [SerializeField] private AudioSource leafAudioSource;
    [SerializeField] private AudioClip groundHitSFX;

    private Vector2 _velocity;
    private bool _isPlayerInteracting = false;
    private Vector3 _throwVelocity;
    private float _swayTime;
    private bool _wasGroundedLastFrame = false;

    // Update is called once per frame
    private void Update()
    {
        HandleInput();
        SimulateGravity();

        // Smoothly restore to default scale every frame
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, 10f * Time.deltaTime);     
    }

    // To avoid using colliders, in case that count as physics, I used this approach
    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0;

            // Player need to click on the actual leaf, to drag it
            _isPlayerInteracting = IsPointerOverLeaf(mouseWorld);
        }

        if (_isPlayerInteracting)
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0;

            // Store position before moving
            Vector3 prevPosition = transform.position;

            // Move leaf to mouse position, with Lerp so it has a delayed "floaty" effect
            transform.position = Vector3.Lerp(transform.position, mouseWorld, dragFollowSpeed * Time.deltaTime);

            // Estimate throw velocity based on how much the leaf moved
            _throwVelocity = (transform.position - prevPosition) / Time.deltaTime;

            if (Input.GetMouseButtonUp(0))
            {
                _isPlayerInteracting = false;

                // Apply throw velocity
                _velocity = _throwVelocity;
            }

            // Direction vector from leaf to cursor
            Vector2 dragDirection = mouseWorld - transform.position;

            if (dragDirection.sqrMagnitude > 0.001f) // avoid NaN on very small movements
            {
                float angle = Mathf.Atan2(dragDirection.y, dragDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, 0f, angle), dragFollowSpeed * Time.deltaTime);
            }
        }
    }

    private bool IsPointerOverLeaf(Vector3 pointer)
    {
        float dist = Vector2.Distance(transform.position, pointer);
        return dist < grabRadius;
    }

    /// <summary>
    /// Applies force in the y-axis to simulate gravity, if the leaf is not being interacted with.
    /// </summary>
    private void SimulateGravity()
    {
        if (_isPlayerInteracting || IsGrounded())
        {
            _velocity = Vector2.zero;
            return;
        }

        // Normal gravity force, y axis
        _velocity.y += gravityMultiplier * Time.deltaTime;

        // Apply air resistance (drag) to both axes (for the the player throws the leaf)
        _velocity *= 1f - airResistance * Time.deltaTime;

        // Move the leaf
        transform.position += (Vector3)_velocity * Time.deltaTime;

        SimulateAirSway();

        // Add a small bounce when touching the ground
        bool isGrounded = IsGrounded();
        if (isGrounded && !_wasGroundedLastFrame)
            BounceOnGroundTouch();

        _wasGroundedLastFrame = isGrounded;
    }

    /// <summary>
    /// Apply movement on the X axis, as if the leaf was moving because of the wind.
    /// Also a simple rotation.
    /// </summary>
    private void SimulateAirSway()
    {
        _swayTime += Time.deltaTime;

        float swayOffset = Mathf.Sin(_swayTime * swayFrequency) * swayAmplitude;
        Vector3 sway = new Vector3(swayOffset, 0f, 0f);

        transform.position += sway * Time.deltaTime;

        // Small rotation as well, to seem more realistic
        float flutterAngle = Mathf.Sin(_swayTime * flutterSpeed) * flutterAngleAmplitude;
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, 0f, flutterAngle), 2f * Time.deltaTime);
    }

    /// <summary>
    /// Small bounce effect when falling on the ground, at certain speedd.
    /// </summary>
    private void BounceOnGroundTouch()
    {
        if (Mathf.Abs(_velocity.y) > 0.5f)
        {
            // Trigger squash
            transform.localScale = new Vector3(1.15f, 0.85f, 1f);

            // Very simple audio clip with random pitch, when hitting the ground
            leafAudioSource.pitch = Random.Range(0.85f, 1.25f);
            leafAudioSource.PlayOneShot(groundHitSFX);
        }
    }

    /// <summary>
    /// This method returns true if the leaf is "on the ground". Since I want to avoid using Triggers or colliders
    /// (could count as physics) it will be based with a fixed ground distance. This will work perfect for this example.
    /// If we were to have un-even terrain, other solution might be better.
    /// </summary>
    private bool IsGrounded()
    {
        return transform.position.y <= 0f;
    }
}
