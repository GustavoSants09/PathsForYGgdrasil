using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Rigidbody))]
public class PickupObject : MonoBehaviourPun, IPunObservable
{
    public bool isHeld = false;
    bool isLocked = false;

    Vector3 holdOffset = new Vector3(0, 1.5f, 2f);
    Transform holdPoint;
    Rigidbody rb;
    Camera mainCamera;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (photonView.IsMine)
        {
            FirstPersonController[] players = FindObjectsOfType<FirstPersonController>();
            foreach (var player in players)
            {
                if (player.ph.IsMine)
                {
                    mainCamera = player.playerCamera;
                    break;
                }
            }
        }
    }

    public void SetCamera(Camera camera)
    {
        mainCamera = camera;
    }

    void Update()
    {
        if (isHeld && photonView.IsMine && !isLocked)
        {
            if (mainCamera == null) return;

            if (holdPoint == null)
            {
                holdPoint = new GameObject("HoldPoint").transform;
                holdPoint.SetParent(mainCamera.transform);
                holdPoint.localPosition = holdOffset;
            }

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.MovePosition(holdPoint.position);
            rb.MoveRotation(Quaternion.identity);

            if (Input.GetMouseButtonUp(0))
            {
                isHeld = false;
                rb.useGravity = true;

                if (holdPoint != null)
                {
                    Destroy(holdPoint.gameObject);
                    holdPoint = null;
                }
            }
        }
    }

    void OnMouseDown()
    {
        if (isLocked) return;

        if (!photonView.IsMine)
        {
            photonView.RequestOwnership();
        }

        isHeld = true;
        rb.useGravity = false;
    }

    public void LockObject(Vector3 basePosition)
    {
        isLocked = true;
        isHeld = false;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.isKinematic = true;
        transform.position = basePosition;

        if (photonView.IsMine)
        {
            photonView.RPC(nameof(RPC_Lock), RpcTarget.AllBuffered, basePosition);
        }
    }

    [PunRPC]
    void RPC_Lock(Vector3 position)
    {
        isLocked = true;
        isHeld = false;

        rb.isKinematic = true;
        rb.useGravity = false;
        transform.position = position;
        transform.rotation = Quaternion.identity;
        if (holdPoint != null) Destroy(holdPoint.gameObject);

        Debug.Log($"✅ [RPC_Lock] {gameObject.name} foi travada.");
    }

    public bool IsLocked()
    {
        return isLocked;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(isHeld);
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            isHeld = (bool)stream.ReceiveNext();
            Vector3 pos = (Vector3)stream.ReceiveNext();
            Quaternion rot = (Quaternion)stream.ReceiveNext();

            if (!isHeld && !isLocked)
            {
                transform.position = Vector3.Lerp(transform.position, pos, Time.deltaTime * 10f);
                transform.rotation = Quaternion.Lerp(transform.rotation, rot, Time.deltaTime * 10f);
            }
        }
    }
}