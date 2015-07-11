using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Net;
using System;
using System.IO;

// This class is repsonsible for controlling inputs to the car.
// Change this code to implement other input types, such as support for analogue input, or AI cars.
[RequireComponent (typeof (Drivetrain))]
public class CarController : MonoBehaviour {

	// Add all wheels of the car here, so brake and steering forces can be applied to them.
	public Wheel[] wheels;
	
	// A transform object which marks the car's center of gravity.
	// Cars with a higher CoG tend to tilt more in corners.
	// The further the CoG is towards the rear of the car, the more the car tends to oversteer. 
	// If this is not set, the center of mass is calculated from the colliders.
	public Transform centerOfMass;

	// A factor applied to the car's inertia tensor. 
	// Unity calculates the inertia tensor based on the car's collider shape.
	// This factor lets you scale the tensor, in order to make the car more or less dynamic.
	// A higher inertia makes the car change direction slower, which can make it easier to respond to.
	public float inertiaFactor = 1.5f;
	
	// current input state
	float brake;
	float throttle;
	float throttleInput;
	float steering;
	float lastShiftTime = -1;
	float handbrake;
		
	// cached Drivetrain reference
	Drivetrain drivetrain;
	
	// How long the car takes to shift gears
	public float shiftSpeed = 0.8f;
	

	// These values determine how fast throttle value is changed when the accelerate keys are pressed or released.
	// Getting these right is important to make the car controllable, as keyboard input does not allow analogue input.
	// There are different values for when the wheels have full traction and when there are spinning, to implement 
	// traction control schemes.
		
	// How long it takes to fully engage the throttle
	public float throttleTime = 1.0f;
	// How long it takes to fully engage the throttle 
	// when the wheels are spinning (and traction control is disabled)
	public float throttleTimeTraction = 10.0f;
	// How long it takes to fully release the throttle
	public float throttleReleaseTime = 0.5f;
	// How long it takes to fully release the throttle 
	// when the wheels are spinning.
	public float throttleReleaseTimeTraction = 0.1f;

	// Turn traction control on or off
	public bool tractionControl = false;
	
	
	// These values determine how fast steering value is changed when the steering keys are pressed or released.
	// Getting these right is important to make the car controllable, as keyboard input does not allow analogue input.
	
	// How long it takes to fully turn the steering wheel from center to full lock
	public float steerTime = 1.2f;
	// This is added to steerTime per m/s of velocity, so steering is slower when the car is moving faster.
	public float veloSteerTime = 0.1f;

	// How long it takes to fully turn the steering wheel from full lock to center
	public float steerReleaseTime = 0.6f;
	// This is added to steerReleaseTime per m/s of velocity, so steering is slower when the car is moving faster.
	public float veloSteerReleaseTime = 0f;
	// When detecting a situation where the player tries to counter steer to correct an oversteer situation,
	// steering speed will be multiplied by the difference between optimal and current steering times this 
	// factor, to make the correction easier.
	public float steerCorrectionFactor = 4.0f;
	
	public GUISkin skin;

	// Used by SoundController to get average slip velo of all wheels for skid sounds.
	public float slipVelo {
		get {
			float val = 0.0f;
			foreach(Wheel w in wheels)
				val += w.slipVelo / wheels.Length;
			return val;
		}
	}
	


	int which;
	
	double[] pos_LH = new double[3];
	double[] quat_LH = new double[4];
	
	double[] pos_RH = new double[3];
	double[] quat_RH = new double[4];
            
	double[] checkPoint_LHip = new double[5];
	double[] checkPoint_RHip = new double[5];
	double[] checkPoint_L = new double[5];
	double[] checkPoint_R = new double[5];
	
	//log data
	double[] pos_bak = new double[3];
	double[] quat_bak = new double[4];

	bool top = false, mid = false, bottom = false, half_crouch = false;

	int count = 0;
	bool begin = false, ready = false;
	
