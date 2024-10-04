using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum DebugMode
{
    Normal,
    Distance,
    Vision
}

public class GameController : MonoBehaviour
{
    public TextMeshProUGUI debugGUI;
    public TextMeshProUGUI playerPosText;
    public TextMeshProUGUI playerVelocityText;
    public Vector3 playerPos;
    public GameObject[] PickUps;
    private LineRenderer[] Lines;
    private List<int> activePickupIndices;

    // Debug mode variables
    public DebugMode currentDebugMode = DebugMode.Normal;
    private LineRenderer velocityLine;
    private GameObject player;
    private Rigidbody playerRb;

    // Colors for different states
    private readonly Color normalColor = Color.white;
    private readonly Color closestColor = Color.blue;
    private readonly Color approachingColor = Color.green;

    private GameObject closestPickup;
    private float closestDistance;
    private int closestPickupIndex = -1;
    private int mostDirectlyApproachedIndex = -1;
    private int previousApproachedIndex = -1; // Track the previously approached pickup

    void Start()
    {
        // Get player references
        player = GameObject.FindGameObjectsWithTag("Player")[0];
        playerRb = player.GetComponent<Rigidbody>();
        playerPos = player.transform.position;

        // Initialize pickup arrays
        PickUps = GameObject.FindGameObjectsWithTag("PickUp");
        Lines = new LineRenderer[PickUps.Length];
        activePickupIndices = new List<int>();

        // Create pickup lines
        for (int i = 0; i < PickUps.Length; i++)
        {
            GameObject lineObj = new GameObject("Line_" + i);
            lineObj.transform.parent = transform;

            Lines[i] = lineObj.AddComponent<LineRenderer>();
            Lines[i].SetPosition(0, playerPos);
            Lines[i].SetPosition(1, PickUps[i].transform.position);
            Lines[i].startWidth = 0.1f;
            Lines[i].endWidth = 0.1f;

            activePickupIndices.Add(i);

            Lines[i].gameObject.SetActive(false);
        }

        // Create velocity line
        GameObject velocityLineObj = new GameObject("VelocityLine");
        velocityLineObj.transform.parent = transform;
        velocityLine = velocityLineObj.AddComponent<LineRenderer>();
        velocityLine.startWidth = 0.1f;
        velocityLine.endWidth = 0.1f;
        velocityLine.material = new Material(Shader.Find("Sprites/Default"));
        velocityLine.GetComponent<Renderer>().material.color = Color.red;
        velocityLine.gameObject.SetActive(false);
    }

    private void Update()
    {
        // Handle debug mode switching
        if (Input.GetKeyDown(KeyCode.Space))
        {
            DebugMode previousMode = currentDebugMode;
            currentDebugMode = (DebugMode)(((int)currentDebugMode + 1) % 3);

            // If we're switching out of Vision mode, ensure all pickups resume rotating
            if (previousMode == DebugMode.Vision)
            {
                ResumeAllPickupRotators();
            }

            UpdateDebugMode();
        }
    }

    private void FixedUpdate()
    {
        playerPos = player.transform.position;
        Vector3 playerVelocity = playerRb.velocity;

        List<int> indicesToRemove = new List<int>();

        // Update pickup lines and handle removal
        foreach (int i in activePickupIndices)
        {
            if (PickUps[i] != null && !PickUps[i].activeSelf)
            {
                indicesToRemove.Add(i);
                if (Lines[i] != null)
                {
                    Destroy(Lines[i].gameObject);
                    Lines[i] = null;
                }
            }
            else if (PickUps[i] != null && Lines[i] != null)
            {
                Lines[i].SetPosition(0, playerPos);
                Lines[i].SetPosition(1, PickUps[i].transform.position);
            }
        }

        foreach (int index in indicesToRemove)
        {
            activePickupIndices.Remove(index);
        }

        FindClosestPickup();

        if (currentDebugMode == DebugMode.Vision)
        {
            previousApproachedIndex = mostDirectlyApproachedIndex;
            FindMostDirectlyApproachedPickup(playerVelocity);
            UpdateVelocityLine(playerVelocity);

            // Handle pickup rotation states when the most directly approached pickup changes
            if (previousApproachedIndex != mostDirectlyApproachedIndex)
            {
                UpdatePickupRotatorStates();
            }
        }
        else if (previousApproachedIndex != -1)
        {
            // If we're not in Vision mode but had a previous approached pickup, ensure all pickups are rotating
            ResumeAllPickupRotators();
            previousApproachedIndex = -1;
            mostDirectlyApproachedIndex = -1;
        }

        UpdatePickupColors();
        UpdateDebugText(playerVelocity);

        if (activePickupIndices.Count == 0)
        {
            debugGUI.text = "All Pick Ups have been collected";
        }
    }

