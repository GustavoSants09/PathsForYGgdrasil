using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Rigidbody))]
public class PickupObject : MonoBehaviourPun, IPunObservable
{
    public bool isHeld = false;
    private bool isLocked = false;

    private Vector3 holdOffset = new Vector3(0, 1.5f, 2f);
    private Transform holdPoint;
    private Rigidbody rb;
    private Camera mainCamera;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (isHeld && photonView.IsMine && !isLocked)
        {
            // Tenta encontrar a câmera se ainda não tiver
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogWarning("Camera.main ainda não encontrada. Aguardando...");
                    return; // aguarda até que ela exista
                }
            }

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
                    Debug.Log("Essa porra ta destruindo");
                }
            }

        }
    }


    private void OnMouseDown()
    {
    {
        if (isLocked) return; // objeto travado na base

        // Pede ownership se ainda não for dono
        if (!photonView.IsMine)
        {
            photonView.RequestOwnership();
        }

        isHeld = true;
        rb.useGravity = false;
    }

}

    // Chamado pela base quando for colocado corretamente
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
            photonView.RPC("RPC_Lock", RpcTarget.AllBuffered, basePosition);
        }
    }


    [PunRPC]
    void RPC_LockObject(Vector3 position)
    {
        isLocked = true;
        isHeld = false;

        rb.isKinematic = true;
        rb.useGravity = false;
        transform.position = position;
        transform.rotation = Quaternion.identity;
        if (holdPoint != null) Destroy(holdPoint.gameObject);
    }

    public bool IsLocked()
    {
        return isLocked;
    }

    // Sincroniza a posição caso esteja sendo segurado
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting && isHeld)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else if (stream.IsReading)
        {
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
