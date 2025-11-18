using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public delegate void AmmoChangedEvent(WeaponRuntime wr);

public class WeaponController : MonoBehaviour
{
    // transform on the hand where weapons will be parented
    [SerializeField] private Transform weaponHoldPoint;
    // list of all the weapons the player has
    [SerializeField] private List<WeaponData> unlockedWeapons = new List<WeaponData>();

    // holds runtime data for spawned weapons like mesh, vfx, ammo ect.. see WeaponRuntime.cs
    private List<WeaponRuntime> weaponRuntimes = new List<WeaponRuntime>();

    // holds the current weapon data
    private WeaponData currentWeapon;
    // holds runtime data for the current weapon
    private WeaponRuntime currentWeaponRuntime;    

    // bool to check if player is holding down attack input
    public bool attackPending = false;

    // index for scrolling through unlockedWeapons
    private int weaponIndex = 0;

    // camera reference for raycasting
    private Camera playerCam;

    // this will handle the hand animations
    private HandAnimController handAnimController;

    // timer for fire rate handling
    private float timer = 0f;

    // for handling consecutive shots for debuff
    private int consecutiveShots = 0;
    private float lastShotTime = -999f;

    // event for ammo changes ie. UI
    public static event AmmoChangedEvent OnAmmoChanged;

    // bool to chek if reloading ie. cannot shoot
    bool isReloading = false;    

    private void Awake()
    {
        playerCam = Camera.main;

        // handles all the hand animations
        handAnimController = GetComponent<HandAnimController>();

        // subsctibe to reload finished event
        // the animation will trigger the event and that will dictate if the player can shoot ect..
        handAnimController.OnReloadFinshed += ReloadFinished;

        // all below are just input subsciptions
        GameServices.Input.Actions.Player.Attack.performed += ctx => attackPending = true;
        GameServices.Input.Actions.Player.Attack.canceled += ctx => attackPending = false;
        GameServices.Input.Actions.Player.WeaponScroll.performed += ctx => WeaponScroll((int)ctx.ReadValue<float>());
        GameServices.Input.Actions.Player.Reload.performed += ctx => Reload();
    }
    

    private bool CanFire() 
    {
        // cannot fire if reloading or drawing weapon
        if(isReloading || handAnimController.IsAnimationPlaying("Draw")) 
        {            
            return false;
        }
        // cannot fire if no ammo in clip
        if (currentWeaponRuntime.ammoInClip <= 0)
        {
            return false;
        }
        // if all checks passed can fire
        return true;
    }
    private void Update()
    {
        // check if we have a weapon
        if (!currentWeapon) 
        {
            return;
        }

        // this is for fire rate handling, attackPending is set by input
        if (attackPending && timer >= currentWeapon.fireRate) 
        {          
            // if melee weapon, always attack, dont need to check ammo
            if(currentWeapon.weaponType == WeaponType.Melee) 
            {
                Attack();
            }
            // will check ammo and reloading states
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
            // reset the timer after an attack 
            timer = 0f;
        }
        timer += Time.deltaTime;
    }    
    private void Reload() 
    {
        // the amount of ammo needed to fill the clip
        int neededAmmo = currentWeaponRuntime.weaponData.magazineSize - currentWeaponRuntime.ammoInClip;
        // clip is full or no reserve ammo
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
        // this is called by an animation event to signal the reload is finished
        isReloading = false;
    }
    public void AddAmmo(AmmoType type, int amount) // To Improve
    {           
        // add ammo by type, ie. Light, Heavy ammo
        
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
        // will scroll through unlocked weapons and equip them
        weaponIndex += value;
        if (weaponIndex < 0) weaponIndex = unlockedWeapons.Count - 1;
        else if (weaponIndex >= unlockedWeapons.Count) weaponIndex = 0;

        currentWeapon = unlockedWeapons[weaponIndex];

        ApplyWeapon(currentWeapon);
    }
    private void ApplyWeapon(WeaponData data)
    {
        if (data == null) return;

        // apply the animation override for the hands
        handAnimController.ApplyOverride(data.handAnimationSet);
        EquipWeapon(data);
    }
    private void EquipWeapon(WeaponData data)
    {
        if (data == null) return;
        currentWeapon = data;
        currentWeaponRuntime = null;

        // weapon exists ie. mesh, vfx ect.. already spawned
        for (int i = 0; i < weaponRuntimes.Count; i++) 
        {
            if (weaponRuntimes[i].weaponData == data)
            {
                // weapon already exists so set the current runtime to it
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

            // this holds runtime data for the weapon
            currentWeaponRuntime = new WeaponRuntime
            {
                weaponData = data,
                weaponInstance = weaponInstance,
                weaponView = weaponView,
                muzzleVfxInstance = vfxParticle,                
            };

            weaponRuntimes.Add(currentWeaponRuntime);
            AddAmmo(data.ammoType, data.magazineSize * 3); // just for testing
        }

        // deactivate all other weapons
        for (int i = 0; i < weaponRuntimes.Count; i++)
        {
            bool active = weaponRuntimes[i].weaponData == data;
            weaponRuntimes[i].weaponInstance.SetActive(active);
        }
        // set the animation controller to trigger Draw
        handAnimController.SetTrigger("Draw");
        // ammo UI
        OnAmmoChanged?.Invoke(currentWeaponRuntime);
    }

    private void Attack() 
    {
        if (currentWeapon == null) return;

        handAnimController.SetTrigger("Attack");

        // different functions for handling differ weapon types
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

    // this is for weapon inaccuracy / spread
    private Vector3 ApplyConeSpread(Vector3 baseDir, float coneAngDeg) 
    {
        if (coneAngDeg <= 0f) return baseDir;

        float yaw = UnityEngine.Random.Range(-coneAngDeg, coneAngDeg);
        float pitch = UnityEngine.Random.Range(-coneAngDeg, coneAngDeg); 

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        return rot * baseDir;
    }
    private void HandleProjectile() // implement 
    {
        
    }
    private void HandleHitscan() 
    {
        // check how many consecutive shots in the window
        float now = Time.time;
        if (now - lastShotTime <= currentWeaponRuntime.weaponData.consequtiveWindow)        
            consecutiveShots++;        
        else        
            consecutiveShots = 1;

        lastShotTime = now;

        // determine spread if the consecutive shots exceed the threshold
        float activeSpread = 0f;
        if (consecutiveShots >= currentWeaponRuntime.weaponData.shotsBeforeDebuff) 
            activeSpread = currentWeaponRuntime.weaponData.debuffSpreadAngle;

        Vector3 origin = playerCam.transform.position;
        Vector3 dir = playerCam.transform.forward;

        // apply spread
        if (activeSpread > 0f) 
            dir = ApplyConeSpread(dir, activeSpread);


        //Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        //Debug.Log("Active spread" + activeSpread);
        Debug.DrawRay(origin, dir * 100f, Color.red, 10f);

        if (Physics.Raycast(origin, dir, out RaycastHit hit))
        {
            // check if we hit a hitbox ie. enemy
            Hitbox hitbox = hit.collider.GetComponent<Hitbox>();
            if (hitbox)
            {
                // create hitinfo to send to hitbox
                // hitbox will forard the data to the relevant damageable component
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
        // play the muzzle vfx
        currentWeaponRuntime.muzzleVfxInstance.Play();
        // reduce ammo
        currentWeaponRuntime.ammoInClip--;
        // invoke the event for UI
        OnAmmoChanged?.Invoke(currentWeaponRuntime);
    }
    private void HandleMelee() // implement
    {
        
    }

}
