using System.Collections;
using TMPro;
using UnityEngine;

public class RaycastObjectCam : MonoBehaviour
{
    [Header("Raycast Settings")]
    public bool raycastStatus = false;
    public GameObject currentHitObject;
    public float rayDistance = 10f;
    public LayerMask layerMask = -1; // All layers by default
    public CoreGameManager coreGameManager;

    public TextMeshProUGUI narratorText;
    [Header("Mobile Touch Settings")]
    [SerializeField] private bool enableTouchInteraction = true;
    [SerializeField] private float touchRadius = 50f; // Radius around screen center for touch detection
    [SerializeField] private bool showTouchDebug = false;

    private Camera playerCamera;
    private bool isHitting = false;
    private RaycastObjectBehaviour currentHitBehaviour = null;
    private Vector2 screenCenter;

    void Start()
    {
        playerCamera = GetComponent<Camera>();
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
    }

    void Update()
    {
        PerformRaycast();
        HandleInteraction();
    }

    void PerformRaycast()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rayDistance, layerMask))
        {
            RaycastObjectBehaviour objectBehaviour = hit.collider.GetComponent<RaycastObjectBehaviour>();

            if (objectBehaviour != null)
            {
                isHitting = true;

                currentHitBehaviour = objectBehaviour;
                currentHitObject = hit.collider.gameObject;

                objectBehaviour.OnRaycastHit(hit);
            }
            else
            {
                isHitting = false;
                currentHitBehaviour = null;
                currentHitObject = null; 
            }
        }
        else
        {
            isHitting = false;
            currentHitBehaviour = null;
            currentHitObject = null; 
        }
    }


    void HandleInteraction()
    {
        raycastStatus = isHitting;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = isHitting ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * rayDistance);
    }
}
