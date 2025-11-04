using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkOwnershipHandler))]
[RequireComponent(typeof(PhotonView))]
public class ControlableOBJ : MonoBehaviourPun, IPunObservable, IControllable
{
    [Header("Follow Settings")]
    public float followForce = 150f;
    public float followDrag = 2f;

    [Header("Net Smoothing")]
    public float positionLerp = 15f;
    public float rotationLerp = 15f;

    [Header("Line Renderer")]
    public Material mat;
    private Color color;

    [Header("Impact Settings")]
    public bool canBreak;
    public float maxResistance = 100f;
    public float breakThreshold = 15f;
    public GameObject breakParticlePrefab;

    private float currentResistance;
    private Rigidbody rb;
    private NetworkOwnershipHandler ownership;
    private Transform holdPoint;
    private bool isHeldLocal = false;

    private Vector3 netPos;
    private Quaternion netRot;
    private Vector3 netVel;

    private LineRenderer lineRenderer;
    private Transform lineStartPoint;
    private bool hasLine = false;

    private Transform requestedHoldPoint;
    private int requestedPlayerViewID = -1;

    private float lineProgress = 0f;
    public float lineAppearSpeed = 3f;
    private bool isBroken = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ownership = GetComponent<NetworkOwnershipHandler>();

        var pv = photonView;
        if (pv.ObservedComponents == null) pv.ObservedComponents = new List<Component>();
        if (!pv.ObservedComponents.Contains(this)) pv.ObservedComponents.Add(this);

        pv.OwnershipTransfer = OwnershipOption.Request;
        ownership.OnOwnerChanged += OnOwnerChanged;

