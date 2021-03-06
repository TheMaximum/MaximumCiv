﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Class handles the camera changes caused by the mouse.
/// </summary>
public class MouseController : MonoBehaviour
{
    /// <summary>
    /// Layer identifier of tiles layer.
    /// </summary>
    public LayerMask tilesLayerId;

    /// <summary>
    /// Map of tiles.
    /// </summary>
    private HexMap map;

    /// <summary>
    /// Tile currently under the mouse.
    /// </summary>
    private Hex tileUnderMouse;

    /// <summary>
    /// Tile previously under the mouse.
    /// </summary>
    private Hex lastTileUnderMouse;

    /// <summary>
    /// Last position of the mouse.
    /// </summary>
    private Vector3 lastMousePosition;

    /// <summary>
    /// Last position of the mouse relative to the ground.
    /// </summary>
    private Vector3 lastMouseGroundPosition;

    /// <summary>
    /// Camera target offset.
    /// </summary>
    private Vector3 cameraTargetOffset;

    /// <summary>
    /// Upper limit of camera height.
    /// </summary>
    private float cameraLimitUp = 20.0f;

    /// <summary>
    /// Lower limit of camera height.
    /// </summary>
    private float cameraLimitDown = 4.0f;

    /// <summary>
    /// Drag barrier (amount of movement before mouse move is considered dragging).
    /// </summary>
    private float dragBarrier = 1.0f;

    /// <summary>
    /// Current path for UnitMovement function.
    /// </summary>
    private ArrayList unitPath;

    /// <summary>
    /// Delegate for function to be called by Update().
    /// </summary>
    private delegate void UpdateFunction();

    /// <summary>
    /// Current function being called on Update().
    /// </summary>
    private UpdateFunction updateCurrentFunction;

    /// <summary>
    /// Current highlighted tile.
    /// </summary>
    private GameObject highlightedTile;

    /// <summary>
    /// Initializes the class.
    /// </summary>
    public void Start()
    {
        updateCurrentFunction = detectUpdateMode;
        map = GameObject.FindObjectOfType<HexMap>();
    }

    /// <summary>
    /// Update function, called every frame.
    /// </summary>
    public void Update()
    {
        tileUnderMouse = mousePositionToTile(Input.mousePosition);

        if(Input.GetKeyDown(KeyCode.Escape))
        {
            cancelUpdateFunction();
        }

        if(updateCurrentFunction != null)
            updateCurrentFunction();

        // TODO: Check if over scrolling UI, then don't zoom map.
        updateScrollZoom();

        lastMousePosition = Input.mousePosition;
        lastTileUnderMouse = tileUnderMouse;
    }
    
    /// <summary>
    /// Detect changes in mouse usage and switch modes.
    /// </summary>
    private void detectUpdateMode()
    {
        if(Input.GetMouseButtonDown(0))
        {
        }
        else if(Input.GetMouseButtonUp(0))
        {
            highlightTile(tileUnderMouse);

            // TODO: Implement cycling through multiple units on same tile.
            Unit[] units = tileUnderMouse.Units();
            if(map.SelectedUnit == null)
            {
                if(units != null && units.Length > 0)
                {
                    map.SelectedUnit = units[0];
                    ArrayList selectedUnitPath = map.SelectedUnit.GetPath();
                    if(selectedUnitPath != null && selectedUnitPath[0] != null)
                    {
                        Hex[] selectedUnitPathTiles = ((Hex[])selectedUnitPath[0]);
                        if(selectedUnitPathTiles.Length > 1)
                        {
                            drawPath(map.SelectedUnit, selectedUnitPath);
                        }
                    }
                }
            }
            else
            {
                cancelUpdateFunction();
            }
        }
        else if(Input.GetMouseButtonDown(1) && map.SelectedUnit != null)
        {
            updateCurrentFunction = updateUnitMovement;
        }
        else if(Input.GetMouseButton(0) && Vector3.Distance(Input.mousePosition, lastMousePosition) > dragBarrier)
        {
            // TODO: consider adding a threshold on position change.
            updateCurrentFunction = updateCameraDrag;
            lastMouseGroundPosition = getCurrentPosition(Input.mousePosition);
            updateCurrentFunction();
        }
        else if(map.SelectedUnit != null && Input.GetMouseButton(1))
        {
            // Got unit, holding down right mouse button - unit moving mode.

        }
    }

