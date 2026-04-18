using UnityEngine;
using UnityEngine.AI;

namespace CampusRPG.AI
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class EnemyMotor : MonoBehaviour
    {
        private NavMeshAgent agent;
        private Vector3 fallbackTargetPosition;
        private float fallbackMoveSpeed = 3.5f;
        private bool isFallbackMoving;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            if (agent != null && agent.isOnNavMesh)
            {
                return;
            }

            if (!isFallbackMoving)
            {
                return;
            }

            Vector3 targetPosition = fallbackTargetPosition;
            targetPosition.y = transform.position.y;
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                fallbackMoveSpeed * Time.deltaTime);
        }

        public void SetMoveSpeed(float speed)
        {
            fallbackMoveSpeed = Mathf.Max(0f, speed);

            if (agent != null)
            {
                agent.speed = fallbackMoveSpeed;
            }
        }

        public void MoveTo(Vector3 targetPosition)
        {
            fallbackTargetPosition = targetPosition;

            if (agent == null || !agent.isOnNavMesh)
            {
                isFallbackMoving = true;
                return;
            }

            isFallbackMoving = false;
            agent.isStopped = false;
            agent.SetDestination(targetPosition);
        }

        public void Stop()
        {
            isFallbackMoving = false;

            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
        }

        public void AdvanceTowardsTarget(Transform target, float distance, float minimumDistance = 0.15f)
        {
            if (target == null || distance <= 0f)
            {
                return;
            }

            Vector3 direction = target.position - transform.position;
            direction.y = 0f;
            float distanceToTarget = direction.magnitude;

            if (distanceToTarget <= Mathf.Epsilon || distanceToTarget <= minimumDistance)
            {
                return;
            }

            float moveDistance = Mathf.Min(distance, Mathf.Max(0f, distanceToTarget - minimumDistance));

            if (moveDistance <= Mathf.Epsilon)
            {
                return;
            }

            Vector3 normalizedDirection = direction / distanceToTarget;
            transform.rotation = Quaternion.LookRotation(normalizedDirection, Vector3.up);
            MoveInstantly(transform.position + (normalizedDirection * moveDistance));
        }

        public void FaceTarget(Transform target, float turnSpeed = 360f)
        {
            if (target == null)
            {
                return;
            }

            Vector3 direction = target.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        private void MoveInstantly(Vector3 targetPosition)
        {
            isFallbackMoving = false;
            targetPosition.y = transform.position.y;

            if (agent != null && agent.isOnNavMesh)
            {
                float sampleRadius = Mathf.Max(0.25f, Vector3.Distance(transform.position, targetPosition) + 0.1f);

                if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, sampleRadius, agent.areaMask))
                {
                    agent.Warp(hit.position);
                    agent.isStopped = true;
                    return;
                }
            }

            transform.position = targetPosition;
        }
    }
}
