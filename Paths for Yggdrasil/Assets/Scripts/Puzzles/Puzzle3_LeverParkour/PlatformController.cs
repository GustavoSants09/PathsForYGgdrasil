using UnityEngine;
using Photon.Pun;

namespace Yggdrasil.Puzzles.LeverParkour
{
    public class PlatformController : MonoBehaviourPunCallbacks
    {
        [Header("Platform Settings")]
        [SerializeField] private Vector3 raisedPosition;
        [SerializeField] private Vector3 loweredPosition;
        [SerializeField] private float moveSpeed = 2f;

        private Vector3 targetPosition;
        private bool isRaised = false;

        private void Start()
        {
            transform.position = loweredPosition;
            targetPosition = loweredPosition;
        }

        private void Update()
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);
        }

        public void RaisePlatform()
        {
            targetPosition = raisedPosition;
            isRaised = true;
        }

        public void LowerPlatform()
        {
            targetPosition = loweredPosition;
            isRaised = false;
        }
    }
}