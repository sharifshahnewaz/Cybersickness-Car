﻿using UnityEngine;
using System.Collections;

public class CAmericaAppearance_2 : MonoBehaviour {
	
	// Use this for initialization
	void Start () {
		gameObject.renderer.enabled = false;
	}
	
	// Update is called once per frame
	void Update () {
		if ((Time.time > 150) && (Time.time <460)) {
			gameObject.renderer.enabled = true;
		} else
			gameObject.renderer.enabled = false;
	}
}