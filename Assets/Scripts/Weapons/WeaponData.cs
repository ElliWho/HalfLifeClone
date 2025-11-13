using UnityEngine;
public enum WeaponType 
{
    Hitscan,
    Projectile,
    Melee
}
[CreateAssetMenu(menuName = "Weapons/Weapon")]
public class WeaponData : ScriptableObject
{
    [Header("Description / Visuals")]
    public string weaponName;
    public Sprite weaponIcon;
    public GameObject mesh;
    public WeaponEffects weaponEffects;
    public WeaponType weaponType;

    [Header("Animation")]
    public HandAnimationSet handAnimationSet;
    public AnimationClip equipAnim;
    public AnimationClip reloadAnim;
    public AnimationClip fireAnim;
    public AnimationClip emptyAnim;

    [Header("Stats")]
    public float baseDamage;
    public float fireRate;
    public int magazineSize;
    public int maxAmmoCapacity;
    [Space]
    public bool isAutomatic;    
    public float dropOffDistance;
    public int projectileAmount;

    [Header("Debuffs")]
    public int shotsBeforeDebuff;
    public float debuffSpreadAngle;
    public float consequtiveWindow = 0.4f;
}
