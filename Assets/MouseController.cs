using System.Collections;
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
    private float cameraLimitDown = 2.0f;

    /// <summary>
    /// Drag barrier (amount of movement before mouse move is considered dragging).
    /// </summary>
    private float dragBarrier = 1.0f;

    /// <summary>
    /// Unit currently selected.
    /// </summary>
    private Unit selectedUnit = null;

    /// <summary>
    /// Current path for UnitMovement function.
    /// </summary>
    private ArrayList unitPath;

    /// <summary>
    /// Line renderer for unit paths.
    /// </summary>
    private LineRenderer lineRenderer;

    /// <summary>
    /// Delegate for function to be called by Update().
    /// </summary>
    private delegate void UpdateFunction();

    /// <summary>
    /// Current function being called on Update().
    /// </summary>
    private UpdateFunction updateCurrentFunction;

    /// <summary>
    /// Initializes the class.
    /// </summary>
    public void Start()
    {
        updateCurrentFunction = detectUpdateMode;
        map = GameObject.FindObjectOfType<HexMap>();
        lineRenderer = transform.GetComponentInChildren<LineRenderer>();
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
            GameObject tileObject = map.GetGameObjectFromTile(tileUnderMouse);
            tileObject.transform.GetChild(2).gameObject.SetActive(true);

            // TODO: Implement cycling through multiple units on same tile.
            Unit[] units = tileUnderMouse.Units();
            if(selectedUnit == null)
            {
                if(units != null && units.Length > 0)
                {
                    selectedUnit = units[0];
                    ArrayList selectedUnitPath = selectedUnit.GetPath();
                    if(selectedUnitPath != null && selectedUnitPath[0] != null)
                    {
                        Hex[] selectedUnitPathTiles = ((Hex[])selectedUnitPath[0]);
                        if(selectedUnitPathTiles.Length > 0)
                        {
                            drawPath(selectedUnitPath);
                        }
                    }
                }
            }
            else
            {
                cancelUpdateFunction();
            }
        }
        else if(Input.GetMouseButtonDown(1) && selectedUnit != null)
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
        else if(selectedUnit != null && Input.GetMouseButton(1))
        {
            // Got unit, holding down right mouse button - unit moving mode.

        }
    }

    /// <summary>
    /// Cancels the current update function and cleans up the UI.
    /// </summary>
    private void cancelUpdateFunction()
    {
        updateCurrentFunction = detectUpdateMode;

        // Also clean up UI if needed.
        selectedUnit = null;
        clearPathUI();

        GameObject tileObject = map.GetGameObjectFromTile(tileUnderMouse);
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
        if(Input.GetMouseButtonUp(1) || selectedUnit == null)
        {
            if(selectedUnit != null)
            {
                // Complete unit movement.
                selectedUnit.SetPath(unitPath);
            }

            cancelUpdateFunction();
            return;
        }

        if(unitPath == null || tileUnderMouse != lastTileUnderMouse)
        {
            clearPathUI();
            unitPath = QPath.QPath.FindPath<Hex>(map, selectedUnit, selectedUnit.Hex, tileUnderMouse, Hex.CostEstimate);
            drawPath(unitPath);
        }
    }

    /// <summary>
    /// Draw provided path with lines on map.
    /// </summary>
    /// <param name="path">Path to be drawn</param>
    private void drawPath(ArrayList path)
    {
        if(((Hex[])path[0]).Length < 2)
        {
            clearPathUI();
            return;
        }

        Hex[] pathTiles = (Hex[])path[0];
        Dictionary<Hex, float> pathCosts = (Dictionary<Hex, float>)path[1];

        float previousTileTurns = 0.0f;

        for(int tileId = 0; tileId < pathTiles.Length; tileId++)
        {
            Hex tile = pathTiles[tileId];
            GameObject tileObject = map.GetGameObjectFromTile(tile);

            if(tile != pathTiles[0])
            {
                TextMesh[] meshes = tileObject.GetComponentsInChildren<TextMesh>(true);
                TextMesh mesh = meshes.First(textMesh => textMesh.name == "HexMovementLabel");

                if(Mathf.Floor(pathCosts[tile]) > Mathf.Floor(previousTileTurns))
                {                    
                    tileObject.transform.GetChild(3).gameObject.SetActive(true);
                    mesh.text = string.Format("{0}", pathCosts[tile]);
                }
                else if(pathTiles.Length > (tileId + 1) && Mathf.Ceil(pathCosts[pathTiles[tileId + 1]]) > Mathf.Ceil(pathCosts[tile]))
                {
                    tileObject.transform.GetChild(3).gameObject.SetActive(true);
                    mesh.text = string.Format("{0}", Mathf.Ceil(pathCosts[tile]));
                }

                previousTileTurns = pathCosts[tile];
            }

            tileObject.transform.GetChild(2).gameObject.SetActive(true);
        }

        Hex lastTile = pathTiles[(pathTiles.Length - 1)];
        GameObject lastTileObject = map.GetGameObjectFromTile(lastTile);
        if(!lastTileObject.transform.GetChild(3).gameObject.activeSelf)
        {
            TextMesh[] meshes = lastTileObject.GetComponentsInChildren<TextMesh>(true);
            TextMesh mesh = meshes.First(textMesh => textMesh.name == "HexMovementLabel");
            lastTileObject.transform.GetChild(3).gameObject.SetActive(true);
            mesh.text = string.Format("{0}", Mathf.Ceil(previousTileTurns));
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
            Mathf.Lerp(30, 75, Camera.main.transform.position.y / cameraLimitUp),
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
