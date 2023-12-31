﻿using System.Collections;
using UnityEngine;
using Utils;

public abstract class Weapon : MonoBehaviour
{
    [Header("Weapon References")]
    [SerializeField] protected Transform ShootTransform;
    [SerializeField] protected WeaponInfo WeaponInfo;
    [SerializeField] protected ParticleSystem ShootParticle;

    [SerializeField] protected Optional<float> BulletSpeed;
    [SerializeField] protected Optional<float> Recoil;

    [SerializeField] protected float Damage;
    [SerializeField] protected float Spread;

    [HideInInspector] public float LookAngle = 0f;

    protected int CurrentAmmo = 0;

    protected float _nextShoot = 0f;

    protected bool IsReloading = false;

    protected virtual void Start()
    {
        IsReloading = false;

        CurrentAmmo = WeaponInfo.Ammo;
    }

    protected abstract void Shoot(OptionalNonSerializable<GameObject> owner);

    public abstract void UseWeapon(PInputAction.PlayerActions inputAction, OptionalNonSerializable<GameObject> owner);
    public abstract void UseWeapon(OptionalNonSerializable<GameObject> owner);

    protected abstract IEnumerator Reload(float time);
}