using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Assertions;

public class RTSCameraBase : MonoBehaviour
{

    private float defaultArmLength = 5f;

    private Transform CameraPivotRef;
    private Transform CameraHolderRef;


    private SharedCameraVariables sharedCameraVariables;
    private SelectionCollection selectionCollection;
    private SelectionGroupOrigin currentGroupOrigin;
    //TODO make settings json or scriptable object used to set some fo these settings

    private Vector2 horizontalMoveDirection = new Vector2();
    private float verticalMoveDirection;
    private float minMoveSpeed = 15f;
    private float maxMoveSpeed = 20f;
    private float moveVelocityHorizontal = 0f;
    private bool moveAccOrDecHorizontal = false;
    private float moveVelocityVertical = 0f;
    private bool moveAccOrDecVertical = false;
    private float moveAccelerationSpeed = 2f;
    private float moveDecelerationSpeed = 3f;

    private Vector2 rotationDirection = new Vector2();
    private float rotationSpeed = 25f;
    private bool invertVerticalRot = false;
    private bool inverthorizontalRot = false;
    private bool rotationToggle = false;
    private float rotVelocity = 0f;
    private float rotAccelerationSpeed = 5f;
    private float rotDecelerationSpeed = 4.5f;
    private bool rotAccOrDec = false;


    private float zoomDirection = 0;
    private float maxZoomOut = 200f;
    private float zoomSpeed = 7f;
    private float currentZoom = 5f;
    private float zoomVelocity = 0f;
    private float zoomAccelerationSpeed = 8f;
    private float zoomDecelerationSpeed = 3f;
    private bool zoomAccOrDec = false;

    public void InputHorizontalMoveDirection(InputAction.CallbackContext context)
    {

        if (context.performed)
        {
            moveAccOrDecHorizontal = true;
            horizontalMoveDirection = context.ReadValue<Vector2>();
        }
        else if (context.canceled) { moveAccOrDecHorizontal = false; }
    }

    public void InputVerticalMoveDirection(InputAction.CallbackContext context)
    {

        if (context.performed)
        {
            moveAccOrDecVertical = true;
            verticalMoveDirection = context.ReadValue<float>();
        }
        else if (context.canceled) { moveAccOrDecVertical = false; }
    }

    public void InputRotationDirection(InputAction.CallbackContext context)
    {

        if (context.performed && rotationToggle)
        {
            rotAccOrDec = true;
            rotationDirection = context.ReadValue<Vector2>();
        }
        else if (context.canceled)
        { rotAccOrDec = false; }
    }

    public void InputRotationToggle(InputAction.CallbackContext context)
    {
        if (context.performed) { rotationToggle = true; }
        else if (context.canceled)
        {
            rotationToggle = false;
            rotAccOrDec = false;
        }
    }

    public void InputZoomDirection(InputAction.CallbackContext context)
    {
        var direction = context.ReadValue<float>();
        if (direction > 0f || direction < 0f)
        {
            zoomAccOrDec = true;
            zoomDirection = direction;
        }
        else if (direction == 0f)
        {
            zoomAccOrDec = false;
        }
    }

