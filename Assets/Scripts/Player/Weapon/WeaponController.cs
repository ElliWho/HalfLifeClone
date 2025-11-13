using NUnit.Framework;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [SerializeField] private Transform weaponHoldPoint;
    [SerializeField] private List<WeaponData> unlockedWeapons = new List<WeaponData>();
    private Dictionary<WeaponData, GameObject> weaponInstances = new Dictionary<WeaponData, GameObject>();
    private WeaponData currentWeapon;
    private int weaponIndex = 0;

    private Camera playerCam;
    bool attackPending = false;

    [SerializeField] private Animator handAnimator;
    [SerializeField] private RuntimeAnimatorController baseHandController;
    private AnimatorOverrideController activeOverride;

    private void Awake()
    {
        playerCam = Camera.main;

        GameServices.Input.Actions.Player.Attack.performed += ctx => Attack(); ;
        //GameServices.Input.Actions.Player.Attack.canceled += ctx => attackPending = false;
        GameServices.Input.Actions.Player.WeaponScroll.performed += ctx => WeaponScroll((int)ctx.ReadValue<float>());

        ApplyWeapon(unlockedWeapons[0]);
    }    
    private void WeaponScroll(int value) 
    {
        weaponIndex += value;
        weaponIndex = Mathf.Clamp(weaponIndex, 0, unlockedWeapons.Count - 1);

        currentWeapon = unlockedWeapons[weaponIndex];

        if (!weaponInstances.ContainsKey(currentWeapon)) 
        {
            ApplyWeapon(currentWeapon);
            EquipWeapon(currentWeapon);
        }
        else 
        {
            EquipWeapon(currentWeapon);
        }
    }
    private void ApplyWeapon(WeaponData data)
    {
        HandAnimationSet set = data.handAnimationSet;
        if(set == null) 
        {
            handAnimator.runtimeAnimatorController = baseHandController;
            Debug.LogError("No Hand Animation Set assigned to weapon: " + data.weaponName);
            return;
        }
        
        handAnimator.runtimeAnimatorController = set.overrideController;
    }
    private void EquipWeapon(WeaponData data) 
    {
        if (data == null) return;
        currentWeapon = data;
        
        if (weaponInstances.ContainsKey(currentWeapon)) 
        {
            foreach (var weapon in weaponInstances) 
            {
                weapon.Value.SetActive(weapon.Key == currentWeapon);
            }
        }
        else 
        {
            GameObject weaponInstance = Instantiate(data.mesh, weaponHoldPoint);
            weaponInstance.transform.localPosition = Vector3.zero;
            weaponInstance.transform.localRotation = Quaternion.identity;

            weaponInstances.Add(currentWeapon, weaponInstance);
        }

        handAnimator.SetTrigger("Draw");
    }    

    private void Attack() 
    {
        if (currentWeapon == null) return;

        handAnimator.SetTrigger("Attack");

        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit)) 
        {
            Hitbox hitbox = hit.collider.GetComponent<Hitbox>();
            if (hitbox) 
            {
                HitOutcome outcome = hitbox.ForwardHit(new HitInfo 
                {
                    point = hit.point,
                    normal = hit.normal,
                    isMelee = false,
                    baseDamage = 10f,
                    hitbox = hitbox.hitboxType
                });
            }
        }
    }
}
