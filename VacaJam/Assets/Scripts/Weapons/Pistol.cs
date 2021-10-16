using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

public class Pistol : Weapon
{
    private void Update()
    {
        transform.rotation = Quaternion.Euler(Quaternion.identity.x, Quaternion.identity.y, LookAngle);
    }

    protected sealed override void Shoot(OptionalNonSerializable<GameObject> owner)
    {
        Projectile projectile = Instantiate(
                original: WeaponInfo.BulletPrefab,
                position: ShootTransform.position,
                rotation: Quaternion.Euler(Quaternion.identity.x, Quaternion.identity.y, LookAngle + Random.Range(-Spread, Spread))
            ).GetComponent<Projectile>();

        if (BulletSpeed.Enabled)
            projectile.Speed = BulletSpeed.Value;

        projectile.Damage = WeaponInfo.Damage;
        projectile.Owner = owner;

        projectile.gameObject.SetActive(true);
    }

    public sealed override void UseWeapon(bool keyShoot, OptionalNonSerializable<GameObject> owner)
    {
        if (keyShoot) {
            Shoot(owner);
        }
    }

    public sealed override void UseWeapon(OptionalNonSerializable<GameObject> owner)
    {
        if (Time.time >= _nextShoot) {
            Shoot(owner);

            _nextShoot = Time.time + 1f / WeaponInfo.FireRate;
        }
    }

    // # Not Implemented yet
    protected sealed override IEnumerator Reload(float time) => null;
}