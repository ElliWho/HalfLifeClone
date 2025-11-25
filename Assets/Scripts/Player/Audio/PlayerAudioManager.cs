using Unity.VisualScripting;
using UnityEngine;

public enum SoundType { Weapon}
public class PlayerAudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource weaponSfxAudioSource;

    public void PlayClip(SoundType type, AudioClip clip) 
    {
        switch (type) 
        {
            case SoundType.Weapon:
                weaponSfxAudioSource.PlayOneShot(clip);
                break;
        }
    }
}
