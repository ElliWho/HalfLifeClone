using TMPro;
using UnityEngine;

public class UiController : MonoBehaviour
{
    [SerializeField] private TMP_Text ammoText;

    private void Awake()
    {
        WeaponController.OnAmmoChanged += UpdateAmmoDisplay;
    }

    private void UpdateAmmoDisplay(WeaponRuntime wr) 
    {
        if(wr.weaponData.weaponType == WeaponType.Melee)
            ammoText.gameObject.SetActive(false);
        else 
        {
            ammoText.gameObject.SetActive(true);
            ammoText.text = $"{wr.ammoInClip} / {wr.ammoInReserve}";
        }        
    }
}
