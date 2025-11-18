using UnityEngine;

[System.Serializable]
public class WeaponRuntime
{
    // data holds the stats and settings for the weapon see WeaponData.cs
    public WeaponData weaponData;
    // the mesh
    public GameObject weaponInstance;
    // holds information about the weapons mesh ie where the muzzle is located
    public WeaponView weaponView;
    // the shooting vfx
    public ParticleSystem muzzleVfxInstance;

    // these will hold ammo counts for the weapon
    public int ammoInClip;
    public int ammoInReserve;
}
