using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StarterAssets
{
    public class ButtonPress : MonoBehaviour
    {

        private StarterAssetsInputs _input;
        // Start is called before the first frame update
        void Start()
        {
            _input = GameObject.Find("PlayerCapsule").GetComponent<StarterAssetsInputs>();

        }

        // Update is called once per frame
        void Update()
        {
            if (_input.press)
            {
                Debug.Log("Press!");
            }
        }
    }

}
