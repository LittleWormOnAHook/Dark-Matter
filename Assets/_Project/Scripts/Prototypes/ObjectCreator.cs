using UnityEngine;

public class ObjectCreator : MonoBehaviour
{
    void Start()
    {
        GameObject largeBlueBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        largeBlueBox.name = "Large Blue Box";
        largeBlueBox.transform.localScale = new Vector3(5, 5, 5); // Make it larger than usual cube
        largeBlueBox.GetComponent<Renderer>().material.color = Color.blue; // Set color to blue
        largeBlueBox.AddComponent<Rigidbody>(); // Add a rigidbody for physics interaction
        largeBlueBox.AddComponent<BoxCollider>(); // Add a box collider for collisions
    }
}