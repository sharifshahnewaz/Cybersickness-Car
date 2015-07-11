using UnityEngine;
using System.Collections;

public class Appearance_SpongeBob : MonoBehaviour {

	// Use this for initialization
	void Start () {
		gameObject.renderer.enabled = false;
	}
	
	// Update is called once per frame
	void Update () {
		if (Time.time > 40 && Time.time < 200) {
						gameObject.renderer.enabled = true;
				} else
						gameObject.renderer.enabled = false;
	}
}
