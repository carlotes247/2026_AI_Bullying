using UnityEngine;

namespace Unity.MLAgentsExamples
{
    public class CameraFollow : MonoBehaviour
    {
        [Tooltip("The target to follow")] public Transform target;

        [Tooltip("The time it takes to move to the new position")]
        public float smoothingTime; //The time it takes to move to the new position

        
        private Vector3 m_Offset;
        private Vector3 m_CamVelocity; //Camera's velocity (used by SmoothDamp)
        public bool m_UseFixedOffset = false;
        public Vector3 m_FixedOffset;

        // Use this for initialization
        void Start()
        {
            if (target == null) return;
            m_Offset = gameObject.transform.position - target.position;
            if (m_UseFixedOffset)
                m_Offset = m_FixedOffset;
        }

        void FixedUpdate()
        {
            if (target == null) return;
            var newPosition = new Vector3(target.position.x + m_Offset.x, transform.position.y,
                target.position.z + m_Offset.z);

            gameObject.transform.position =
                Vector3.SmoothDamp(transform.position, newPosition, ref m_CamVelocity, smoothingTime, Mathf.Infinity,
                    Time.fixedDeltaTime);
        }
    }
}
