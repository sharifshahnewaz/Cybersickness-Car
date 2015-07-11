using UnityEngine;
using System.Collections;

public class CAmericaAppearance_1 : MonoBehaviour {
	
	// Use this for initialization
	void Start () {
		gameObject.renderer.enabled = false;
	}
	
	// Update is called once per frame
	void Update () {
		if (((Time.time > 150) && (Time.time <307)) || ((Time.time >460) && (Time.time < 610))) {
			gameObject.renderer.enabled = true;
		} else
			gameObject.renderer.enabled = false;
	}
}