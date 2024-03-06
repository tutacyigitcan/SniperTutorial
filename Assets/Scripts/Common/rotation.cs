using UnityEngine;
public class Rotation : MonoBehaviour {

	public Vector3 Speed;
	void Start () {
	
	}
	void Update () {
		this.transform.Rotate(Speed);
	}
}
