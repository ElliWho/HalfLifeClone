using UnityEngine;

public class WeaponView : MonoBehaviour
{
    [Header("Vfx / MufflePoint")]
    [SerializeField] private Transform muzzlePoint;
    public Transform MuzzlePoint => muzzlePoint;
}
