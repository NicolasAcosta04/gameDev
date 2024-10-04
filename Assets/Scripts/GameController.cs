using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public TextMeshProUGUI debugGUI;
    public Vector3 playerPos;
    public GameObject[] PickUps;
    private LineRenderer[] Lines;
    private List<int> activePickupIndices;
    private GameObject closestPickup;
    private float closestDistance;
    private int closestPickupIndex;
    public Color normalPickUpColor = Color.white;
    public Color closestPickUpColor = Color.blue;

    // Start is called before the first frame update
    void Start()
    {
        playerPos = GameObject.FindGameObjectsWithTag("Player")[0].transform.position;
        PickUps = GameObject.FindGameObjectsWithTag("PickUp");
        Lines = new LineRenderer[PickUps.Length];
        activePickupIndices = new List<int>();

        for (int i = 0; i < PickUps.Length; i++)
        {
            GameObject lineObj = new GameObject("Line_" + i);
            lineObj.transform.parent = transform;

            // Add and configure LineRenderer component
            Lines[i] = lineObj.AddComponent<LineRenderer>();
            Lines[i].SetPosition(0, playerPos);
            Lines[i].SetPosition(1, PickUps[i].transform.position);
            Lines[i].startWidth = 0.1f;
            Lines[i].endWidth = 0.1f;
            PickUps[i].GetComponent<Renderer>().material.color = normalPickUpColor;



            // Add index to active pickups list
            activePickupIndices.Add(i);
        }
    }

    private void FindClosestPickup()
    {
        closestDistance = float.MaxValue;
        closestPickupIndex = -1;

        foreach (int i in activePickupIndices)
        {
            if (PickUps[i] != null && PickUps[i].activeSelf)
            {
                float distance = Vector3.Distance(playerPos, PickUps[i].transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPickupIndex = i;
                    closestPickup = PickUps[i];
                }
            }
        }

        // Update line colors based on closest pickup
        foreach (int i in activePickupIndices)
        {
            if (Lines[i] != null)
            {
                Color pickUpColor = (i == closestPickupIndex) ? closestPickUpColor : normalPickUpColor;
                PickUps[i].GetComponent<Renderer>().material.color = pickUpColor;
            }
        }

        // Update debug text if needed
        if (debugGUI != null && closestPickup != null)
        {
            debugGUI.text = $"Closest Pickup: {closestDistance.ToString("0.00")}m away";
        }
    }

    private void FixedUpdate()
    {
        playerPos = GameObject.FindGameObjectsWithTag("Player")[0].transform.position;

        // Create a temporary list to store indices to remove
        List<int> indicesToRemove = new List<int>();

        // Update only active pickup lines
        foreach (int i in activePickupIndices)
        {
            if (PickUps[i] != null && !PickUps[i].activeSelf)
            {
                // Mark for removal
                indicesToRemove.Add(i);

                // Clean up the line
                if (Lines[i] != null)
                {
                    Destroy(Lines[i].gameObject);
                    Lines[i] = null;
                }
            }
            else if (PickUps[i] != null && Lines[i] != null)
            {
                // Update active lines
                Lines[i].SetPosition(0, playerPos);
                Lines[i].SetPosition(1, PickUps[i].transform.position);
            }
        }

        // Remove deactivated pickups from the active list
        foreach (int index in indicesToRemove)
        {
            activePickupIndices.Remove(index);
        }

        // Find closest pickup after updating positions
        FindClosestPickup();

        if (activePickupIndices.Count == 0)
        {
            debugGUI.text = "All Pick Ups have been collected";
        }
    }
}
