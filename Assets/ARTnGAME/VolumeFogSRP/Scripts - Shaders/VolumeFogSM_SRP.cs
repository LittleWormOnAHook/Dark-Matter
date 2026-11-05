using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(VolumeFogSM_SRPRenderer), PostProcessEvent.AfterStack, "Custom/VolumeFogSM_SRP")]
public sealed class VolumeFogSM_SRP : PostProcessEffectSettings
{
    [Range(0f, 1f), Tooltip("SunShafts effect intensity.")]
    public FloatParameter blend = new FloatParameter { value = 0.5f };
    public ColorParameter _FogColor = new ColorParameter { value = Color.white/2 };   

    //fog params
    public TextureParameter noiseTexture = new TextureParameter {  };
    public FloatParameter _startDistance = new FloatParameter { value = 30f };
    public FloatParameter _fogHeight = new FloatParameter { value = 0.75f };
    public FloatParameter _fogDensity = new FloatParameter { value = 1f };
    public FloatParameter _cameraRoll = new FloatParameter { value = 0.0f };
    public Vector4Parameter _cameraDiff = new Vector4Parameter { value = new Vector4(0.0f,0.0f, 0.0f, 0.0f) };
    public FloatParameter _cameraTiltSign = new FloatParameter { value = 1f };
    public FloatParameter heightDensity = new FloatParameter { value = 1f };

    public FloatParameter noiseDensity = new FloatParameter { value = 1f };
    public FloatParameter noiseScale = new FloatParameter { value = 1f };  
    public FloatParameter noiseThickness = new FloatParameter { value = 1f };
    public Vector3Parameter noiseSpeed = new Vector3Parameter { value = new Vector4(1f, 1f, 1f) };
    public FloatParameter occlusionDrop = new FloatParameter { value = 1f };
    public FloatParameter occlusionExp = new FloatParameter { value = 1f };
    public IntParameter noise3D = new IntParameter { value = 0 };

    public FloatParameter startDistance = new FloatParameter { value = 1f };
    public FloatParameter luminance = new FloatParameter { value = 1f };
    public FloatParameter lumFac = new FloatParameter { value = 1f };
    public FloatParameter ScatterFac = new FloatParameter { value = 1f };
    public FloatParameter TurbFac = new FloatParameter { value = 1f };
    public FloatParameter HorizFac = new FloatParameter { value = 1f };
    public FloatParameter turbidity = new FloatParameter { value = 1f };
    public FloatParameter reileigh = new FloatParameter { value = 1f };
    public FloatParameter mieCoefficient = new FloatParameter { value = 1f };
    public FloatParameter mieDirectionalG = new FloatParameter { value = 1f };
    public FloatParameter bias = new FloatParameter { value = 1f };
    public FloatParameter contrast = new FloatParameter { value = 1f };    
    public ColorParameter TintColor = new ColorParameter { value = new Color(1,1,1,1)};
    public Vector4Parameter Sun = new Vector4Parameter { value = new Vector4(0.0f, 0.0f, 0.0f, 0.0f) };
    public BoolParameter FogSky = new BoolParameter();
    public FloatParameter ClearSkyFac = new FloatParameter { value = 1f };
    public Vector4Parameter PointL = new Vector4Parameter { value = new Vector4(0.0f, 0.0f, 0.0f, 0.0f) };
    public Vector4Parameter PointLParams = new Vector4Parameter { value = new Vector4(0.0f, 0.0f, 0.0f, 0.0f) };
}

public sealed class VolumeFogSM_SRPRenderer : PostProcessEffectRenderer<VolumeFogSM_SRP>
{    
    #region Properties    
    // Use radial distance
    [SerializeField]
    bool _useRadialDistance = false;
       
    // Fade-to-skybox flag
    [SerializeField]
    bool _fadeToSkybox= true;
    #endregion    
   
