using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightShipMovementComp : UnitMovement
{

    public override Transform TargetBeacon
    {
        get { return target; }
        set
        {
            target = value;
            if (reachedHorizontalTarget)//or the rotation is too large
            {
                travelVelocity = 0f;
            }
            reachedHorizontalTarget = false;
            reachedVerticalTarget = false;
            targetDistance = Vector3.Distance(transform.position, TargetBeacon.transform.position);
            targetDirection = TargetBeacon.transform.position - transform.position;
            var rotationDirection = Vector3.RotateTowards(transform.forward, targetDirection, 360f, 1f);
            targetRotation = Quaternion.LookRotation(rotationDirection);

            rotationModifier = 1f;
            travelModifier = 1f;
            vertSpeedMod = 1f;
            horiSpeedMod = 1f;

            finalRotation = Quaternion.LookRotation(TargetBeacon.transform.forward);
            HasArrived.Invoke(false);
        }
    }
    private Transform target;


    private float maxAngularVelocity = 5f;

    private Rigidbody rb;

    private Quaternion targetRotation;
    private Quaternion finalRotation;
    private Vector3 targetDirection;
    private float targetDistance;
    private float travelVelocity = 0f;
    private float rotationVelocity = 0f;
    private float travelModifier = 1f;
    private float rotationModifier = 1f;
    private float distanceToTarget = 2f;
    private float angleToTarget = 2f;
    private bool reachedHorizontalTarget = false;
    private bool reachedVerticalTarget = false;
    private bool allowedToMoveH = true;

    //TODO set these using scriptabel object for each unit
    private float rotationSpeed = 0.1f;
    private float travelAngle = 15f;
    private float travelSpeed = 50f;
    private float travelDecelerationSpeed = 0.99f;
    private float travelAccelerationSpeed = 1f;

    private float vertSpeedMod = 1f;
    private float horiSpeedMod = 1f;

    private const float reachTargetAdditive = 0.3f;

    private UnitComp unitComp;

    public override void SetAllowedToArrive(bool value)
    {
        base.SetAllowedToArrive(value);
        if (value && reachedHorizontalTarget && reachedVerticalTarget)
        {
            transform.position = target.position;
        }
    }

    private void Start()
    {
        unitComp = GetComponent<UnitComp>();
    }

    private void FixedUpdate()
    {
        MoveUnitToTarget();
    }

    private void UpdateTravelVelocity()
    {
        var currentDist = Vector3.Distance(transform.position, TargetBeacon.transform.position);
        var decelDist = targetDistance * 0.2f;

        var velocityChange = VelocityCalc.GetVelocityChangeBasedOnDistFixed(
            travelVelocity,
            travelDecelerationSpeed,
            decelDist,
            travelAccelerationSpeed,
            decelDist <= currentDist);

        travelVelocity = Mathf.Clamp(travelVelocity + velocityChange, 0f, 1f);
    }

    private void UpdateRotationVelocity()
    {
        var velocityChange = VelocityCalc.GetVelocityChange(
            rotationVelocity,
            travelDecelerationSpeed,
            travelAccelerationSpeed,
            angleToTarget > 1f && distanceToTarget > 1f);

        rotationVelocity = Mathf.Clamp(rotationVelocity + velocityChange, 0f, 1f);
    }

    protected override void MoveUnitToTarget()
    {
        if (TargetBeacon == null) { return; }

        targetDistance = Vector3.Distance(transform.position, TargetBeacon.transform.position);
        angleToTarget = Vector3.Angle(transform.forward, targetDirection);

        UpdateRotationVelocity();
        targetDirection = TargetBeacon.transform.position - transform.position;

        if (targetDistance < 1f)
        {
            //TODO fix when vertical move is longer that it does not rotate correctly at the end of movement
            targetRotation = finalRotation;
        }
        else
        {
            var rotationDirection = Vector3.RotateTowards(transform.forward, targetDirection, 360f, 1f);
            targetRotation = Quaternion.LookRotation(rotationDirection);
        }


        var rotVel = travelVelocity + (rotationVelocity);
        var rotSpeed = travelSpeed * (rotationSpeed * rotationModifier);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            Time.fixedDeltaTime * travelSpeed + (rotSpeed * rotVel));

        //TODO when done rotationg to final rotation remove target
        //if (transform.rotation == finalRotation && reachedHorizontalTarget && reachedVerticalTarget)
        //{
        //    Destroy(target.gameObject);
        //}
        if (!allowedToArrive)
        {
            reachedVerticalTarget = false;
            reachedHorizontalTarget = false;
        }
        if (reachedVerticalTarget && reachedHorizontalTarget) { return; }

        //TODO might want to gradually transition between these modfier values,
        //because now it can be a bit snappy 
        if (distanceToTarget < 15f)
        {
            if (angleToTarget > travelAngle)
            {
                rotationModifier = 1.5f;
                travelModifier = 0.5f;
            }
            else
            {
                rotationModifier = 1f;
                travelModifier = 1f;
            }
        }

        UpdateTravelVelocity();

        var vPos = new Vector3(0f, transform.position.y, 0f);
        var vTargetPos = new Vector3(0f, TargetBeacon.transform.position.y, 0f);
        var verticalDistance = Vector3.Distance(vPos, vTargetPos);

        var hPos = new Vector3(transform.position.x, 0f, transform.position.z);
        var hTargetPos = new Vector3(TargetBeacon.transform.position.x, 0f, TargetBeacon.transform.position.z);
        var horizontalDistance = Vector3.Distance(hPos, hTargetPos);

        if (horizontalDistance > verticalDistance)
        { vertSpeedMod = verticalDistance / horizontalDistance; }
        else
        { horiSpeedMod = horizontalDistance / verticalDistance; }

        if (!reachedVerticalTarget)
        {
            var verticalDir = new Vector3(0f, targetDirection.y, 0f).normalized;
            var vertSpeed = (travelSpeed * (travelVelocity * travelModifier) * vertSpeedMod) * Time.fixedDeltaTime;

            if (!float.IsNaN(vertSpeed))
            {
                transform.position += verticalDir * vertSpeed;

                if (verticalDistance < vertSpeed + reachTargetAdditive)
                {
                    reachedVerticalTarget = true;
                }

            }
            else { reachedVerticalTarget = true; }
        }

        if (!reachedHorizontalTarget)
        {
            var horizontalDir = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
            var horiSpeed = (travelSpeed * (travelVelocity * travelModifier) * horiSpeedMod) * Time.fixedDeltaTime;

            if (!float.IsNaN(horiSpeed))
            {
                transform.position += horizontalDir * horiSpeed;

                if (horizontalDistance < horiSpeed + reachTargetAdditive)
                {
                    reachedHorizontalTarget = true;
                }
            }
            else { reachedHorizontalTarget = true; }


        }

        if (reachedVerticalTarget && reachedHorizontalTarget)
        {
            transform.position = target.position;
            HasArrived.Invoke(true);
        }
    }
}
