using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MuzzleFlash : MonoBehaviour
{
    public float Speed = 1;
    Light lightComp;

    void Start ()
    {
        lightComp = this.GetComponent<Light> ();
    }

    void Update ()
    {
        if (lightComp != null) {
            if (lightComp.intensity > 0)
                lightComp.intensity -= Speed * Time.deltaTime;

            if (lightComp.intensity < 0.05)
                lightComp.intensity = 0;
        }
    }
}
