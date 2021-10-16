using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class Enemy : MonoBehaviour, IDamageable
{
    [Header("Debug")]
    [SerializeField] protected bool IsDebugEnable = true;
    [SerializeField] protected bool ShouldDie = true;

    [Header("Enemy References")]
    [SerializeField] protected LayerMask WhatIsTarget;

    [Header("Enemy Stats")]
    [SerializeField] protected float Health;

    protected Rigidbody2D Rigidbody;

    protected float CurrentHealth;

    protected virtual void Awake()
    {
        Rigidbody = GetComponent<Rigidbody2D>();
    }

    protected virtual void Start()
    {
        CurrentHealth = Health;
    }

    public abstract void TakeDamage(float damage);

    public abstract void Die();
}

[RequireComponent(typeof(Seeker))]
public abstract class PathFinderEnemy : Enemy
{
    [Header("Seeking config")]
    [SerializeField] protected float NextWaypointDistance = 3f;
    [SerializeField] private float _refreshPathRate;

    protected Transform TargetTransform;
    protected Path Path;
    protected Seeker Seeker;

    protected int CurrentWaypoint = 0;

    protected override void Awake()
    {
        base.Awake();

        Seeker = GetComponent<Seeker>();
    }

    protected override void Start()
    {
        base.Start();

        InvokeRepeating(nameof(UpdatePath), 0f, _refreshPathRate);
    }

    protected virtual void UpdatePath()
    {
        if (Seeker.IsDone()) {
            Seeker.StartPath(transform.position, TargetTransform.position, (Path p) => {
                if (!p.error) {
                    Path = p;
                    CurrentWaypoint = 0;
                }
                else {
                    throw new Exception($"Can't Load Path: { p.errorLog }");
                }
            });
        }
    }

    public abstract override void TakeDamage(float damage);
    public abstract override void Die();
}