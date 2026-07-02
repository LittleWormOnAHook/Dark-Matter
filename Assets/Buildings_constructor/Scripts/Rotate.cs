using UnityEngine;
using System.Collections;

public class Rotate : MonoBehaviour {

	public float rotSpeed=3f;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		transform.Rotate(new Vector3(0,rotSpeed*Time.deltaTime,0));

	}
}
