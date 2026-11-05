using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[ExecuteInEditMode]
public class controlVolumeFogSRPPOSTFX : MonoBehaviour
{
    public Transform Sun;
    public Light localLightA;
    public float localLightIntensity;
    public float localLightRadius;

    PostProcessProfile postProfile;
    // Start is called before the first frame update
    void Start()
    {
        if (Application.isPlaying)
        {
            postProfile = GetComponent<PostProcessVolume>().profile;
        }
    }

    //Vector3 prevRot;

    // Update is called once per frame
    void Update()
    {
        if (!Application.isPlaying)
        {
            postProfile = GetComponent<PostProcessVolume>().sharedProfile;
        }

        var volFog = postProfile.GetSetting<VolumeFogSM_SRP>();
        if (volFog != null)
        {
            if (localLightA != null)
            {
                
                //volFog.sunTransform.value = sun.transform.position;
            }
            Camera cam = Camera.current;
            if (cam == null)
            {
                cam = Camera.main;
            }
            volFog._cameraRoll.value = cam.transform.eulerAngles.z;

            volFog._cameraDiff.value = cam.transform.eulerAngles;// - prevRot;

            if(cam.transform.eulerAngles.y > 360)
            {
                volFog._cameraDiff.value.y = cam.transform.eulerAngles.y % 360;
            }
            if (cam.transform.eulerAngles.y > 180)
            {
                volFog._cameraDiff.value.y = -(360 - volFog._cameraDiff.value.y);
            }

            //slipt in 90 degs, 90 to 180 mapped to 90 to zero
            //volFog._cameraDiff.value.w = 1;
            if (volFog._cameraDiff.value.y > 90 && volFog._cameraDiff.value.y < 180)
            {
                volFog._cameraDiff.value.y = 180 - volFog._cameraDiff.value.y;
                volFog._cameraDiff.value.w = -1;
                //volFog._cameraDiff.value.w = Mathf.Lerp(volFog._cameraDiff.value.w ,- 1, Time.deltaTime * 20);
            }
            else if (volFog._cameraDiff.value.y < -90 && volFog._cameraDiff.value.y > -180)
            {
                volFog._cameraDiff.value.y = -180 - volFog._cameraDiff.value.y;
                volFog._cameraDiff.value.w = -1;
                //volFog._cameraDiff.value.w = Mathf.Lerp(volFog._cameraDiff.value.w, -1, Time.deltaTime * 20);
                //Debug.Log("dde");
            }
            else
            {
                //volFog._cameraDiff.value.w = Mathf.Lerp(volFog._cameraDiff.value.w, 1, Time.deltaTime * 20);
                volFog._cameraDiff.value.w = 1;
            }

            //vertical fix
            if (cam.transform.eulerAngles.x > 360)
            {
                volFog._cameraDiff.value.x = cam.transform.eulerAngles.x % 360;
            }
            if (cam.transform.eulerAngles.x > 180)
            {
                volFog._cameraDiff.value.x = 360 - volFog._cameraDiff.value.x;
            }
            //Debug.Log(cam.transform.eulerAngles.x);
            if (cam.transform.eulerAngles.x > 0 && cam.transform.eulerAngles.x < 180)
            {
                volFog._cameraTiltSign.value = 1;
            }
            else
            {
               // Debug.Log(cam.transform.eulerAngles.x);
                volFog._cameraTiltSign.value = -1;
            }
            if (Sun != null)
            {
                Vector3 sunDir = Sun.transform.forward;
                sunDir = Quaternion.AngleAxis(-cam.transform.eulerAngles.y, Vector3.up) * -sunDir;
                sunDir = Quaternion.AngleAxis(cam.transform.eulerAngles.x, Vector3.left) * sunDir;
                sunDir = Quaternion.AngleAxis(-cam.transform.eulerAngles.z, Vector3.forward) * sunDir;
                // volFog.Sun.value = -new Vector4(sunDir.x, sunDir.y, sunDir.z, 1);
                volFog.Sun.value = new Vector4(sunDir.x, sunDir.y, sunDir.z, 1);
            }
            if(localLightA != null)
            {
                volFog.PointL.value = new Vector4(localLightA.transform.position.x, localLightA.transform.position.y, localLightA.transform.position.z, localLightIntensity);
                volFog.PointLParams.value = new Vector4(localLightA.color.r, localLightA.color.g, localLightA.color.b, localLightRadius);
            }
            //Debug.Log(volFog._cameraDiff.value);
            //prevRot = cam.transform.eulerAngles;
        }


        var volFogA = postProfile.GetSetting<VolumeFogSM_SRP_Before_Stack>();
        if (volFogA != null)
        {
            if (localLightA != null)
            {

                //volFogA.sunTransform.value = sun.transform.position;
            }
            Camera cam = Camera.current;
            if (cam == null)
            {
                cam = Camera.main;
            }
            volFogA._cameraRoll.value = cam.transform.eulerAngles.z;

            volFogA._cameraDiff.value = cam.transform.eulerAngles;// - prevRot;

            if (cam.transform.eulerAngles.y > 360)
            {
                volFogA._cameraDiff.value.y = cam.transform.eulerAngles.y % 360;
            }
            if (cam.transform.eulerAngles.y > 180)
            {
                volFogA._cameraDiff.value.y = -(360 - volFogA._cameraDiff.value.y);
            }

            //slipt in 90 degs, 90 to 180 mapped to 90 to zero
            //volFog._cameraDiff.value.w = 1;
            if (volFogA._cameraDiff.value.y > 90 && volFogA._cameraDiff.value.y < 180)
            {
                volFogA._cameraDiff.value.y = 180 - volFogA._cameraDiff.value.y;
                volFogA._cameraDiff.value.w = -1;
                //volFog._cameraDiff.value.w = Mathf.Lerp(volFog._cameraDiff.value.w ,- 1, Time.deltaTime * 20);
            }
            else if (volFogA._cameraDiff.value.y < -90 && volFogA._cameraDiff.value.y > -180)
            {
                volFogA._cameraDiff.value.y = -180 - volFogA._cameraDiff.value.y;
                volFogA._cameraDiff.value.w = -1;
                //volFog._cameraDiff.value.w = Mathf.Lerp(volFog._cameraDiff.value.w, -1, Time.deltaTime * 20);
                //Debug.Log("dde");
            }
            else
            {
                //volFog._cameraDiff.value.w = Mathf.Lerp(volFog._cameraDiff.value.w, 1, Time.deltaTime * 20);
                volFogA._cameraDiff.value.w = 1;
            }

            //vertical fix
            if (cam.transform.eulerAngles.x > 360)
            {
                volFogA._cameraDiff.value.x = cam.transform.eulerAngles.x % 360;
            }
            if (cam.transform.eulerAngles.x > 180)
            {
                volFogA._cameraDiff.value.x = 360 - volFogA._cameraDiff.value.x;
            }
            //Debug.Log(cam.transform.eulerAngles.x);
            if (cam.transform.eulerAngles.x > 0 && cam.transform.eulerAngles.x < 180)
            {
                volFogA._cameraTiltSign.value = 1;
            }
            else
            {
                // Debug.Log(cam.transform.eulerAngles.x);
                volFogA._cameraTiltSign.value = -1;
            }
            if (Sun != null)
            {
                Vector3 sunDir = Sun.transform.forward;
                sunDir = Quaternion.AngleAxis(-cam.transform.eulerAngles.y, Vector3.up) * -sunDir;
                sunDir = Quaternion.AngleAxis(cam.transform.eulerAngles.x, Vector3.left) * sunDir;
                sunDir = Quaternion.AngleAxis(-cam.transform.eulerAngles.z, Vector3.forward) * sunDir;
                // volFog.Sun.value = -new Vector4(sunDir.x, sunDir.y, sunDir.z, 1);
                volFogA.Sun.value = new Vector4(sunDir.x, sunDir.y, sunDir.z, 1);
            }
            if (localLightA != null)
            {
                volFogA.PointL.value = new Vector4(localLightA.transform.position.x, localLightA.transform.position.y, localLightA.transform.position.z, localLightIntensity);
                volFogA.PointLParams.value = new Vector4(localLightA.color.r, localLightA.color.g, localLightA.color.b, localLightRadius);
            }
            //Debug.Log(volFog._cameraDiff.value);
            //prevRot = cam.transform.eulerAngles;
        }


    }
}
