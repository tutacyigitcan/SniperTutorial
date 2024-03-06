using UnityEngine;
using System.Collections;

public class ActionPreset : MonoBehaviour
{
	[HideInInspector]
	public ActionCamera ActionCam;
	public float BaseDistance;
	
	public void Start ()
	{
		Initialize ();
	}

	public void Initialize ()
	{
		ActionCam = (ActionCamera)FindObjectOfType (typeof(ActionCamera));
	}
	public virtual void Shoot (GameObject bullet)
	{
		if (!ActionCam) {
			return;	
		}
		ActionCam.BaseDistance = BaseDistance;
		ActionCam.CurrentBullet = bullet;
	}
	public virtual void FirstDetected (Bullet bullet, BulletHiter target, Vector3 point)
	{
		if (!ActionCam) {
			return;	
		}
		ActionCam.BaseDistance = BaseDistance;
		ActionCam.Detected = true;
		ActionCam.ObjectLookAtRoot = target.RootObject;
		ActionCam.TargetShouldHit = target.gameObject;
		ActionCam.PositionFirstDetect = bullet.transform.position;
	}
	public virtual void TargetDetected (Bullet bullet, BulletHiter target, Vector3 point)
	{
		if (!ActionCam) {
			return;	
		}
		ActionCam.BaseDistance = BaseDistance;
		ActionCam.Detected = true;
		ActionCam.ObjectLookAtRoot = target.RootObject;
		ActionCam.TargetShouldHit = target.gameObject;
		ActionCam.PositionFirstDetect = bullet.transform.position;
	}
	public virtual void TargetHited (Bullet bullet, BulletHiter target, Vector3 point)
	{
		if (!ActionCam) {
			return;	
		}
		ActionCam.BaseDistance = BaseDistance;
		ActionCam.HitTarget = true;
		ActionCam.ObjectLookAtRoot = target.RootObject;
		ActionCam.PositionHit = point;
	}
	
	public virtual void OnBulletDestroy ()
	{
		
		if (!ActionCam) {
			return;	
		}
		ActionCam.BaseDistance = BaseDistance;
		if (!ActionCam.HitTarget) {
			ActionCam.ClearTarget ();
			ActionCam.TimeSet (1);
		}
	}
	
	
	public RaycastHit PositionOnTerrain (Vector3 position)
	{
		RaycastHit res = new RaycastHit ();
		res.point = position;
		if (GameObject.FindObjectOfType (typeof(Terrain))) {
			Terrain terrain = (Terrain)GameObject.FindObjectOfType (typeof(Terrain));
			if (terrain) {
				RaycastHit hit;
				if (Physics.Raycast (position, -Vector3.up, out hit)) {
					res = hit;
				}
			} else {
				Debug.Log ("No Terrain");	
			}	
		}
		return res;
	}

	public Vector3 TerrainFloor (Vector3 position)
	{
		Vector3 res = position;
		RaycastHit positionSpawn = PositionOnTerrain (position + (Vector3.up * 100));
		if (positionSpawn.point.y > position.y) {
			res = new Vector3 (position.x, positionSpawn.point.y + 1, position.z);
		}
		return res;
	}
	
	public Vector3 GetRandomArea(Vector3 position,float size){
		return position + new Vector3(Random.Range(-size,size),0,Random.Range(-size,size));	
	}
	
}
