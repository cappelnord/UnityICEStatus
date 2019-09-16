using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ICEStatusEventTest : MonoBehaviour
{
    public void OnICEUpdate(ICEStatusBehaviour status)
    {
        Debug.Log("OnICEUpdate");
    }

    public void OnICEArrivedAtStation(ICEStatusBehaviour status)
    {
        Debug.Log("OnICEArrivedAtStation");
    }

    public void OnICEDepartedFromStation(ICEStatusBehaviour status)
    {
        Debug.Log("OnICEDepartedFromStation");
    }

    public void OnICEStationsHaveChanged(ICEStatusBehaviour status)
    {
        Debug.Log("OnICEStationsHaveChanged");
    }
}
