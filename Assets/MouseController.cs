using System.Collections;
using System.Collections.Generic;
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
    private Hex[] unitPath;

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
            // TODO: Implement cycling through multiple units on same tile.
            Unit[] units = tileUnderMouse.Units();
            if(units.Length > 0)
            {
                selectedUnit = units[0];
            }

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
            unitPath = QPath.QPath.FindPath<Hex>(map, selectedUnit, selectedUnit.Hex, tileUnderMouse, Hex.CostEstimate);
            drawPath(unitPath);
        }
    }

    /// <summary>
    /// Draw provided path with lines on map.
    /// </summary>
    /// <param name="path">Path to be drawn</param>
    private void drawPath(Hex[] path)
    {
        if(path.Length == 0)
        {
            lineRenderer.enabled = false;
            return;
        }

        lineRenderer.enabled = true;
        Vector3[] positions = new Vector3[path.Length];

        for(int tileNo = 0; tileNo < path.Length; tileNo++)
        {
            GameObject tileObject = map.GetGameObjectFromTile(path[tileNo]);
            positions[tileNo] = tileObject.transform.position + (Vector3.up * 0.01f);
        }

        lineRenderer.positionCount = positions.Length;
        lineRenderer.SetPositions(positions);
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
