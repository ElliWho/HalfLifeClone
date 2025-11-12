using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimationController : MonoBehaviour
{
    private Animator animator;

    [SerializeField] private AnimationClip flipOffAnim;
    [SerializeField] private AnimationClip waveAnim;
    [SerializeField] private AnimationClip defaultAnim;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }
    bool f = false;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            f = !f;
            if (f)
                PlayAnimation(flipOffAnim.name);
            else
                PlayAnimation(defaultAnim.name);
        }
        if (Input.GetKeyDown(KeyCode.G)) 
        {
            PlayAnimation(waveAnim.name);
        }
    }
    private void PlayAnimation(string anim) 
    {
        animator.Play(anim);
    }
}
