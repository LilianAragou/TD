using UnityEngine;

public class AnimatorBoolToggle : MonoBehaviour
{
    public Animator animator;
    public string boolName;

    public void SetBoolTrue()
    {
        animator.SetBool(boolName, true);
    }

    public void SetBoolFalse()
    {
        animator.SetBool(boolName, false);
    }

    public void ToggleBool()
    {
        bool current = animator.GetBool(boolName);
        animator.SetBool(boolName, !current);
    }
}
