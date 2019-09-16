using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System;
using UnityEngine.Events;


[System.Serializable]
public class ICEStatus
{
    public bool connection;
    public string servicelevel;
    public string internet;
    public int speed;
    public string gpsStatus;
    public string tzn;
    public string series;
    public float latitude;
    public float longitude;
    public long serverTime;
    public string wagonClass;
    public string navigationChange;
}

[System.Serializable]
public class ICESTripInfoStopInfo
{
    public string scheduledNext;
    public string actualNext;
    public string actualLast;
    public string actualLastStarted;
    public string finalStationName;
    public string finalStationEvaNr;
}

[System.Serializable]
public class ICEGeoCoordinates
{
    public float latitude;
    public float longitude;
}

[System.Serializable]
public class ICESTripInfoStation
{
    public string evaNr;
    public string name;
    public ICEGeoCoordinates geocoordinates;
}

[System.Serializable]
public class ICESTripInfoTimetable
{
    public long scheduledArrivalTime;
    public long actualArrivalTime;
    public bool showActualArrivalTime;
    public string arrivalDelay;
    public long scheduledDepartureTime;
    public long actualDepartureTime;
    public bool showActualDepartureTime;
    public string departureDelay;
}

[System.Serializable]
public class ICESTripInfoTrack
{
    public string scheduled;
    public string actual;
}

[System.Serializable]
public class ICESTripInfoInfo
{
    public int status;
    public bool passed;
    public string positionStatus;
    public int distance;
    public int distanceFromStart;
}

[System.Serializable]
public class ICESTripInfoStop
{
    public ICESTripInfoStation station;
    public ICESTripInfoTimetable timetable;
    public ICESTripInfoTrack track;
    public ICESTripInfoInfo info;
    public string delayReasons;

}

[System.Serializable]
public class ICESTripInfoConflictInfo
{
    public string status;
    public string text;
}

[System.Serializable]
public class ICESTripInfoSelectedRoute
{
    public ICESTripInfoConflictInfo conflictInfo;
    public string mobility;
}

[System.Serializable]
public class ICETripInfoTrip
{
    public string tripDate;
    public string trainType;
    public string vzn;
    public int actualPosition;
    public int distanceFromLastStop;
    public int totalDistance;
    public ICESTripInfoStopInfo stopInfo;
    public ICESTripInfoStop[] stops;
}

[System.Serializable]
public class ICETripInfo
{
    public ICETripInfoTrip trip;
    public string connection;
    public ICESTripInfoSelectedRoute selectedRoute;
    public string active;
}

[System.Serializable]
public class ICEStatusChange : UnityEvent<ICEStatusBehaviour> {};

public class ICEStatusBehaviour : MonoBehaviour
{
    private string statusURL = "https://iceportal.de/api1/rs/status";
    private string tripInfoURL = "https://iceportal.de/api1/rs/tripInfo/trip";
    

    // These things are unpacked so that they can be accessed/used easier

    // in km/h
    public float speed;

    public float geoLatitude;
    public float geoLongitude;

    // as normalized float
    public float segmentDistanceProgress;

    // in m
    public float segmentDistance;

    // as a string
    public string nextArrivalDelay;

    public string lastStopName;
    public string nextStopName;
    public long nextStopMillisToArrival;

    public bool isStandingInStation;

    // seems convenient:
    [HideInInspector]
    public Dictionary<string, ICESTripInfoStop> stations;

    // raw data access
    public ICETripInfo tripInfo;
    public ICEStatus status;

    private UnityWebRequest statusRequest;
    private UnityWebRequest tripInfoRequest;

    public bool connected = false;

    public ICEStatusChange onICEUpdate;
    public ICEStatusChange onICEArrivedAtStation;
    public ICEStatusChange onICEDepartedFromStation;
    public ICEStatusChange onICEStationsHaveChanged;

    private float waitTime = 5.0f;



    // Start is called before the first frame update
    void Start()
    {
        if(onICEUpdate == null)
        {
            onICEUpdate = new ICEStatusChange();
        }

        if(onICEArrivedAtStation == null)
        {
            onICEArrivedAtStation = new ICEStatusChange();
        }

        if (onICEDepartedFromStation == null)
        {
            onICEDepartedFromStation = new ICEStatusChange();
        }

        if (onICEStationsHaveChanged == null)
        {
            onICEStationsHaveChanged = new ICEStatusChange();
        }


        stations = new Dictionary<string, ICESTripInfoStop>();

        // can be set to true in code for testing purposes
        bool useTestData = false;
        if(useTestData)
        {
            statusURL = Path.Combine(Application.streamingAssetsPath, "test-status.json");
            tripInfoURL = Path.Combine(Application.streamingAssetsPath, "test-tripInfo.json");
        }

        StartCoroutine(GetStatus());
        StartCoroutine(GetTripInfo());

    }

