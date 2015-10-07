using UnityEngine;
using System.Collections;

public class ComputerControlPuzzle : MonoBehaviour {

    public GameObject lampPlug;
    public GameObject desktopPlug;
    public GameObject monitorPlug;

    public GameObject lamp;
    public GameObject desktop;
    public GameObject monitor;

    public bool isLampPluggedIn;
    public bool isDesktopPluggedIn;
    public bool isMonitorPluggedIn;

    public bool lampPower;
    public bool desktopPower;
    public bool monitorPower;

    public bool roomForDesktop;


    // Use this for initialization
    void Start () {

        isDesktopPluggedIn = true;
        isMonitorPluggedIn = true;
	
	}
	
	// Update is called once per frame
	void Update () {


        DesktopPower();
        LampPower();
        MonitorPower();
        PowerCheck();

	}

    void PowerCheck()
    {
        if (isLampPluggedIn && isMonitorPluggedIn == true)
        {
            roomForDesktop = false;
        }
        else roomForDesktop = true;
    }

    void MonitorPower()
    {
        if (monitorPlug.GetComponent<MouseOver>().mouseOver)
        {
            if (Input.GetButtonDown("Fire1"))
            {
                if (monitorPower == true)
                {
                    monitorPower = false;
                }
                isMonitorPluggedIn = PlugIn(isMonitorPluggedIn);
            }
        }


        if (monitor.GetComponent<MouseOver>().mouseOver == true)
        {
            if (Input.GetButtonDown("Fire1"))
            {
                monitorPower = PowerOn(isMonitorPluggedIn, monitorPower);
            }

        }
    }

    void LampPower()
    {
        if (lampPlug.GetComponent<MouseOver>().mouseOver == true)
        {
            if (Input.GetButtonDown("Fire1"))
            {
                if (lampPower == true)
                {
                    lampPower = false;
                }
                isLampPluggedIn = PlugIn(isLampPluggedIn);
            }
        }


        if (lamp.GetComponent<MouseOver>().mouseOver == true)
        {
            if (Input.GetButtonDown("Fire1"))
            {
                lampPower = PowerOn(isLampPluggedIn, lampPower);
            }

        }
    }

    void DesktopPower()
    {
        if (desktopPlug.GetComponent<MouseOver>().mouseOver && roomForDesktop == true)
        {
            if (Input.GetButtonDown("Fire1"))
            {
                if (desktopPower == true)
                {
                    desktopPower = false;
                }
                isDesktopPluggedIn = PlugIn(isDesktopPluggedIn);
            }
        }


        if (desktop.GetComponent<MouseOver>().mouseOver == true)
        {
            if (Input.GetButtonDown("Fire1"))
            {
                desktopPower = PowerOn(isDesktopPluggedIn, desktopPower);
            }

        }
    }

    bool PowerOn(bool x, bool y)
    {
        if (y == true)
        {
            return !x;
        }
        else
        {
            return x;
        }
    }

    bool PlugIn(bool x)
    {
        return !x;
    }
    
}

