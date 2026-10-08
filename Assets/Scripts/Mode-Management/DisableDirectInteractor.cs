using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DisableDirectInteractor : MonoBehaviour
{
    private Savefile savefile;

    // Start is called before the first frame update
    void Awake()
    {
        savefile = GameObject.Find("SavefileManager").GetComponent<Savefile>();
        savefile.Load();
    }

    // Update is called once per frame
    void Update()
    {
        if (savefile.avatarInput != AvatarInput.VARJO)
        {
            this.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor>().enabled = false;
        }
    }
}