    public override void Render(PostProcessRenderContext context)
    {        
        var _material = context.propertySheets.Get(Shader.Find("Hidden/InverseProjectVFogLWRP"));        
        _material.properties.SetFloat("_DistanceOffset", settings._startDistance);
        _material.properties.SetFloat("_Height", settings._fogHeight); //v0.1                                                                      
        _material.properties.SetFloat("_cameraRoll", settings._cameraRoll);
        _material.properties.SetVector("_cameraDiff", settings._cameraDiff);
        _material.properties.SetFloat("_cameraTiltSign", settings._cameraTiltSign);

        var mode = RenderSettings.fogMode;
        if (mode == FogMode.Linear)
        {
            var start = RenderSettings.fogStartDistance;
            var end = RenderSettings.fogEndDistance;
            var invDiff = 1.0f / Mathf.Max(end - start, 1.0e-6f);
            _material.properties.SetFloat("_LinearGrad", -invDiff);
            _material.properties.SetFloat("_LinearOffs", end * invDiff);
            _material.DisableKeyword("FOG_EXP");
            _material.DisableKeyword("FOG_EXP2");
        }
        else if (mode == FogMode.Exponential)
        {
            const float coeff = 1.4426950408f; // 1/ln(2)
            var density = RenderSettings.fogDensity;
            _material.properties.SetFloat("_Density", coeff * density * settings._fogDensity);
            _material.EnableKeyword("FOG_EXP");
            _material.DisableKeyword("FOG_EXP2");
        }
        else // FogMode.ExponentialSquared
        {
            const float coeff = 1.2011224087f; // 1/sqrt(ln(2))
            var density = RenderSettings.fogDensity;
            _material.properties.SetFloat("_Density", coeff * density * settings._fogDensity);
            _material.DisableKeyword("FOG_EXP");
            _material.EnableKeyword("FOG_EXP2");
        }
        if (_useRadialDistance)
            _material.EnableKeyword("RADIAL_DIST");
        else
            _material.DisableKeyword("RADIAL_DIST");

        if (_fadeToSkybox)
        {
            _material.DisableKeyword("USE_SKYBOX");
            _material.properties.SetColor("_FogColor", settings._FogColor);// RenderSettings.fogColor);//v0.1            
        }
        else
        {
            _material.DisableKeyword("USE_SKYBOX");
            _material.properties.SetColor("_FogColor", settings._FogColor);// RenderSettings.fogColor);
        }

        //v0.1
        if (settings.noiseTexture.value == null)
        {
            settings.noiseTexture.value = new Texture2D(1280, 720);
        }
        if (_material != null && settings.noiseTexture != null)
        {
            if (settings.noiseTexture.value == null)
            {
                settings.noiseTexture.value = new Texture2D(1280, 720);
            }
            _material.properties.SetTexture("_NoiseTex", settings.noiseTexture.value);
        }
        
        // Calculate vectors towards frustum corners.
        Camera camera = Camera.main;
        var cam = camera;// GetComponent<Camera>();
        var camtr = cam.transform;        


        ////////// SCATTER
        var camPos = camtr.position;
        float FdotC = camPos.y - settings._fogHeight;
        float paramK = (FdotC <= 0.0f ? 1.0f : 0.0f);
        //_material.properties.SetMatrix("_FrustumCornersWS", frustumCorners);
        _material.properties.SetVector("_CameraWS", camPos);
        _material.properties.SetVector("_HeightParams", new Vector4(settings._fogHeight, FdotC, paramK, settings.heightDensity * 0.5f));
        _material.properties.SetVector("_DistanceParams", new Vector4(-Mathf.Max(settings.startDistance, 0.0f), 0, 0, 0));
        _material.properties.SetFloat("_NoiseDensity", settings.noiseDensity);
        _material.properties.SetFloat("_NoiseScale", settings.noiseScale);
        _material.properties.SetFloat("_NoiseThickness", settings.noiseThickness);
        _material.properties.SetVector("_NoiseSpeed", settings.noiseSpeed);
        _material.properties.SetFloat("_OcclusionDrop", settings.occlusionDrop);
        _material.properties.SetFloat("_OcclusionExp", settings.occlusionExp);
        _material.properties.SetInt("noise3D", settings.noise3D);        
        //SM v1.7
        _material.properties.SetFloat("luminance", settings.luminance);
        _material.properties.SetFloat("lumFac", settings.lumFac);
        _material.properties.SetFloat("Multiplier1", settings.ScatterFac);
        _material.properties.SetFloat("Multiplier2", settings.TurbFac);
        _material.properties.SetFloat("Multiplier3", settings.HorizFac);
        _material.properties.SetFloat("turbidity", settings.turbidity);
        _material.properties.SetFloat("reileigh", settings.reileigh);
        _material.properties.SetFloat("mieCoefficient", settings.mieCoefficient);
        _material.properties.SetFloat("mieDirectionalG", settings.mieDirectionalG);
        _material.properties.SetFloat("bias", settings.bias);
        _material.properties.SetFloat("contrast", settings.contrast);
        _material.properties.SetVector("v3LightDir", settings.Sun);//.forward);
        _material.properties.SetVector("_TintColor", new Vector4(settings.TintColor.value.r, settings.TintColor.value.g, settings.TintColor.value.b, 1));//68, 155, 345

        float Foggy = 0;
        if (settings.FogSky) //ClearSkyFac
        {
            Foggy = 1;
        }
        _material.properties.SetFloat("FogSky", Foggy);
        _material.properties.SetFloat("ClearSkyFac", settings.ClearSkyFac);
        //////// END SCATTER

        //LOCAL LIGHT
        _material.properties.SetVector("localLightPos", new Vector4(settings.PointL.value.x, settings.PointL.value.y, settings.PointL.value.z, settings.PointL.value.w));//68, 155, 345
        _material.properties.SetVector("localLightColor", new Vector4(settings.PointLParams.value.x, settings.PointLParams.value.y, settings.PointLParams.value.z, settings.PointLParams.value.w));//68, 155, 345
        //END LOCAL LIGHT

        //RENDER FINAL EFFECT
        var format = camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default; //v3.4.9
        RenderTexture tmpBuffer1 = RenderTexture.GetTemporary(context.width, context.height, 0, format);
        RenderTexture.active = tmpBuffer1;

        GL.ClearWithSkybox(false, camera);       
        context.command.BlitFullscreenTriangle(context.source, tmpBuffer1);       
        _material.properties.SetTexture("_MainTex", tmpBuffer1);

        //WORLD RECONSTRUCT        
        Matrix4x4 camToWorld = context.camera.cameraToWorldMatrix;
        _material.properties.SetMatrix("_InverseView", camToWorld); 
        
        context.command.BlitFullscreenTriangle(context.source, context.destination, _material, 0);
        RenderTexture.ReleaseTemporary(tmpBuffer1);
        //END RENDER FINAL EFFECT
    }
}