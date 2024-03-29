using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class ActionCamera : MonoBehaviour
{
    public ActionPreset[] ActionPresets;

    [HideInInspector] public float FOVTarget;
    
    public GameObject ObjectLookAt, ObjectFollowing, ObjectLookAtRoot;
    [HideInInspector] public Vector3 PositionHit;
    [HideInInspector] public Vector3 PositionLookAt;
    [HideInInspector] public float Raduis = 5;
    [HideInInspector] public float TimeDuration = 2;
    [HideInInspector] public float SlowTimeDuration = 2;
    public int PresetIndex = 0;
    public bool RandomIndex = true;
    [HideInInspector] public bool InAction;
    [HideInInspector] public bool Detected;
    [HideInInspector] public bool HitTarget;
    public float TimeChangeSpeed = 10;

    private static float initialFixedTimeStep = 1;
    
    private float timeTemp, slowtimeTemp;
    private bool[] cameraEnabledTemp;
    private bool[] audiolistenerEnabledTemp;
    private float timeScaleTarget = 1;
    [HideInInspector] public bool cameraTemp;
    [HideInInspector] public bool Follow = false;
    public float Length = 15;
    [HideInInspector] public float LengthMult = 0;
    [HideInInspector] public float BaseDistance;
    private float lengthCast = 0;
    public float Damping = 10;
    public int IgnoreCameraLayer = 11;
    public float ColliderOffset = 1.0f;
    public Light SpotLight;
    public Camera MainCamera;
    private bool timeSetByASK;
    private float fovTemp;
    [HideInInspector] public GameObject TargetShouldHit;
    [HideInInspector] public Vector3 PositionFirstDetect;
    [HideInInspector] public GameObject CurrentBullet;

    public Quaternion initialRotationTmp;
    public Vector3 initialPositionTmp;


    public ActionPreset GetPresets()
    {
        if (ActionPresets.Length <= 0)
        {
            return null;
        }

        ActionPreset res = ActionPresets[Random.Range(0, ActionPresets.Length)];
        if (!RandomIndex)
        {
            if (PresetIndex >= 0 && PresetIndex < ActionPresets.Length)
            {
                res = ActionPresets[PresetIndex];
            }
        }

        return res;
    }

    void Awake()
    {
        SniperKit.ActionCam = this;
        initialPositionTmp = this.transform.position;
        initialRotationTmp = this.transform.rotation;
    }

    void Start()
    {
        for (int i = 0; i < ActionPresets.Length; i++)
        {
            ActionPresets[i].Initialize();
        }

        if (GetComponent<Camera>())
            fovTemp = GetComponent<Camera>().fieldOfView;
        MainCamera = this.gameObject.GetComponent<Camera>();
        initialFixedTimeStep = Time.fixedDeltaTime;
        cameraPosition = this.transform.position;
    }

    public void ActionBullet(float actionduration)
    {
        TimeDuration = actionduration;
        timeTemp = Time.realtimeSinceStartup;
        CollisionEnabled = true;
        setTarget();
    }

    public void SetLookAtPosition(Vector3 pos)
    {
        lookAtPosition = pos;
        ObjectLookAt = null;
    }

    public void Slowmotion(float timescale, float slowduration)
    {
        TimeSet(timescale);
        SlowTimeDuration = slowduration;
        slowtimeTemp = Time.realtimeSinceStartup;
    }

    public void SlowmotionOnce(float timescale, float slowduration)
    {
        if (timeScaleTarget != timescale)
        {
            TimeSet(timescale);
            SlowTimeDuration = slowduration;
            slowtimeTemp = Time.realtimeSinceStartup;
        }
    }

    public void SlowmotionNow(float timescale, float slowduration)
    {
        TimeSet(timescale);
        Time.timeScale = timescale;
        SlowTimeDuration = slowduration;
        slowtimeTemp = Time.realtimeSinceStartup;
    }

    public void TimeSet(float scale)
    {
        timeSetByASK = true;
        timeScaleTarget = scale;
    }

    public void TimeSetNow(float scale)
    {
        timeSetByASK = true;
        timeScaleTarget = scale;
        Time.timeScale = scale;
    }

    private void setTarget()
    {
        InAction = true;
        CameraActive();
        this.GetComponent<Camera>().enabled = true;
        if (this.GetComponent<Camera>().gameObject.GetComponent<AudioListener>())
            this.GetComponent<Camera>().gameObject.GetComponent<AudioListener>().enabled = true;
    }

    private Camera[] cams;

    public void CameraActive()
    {
        if (!cameraTemp)
        {
            cams = (Camera[])GameObject.FindObjectsOfType(typeof(Camera));
            audiolistenerEnabledTemp = new bool[cams.Length];
            cameraEnabledTemp = new bool[cams.Length];
            for (int i = 0; i < cams.Length; i++)
            {
                if (cams[i].gameObject.layer != 5)
                {
                    cameraEnabledTemp[i] = cams[i].enabled;

                    if (cams[i].gameObject.GetComponent<AudioListener>())
                    {
                        audiolistenerEnabledTemp[i] = cams[i].gameObject.GetComponent<AudioListener>().enabled;
                    }

                    cams[i].enabled = false;
                    if (cams[i].gameObject.GetComponent<AudioListener>())
                    {
                        cams[i].gameObject.GetComponent<AudioListener>().enabled = false;
                    }
                }
            }

            cameraTemp = true;
        }
    }

    public void CameraRestore()
    {
        if (cameraTemp)
        {
            cameraTemp = false;
            cams = (Camera[])GameObject.FindObjectsOfType(typeof(Camera));
            if (cameraEnabledTemp != null && cams != null)
            {
                if (cams.Length > 0 && cameraEnabledTemp.Length > 0 && cameraEnabledTemp.Length == cams.Length)
                {
                    for (int i = 0; i < cams.Length; i++)
                    {
                        if (cams[i].gameObject.layer != 5)
                        {
                            cams[i].enabled = cameraEnabledTemp[i];
                            if (cams[i].gameObject.GetComponent<AudioListener>())
                            {
                                cams[i].gameObject.GetComponent<AudioListener>().enabled = audiolistenerEnabledTemp[i];
                            }
                        }
                    }
                }
            }
        }

        this.transform.position = initialPositionTmp;
        this.transform.rotation = initialRotationTmp;
    }

    public void ClearTarget()
    {
        Follow = false;
        InAction = false;
        HitTarget = false;
        Detected = false;
        ObjectFollowing = null;
        ObjectLookAt = null;
        ObjectLookAtRoot = null;
        lengthCast = 0;
        LengthMult = 1;
        FOVspeed = 1;
        LookAtOffset = Vector3.zero;
        CameraOffset = Vector3.zero;
        ResetFOV();
        CameraRestore();
    }

    public void ClearTime()
    {
        Time.timeScale = 1;
        timeScaleTarget = 1;
    }

    public void ResetFOV()
    {
        FOVTarget = fovTemp;
        FOVspeed = 1;
    }

    Vector3 Direction(Vector3 point1, Vector3 point2)
    {
        return (point1 - point2).normalized;
    }

    [HideInInspector] public Vector3 CameraOffset;
    [HideInInspector] public Vector3 cameraPosition, LookAtOffset, lookAtPosition;
    [HideInInspector] public bool CollisionEnabled = true;

    void CameraUpdate()
    {
        if (ObjectLookAt != null)
        {
            lookAtPosition = ObjectLookAt.transform.position;
        }

        if (onWallCollision)
        {
            this.transform.position = (lookAtPosition + ((-this.transform.forward) * lengthCast));
            cameraPosition = this.transform.position;
        }
        else
        {
            this.transform.position = Vector3.Lerp(this.transform.position,
                cameraPosition + ((-this.transform.forward) * BaseDistance), Time.fixedUnscaledDeltaTime * 30);
        }

        if (MainCamera)
        {
            MainCamera.fieldOfView =
                Mathf.Lerp(MainCamera.fieldOfView, FOVTarget, Time.fixedUnscaledDeltaTime * 30 * FOVspeed);
        }

        gameObject.transform.LookAt(lookAtPosition + LookAtOffset);

        if (Follow)
        {
            cameraPosition = (lookAtPosition + ((-this.transform.forward) * lengthCast)) + CameraOffset;
        }

        lengthCast = (Length * LengthMult);
    }

    [HideInInspector] public bool onWallCollision = false;

    void CameraCollision()
    {
        onWallCollision = false;
        Vector3 lookatpoint = lookAtPosition + LookAtOffset;
        float distance = Vector3.Distance(lookatpoint, this.transform.position);
        float range = distance + ColliderOffset;
        Vector3 lookbackdir = (lookatpoint - this.transform.position).normalized;
        //Vector3 endpoint = lookatpoint - (lookbackdir * range);

        if (distance <= 0)
            distance = 0.01f;

        if (distance > lengthCast)
            distance = lengthCast;

        if (InAction)
        {
            RaycastHit hit;

            if (Physics.Raycast(lookatpoint, -lookbackdir, out hit, range))
            {
                if (hit.collider.gameObject != this.gameObject && hit.collider.gameObject.layer != IgnoreCameraLayer &&
                    !hit.collider.GetComponent<BulletHiter>())
                {
                    lengthCast = hit.distance;
                    onWallCollision = true;
                }
            }
        }

        if (!CollisionEnabled)
            onWallCollision = false;
    }

    [HideInInspector] public float FOVspeed;

    public void SetFOV(float target, bool blend, float speed)
    {
        FOVspeed = speed;
        FOVTarget = target;
        if (!blend)
        {
            if (MainCamera)
                MainCamera.fieldOfView = target;
        }
    }

    public void SetPosition(Vector3 position, bool blend)
    {
        cameraPosition = position;

        if (!blend)
        {
            this.transform.position = cameraPosition;
        }
    }


    public void SetPositionDistance(Vector3 position, bool blend)
    {
        cameraPosition = position + ((-this.transform.forward + CameraOffset) * lengthCast);
        if (!blend)
        {
            this.transform.position = cameraPosition;
        }
    }

    void FixedUpdate()
    {
        cameraUpdate();
        cameraCollision();
    }

    void LateUpdate()
    {
        if (TargetShouldHit && CurrentBullet && !HitTarget && InAction)
        {
            Vector3 dirTarget = (TargetShouldHit.transform.position - CurrentBullet.transform.position).normalized;
            Vector3 dirFirstDetect = (PositionFirstDetect - TargetShouldHit.transform.position).normalized;

            if (Vector3.Dot(dirTarget, dirFirstDetect) > 0)
            {
                ClearTarget();
                ClearTime();
            }
        }
    }

    public bool HaveAnotherTimeSytem;

    void TimeUpdate()
    {
        if (timeSetByASK || !HaveAnotherTimeSytem)
        {
            Time.timeScale = Mathf.Lerp(Time.timeScale, timeScaleTarget, Time.unscaledDeltaTime * 30);
            Time.fixedDeltaTime = (initialFixedTimeStep * Time.timeScale);

            if (Time.realtimeSinceStartup >= (slowtimeTemp + SlowTimeDuration))
            {
                TimeSet(1);
                timeSetByASK = false;
            }
        }
    }

    void Update()
    {
        TimeUpdate();

        if (Time.realtimeSinceStartup >= timeTemp + TimeDuration)
        {
            InAction = true;
            ClearTarget();
        }
        else
        {
            InAction = false;
        }

        if (cameraTemp)
        {
            for (int i = 0; i < cams.Length; i++)
            {
                if (cams[i] != this.GetComponent<Camera>() && cams[i].gameObject.layer != 5)
                {
                    cams[i].enabled = false;
                }
            }
        }

        if (MainCamera && SpotLight)
        {
            SpotLight.enabled = MainCamera.enabled;
        }
    }
}