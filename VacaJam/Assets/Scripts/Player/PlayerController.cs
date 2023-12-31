﻿using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

[RequireComponent(typeof(Rigidbody2D), typeof(CollisionDetection))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController : MonoBehaviour, IDamageable, IShooteable
{
    [Header("References")]
    [SerializeField] private InputManager _input;
    [SerializeField] private Weapon _weapon;
    [SerializeField] private PlayerGraphics _playerGraphics;
    [SerializeField] private ParticleSystem _deathParticles;

    [Header("Stats")]
    public float Health;

    [Header("Config")]
    [Range(0.1f, 10f)]
    [SerializeField] private float _maxWeaponRotationDistance = 1f;

    [Header("Movement")]
    [SerializeField] private float _jumpForce;
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _friction;

    [Header("Jump")]
    [SerializeField] private float _fallMultiplier;
    [SerializeField] private float _lowJumpMultiplier;
    [SerializeField] private float _jumpBufferLength = .1f;

    [SerializeField] private int _hangTime = 8;

    [Range(.1f, 10f)]
    [SerializeField] private float _attackLine;

    [Header("Debug")]
    [SerializeField] private bool _shouldDie;

    [HideInInspector] public float CurrentHealth;

    [HideInInspector] public bool IsEnabled = true;

    public Rigidbody2D Rigidbody => _rigidbody;
    public InputManager Input => _input;

    private Rigidbody2D _rigidbody;
    private CollisionDetection _collision;
    private Collider2D _collider;

    private Vector3 _spawnPosition;

    private float _jumpBufferCount;
    private float _lookAngle;

    private int _canJump = 0;

    private bool _isJumpDisabled = false;
    private bool _canMove = true;
    private bool _isGroundedPrev = false;
    private bool _isBetterJumpDisabled = false;

    private void Awake()
    {
        _collision = GetComponent<CollisionDetection>();
        _rigidbody = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        _spawnPosition = transform.position;

        CurrentHealth = Health;
    }

    private void Update()
    {
        if (!IsEnabled || PauseMenu.isGamePaused)
            return;

        BetterJump();

        _lookAngle = LookDir.GetDir(transform.position);

        _canJump--;

        _playerGraphics.Side = _lookAngle < 90 && _lookAngle > -90 ? 1 : -1;

        _playerGraphics.FlipObject();

        if (Rigidbody.velocity.y >= 24f) {
            Rigidbody.velocity = new Vector2(Rigidbody.velocity.x, 24f);
        }

        if (_collision.IsGrounded && !_isGroundedPrev) {
            _playerGraphics.SetHeightTrigger("Squash");
            _playerGraphics.PlayerJumpParticle();
            AudioManager._I.PlaySound2D("Player-Land", 1.4f, 120);
        }

        if (_collision.IsGrounded) {
            _canJump = _hangTime;
            _isBetterJumpDisabled = false;
            _rigidbody.gravityScale = 2;
        }

        if (_input.KeyJump) {
            _jumpBufferCount = _jumpBufferLength;
        }
        else {
            _jumpBufferCount -= Time.deltaTime;
        }

        if (_canJump > 0 && _jumpBufferCount >= 0) {
            Jump();
        }

        _weapon.UseWeapon(_input.InputAction.Player, new OptionalNonSerializable<GameObject>(this.gameObject));
        _weapon.LookAngle = _lookAngle;
        _weapon.transform.position = transform.position + LookDir.AngleAxisToVector3(_lookAngle, _maxWeaponRotationDistance);

        if (transform.position.y < -14)
            TakeDamage(Mathf.Infinity);

        _isGroundedPrev = _collision.IsGrounded;
    }

    private void FixedUpdate()
    {
        if (!IsEnabled)
            return;

        if (_canMove)
            Movement();
    }

    private void Movement()
    {
        if (_input.MovementVec.x != 0) {
            _rigidbody.velocity = new Vector2(_input.MovementVec.x * _moveSpeed, _rigidbody.velocity.y);

            //AudioManager._I.PlaySound2D("Walk");
        }
        else {
            _rigidbody.velocity = new Vector2(Mathf.Lerp(_rigidbody.velocity.x, 0f, _friction * Time.fixedDeltaTime), _rigidbody.velocity.y);
        }
    }

    private void Jump()
    {
        if (_isJumpDisabled)
            return;

        AudioManager._I.PlaySound2D("Jump");
        _playerGraphics.SetHeightTrigger("Stretch");
        _playerGraphics.PlayerJumpParticle();

        _rigidbody.velocity = new Vector2(_rigidbody.velocity.x, _jumpForce);

        StartCoroutine(DisableJump());
    }

    private IEnumerator DisableJump()
    {
        _isJumpDisabled = true;

        yield return new WaitForSeconds(.25f);

        _isJumpDisabled = false;
    }

    /// <summary>
    /// if falling, add fallMultiplier
    /// if jumping and not holding spacebar or walljumping, increase gravity to peform a small jump
    /// if jumping and holding spacebar, perform a full jump
    /// </summary>
    private void BetterJump()
    {
        if (_rigidbody.velocity.y < 0) {
            _rigidbody.velocity += (_fallMultiplier - 1) * Physics.gravity.y * Time.deltaTime * Vector2.up;
        }
        else if ((_rigidbody.velocity.y > 0 && !_input.KeyJumpHold) || _isBetterJumpDisabled) {
            _rigidbody.velocity += (_lowJumpMultiplier - 1) * Physics.gravity.y * Time.deltaTime * Vector2.up;
        }
    }

    private IEnumerator ResetPlayer(float time)
    {
        yield return new WaitForSeconds(time);

        _playerGraphics.SetTrigger("Revive");
        _deathParticles.Play();
        transform.position = _spawnPosition;

        IsEnabled = true;
        _rigidbody.bodyType = RigidbodyType2D.Dynamic;
        _collider.enabled = true;
        _playerGraphics.SpriteRenderer.enabled = true;
        CurrentHealth = Health;
        _weapon.gameObject.SetActive(true);
        PlayerManager.I.ResetHearts();
    }

    public IEnumerator DisablePlayer(float time)
    {
        IsEnabled = false;

        yield return new WaitForSeconds(time);

        IsEnabled = true;
    }

    public IEnumerator DisableMovement(float time)
    {
        _canMove = false;

        yield return new WaitForSeconds(time);

        _canMove = true;
    }

    public void ShootFeedback(float force, float angle)
    {
        if (_collision.IsGrounded) {
            _rigidbody.velocity = new Vector2(Mathf.Lerp(Rigidbody.velocity.x, 0f, 16f * Time.deltaTime), Rigidbody.velocity.y);
        }
        else {
            _rigidbody.gravityScale = 3f;
        }

        _isBetterJumpDisabled = true;

        _rigidbody.velocity -= ((Vector2) LookDir.GetDir(angle)).normalized * force;
        StartCoroutine(DisableMovement(.1f));
    }

    public void TakeDamage(float damage)
    {
        CurrentHealth -= damage;

        StartCoroutine(_playerGraphics.Blink());
        CinemachineShake.ShakeCamera(3.5f, .1f);
        AudioManager._I.PlaySound2D("Player-Hit", 1.2f, 129);

        if (CurrentHealth <= 0) {
            _playerGraphics.SetTrigger("Death");
            IsEnabled = false;
            _rigidbody.bodyType = RigidbodyType2D.Static;
            _collider.enabled = false;
            _weapon.gameObject.SetActive(false);

            AudioManager._I.PlaySound2D("Player-Death");
        }
    }

    public void Die()
    {
#if UNITY_EDITOR
        if (!_shouldDie) {
            Debug.Log("Died");

            return;
        }
#endif

        PlayerManager.PlayerPoints -= 5;
        _deathParticles.Play();
        _playerGraphics.SpriteRenderer.enabled = false;
        StartCoroutine(ResetPlayer(.6f));
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.collider.gameObject.CompareTag("Enemy")) {
            if (!_collision.IsGrounded && Physics2D.Raycast(transform.position, Vector2.down, _attackLine)) {
                GameObject @object = other.gameObject;

                @object.GetComponent<IDamageable>()?.TakeDamage(10f);
                @object.GetComponent<IShooteable>()?.ShootFeedback(_jumpForce, LookDir.GetDir(@object.transform.position, transform.position));

                Jump();
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawLine(transform.position, (Vector2) transform.position + new Vector2(0f, -_attackLine));
    }
}
