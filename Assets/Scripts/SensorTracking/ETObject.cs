
using UnityEngine;

public class ETObject : MonoBehaviour
{
    protected bool isFocused = false;
    public virtual void IsFocused()
    {
        isFocused = true;
    }

    public virtual void UnFocused()
    { 
        isFocused = false; 
    }

}