	DateTime startTime = System.DateTime.Now, endTime = System.DateTime.Now;
	DateTime startTime_good = System.DateTime.Now, endTime_good = System.DateTime.Now;
	TimeSpan ts , ts_good;
	double freq = 0, freq_thro = 0, goodTime = 0, prev_goodTime = 0, total_goodTime = 0, goodRate = 0, total_time = 630.0, time_left = 630.0;
	bool startFlag = true, write_result_flag = false;
	double time_diff;
	double total_dist = 0, loop_dist = 0;
	bool accelKey = false;
	bool brakeKey = false;
	double prev_pos = 10000;
	string body_status = "Keep";
	
	double[,] trackRecords;
	long record_count = 1;
	long track_count = 0;
	long tc = 0;
	
	long searchPoint = -1;
	long record_amount = 0;

	//versions control
	bool record = false, playback = true;
	bool act_Freq = false, act_Stab = false;
	bool time_version = false, ready_flag = false;

	// Initialize
	void Start () 
	{

		//read in initial data
		string InitName = "initData.txt";
		StreamReader init_reader = null;
		try
		{
			init_reader = new StreamReader(InitName);
			for(string line = init_reader.ReadLine(); line != null; line = init_reader.ReadLine())
			{
				if(line != "" && line != "\n")
				{
					string[]	a = line.Split(',');
					checkPoint_L[0] = Convert.ToDouble(a[0]);
					checkPoint_L[2] = Convert.ToDouble(a[1]);
					checkPoint_L[4] = Convert.ToDouble(a[2]);
				}
				//Console.WriteLine(line);
			}
		}
		catch(IOException e)
		{
			Console.WriteLine(e.Message);
		}
		finally
		{
			if(init_reader!=null)
			init_reader.Close();
		} 
			
		//read in recorded data
		string AmountName = "trackAmount.txt";
		if(record)
			File.Delete(AmountName);
		if(playback)
		{
			StreamReader reader = null;
			try
			{
				reader = new StreamReader(AmountName);
				for(string line = reader.ReadLine(); line != null; line = reader.ReadLine())
				{
					if(line != "" && line != "\n")
					{
						record_amount = Convert.ToInt64(line);
					}
					//Console.WriteLine(line);
				}
			}
			catch(IOException e)
			{
				Console.WriteLine(e.Message);
			}
			finally
			{
				if(reader!=null)
				reader.Close();
			} 
			//print(record_amount);
		}
		
		
		string FileName = "CarTrack.txt";
		if(record)
			File.Delete(FileName);
		if(playback)
		{
			StreamReader reader_data = null;
			trackRecords = new double[8, record_amount];
			try
			{
				reader_data = new StreamReader(FileName);
				for(string line = reader_data.ReadLine(); line != null; line = reader_data.ReadLine())
				{
					if(line != "" && line != "\n")
					{
						string[]	a = line.Split(',');
						for (int i = 0; i < 8; i++)
						{
							trackRecords[i, track_count] = Convert.ToDouble(a[i]);
						}
						//print(trackRecords[6, track_count]);
						track_count++;
						
					}
					//Console.WriteLine(line);
				}
			}
			catch(IOException e)
			{
				Console.WriteLine(e.Message);
			}
			finally
			{
				if(reader_data!=null)
				reader_data.Close();
			} 
		}
		
		if (centerOfMass != null)
		rigidbody.centerOfMass = centerOfMass.localPosition;
		rigidbody.inertiaTensor *= inertiaFactor;
		drivetrain = GetComponent (typeof (Drivetrain)) as Drivetrain;
		
		checkPoint_LHip[0] = checkPoint_RHip[0] = 1000;
		checkPoint_LHip[4] = checkPoint_RHip[4] = -1000; 
		
		/*checkPoint_L[0] = -0.5550;
		checkPoint_L[2] = -0.2761;
		checkPoint_L[4] = 0.0028;*/
		

	}
	

