using UnityEngine;

[CreateAssetMenu(menuName = "HandSet/HandAnimSet")]
public class HandAnimationSet : ScriptableObject
{
    public AnimatorOverrideController overrideController;
    public bool fireIsLooping;
}
