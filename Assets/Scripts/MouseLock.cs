using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MouseLock
{
    private static bool mouseLocked;


    public static bool MouseLocked
    {
        get { return mouseLocked; }
        set
        {
            mouseLocked = value;


            Screen.lockCursor = mouseLocked;

            Cursor.visible = !value;
            if (Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }
	

}