    public void InputAttatchToGroup(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (transform.parent)
            {
                transform.parent = null;
            }
            else if (currentGroupOrigin &&
                (selectionCollection != null && selectionCollection.SelectedEnteties.Count > 0))
            {
                transform.parent = currentGroupOrigin.transform;
                transform.localPosition = Vector3.zero;
            }
        }
    }

    private void OnValidate()
    {
        Setup();
    }

    void Start()
    {
        Setup();
        //Cursor.lockState = CursorLockMode.Confined;
        sharedCameraVariables = GetComponent<SharedCameraVariables>();
        selectionCollection = sharedCameraVariables.selectionCollection;
        currentGroupOrigin = sharedCameraVariables.currentGroupOrigin;

    }

    protected virtual void Setup()
    {

        CameraPivotRef = transform.GetChild(0);
        Assert.IsNotNull(CameraPivotRef);

        CameraHolderRef = transform.GetChild(0).GetChild(0);
        CameraHolderRef.transform.localPosition = new Vector3(0f, 0f, -defaultArmLength);
        currentZoom = defaultArmLength;
        Assert.IsNotNull(CameraHolderRef);
    }

    private void Update()
    {
        UpdateMoveVelocity();
        var currentHorizontalMoveSpeed = (minMoveSpeed * moveVelocityHorizontal) * Mathf.Sqrt(currentZoom) * Time.deltaTime;
        var currentVerticalMoveSpeed = (minMoveSpeed * moveVelocityVertical) * Mathf.Sqrt(currentZoom) * Time.deltaTime;
        var forwardMovement = transform.forward * horizontalMoveDirection.y * currentHorizontalMoveSpeed;
        var sideMovement = transform.right * horizontalMoveDirection.x * currentHorizontalMoveSpeed;
        var horizontalMovement = forwardMovement + sideMovement;
        var verticalMovement = transform.up * verticalMoveDirection * currentVerticalMoveSpeed;
        CheckStopFollowingGroup(ref horizontalMovement, ref verticalMovement);
        transform.Translate(horizontalMovement + verticalMovement, Space.World);


        UpdateRotVelocity();
        var rotSpeed = (rotationSpeed * rotVelocity) * Time.deltaTime;
        var rotX = inverthorizontalRot ? -rotationDirection.x : rotationDirection.x;
        transform.Rotate(new Vector3(0f, rotX * rotSpeed, 0f));

        if (CameraPivotRef)
        {
            var rotY = invertVerticalRot ? rotationDirection.y : -rotationDirection.y;
            var verticalRot = Quaternion.Euler(CameraPivotRef.eulerAngles.x + (rotY * rotSpeed), 0f, 0f);
            CameraPivotRef.localRotation = verticalRot;
        }


        if (CameraHolderRef)
        {
            UpdateZoomVelocity();
            var zoomChange = (zoomSpeed * zoomVelocity) * Time.deltaTime;
            currentZoom = Mathf.Clamp(Mathf.Abs(CameraHolderRef.localPosition.z) - zoomDirection * zoomChange, 1f, maxZoomOut);
            var zoomPos = new Vector3(CameraHolderRef.localPosition.x, CameraHolderRef.localPosition.y, -currentZoom);
            CameraHolderRef.localPosition = zoomPos;
        }
    }

    private void CheckStopFollowingGroup(ref Vector3 horizontalMovement, ref Vector3 verticalMovement)
    {
        if (transform.parent != null &&
            (
                (horizontalMovement.magnitude > 0f || verticalMovement.magnitude > 0f) ||
                (selectionCollection != null && selectionCollection.SelectedEnteties.Count == 0)
            )
           )
        {
            transform.parent = null;
        }
    }

    private void UpdateMoveVelocity()
    {
        var velocityChangeHorizontal = VelocityCalc.GetVelocityChange(
            moveVelocityHorizontal,
            moveDecelerationSpeed,
            moveAccelerationSpeed,
            moveAccOrDecHorizontal);

        moveVelocityHorizontal = Mathf.Clamp(moveVelocityHorizontal + velocityChangeHorizontal, 0f, 1f);


        var velocityChangeVertical = VelocityCalc.GetVelocityChange(
            moveVelocityVertical,
            moveDecelerationSpeed,
            moveAccelerationSpeed,
            moveAccOrDecVertical);

        moveVelocityVertical = Mathf.Clamp(moveVelocityVertical + velocityChangeVertical, 0f, 1f);
    }

    private void UpdateRotVelocity()
    {
        var velocityChange = VelocityCalc.GetVelocityChange(
            rotVelocity,
            rotDecelerationSpeed,
            rotAccelerationSpeed,
            rotationToggle && rotAccOrDec);

        rotVelocity = Mathf.Clamp(rotVelocity + velocityChange, 0f, 1f);
    }

    private void UpdateZoomVelocity()
    {
        var velocityChange = VelocityCalc.GetVelocityChange(
            zoomVelocity,
            zoomDecelerationSpeed,
            zoomAccelerationSpeed,
            zoomAccOrDec);

        zoomVelocity = Mathf.Clamp(zoomVelocity + velocityChange, 0f, 1f);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 1f);
        Gizmos.DrawRay(CameraHolderRef.position, CameraHolderRef.forward * 2f);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, CameraHolderRef.position);
    }
}
