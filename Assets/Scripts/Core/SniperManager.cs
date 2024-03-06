using UnityEngine;
using System.Collections;

public class SniperManager : MonoBehaviour
{

	void Awake ()
	{
		SniperKit.Environment = (Environment)GameObject.FindObjectOfType (typeof(Environment));	
	}

	void Start ()
	{
	
	}
}

public static class SniperKit
{
	public static Environment Environment;
	public static ActionCamera ActionCam;
}

