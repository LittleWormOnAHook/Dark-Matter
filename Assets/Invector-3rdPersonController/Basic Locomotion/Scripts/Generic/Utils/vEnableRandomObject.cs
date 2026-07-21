using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class vEnableRandomObject : MonoBehaviour
{
    public GameObject[] objects;
    public bool enableOnStart;

    System.Random random;
    static int seedCounter;
    // Start is called before the first frame update
    protected void Awake()
    {
        int seed = unchecked(System.Environment.TickCount * 397 ^ ++seedCounter);
        random = new System.Random(seed);
        if (enableOnStart)
            EnableObject();
    }

    public virtual void EnableObject()
    {
        int indexToEnable = random.Next(0, objects.Length );
        for (int i=0;i<objects.Length;i++)
        {
            objects[i].SetActive(i == indexToEnable);
        }
    }
   
}
