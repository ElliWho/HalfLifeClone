using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public delegate void AmmoChangedEvent(WeaponRuntime wr);

public class WeaponController : MonoBehaviour
{
    [SerializeField] private Transform weaponHoldPoint;
    [SerializeField] private List<WeaponData> unlockedWeapons = new List<WeaponData>();
    private List<WeaponRuntime> weaponRuntimes = new List<WeaponRuntime>();

    private WeaponData currentWeapon; 
    private WeaponRuntime currentWeaponRuntime;

    

    public bool attackPending = false;
    

    private int weaponIndex = 0;

    private Camera playerCam;

    private HandAnimController handAnimController;

    private int consecutiveShots = 0;
    private float lastShotTime = -999f;

    public static event AmmoChangedEvent OnAmmoChanged;

    private void Awake()
    {
        playerCam = Camera.main;
        handAnimController = GetComponent<HandAnimController>();

        handAnimController.OnReloadFinshed += ReloadFinished;

        GameServices.Input.Actions.Player.Attack.performed += ctx => attackPending = true;
        GameServices.Input.Actions.Player.Attack.canceled += ctx => attackPending = false;
        GameServices.Input.Actions.Player.WeaponScroll.performed += ctx => WeaponScroll((int)ctx.ReadValue<float>());
        GameServices.Input.Actions.Player.Reload.performed += ctx => Reload();
    }
    private float timer = 0f;