    private IEnumerator GetStatus()
    {
        // Debug.Log("GetStatus");

        statusRequest = UnityWebRequest.Get(statusURL);
        statusRequest.timeout = 3;

        yield return statusRequest.SendWebRequest();

        if (statusRequest.isNetworkError || statusRequest.isHttpError)
        {
            Debug.Log(statusRequest.error);
            connected = false;
        }
        else
        {
            try
            {
                status = JsonUtility.FromJson<ICEStatus>(statusRequest.downloadHandler.text);
                ProcessStatus();
            }
            catch
            {
                Debug.Log("Could not load ICEStatus JSON");
                connected = false;
            }
        }

        yield return new WaitForSeconds(waitTime);
        StartCoroutine(GetStatus());
    }

    private IEnumerator GetTripInfo()
    {
        // Debug.Log("GetTrip");

        tripInfoRequest = UnityWebRequest.Get(tripInfoURL);
        tripInfoRequest.timeout = 3;

        yield return tripInfoRequest.SendWebRequest();

        if (tripInfoRequest.isNetworkError || tripInfoRequest.isHttpError)
        {
            Debug.Log(tripInfoRequest.error);
            connected = false;
        }
        else
        {
            try
            {
                tripInfo = JsonUtility.FromJson<ICETripInfo>(tripInfoRequest.downloadHandler.text);
                ProcessTripInfo();
            }
            catch
            {
                Debug.Log("Could not load ICETripInfo JSON");
                connected = false;
            }
        }

        yield return new WaitForSeconds(waitTime);
        StartCoroutine(GetTripInfo());
    }

    private void ProcessStatus()
    {
        if (status.gpsStatus == "VALID")
        {
            speed = status.speed;
        }
        geoLatitude = status.latitude;
        geoLongitude = status.longitude;

        TryProcessBoth();
    }

    private void ProcessTripInfo()
    {
        stations.Clear();
        foreach(ICESTripInfoStop stop in tripInfo.trip.stops)
        {
            stations[stop.station.evaNr] = stop;
        }

        bool stationsHaveChanged = (lastStopName != null && lastStopName != "") && (lastStopName != stations[tripInfo.trip.stopInfo.actualLast].station.name);

        lastStopName = stations[tripInfo.trip.stopInfo.actualLast].station.name;
        nextStopName = stations[tripInfo.trip.stopInfo.actualNext].station.name;
        segmentDistance = stations[tripInfo.trip.stopInfo.actualNext].info.distance;
        segmentDistanceProgress = tripInfo.trip.distanceFromLastStop / (float)segmentDistance;
        nextArrivalDelay = stations[tripInfo.trip.stopInfo.actualNext].timetable.arrivalDelay;

        // argh, the speed measure is super inaccurate in some stations!
        // trying to trick here to fix some things ...

        bool newIsStanding;

        if (isStandingInStation)
        {
            newIsStanding = stations[tripInfo.trip.stopInfo.actualLast].info.positionStatus == "arrived" && speed <= 15;
        } else
        {
            newIsStanding = stations[tripInfo.trip.stopInfo.actualLast].info.positionStatus == "arrived" && speed <= 5;
        }

        bool departedFromStation = isStandingInStation && !newIsStanding;
        bool arrivedAtStation = !isStandingInStation && newIsStanding;

        isStandingInStation = newIsStanding;

        TryProcessBoth();

        try
        {
            if (stationsHaveChanged)
            {
                onICEStationsHaveChanged.Invoke(this);
            }
            if (arrivedAtStation) {
                onICEArrivedAtStation.Invoke(this);
            }
            if (departedFromStation)
            {
                onICEDepartedFromStation.Invoke(this);
            }
        } catch (Exception e)
        {
            Debug.LogException(e);
            Debug.Log("Possibly other events were not processed as well!");
        }
    }

    private void TryProcessBoth()
    {
        if(stations.Count > 0)
        {
            connected = true;
            nextStopMillisToArrival = stations[tripInfo.trip.stopInfo.actualNext].timetable.actualArrivalTime - status.serverTime;

            try
            {
                onICEUpdate.Invoke(this);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.Log("Possibly other events were not processed as well!");
            }
        }
    }
}