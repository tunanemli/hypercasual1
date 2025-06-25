using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 20f;
    [SerializeField] private float liftSpeed = 15f;
    [SerializeField] private float tiltAngle = 15f;
    [SerializeField] private float smoothness = 0.1f;
    [SerializeField] private float flyHeight = 20f;
    [SerializeField] private float heightAdjustSpeed = 5f;

    [Header("Game Settings")]
    [SerializeField] private KeyCode liftKey = KeyCode.Space;
    [SerializeField] private KeyCode bombKey = KeyCode.LeftControl;
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private float bombCooldown = 0.5f;

    [Header("Camera Settings")]
    [SerializeField] private float cameraHeight = 15f;
    [SerializeField] private float cameraDistance = 10f;
    [SerializeField] private float cameraFollowSpeed = 10f;

    [Header("Boundaries")]
    [SerializeField] private float horizontalBoundary = 40f;
    [SerializeField] private float forwardBoundary = 40f;

    private Rigidbody rb;
    private Camera mainCamera;
    private Vector2 smoothedInput;
    private Vector2 inputVelocity;
    private bool isFlying = false;
    private float nextBombTime = 0f;
    private float startHeight;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;
        startHeight = transform.position.y;
        SetupRigidbody();
        SetupCamera();
    }

    private void SetupRigidbody()
    {
        if (rb != null)
        {
            rb.useGravity = true;
            rb.drag = 1f;
            rb.angularDrag = 2f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    private void SetupCamera()
    {
        if (mainCamera != null)
        {
            mainCamera.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
        }
    }

    private void Update()
    {
        HandleInput();
        UpdateCamera();
        HandleBombing();
    }

    private void FixedUpdate()
    {
        if (rb != null)
        {
            HandleLift();
            if (isFlying)
            {
                MoveAirplane();
            }
            ClampPosition();
        }
    }

    private void HandleInput()
    {
        // Yatay ve ileri/geri hareket inputları
        float horizontal = Input.GetAxis("Horizontal"); // GetAxisRaw yerine GetAxis kullanıyoruz
        float vertical = Input.GetAxis("Vertical");     // daha yumuşak hareket için

        // Smooth input
        smoothedInput = Vector2.SmoothDamp(
            smoothedInput,
            new Vector2(horizontal, vertical),
            ref inputVelocity,
            Mathf.Max(0.001f, smoothness)
        );
    }

    private void HandleLift()
    {
        if (Input.GetKey(liftKey) && !isFlying)
        {
            rb.AddForce(Vector3.up * liftSpeed, ForceMode.Force);

            if (transform.position.y >= flyHeight)
            {
                isFlying = true;
                rb.useGravity = false;
            }
        }
        else if (isFlying)
        {
            float heightDiff = flyHeight - transform.position.y;
            Vector3 currentVelocity = rb.velocity;
            currentVelocity.y = heightDiff * heightAdjustSpeed;
            rb.velocity = currentVelocity;
        }
        else
        {
            rb.useGravity = true;
        }
    }

    private void HandleBombing()
    {
        if (!isFlying) return;

        if (Input.GetKeyDown(bombKey) && Time.time >= nextBombTime && bombPrefab != null)
        {
            GameObject bomb = Instantiate(bombPrefab, transform.position + Vector3.down * 0.5f, Quaternion.identity);
            if (bomb.TryGetComponent<Rigidbody>(out Rigidbody bombRb))
            {
                bombRb.velocity = Vector3.down * 10f;
            }
            nextBombTime = Time.time + bombCooldown;
        }
    }

    private void MoveAirplane()
    {
        // İleri/geri ve yatay hareket vektörünü oluştur
        Vector3 moveDirection = new Vector3(smoothedInput.x, 0, smoothedInput.y);

        // Hareket vektörünü kamera yönüne göre ayarla
        moveDirection = Quaternion.Euler(0, mainCamera.transform.eulerAngles.y, 0) * moveDirection;

        // Hızı uygula
        Vector3 targetVelocity = moveDirection * moveSpeed;
        targetVelocity.y = rb.velocity.y; // Dikey hızı koru

        // Yumuşak geçiş uygula
        rb.velocity = Vector3.Lerp(rb.velocity, targetVelocity, Time.fixedDeltaTime * 10f);

        // Yatay hareket için eğilme
        if (Mathf.Abs(smoothedInput.x) > 0.01f)
        {
            float targetTilt = -smoothedInput.x * tiltAngle;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.Euler(0, 0, targetTilt),
                Time.fixedDeltaTime * 5f
            );
        }
        else
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.identity,
                Time.fixedDeltaTime * 5f
            );
        }
    }

    private void UpdateCamera()
    {
        if (!mainCamera) return;

        Vector3 targetPosition = transform.position + Vector3.up * cameraHeight - Vector3.forward * cameraDistance;
        mainCamera.transform.position = Vector3.Lerp(
            mainCamera.transform.position,
            targetPosition,
            Time.deltaTime * cameraFollowSpeed
        );
    }

    private void ClampPosition()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, -horizontalBoundary, horizontalBoundary);
        pos.z = Mathf.Clamp(pos.z, -forwardBoundary, forwardBoundary);

        if (!isFlying)
        {
            pos.y = Mathf.Max(pos.y, startHeight);
        }

        transform.position = pos;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3(0, flyHeight, 0);
        Vector3 size = new Vector3(horizontalBoundary * 2, 1, forwardBoundary * 2);
        Gizmos.DrawWireCube(center, size);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(
            new Vector3(-horizontalBoundary, flyHeight, 0),
            new Vector3(horizontalBoundary, flyHeight, 0)
        );
    }
}