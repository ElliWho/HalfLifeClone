using System;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class HandAnimController : MonoBehaviour
{
    [SerializeField] private RuntimeAnimatorController baseHandController;
    [SerializeField] private Animator handAnimator;

    public event Action OnReloadFinshed;
    public void ReloadFinishedEvent() 
    {
        Debug.Log("Reload Finished Event Triggered");
        OnReloadFinshed?.Invoke();
    }
    public void ApplyOverride(HandAnimationSet set) 
    {
        if (set == null)
        {
            handAnimator.runtimeAnimatorController = baseHandController;
            Debug.LogError("No Hand Animation Set assigned");
            return;
        }
        handAnimator.runtimeAnimatorController = set.overrideController;
    }
    public void SetTrigger(string name) 
    {
        handAnimator.ResetTrigger("Draw");
        handAnimator.ResetTrigger("Attack");
        handAnimator.ResetTrigger("Reload");

        handAnimator.SetTrigger(name);
    }
    public bool IsAnimationPlaying(string stateName) 
    {
        var stateInfo = handAnimator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName(stateName) && stateInfo.normalizedTime < 1f;
    }
}
