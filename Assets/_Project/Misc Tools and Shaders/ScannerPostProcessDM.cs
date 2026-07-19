using UnityEngine;

public class ScannerPostProcess : MonoBehaviour
{
    public Material scanlinesMaterial;
    public float scanSpeed = 2f;
    public float lineThickness = 2f;
    public Color scanColor = new Color(0, 1, 1, 0.3f);

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (scanlinesMaterial == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        scanlinesMaterial.SetFloat("_ScanSpeed", scanSpeed);
        scanlinesMaterial.SetFloat("_LineThickness", lineThickness);
        scanlinesMaterial.SetColor("_ScanColor", scanColor);
        scanlinesMaterial.SetFloat("_TimeOffset", Time.time);

        Graphics.Blit(source, destination, scanlinesMaterial);
    }
}