    /// <summary>
    /// Highlight (or disable highlighting) provided tile.
    /// </summary>
    /// <param name="tile">Tile to be highlighted</param>
    private void highlightTile(Hex tile)
    {
        GameObject currentTile = map.GetGameObjectFromTile(tile);

        if(highlightedTile != null)
        {
            if(highlightedTile != currentTile)
            {
                highlightedTile.transform.GetChild(2).gameObject.SetActive(false);
                highlightedTile = currentTile;
                highlightedTile.transform.GetChild(2).gameObject.SetActive(true);
            }
            else
            {
                highlightedTile.transform.GetChild(2).gameObject.SetActive(false);
                highlightedTile = null;
            }
        }
        else
        {
            highlightedTile = currentTile;
            highlightedTile.transform.GetChild(2).gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Cancels the current update function and cleans up the UI.
    /// </summary>
    private void cancelUpdateFunction()
    {
        updateCurrentFunction = detectUpdateMode;

        // Also clean up UI if needed.
        map.SelectedUnit = null;
        clearPathUI();

        GameObject tileObject = map.GetGameObjectFromTile(tileUnderMouse);
        if(tileObject != null)
            tileObject.transform.GetChild(2).gameObject.SetActive(false);
    }

    /// <summary>
    /// Clear the path interface.
    /// </summary>
    private void clearPathUI()
    {
        if(unitPath == null)
            return;

        foreach(Hex tile in (Hex[])unitPath[0])
        {
            GameObject tileObject = map.GetGameObjectFromTile(tile);
            tileObject.transform.GetChild(2).gameObject.SetActive(false);
            tileObject.transform.GetChild(3).gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Update when moving units.
    /// </summary>
    private void updateUnitMovement()
    {
        if(Input.GetMouseButtonUp(1) || map.SelectedUnit == null)
        {
            if(map.SelectedUnit != null)
            {
                // Complete unit movement.
                map.SelectedUnit.SetPath(unitPath);
                map.SelectedUnit.ExecuteMovement();
            }

            cancelUpdateFunction();
            return;
        }

        if(unitPath == null || tileUnderMouse != lastTileUnderMouse)
        {
            clearPathUI();
            unitPath = QPath.QPath.FindPath<Hex>(map.SelectedUnit, map.SelectedUnit.Hex, tileUnderMouse, Hex.CostEstimate);
            drawPath(map.SelectedUnit, unitPath);
        }
    }

    /// <summary>
    /// Draw provided path with lines on map.
    /// </summary>
    /// <param name="path">Path to be drawn</param>
    private void drawPath(Unit unit, ArrayList path)
    {
        Hex[] pathTiles = (Hex[])path[0];

        if(pathTiles.Length < 1)
        {
            clearPathUI();
            return;
        }

        float turnMovement = (1.0f - ((float)unit.MovementRemaining / unit.Movement));
        int turn = 1;

        Dictionary<int, TextMesh> tileMeshes = new Dictionary<int, TextMesh>();

        for(int tileId = 0; tileId < pathTiles.Length; tileId++)
        {
            Hex tile = pathTiles[tileId];

            GameObject tileObject = map.GetGameObjectFromTile(tile);
            tileObject.transform.GetChild(2).gameObject.SetActive(true);

            if(tile == unit.Hex)
                continue;

            float tileMovement = ((float)tile.MovementCost / unit.Movement);
            turnMovement += tileMovement;
            if(turnMovement > 1.0f && Unit.MOVEMENT_RULES_LIKE_CIV6)
            {
                turn++;
                turnMovement = 0.0f + tileMovement;
            }

            TextMesh[] meshes = tileObject.GetComponentsInChildren<TextMesh>(true);
            tileMeshes[turn] = meshes.First(textMesh => textMesh.name == "HexMovementLabel");

            if(turnMovement > 1.0f && !Unit.MOVEMENT_RULES_LIKE_CIV6)
            {
                turn++;
                turnMovement = 0.0f + tileMovement;
            }
        }

        foreach(KeyValuePair<int, TextMesh> mesh in tileMeshes)
        {
            mesh.Value.text = string.Format("{0}", mesh.Key);
            mesh.Value.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Update when dragging the camera.
    /// </summary>
    private void updateCameraDrag() 
	{
        if(Input.GetMouseButtonUp(0))
        {
            cancelUpdateFunction();
            return;
        }

        Vector3 hitPosition = getCurrentPosition(Input.mousePosition);
        Vector3 difference = lastMouseGroundPosition - hitPosition;
        Camera.main.transform.Translate(difference, Space.World);
        lastMouseGroundPosition = hitPosition = getCurrentPosition(Input.mousePosition);
    }

    /// <summary>
    /// Update when scrolling the scrollwheel.
    /// </summary>
    private void updateScrollZoom()
    {
        Vector3 hitPosition = getCurrentPosition(Input.mousePosition);

        // Zoom to scrollwheel
        float scrollAmount = Input.GetAxis("Mouse ScrollWheel");

        // Move camera towards hitPosition
        Vector3 direction = hitPosition - Camera.main.transform.position;
        Vector3 position = Camera.main.transform.position;

        // Stop zooming out at a certain distance.
        if((position.y < (cameraLimitUp - 0.1f) && position.y > (cameraLimitDown + 0.1f)) ||
           (scrollAmount > 0 && position.y >= (cameraLimitUp - 0.1f)) ||
           (scrollAmount < 0 && position.y <= (cameraLimitDown + 0.1f)))
        {
            cameraTargetOffset += direction * scrollAmount;
        }

        Vector3 lastCameraPosition = Camera.main.transform.position;
        Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, Camera.main.transform.position + cameraTargetOffset, Time.deltaTime * 5f);
        cameraTargetOffset -= Camera.main.transform.position - lastCameraPosition;

        position = Camera.main.transform.position;

        if(position.y < cameraLimitDown)
        {
            position.y = cameraLimitDown;
        }
        else if(position.y > cameraLimitUp)
        {
            position.y = cameraLimitUp;
        }

        Camera.main.transform.position = position;

        // Change camera angle
        Camera.main.transform.rotation = Quaternion.Euler(
            Mathf.Lerp(35, 75, Camera.main.transform.position.y / cameraLimitUp),
            Camera.main.transform.rotation.eulerAngles.y,
            Camera.main.transform.rotation.eulerAngles.z
        );
    }

    /// <summary>
    /// Get current 'hit position'.
    /// </summary>
    /// <returns>Hit position</returns>
    private Vector3 getCurrentPosition(Vector3 mousePosition)
    {
        Ray mouseRay = Camera.main.ScreenPointToRay(mousePosition);
        if(mouseRay.direction.y >= 0)
        {
            return new Vector3();
        }

        float rayLength = (mouseRay.origin.y / mouseRay.direction.y);
        Vector3 hitPosition = mouseRay.origin - (mouseRay.direction * rayLength);
        return hitPosition;
    }

    /// <summary>
    /// Get tile the mouse is currently hovering over.
    /// </summary>
    /// <param name="mousePosition">Current mouse position</param>
    /// <returns>Current tile</returns>
    private Hex mousePositionToTile(Vector3 mousePosition)
    {
        Ray mouseRay = Camera.main.ScreenPointToRay(mousePosition);

        RaycastHit hitInfo;
        if(Physics.Raycast(mouseRay, out hitInfo, Mathf.Infinity, tilesLayerId))
        {
            GameObject hexObject = hitInfo.rigidbody.gameObject;
            Hex tile = map.GetTileFromGameObject(hexObject);

            return tile;
        }

        return null;
    }
}
