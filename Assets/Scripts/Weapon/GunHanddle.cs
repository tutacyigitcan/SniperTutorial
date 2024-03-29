using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunHanddle : MonoBehaviour
{
    public Camera FPScamera;
    public Gun[] Guns;
    public int GunIndex;
    [HideInInspector] public Gun CurrentGun;

    void Start()
    {
        if (Guns.Length < 1)
        {
            Guns = this.gameObject.GetComponentsInChildren<Gun>();
        }

        for (int i = 0; i < Guns.Length; i++)
        {
            if (FPScamera)
                Guns[i].NormalCamera = FPScamera;
            Guns[i].fovTemp = FPScamera.fieldOfView;
        }

        SwitchGun(0);
    }

    public void Zoom()
    {
        if (CurrentGun)
            CurrentGun.Zoom();
    }

    public void ZoomToggle()
    {
        if (CurrentGun)
            CurrentGun.ZoomToggle();
    }

    public void Reload()
    {
        if (CurrentGun)
            CurrentGun.Reload();
    }

    public void ZoomAdjust(int delta)
    {
        if (CurrentGun)
            CurrentGun.ZoomDelta(delta);
    }

    public void OffsetAdjust(Vector2 delta)
    {
        if (CurrentGun)
            CurrentGun.OffsetAdjust(delta);
    }

    public void SwitchGun(int index)
    {
        if (FPScamera.enabled)
        {
            for (int i = 0; i < Guns.Length; i++)
            {
                Guns[i].SetActive(false);
            }

            if (Guns.Length > 0 && index < Guns.Length && index >= 0)
            {
                GunIndex = index;
                CurrentGun = Guns[GunIndex].gameObject.GetComponent<Gun>();
                Guns[GunIndex].SetActive(true);
            }
        }
    }

    public void SwitchGun()
    {
        int index = GunIndex + 1;
        if (index >= Guns.Length)
            index = 0;

        SwitchGun(index);
    }

    public void Shoot()
    {
        if (CurrentGun)
            CurrentGun.Shoot();
    }

    public void HoldBreath(int noiseMult)
    {
        if (CurrentGun)
            CurrentGun.FPSmotor.Holdbreath(noiseMult);
    }
}