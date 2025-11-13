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
    private GameObject currentWeaponInstance;

    public bool attackPending = false;
    

    private int weaponIndex = 0;

    private Camera playerCam;

    private HandAnimController handAnimController;

    private int consecutiveShots = 0;
    private float lastShotTime = -999f;


    private void Awake()
    {
        playerCam = Camera.main;
        handAnimController = GetComponent<HandAnimController>();

        GameServices.Input.Actions.Player.Attack.performed += ctx => attackPending = true;
        GameServices.Input.Actions.Player.Attack.canceled += ctx => attackPending = false;
        GameServices.Input.Actions.Player.WeaponScroll.performed += ctx => WeaponScroll((int)ctx.ReadValue<float>());                       
    }
    private float timer = 0f;

    private bool CanFire() 
    {
        if(!handAnimController.IsAnimationPlaying("Draw")) 
            return true;
        return false;
    }
    private void Update()
    {
        if (!currentWeapon) 
        {
            Debug.LogWarning("No weapon equipped");
            return;
        }
        
        if (attackPending && CanFire() && timer >= currentWeapon.fireRate) 
        {          
            if (currentWeapon.isAutomatic) 
            {
                Attack();                
            }
            else 
            {
                Attack();
                attackPending = false;
            }
            timer = 0f;
        }
        timer += Time.deltaTime;
    }
    private void WeaponScroll(int value) 
    {
        weaponIndex += value;
        weaponIndex = Mathf.Clamp(weaponIndex, 0, unlockedWeapons.Count - 1);

        currentWeapon = unlockedWeapons[weaponIndex];

        ApplyWeapon(currentWeapon);
    }
    private void ApplyWeapon(WeaponData data)
    {
        if (data == null) return;

        handAnimController.ApplyOverride(data.handAnimationSet);
        EquipWeapon(data);
    }
    private void EquipWeapon(WeaponData data) 
    {
        if (data == null) return;
        currentWeapon = data;

        if (!weaponInstances.TryGetValue(data, out GameObject weaponInstance))
        {
            weaponInstance = Instantiate(data.mesh, weaponHoldPoint);
            
            weaponInstance.transform.SetParent(weaponHoldPoint, false);
            weaponInstance.transform.localPosition = Vector3.zero;
            weaponInstance.transform.localRotation = Quaternion.identity;
            
            weaponInstances.Add(data, weaponInstance);
        }

        foreach (var kv in weaponInstances)
        {            
            if(kv.Key == data) 
            {
                kv.Value.SetActive(true);
                currentWeaponInstance = kv.Value;
            }
            else                
            {
                kv.Value.SetActive(false);
            }
        }

        handAnimController.SetTrigger("Draw");
    }    

    private void Attack() 
    {
        if (currentWeapon == null) return;

        handAnimController.SetTrigger("Attack");

        switch (currentWeapon.weaponType) 
        {
            case WeaponType.Hitscan:
                HandleHitscan();
                break;
            case WeaponType.Projectile:
                HandleProjectile();
                break;
            case WeaponType.Melee:
                HandleMelee();
                break;
        }
    }
    private Vector3 ApplyConeSpread(Vector3 baseDir, float coneAngDeg) 
    {
        if (coneAngDeg <= 0f) return baseDir;

        float yaw = Random.Range(-coneAngDeg, coneAngDeg);
        float pitch = Random.Range(-coneAngDeg, coneAngDeg); 

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        return rot * baseDir;
    }
    private void HandleProjectile() 
    {
        
    }
    private void HandleHitscan() 
    {
        float now = Time.time;
        if (now - lastShotTime <= currentWeapon.consequtiveWindow)        
            consecutiveShots++;        
        else        
            consecutiveShots = 1;

        lastShotTime = now;

        float activeSpread = 0f;
        if (consecutiveShots >= currentWeapon.shotsBeforeDebuff) 
            activeSpread = currentWeapon.debuffSpreadAngle;

        Vector3 origin = playerCam.transform.position;
        Vector3 dir = playerCam.transform.forward;

        if(activeSpread > 0f) 
            dir = ApplyConeSpread(dir, activeSpread);


        //Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        //Debug.Log("Active spread" + activeSpread);
        Debug.DrawRay(origin, dir * 100f, Color.red, 10f);
        if (Physics.Raycast(origin, dir, out RaycastHit hit))
        {
            Hitbox hitbox = hit.collider.GetComponent<Hitbox>();
            if (hitbox)
            {
                HitOutcome outcome = hitbox.ForwardHit(new HitInfo
                {
                    point = hit.point,
                    normal = hit.normal,
                    isMelee = false,
                    baseDamage = currentWeapon.baseDamage,
                    hitbox = hitbox.hitboxType
                });
            }
        }
    }
    private void HandleMelee() 
    {
        
    }

}