    private void UpdatePickupRotatorStates()
    {
        // Resume rotation for the previously approached pickup if it exists
        if (previousApproachedIndex != -1 && previousApproachedIndex < PickUps.Length &&
            PickUps[previousApproachedIndex] != null && PickUps[previousApproachedIndex].activeSelf)
        {
            Rotator prevRotation = PickUps[previousApproachedIndex].GetComponent<Rotator>();
            if (prevRotation != null)
            {
                prevRotation.enabled = true;
            }
        }

        // Stop rotation and orient the new most directly approached pickup
        if (mostDirectlyApproachedIndex != -1 && mostDirectlyApproachedIndex < PickUps.Length &&
            PickUps[mostDirectlyApproachedIndex] != null && PickUps[mostDirectlyApproachedIndex].activeSelf)
        {
            Rotator currentRotation = PickUps[mostDirectlyApproachedIndex].GetComponent<Rotator>();
            if (currentRotation != null)
            {
                currentRotation.enabled = false;
                PickUps[mostDirectlyApproachedIndex].transform.LookAt(player.transform);
            }
        }
    }

    private void ResumeAllPickupRotators()
    {
        foreach (int i in activePickupIndices)
        {
            if (PickUps[i] != null && PickUps[i].activeSelf)
            {
                Rotator pickupRotation = PickUps[i].GetComponent<Rotator>();
                if (pickupRotation != null)
                {
                    pickupRotation.enabled = true;
                }
            }
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
    }

    private void FindMostDirectlyApproachedPickup(Vector3 playerVelocity)
    {
        float bestAlignment = -1f;
        mostDirectlyApproachedIndex = -1;

        if (playerVelocity.magnitude < 0.1f)
        {
            if (previousApproachedIndex != -1)
            {
                // If player stops moving, resume rotation of the previous approached pickup
                ResumeAllPickupRotators();
                previousApproachedIndex = -1;
            }
            return;
        }

        foreach (int i in activePickupIndices)
        {
            if (PickUps[i] != null && PickUps[i].activeSelf)
            {
                Vector3 directionToPickup = (PickUps[i].transform.position - playerPos).normalized;
                float alignment = Vector3.Dot(playerVelocity.normalized, directionToPickup);

                if (alignment > bestAlignment)
                {
                    bestAlignment = alignment;
                    mostDirectlyApproachedIndex = i;
                }
            }
        }
    }

    private void UpdateVelocityLine(Vector3 playerVelocity)
    {
        velocityLine.SetPosition(0, playerPos);
        velocityLine.SetPosition(1, playerPos + playerVelocity);
        velocityLine.gameObject.SetActive(true);
    }

    private void UpdatePickupColors()
    {
        foreach (int i in activePickupIndices)
        {
            if (Lines[i] != null)
            {
                Color lineColor = normalColor;

                if (currentDebugMode == DebugMode.Distance && i == closestPickupIndex)
                {
                    lineColor = closestColor;
                }
                else if (currentDebugMode == DebugMode.Vision && i == mostDirectlyApproachedIndex)
                {
                    lineColor = approachingColor;
                }

                PickUps[i].GetComponent<Renderer>().material.color = lineColor;

                //Lines[i].startColor = lineColor;
                //Lines[i].endColor = lineColor;
            }
        }
    }

    private void UpdateDebugMode()
    {
        velocityLine.gameObject.SetActive(currentDebugMode == DebugMode.Vision);
        debugGUI.gameObject.SetActive(currentDebugMode != DebugMode.Normal);
        foreach (int i in activePickupIndices)
        {
            Lines[i].gameObject.SetActive(currentDebugMode == DebugMode.Distance || currentDebugMode == DebugMode.Vision);

        }
    }

    private void UpdateDebugText(Vector3 playerVelocity)
    {
        if (currentDebugMode == DebugMode.Normal)
        {
            debugGUI.text = "";
            playerPosText.text = "";
            playerVelocityText.text = "";
            return;
        }

        if (currentDebugMode == DebugMode.Distance || currentDebugMode == DebugMode.Vision)
        {
            debugGUI.text = $"Closet Pickup: {closestDistance.ToString("0.00")}m";
            playerPosText.text = $"Position: {playerPos.ToString("0.00")}";
            playerVelocityText.text = $"Velocity: {playerVelocity.magnitude.ToString("0.00")} m/s";
        }
    }
}
