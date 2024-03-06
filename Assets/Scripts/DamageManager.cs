using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageManager : MonoBehaviour
{
    public GameObject[] deadbody;
    public float DeadBodyLifeTime = 60;
    public AudioClip[] hitsound;
    public int hp = 100;
    public int GiveScore = 10;
    private float distancedamage;
    public bool IsPlayer;

    private int deadsuffix = 0;
    private Vector3 hitvelocity;
    private GameObject hitobject;
    private Vector3 hitpoint;


    void Update()
    {
        if (hp <= 0)
        {
            Dead(deadsuffix);
        }
    }

    public virtual void AfterDead(int suffix)
    {
        ScoreManager score = (ScoreManager)GameObject.FindObjectOfType(typeof(ScoreManager));
        if (score)
        {
            score.AddScore(GiveScore, distancedamage, suffix);
        }
    }

    public virtual bool ApplyDamage(int damage, Vector3 velosity, GameObject hit, float distance)
    {
        if (hp <= 0)
        {
            return true;
        }

        hitvelocity = velosity;
        hitobject = hit;
        SendMessage("OnHit", SendMessageOptions.DontRequireReceiver);
        distancedamage = distance;
        hp -= damage;

        if (hp <= 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public virtual bool ApplyDamage(int damage, Vector3 velosity, GameObject hit, float distance, int suffix)
    {
        if (hp <= 0)
        {
            return true;
        }

        hitvelocity = velosity;
        hitobject = hit;
        deadsuffix = suffix;
        SendMessage("OnHit", SendMessageOptions.DontRequireReceiver);
        distancedamage = distance;
        hp -= damage;
        if (hp <= 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }


    public void Dead(int suffix)
    {
        if (deadbody.Length > 0)
        {
            GameObject deadReplace = (GameObject)Instantiate(deadbody[Random.Range(0, deadbody.Length)],
                this.transform.position, this.transform.rotation);
            CopyTransformsRecurse(this.transform, deadReplace);
            Destroy(deadReplace, DeadBodyLifeTime);
        }

        Destroy(this.gameObject, 1);
        this.gameObject.SetActive(false);
        if (!IsPlayer)
            AfterDead(suffix);
    }

    public void CopyTransformsRecurse(Transform src, GameObject dst)
    {
        dst.transform.rotation = src.rotation;
        dst.transform.position = src.position;

        foreach (Transform child in dst.transform)
        {
            var curSrc = src.Find(child.name);
            if (curSrc)
            {
                Transform fx = src.transform.Find("FX");
                if (fx)
                {
                    fx.parent = dst.transform;
                }

                CopyTransformsRecurse(curSrc, child.gameObject);
            }
        }

        if (hitobject != null && dst.name == hitobject.name)
        {
            if (SniperKit.ActionCam)
            {
                SniperKit.ActionCam.ObjectLookAt = dst.gameObject;
            }

            Rigidbody rig = dst.GetComponent<Rigidbody>();

            if (rig)
            {
                rig.AddForce(hitvelocity, ForceMode.Impulse);
            }
        }
    }
}