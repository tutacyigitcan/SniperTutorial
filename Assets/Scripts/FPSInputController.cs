using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(FPSController))]
public class FPSInputController : MonoBehaviour
{
    private GunHanddle gunHandle;
    private FPSController FPSMotor;

    private void Start()
    {
        Application.targetFrameRate = 60;
        MouseLock.MouseLocked = true;
    }

    private void Awake()
    {
        FPSMotor = GetComponent<FPSController>();
        gunHandle = GetComponent<GunHanddle>();
    }

    private void Update()
    {
        FPSMotor.Aim(new Vector2(Input.GetAxis("Mouse X"),Input.GetAxis("Mouse Y")));
        FPSMotor.Move (new Vector3 (Input.GetAxis ("Horizontal"), 0, Input.GetAxis ("Vertical")));
        FPSMotor.Jump (Input.GetButton ("Jump"));

        if(Input.GetKey(KeyCode.LeftShift)){
            FPSMotor.Boost(1.7f);	
        }

        FPSMotor.Holdbreath(1);	
        if(Input.GetKey(KeyCode.LeftShift)){
            FPSMotor.Holdbreath(0);	
        }

        if(Input.GetButton("Fire1")){
            gunHandle.Shoot();	
        }
        if(Input.GetButtonDown("Fire2")){
            gunHandle.Zoom();	
        }
        if (Input.GetAxis("Mouse ScrollWheel") < 0){
            gunHandle.ZoomAdjust(-1);	
        }
        if (Input.GetAxis("Mouse ScrollWheel") > 0){
            gunHandle.ZoomAdjust(1);	
        }
        if(Input.GetKeyDown(KeyCode.R)){
            gunHandle.Reload();
        }
        if(Input.GetKeyDown(KeyCode.Q)){
            gunHandle.SwitchGun();
        }
        if(Input.GetKeyDown(KeyCode.Z)){
            gunHandle.OffsetAdjust(new Vector2(0,-1));
        }
        if(Input.GetKeyDown(KeyCode.X)){
            gunHandle.OffsetAdjust(new Vector2(0,1));
        }
        if(Input.GetKeyDown(KeyCode.C)){
            gunHandle.OffsetAdjust(new Vector2(-1,0));
        }
        if(Input.GetKeyDown(KeyCode.V)){
            gunHandle.OffsetAdjust(new Vector2(1,0));
        }
    }
}