    private bool CanFire() 
    {
        if(isReloading || handAnimController.IsAnimationPlaying("Draw")) 
        {            
            Debug.Log("Cannot fire: Reloading or Drawing");
            return false;
        }
        if (currentWeaponRuntime.ammoInClip <= 0)
        {
            Debug.Log("Cannot fire: No ammo in clip");
            return false;
        }
        Debug.Log("Can fire");
        return true;
    }
    private void Update()
    {
        if (!currentWeapon) 
        {
            return;
        }
        
        if (attackPending && timer >= currentWeapon.fireRate) 
        {          
            if(currentWeapon.weaponType == WeaponType.Melee) 
            {
                Attack();
            }
            if (CanFire()) 
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
            }            
            timer = 0f;
        }
        timer += Time.deltaTime;
    }
    /*
    private IEnumerator IReload() 
    {
        int neededAmmo = currentWeaponRuntime.weaponData.magazineSize - currentWeaponRuntime.ammoInClip;

        if (neededAmmo <= 0 || currentWeaponRuntime.ammoInReserve <= 0) yield break;
        handAnimController.SetTrigger("Reload");

        yield return new WaitForSeconds(0.1f);
        yield return new WaitUntil(() => !handAnimController.IsAnimationPlaying("Reload"));

        int ammoToLoad = Mathf.Clamp(neededAmmo, 1, currentWeaponRuntime.ammoInReserve);

        currentWeaponRuntime.ammoInReserve -= ammoToLoad;
        currentWeaponRuntime.ammoInClip += ammoToLoad;

        OnAmmoChanged?.Invoke(currentWeaponRuntime);
    }
    */
    bool isReloading = false;
    private void Reload() 
    {                
        int neededAmmo = currentWeaponRuntime.weaponData.magazineSize - currentWeaponRuntime.ammoInClip;
        if (neededAmmo <= 0 || currentWeaponRuntime.ammoInReserve <= 0) return;

        handAnimController.SetTrigger("Reload");
        isReloading = true;

        int ammoToLoad = Mathf.Clamp(neededAmmo, 1, currentWeaponRuntime.ammoInReserve);

        currentWeaponRuntime.ammoInReserve -= ammoToLoad;
        currentWeaponRuntime.ammoInClip += ammoToLoad;

        

        OnAmmoChanged?.Invoke(currentWeaponRuntime);
    }    
    public void ReloadFinished() 
    {
        isReloading = false;
    }
    public void AddAmmo(AmmoType type, int amount) 
    {           
        for (int i  = 0; i < weaponRuntimes.Count; i++) 
        {
            if (weaponRuntimes[i].weaponData.ammoType == type && weaponRuntimes[i].weaponData.weaponType != WeaponType.Melee) 
            {
                weaponRuntimes[i].ammoInReserve += amount;
                OnAmmoChanged?.Invoke(currentWeaponRuntime);
                break;
            }
        }
    }
    private void WeaponScroll(int value) 
    {
        weaponIndex += value;
        if (weaponIndex < 0) weaponIndex = unlockedWeapons.Count - 1;
        else if (weaponIndex >= unlockedWeapons.Count) weaponIndex = 0;

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

        currentWeaponRuntime = null;
        for (int i = 0; i < weaponRuntimes.Count; i++) // weapon exists
        {
            if (weaponRuntimes[i].weaponData == data)
            {
                currentWeaponRuntime = weaponRuntimes[i];
                break;
            }
        }

        if (currentWeaponRuntime == null) // weapon doesn't exist yet 
        {
            // spawn weapon and reset its position after parent
            GameObject weaponInstance = Instantiate(data.mesh, weaponHoldPoint);
            weaponInstance.transform.localPosition = Vector3.zero;
            weaponInstance.transform.localRotation = Quaternion.identity;

            // will hold referenceces to positions on the weapon mesh, e.g. muzzle point 
            WeaponView weaponView = weaponInstance.GetComponent<WeaponView>();


            // spawn the Vfx
            ParticleSystem vfxParticle = null;
            if (data.weaponEffects != null && data.weaponEffects.fireVfxPrefab != null)
            {
                GameObject vfxInstance = Instantiate(data.weaponEffects.fireVfxPrefab);
                vfxInstance.transform.SetParent(weaponView.MuzzlePoint, false);
                vfxInstance.transform.localPosition = Vector3.zero;
                vfxInstance.transform.localRotation = Quaternion.identity;
                vfxParticle = vfxInstance.GetComponent<ParticleSystem>();

                if (vfxParticle != null)
                {
                    var main = vfxParticle.main;
                    main.simulationSpace = ParticleSystemSimulationSpace.World;

                    vfxParticle.Stop();
                }
            }

            currentWeaponRuntime = new WeaponRuntime
            {
                weaponData = data,
                weaponInstance = weaponInstance,
                weaponView = weaponView,
                muzzleVfxInstance = vfxParticle,                
            };

            weaponRuntimes.Add(currentWeaponRuntime);
            AddAmmo(data.ammoType, data.magazineSize * 3);
        }
        
        for (int i = 0; i < weaponRuntimes.Count; i++)
        {
            bool active = weaponRuntimes[i].weaponData == data;
            weaponRuntimes[i].weaponInstance.SetActive(active);
        }

        handAnimController.SetTrigger("Draw");
        OnAmmoChanged?.Invoke(currentWeaponRuntime);
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

        float yaw = UnityEngine.Random.Range(-coneAngDeg, coneAngDeg);
        float pitch = UnityEngine.Random.Range(-coneAngDeg, coneAngDeg); 

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        return rot * baseDir;
    }
    private void HandleProjectile() 
    {
        
    }
    private void HandleHitscan() 
    {
        float now = Time.time;
        if (now - lastShotTime <= currentWeaponRuntime.weaponData.consequtiveWindow)        
            consecutiveShots++;        
        else        
            consecutiveShots = 1;

        lastShotTime = now;

        float activeSpread = 0f;
        if (consecutiveShots >= currentWeaponRuntime.weaponData.shotsBeforeDebuff) 
            activeSpread = currentWeaponRuntime.weaponData.debuffSpreadAngle;

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
                    baseDamage = currentWeaponRuntime.weaponData.baseDamage,
                    hitbox = hitbox.hitboxType
                });
                Debug.Log("Hit " + hit.collider.name + " Damage: " + outcome.damageApplied);
            }
        }
        currentWeaponRuntime.muzzleVfxInstance.Play();
        currentWeaponRuntime.ammoInClip--;
        OnAmmoChanged?.Invoke(currentWeaponRuntime);
    }
    private void HandleMelee() 
    {
        
    }

}
