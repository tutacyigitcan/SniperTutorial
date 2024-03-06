using UnityEngine;
public class Hit_Normal : BulletHiter
{
	public override void OnHit (RaycastHit hit, Bullet bullet)
	{
		AddAudio (hit.point);
		base.OnHit (hit, bullet);
	}
}