	void Update () 
	{
		// Steering
		Vector3 carDir = transform.forward;
		float fVelo = rigidbody.velocity.magnitude;
		Vector3 veloDir = rigidbody.velocity * (1/fVelo);
		float angle = -Mathf.Asin(Mathf.Clamp( Vector3.Cross(veloDir, carDir).y, -1, 1));
		float optimalSteering = angle / (wheels[0].maxSteeringAngle * Mathf.Deg2Rad);
		if (fVelo < 1)
			optimalSteering = 0;
				
		float steerInput = 0;
		

		if(startFlag && playback)
		{
			searchPoint= GetDistanceIndex(searchPoint, record_amount);
			//print(searchPoint);
			//print(loop_dist);
			if(searchPoint < 7847)
			{
				//print(tc);
				transform.rotation = new Quaternion((float)trackRecords[0, searchPoint], (float)trackRecords[1, searchPoint], (float)trackRecords[2, searchPoint], (float)trackRecords[3, searchPoint]);
				double p2p = Math.Sqrt(Math.Pow((double)transform.position[0] - trackRecords[4, searchPoint], 2) + Math.Pow((double)transform.position[1] - trackRecords[5, searchPoint], 2) + Math.Pow((double)transform.position[2] - trackRecords[6, searchPoint], 2));
				if(throttle < 1 && p2p > 20)
				{
					transform.position = new Vector3((float)trackRecords[4, searchPoint], (float)trackRecords[5, searchPoint], (float)trackRecords[6, searchPoint]);
				}
				else
				{
					transform.position = new Vector3((float)trackRecords[4, searchPoint], (float)trackRecords[5, searchPoint], (float)trackRecords[6, searchPoint]);
				}
				//tc++;
			}
			else
			{
				searchPoint = 20;
				loop_dist = 0;
				transform.rotation = new Quaternion((float)trackRecords[0, searchPoint], (float)trackRecords[1, searchPoint], (float)trackRecords[2, searchPoint], (float)trackRecords[3, searchPoint]);
				transform.position = new Vector3((float)trackRecords[4, searchPoint], (float)trackRecords[5, searchPoint], (float)trackRecords[6, searchPoint]);
			}
			//searchPoint ++;
			//print(trackRecords[4, tc]);
			//print(total_dist);
			//print(total_dist - trackRecords[4, tc]);
			/*if(Math.Abs(total_dist - trackRecords[7, tc]) < 0.001)
			{
				print(tc);
				transform.rotation = new Quaternion((float)trackRecords[0, tc], (float)trackRecords[1, tc], (float)trackRecords[2, tc], (float)trackRecords[3, tc]);
				transform.position = new Vector3((float)trackRecords[4, tc], (float)trackRecords[5, tc], (float)trackRecords[6, tc]);
				tc++;
			}*/
			
			/*if(Math.Abs(transform.position[0] - trackRecords[4, tc]) < 0.001 && Math.Abs(transform.position[1] - trackRecords[5, tc]) < 0.001 && Math.Abs(transform.position[2] - trackRecords[6, tc]) < 0.001)
			{
				transform.position = new Vector3 ((float)trackRecords[4, tc], (float)trackRecords[5, tc], (float)trackRecords[6, tc]);
				print(tc);
				transform.rotation = new Quaternion((float)trackRecords[0, tc], (float)trackRecords[1, tc], (float)trackRecords[2, tc], (float)trackRecords[3, tc]);
				tc++;
			}*/
		}
		
		if (Input.GetKey (KeyCode.H) && !startFlag)
		{
			startTime = System.DateTime.Now;
			startFlag = true;
			ready_flag = false;
		}
		else if (startFlag)
		{
			endTime = System.DateTime.Now;
			ts = endTime - startTime;
			time_diff= ts.TotalSeconds;
			time_left = total_time - time_diff;
			//print(rigidbody.velocity.magnitude);
			if(time_left > 0)
				if(rigidbody.velocity.magnitude > 0.04)
				{
					total_dist += rigidbody.velocity.magnitude * 3.6f * 0.91f *0.001f;
					loop_dist += rigidbody.velocity.magnitude * 3.6f * 0.91f *0.001f;
				}
		}
		
		if (Input.GetKey (KeyCode.J) && startFlag && !time_version)
		{
			time_left = 0;
		}
		
		if(Input.GetKey (KeyCode.R) && startFlag && time_left > 0)
		{
			transform.rotation = new Quaternion((float)trackRecords[0, searchPoint], (float)trackRecords[1, searchPoint], (float)trackRecords[2, searchPoint], (float)trackRecords[3, searchPoint]);
			transform.position = new Vector3((float)trackRecords[4, searchPoint], (float)trackRecords[5, searchPoint], (float)trackRecords[6, searchPoint]);
		}
		
		if (Input.GetKey (KeyCode.LeftArrow))
			steerInput = -1;
		if (Input.GetKey (KeyCode.RightArrow))
			steerInput = 1;
		
		//steering = (float)(quat_T[1] *-1.5);

		if (steerInput < steering)
		{
			float steerSpeed = (steering>0)?(1/(steerReleaseTime+veloSteerReleaseTime*fVelo)) :(1/(steerTime+veloSteerTime*fVelo));
			if (steering > optimalSteering)
				steerSpeed *= 1 + (steering-optimalSteering) * steerCorrectionFactor;
			steering -= steerSpeed * Time.deltaTime;
			if (steerInput > steering)
				steering = steerInput;
		}
		else if (steerInput > steering)
		{
			float steerSpeed = (steering<0)?(1/(steerReleaseTime+veloSteerReleaseTime*fVelo)) :(1/(steerTime+veloSteerTime*fVelo));
			if (steering < optimalSteering)
				steerSpeed *= 1 + (optimalSteering-steering) * steerCorrectionFactor;
			steering += steerSpeed * Time.deltaTime;
			if (steerInput < steering)
				steering = steerInput;
		}
		
		// Throttle/Brake
		
		if(!act_Freq && !act_Stab)
		{		
			accelKey = true;
			brakeKey = Input.GetKey (KeyCode.DownArrow);
		}
		/*bool acc = (diff_pos_LW > 0.007 || diff_pos_LW < -0.007) || (diff_pos_RW > 0.007 || diff_pos_RW < -0.007);
		print(acc);
		if(acc)
		{
			brakeKey = false;
			accelKey = true;
		}
		else
		{
			accelKey = false;
			brakeKey = true;
		}
		*/
		
		if (drivetrain.automatic && drivetrain.gear == 0)
		{
			accelKey = Input.GetKey (KeyCode.DownArrow);
			brakeKey = Input.GetKey (KeyCode.UpArrow);
			
			/*if((pos_LW[1]<0.1 && pos_RW[1] < 0.1))
			{
				brakeKey = false;
				accelKey = true;
			}
			else
			{
				accelKey = false;
				brakeKey = true;
			}*/
		}
		
		//print(brakeKey);
		
		if (Input.GetKey (KeyCode.LeftShift))
		{
			throttle += Time.deltaTime / throttleTime;
			throttleInput += Time.deltaTime / throttleTime;
		}
		else if (accelKey)
		{
			if (drivetrain.slipRatio < 0.10f)
				throttle += Time.deltaTime / throttleTime;
			else if (!tractionControl)
				throttle += Time.deltaTime / throttleTimeTraction;
			else
				throttle -= Time.deltaTime / throttleReleaseTime;

			if (throttleInput < 0)
				throttleInput = 0;
			throttleInput += Time.deltaTime / throttleTime;
			brake = 0;
		}
		else 
		{
			if (drivetrain.slipRatio < 0.2f)
				throttle -= Time.deltaTime / throttleReleaseTime;
			else
				throttle -= Time.deltaTime / throttleReleaseTimeTraction;
		}
		throttle = Mathf.Clamp01 (throttle);
		if(act_Freq)
		{
			throttle = (float)freq_thro/40;
			throttleInput = 1;
		}

		if (brakeKey)
		{
			if (drivetrain.slipRatio < 0.2f)
				brake += Time.deltaTime / throttleTime;
			else
				brake += Time.deltaTime / throttleTimeTraction;
			throttle = 0;
			throttleInput -= Time.deltaTime / throttleTime;
		}
		else 
		{
			if (drivetrain.slipRatio < 0.2f)
				brake -= Time.deltaTime / throttleReleaseTime;
			else
				brake -= Time.deltaTime / throttleReleaseTimeTraction;
		}
		brake = Mathf.Clamp01 (brake);
		throttleInput = Mathf.Clamp (throttleInput, -1, 1);
		//throttleInput = throttleInput * 0.01f;
				
		// Handbrake
		handbrake = Mathf.Clamp01 ( handbrake + (Input.GetKey (KeyCode.Space)? Time.deltaTime: -Time.deltaTime) );
		
		// Gear shifting
		float shiftThrottleFactor = Mathf.Clamp01((Time.time - lastShiftTime)/shiftSpeed);
		drivetrain.throttle = throttle * shiftThrottleFactor;
		drivetrain.throttleInput = throttleInput;
		
		if(Input.GetKeyDown(KeyCode.A))
		{
			lastShiftTime = Time.time;
			drivetrain.ShiftUp ();
		}
		if(Input.GetKeyDown(KeyCode.Z))
		{
			lastShiftTime = Time.time;
			drivetrain.ShiftDown ();
		}
		
		// Apply inputs
		foreach(Wheel w in wheels)
		{
			w.brake = brake;
			w.handbrake = handbrake;
			w.steering = steering;
		}
		
		//print(transform.rotation);

	}
	
