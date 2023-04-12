using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    // Start is called before the first frame update
    public PortalType Type;

    // OnCollision
        // Spawn
            // Nothing?
        // Exit
            // Win scenario
        // Teleport
            // Load scene
                
}


public enum PortalType
{
    Spawn,
    Teleport,
    Exit
}