        currentResistance = maxResistance;
    }

    void Start()
    {
        ApplyAuthorityPhysics();
        netPos = transform.position;
        netRot = transform.rotation;

        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.material = mat;
            lineRenderer.startWidth = 0.01f;
            lineRenderer.endWidth = 0.09f;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.enabled = false;
        }
    }

    void FixedUpdate()
    {
        if (ownership.IsMine && isHeldLocal && holdPoint != null)
        {
            Vector3 dir = (holdPoint.position - rb.position);
            Vector3 vel = rb.linearVelocity;
            Vector3 force = dir * followForce - vel * 5f;
            rb.AddForce(force, ForceMode.Acceleration);
            rb.linearDamping = followDrag;
        }
    }

    void Update()
    {
        if (!ownership.IsMine)
        {
            transform.position = Vector3.Lerp(transform.position, netPos, Time.deltaTime * positionLerp);
            transform.rotation = Quaternion.Lerp(transform.rotation, netRot, Time.deltaTime * rotationLerp);
        }
    }

    void LateUpdate()
    {
        if (holdPoint != null && lineStartPoint != null && hasLine)
            lineProgress = Mathf.MoveTowards(lineProgress, 1f, Time.deltaTime * lineAppearSpeed);
        else
            lineProgress = Mathf.MoveTowards(lineProgress, 0f, Time.deltaTime * lineAppearSpeed);

        if (lineProgress > 0f)
        {
            lineRenderer.enabled = true;
            UpdateLineBezier(lineProgress);
        }
        else
        {
            lineRenderer.enabled = false;
        }
    }

    private void UpdateLineBezier(float progress)
    {
        const int resolution = 20;
        lineRenderer.positionCount = resolution;

        Vector3 p0 = lineStartPoint != null ? lineStartPoint.position : transform.position;
        Vector3 p1 = holdPoint != null ? holdPoint.position : transform.position;
        Vector3 p2 = transform.position;

        for (int i = 0; i < resolution; i++)
        {
            float t = i / (resolution - 1f);
            t *= progress;
            Vector3 point = Mathf.Pow(1 - t, 2) * p0 +
                            2 * (1 - t) * t * p1 +
                            Mathf.Pow(t, 2) * p2;
            lineRenderer.SetPosition(i, point);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!ownership.IsMine || !canBreak) return;

        float impact = collision.relativeVelocity.magnitude;

        if (impact >= breakThreshold)
        {
            currentResistance -= impact;
            if (currentResistance <= 0f)
            {
                photonView.RPC("RPC_BreakObject", RpcTarget.AllBuffered);
            }
        }
    }

    [PunRPC]
    void RPC_BreakObject()
    {
        if (isBroken) return;
        isBroken = true;

        if (breakParticlePrefab != null)
        {
            GameObject particle = Instantiate(breakParticlePrefab, transform.position, Quaternion.identity);
            Destroy(particle, 1f);
        }

        if (photonView.IsMine)
            PhotonNetwork.Instantiate("BreakTriggerPrefab", transform.position, Quaternion.identity);

        if (photonView.IsMine)
            PhotonNetwork.Destroy(gameObject);
    }

    public void RequestPickUp(Transform newHoldPoint)
    {
        if (newHoldPoint == null) return;

        int playerViewID = newHoldPoint.root.GetComponent<PhotonView>().ViewID;

        if (ownership.IsMine)
        {
            FinalizeLocalPickup(newHoldPoint, playerViewID);
            return;
        }

        requestedHoldPoint = newHoldPoint;
        requestedPlayerViewID = playerViewID;

        var currentOwner = photonView.Owner;
        if (currentOwner != null && currentOwner.ActorNumber != 0)
        {
            photonView.RPC("RPC_RequestOwnership", currentOwner, PhotonNetwork.LocalPlayer.ActorNumber);
        }
        else
        {
            photonView.RPC("RPC_RequestOwnership", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }

    private void FinalizeLocalPickup(Transform newHoldPoint, int playerViewID)
    {
        AttachLocal(newHoldPoint);
        photonView.RPC("RPC_SetLineStart", RpcTarget.All, playerViewID);
        NotifyHolderAcquiredByViewID(playerViewID);
        requestedHoldPoint = null;
        requestedPlayerViewID = -1;
    }

    [PunRPC]
    private void RPC_RequestOwnership(int requestingActor, PhotonMessageInfo info)
    {
        if (!photonView.IsMine && !PhotonNetwork.IsMasterClient) return;
        photonView.TransferOwnership(requestingActor);
    }

    public void Drop()
    {
        if (!ownership.IsMine) return;

        isHeldLocal = false;
        holdPoint = null;
        rb.linearDamping = 0f;

        photonView.RPC("RPC_ClearLine", RpcTarget.All);

        if (photonView.Owner != null)
        {
            photonView.RPC("RPC_OnDroppedObject", photonView.Owner, photonView.ViewID);
        }
    }

    private void AttachLocal(Transform newHoldPoint)
    {
        holdPoint = newHoldPoint;
        isHeldLocal = true;
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.linearDamping = followDrag;
    }

    private void OnOwnerChanged(Player newOwner)
    {
        ApplyAuthorityPhysics();

        if (ownership.IsMine && requestedHoldPoint != null)
        {
            FinalizeLocalPickup(requestedHoldPoint, requestedPlayerViewID);
        }

        if (!ownership.IsMine)
        {
            isHeldLocal = false;
        }
    }

    private void ApplyAuthorityPhysics()
    {
        if (ownership.IsMine)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
        else
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void NotifyHolderAcquiredByViewID(int playerViewID)
    {
        PhotonView pv = PhotonView.Find(playerViewID);
        if (pv != null)
        {
            pv.RPC("RPC_OnHoldingObject", pv.Owner, photonView.ViewID);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(rb.linearVelocity);
            stream.SendNext(isHeldLocal);
        }
        else
        {
            netPos = (Vector3)stream.ReceiveNext();
            netRot = (Quaternion)stream.ReceiveNext();
            netVel = (Vector3)stream.ReceiveNext();
            isHeldLocal = (bool)stream.ReceiveNext();
        }
    }

    [PunRPC]
    private void RPC_SetLineStart(int playerViewID)
    {
        PhotonView pv = PhotonView.Find(playerViewID);
        if (pv != null)
        {
            var player = pv.GetComponent<PlayerControlOBJs>();
            if (player != null)
            {
                lineStartPoint = player.lineStartPoint;
                holdPoint = player.holdPoint;
                hasLine = true;
            }
        }
    }

    [PunRPC]
    private void RPC_ClearLine()
    {
        hasLine = false;
    }

    [PunRPC]
    private void RPC_OnDroppedObject(int objectViewID, PhotonMessageInfo info)
    {
        PhotonView pv = PhotonView.Find(objectViewID);
        if (pv != null)
        {
            var player = pv.GetComponent<PlayerControlOBJs>();
            if (player != null)
            {
                player.ClearHeld();
            }
        }
    }
}