	void RecordTrack(StreamWriter rw, double qx, double qy, double qz, double qw, double x, double y, double z)
	{
		if(total_dist>0)
		{
			rw.WriteLine(qx + ", " + qy + ", " + qz + ", " + qw + ", " + x + ", " + y + ", " + z + ", " + total_dist);   
			record_count++;
		}
		//rw.WriteLine(qx + ", " + qy + ", " + qz + ", " + qw + ", " + x + ", " + y + ", " + z);   
		
	}
	
	long GetDistanceIndex(long startPoint, long endPoint)
	{
		long index = (endPoint - startPoint) / 2;
		double difference = 1000;
		for ( long i = startPoint + 1; i < startPoint + 20; i++)
		{
			difference = Math.Abs(loop_dist - trackRecords[7, i]);
			if(difference < 0.05)
			{
				return i;
			}
		}
		if (startPoint >=0 )
			return startPoint;
		else
			return 0;
	}
	
	// Debug GUI. Disable when not needed.
	void OnGUI ()
	{
		if(Input.GetKeyDown(KeyCode.F))
			Screen.fullScreen = !Screen.fullScreen;
		if(Input.GetKeyDown(KeyCode.Escape))
		{
			Application.Quit();
		}
		GUI.skin = skin;
		//GUI.Label (new Rect(0,40,200,200),"mph: "+rigidbody.velocity.magnitude * 3.6f * 0.62f);
		//GUI.Label (new Rect(0,0,600,200),"Distance(ft): "+ (float)total_dist);
		//tractionControl = GUI.Toggle(new Rect(0,80,300,20), tractionControl, "Traction Control (bypassed by shift key)");
		if(time_left > 0)
		{
			//if(time_version)
				//GUI.Label (new Rect(360,0,300,200),"Time: "+(float)time_left);
			//else
				//GUI.Label (new Rect(360,0,300,200),"Time: "+(float)time_diff);
		}
		else
		{
			time_left = 0;

			//if(time_version)
				//GUI.Label (new Rect(360,0,300,200),"Time: "+ 0);
			//else
				//GUI.Label (new Rect(360,0,300,200),"Time: "+(float)time_diff);
			GUI.Label (new Rect(500,0,300,200),"Finished!");
			startFlag = false;
			throttle = 0;
			freq_thro = 0;
		}
		
		if(act_Freq)
		{
			GUI.Label (new Rect(0,0,630,200),"Total Times: "+count);
			//GUI.Label (new Rect(0,40,600,200),"Freq (millisecond/time): "+ (float)freq);
		}

		if(!act_Freq && act_Stab)
		{
			//Console.WriteLine("Total Time: {0}, Good Time: {1}, Rate: {2}", diff, total_goodTime, goodRate);
			GUI.Label (new Rect(0,0,630,200), body_status);
			//GUI.Label (new Rect(0,80,600,200),"Good Rate: "+ (float)goodRate);
		}
		
		if(ready_flag)
		{
			GUI.Label (new Rect(500,0,300,200),"Ready?");
		}
	}
}
