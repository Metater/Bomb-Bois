using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    #region Fields
    // References
    [Header("General")]
    private GameManager manager;
    [SerializeField] private Player player;
    [SerializeField] private CharacterController controller;
    [Header("Transforms")]
    [SerializeField] private Transform handsTransform;
    [Header("Move")]
    [SerializeField] private float walkingSpeed;
    [SerializeField] private float runningSpeed;
    [SerializeField] private float jumpSpeed;
    [SerializeField] private float gravity;
    [SerializeField] private float moveSmoothTime = 0;
    private Vector3 moveVelocity = Vector3.zero;
    private float moveX = 0;
    private float moveY = 0;
    private float moveXSmoothVelocity = 0;
    private float moveYSmoothVelocity = 0;
    [Header("Look")]
    [SerializeField] private float lookSpeed;
    [SerializeField] private float lookXLimit;
    private float rotationX = 0;
    [Header("Velocity Calculation")]
    [SerializeField] private int velocityAveragingQueueSize;
    private Vector3 lastPosition = Vector3.zero;
    public Vector3 Velocity { get; private set; } = Vector3.zero;
    private Queue<Vector3> velocities;
    #endregion Fields

    #region Player Callbacks
    public void PlayerAwake()
    {
        manager = FindObjectOfType<GameManager>(true);

        velocities = new();
    }
    public void PlayerStart()
    {
        lastPosition = transform.position;
    }
    public void PlayerUpdate()
    {
        if (!isLocalPlayer || !manager.HasStarted)
        {
            return;
        }

        #region Update Velocity Calculation
        Vector3 rawVelocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;
        velocities.Enqueue(rawVelocity);
        while (velocities.Count > velocityAveragingQueueSize)
        {
            velocities.Dequeue();
        }
        Velocity = Vector3.zero;
        foreach (Vector3 v in velocities)
        {
            Velocity += v;
        }
        Velocity /= velocities.Count;
        #endregion Update Velocity Calculation

        #region Update Movement Speeds
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float lastMoveX = moveX;
        float lastMoveY = moveY;
        moveX = (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Vertical");
        moveY = (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Horizontal");
        moveX = Mathf.SmoothDamp(lastMoveX, moveX, ref moveXSmoothVelocity, moveSmoothTime);
        moveY = Mathf.SmoothDamp(lastMoveY, moveY, ref moveYSmoothVelocity, moveSmoothTime);
        #endregion Update Movement Speeds

        UpdateMovement();
    }
    #endregion Player Callbacks

    private void UpdateMovement()
    {
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        float moveVelocityY = moveVelocity.y;
        moveVelocity = (forward * moveX) + (right * moveY);

        if (controller.isGrounded && Input.GetButtonDown("Jump"))
            moveVelocity.y = jumpSpeed;
        else
            moveVelocity.y = moveVelocityY;

        if (!controller.isGrounded)
            moveVelocity.y -= gravity * Time.deltaTime;

        Vector3 moveDelta = moveVelocity * Time.deltaTime;
        controller.Move(moveDelta);

        rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

        Quaternion rotation = Quaternion.Euler(rotationX, 0, 0);
        Camera.main.transform.localRotation = rotation;
        handsTransform.transform.localRotation = rotation;

        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);

        // Teleport to center if you fall off of the map
        if (transform.position.y < -25)
        {
            // May need to do networkTransform.CmdTeleport instead, to avoid lerp?
            transform.position = new Vector3(0, 25, 0);
            moveVelocity.y = 0f;
        }
    }
}
