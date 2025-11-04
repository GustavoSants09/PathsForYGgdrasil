using UnityEngine;
using Photon.Pun;

public class PlayerControlOBJs : MonoBehaviourPun
{
    [Header("Pickup Settings")]
    public Transform holdPoint;
    public Transform pontaDaVarinha;
    public LayerMask pickupMask;
    public float pickUpRange = 3f;

    [Header("Line Settings")]
    public Transform lineStartPoint;

    [Header("Effect Settings")]
    public GameObject effectObject;
    public float scaleSpeed = 5f;

    private IControllable held;
    private Transform cameraTrans;
    private Vector3 targetScale = Vector3.zero;

    void Start()
    {
        if (effectObject != null)
            effectObject.transform.localScale = Vector3.zero;

        if (photonView.IsMine)
            cameraTrans = GetComponentInChildren<Camera>().transform;
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (held == null) TryPickUp();
            else TryDrop();
        }

        // Verifica se o objeto segurado foi destru�do
        if (held != null)
        {
            MonoBehaviour mb = held as MonoBehaviour;
            if (mb == null || mb.gameObject == null)
                held = null;
        }

        // Define o alvo da escala com base em estar segurando algo
        targetScale = held != null ? new Vector3(0.08f, 0.08f, 0.016f) : Vector3.zero;

        if (effectObject != null)
        {
            effectObject.transform.localScale = Vector3.Lerp(effectObject.transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
            effectObject.SetActive(effectObject.transform.localScale.magnitude > 0.01f);
        }
    }

    private void TryPickUp()
    {
        Ray ray = new Ray(cameraTrans.position, cameraTrans.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickUpRange))
        {
            if (((1 << hit.collider.gameObject.layer) & pickupMask) != 0)
            {
                if (hit.collider.TryGetComponent<IControllable>(out var controllable))
                {
                    controllable.RequestPickUp(holdPoint);
                }
            }
        }
    }

    private void TryDrop()
    {
        if (held == null) return;

        MonoBehaviour mb = held as MonoBehaviour;
        if (mb == null || mb.gameObject == null)
        {
            held = null;
            return;
        }

        if (mb.TryGetComponent<PhotonView>(out var pv))
        {
            if (pv != null && pv.AmOwner)
            {
                held.Drop();
                held = null;
            }
            else
            {
                held = null;
            }
        }
        else
        {
            held = null;
        }
    }

    [PunRPC]
    private void RPC_OnHoldingObject(int objectViewID)
    {
        if (!photonView.IsMine) return;

        PhotonView objPV = PhotonView.Find(objectViewID);
        if (objPV == null) return;

        if (objPV.AmOwner && objPV.TryGetComponent<IControllable>(out var controllable))
        {
            held = controllable;
        }
    }

    public void ClearHeld()
    {
        held = null;
    }
}