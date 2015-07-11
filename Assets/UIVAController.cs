using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using UIVA;
using System.Net.Sockets;
using System.Net;
using System;
using System.IO;

public class UIVAController : MonoBehaviour {

	// Use this for initialization
	UIVA_Client client;
	double x,y,z,qx,qy,qz,qw;

	//[DllImport ("UIVA")]
	//private static extern float GetKinectJointData(int which, int joint, ref double[] positions, ref double[] quaternions);
	
	int which;
	
	double[] pos = new double[3];
	double[] quat = new double[4];


	void Start () {

		client = new UIVA_Client ("127.0.0.1");

	
	}
	
	// Update is called once per frame
	void Update () {
		client.GetNaturalPointData (1 ,out pos[0], out pos[1], out pos[2], out quat[0], out quat[1], out quat[2], out quat[3]);

		transform.rotation = new Quaternion ((float)quat [0], (float)quat [1], (float)quat [2], (float)quat [3]);
		/*Debug.Log ("X= "+pos[0]);
		Debug.Log ("Y= "+pos[1]);
		Debug.Log ("Z= "+pos[2]);
		Debug.Log ("qX= "+quat[0]);
		Debug.Log ("qY= "+quat[1]);
		Debug.Log ("qZ= "+quat[2]);
		Debug.Log ("QW= "+quat[3]);*/

		Debug.Log ("X= "+transform.rotation.eulerAngles.x);
		Debug.Log ("Y= "+transform.rotation.eulerAngles.y);
		Debug.Log ("Z= "+transform.rotation.eulerAngles.z);
	}
}
