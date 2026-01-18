using UnityEngine;
using PetGrooming.AI;

namespace PetGrooming.Systems.Skills
{
    /// <summary>
    /// 捕获网技能的投射物组件。
    /// 处理移动以及与宠物的碰撞检测。
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CaptureNetProjectile : MonoBehaviour
    {
        #region Private Fields
        private CaptureNetSkill _ownerSkill;
        private Vector3 _direction;
        private float _speed;
        private float _maxRange;
        private Vector3 _startPosition;
        private bool _hasHit;
        private Rigidbody _rigidbody;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody != null)
            {
                _rigidbody.useGravity = false;
                _rigidbody.isKinematic = false;
                _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
        }

        private void Update()
        {
            if (_hasHit) return;
            
            // 检查是否超过了最大范围
            float distanceTraveled = Vector3.Distance(_startPosition, transform.position);
            if (distanceTraveled >= _maxRange)
            {
                DestroyProjectile();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasHit) return;
            
            HandleCollision(other.gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_hasHit) return;
            
            HandleCollision(collision.gameObject);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 使用移动参数初始化投射物。
        /// </summary>
        /// <param name="ownerSkill">创建该投射物的技能</param>
        /// <param name="direction">飞行方向</param>
        /// <param name="speed">投射物速度</param>
        /// <param name="maxRange">最大飞行距离</param>
        public void Initialize(CaptureNetSkill ownerSkill, Vector3 direction, float speed, float maxRange)
        {
            _ownerSkill = ownerSkill;
            _direction = direction.normalized;
            _speed = speed;
            _maxRange = maxRange;
            _startPosition = transform.position;
            _hasHit = false;
            
            // 设置速度
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = _direction * _speed;
            }
        }
        #endregion

        #region Private Methods
        private void HandleCollision(GameObject hitObject)
        {
            // 尝试在撞击物体上寻找 PetAI
            PetAI pet = hitObject.GetComponent<PetAI>();
            if (pet == null)
            {
                pet = hitObject.GetComponentInParent<PetAI>();
            }
            
            if (pet != null)
            {
                _hasHit = true;
                
                // 通知技能发生了撞击
                if (_ownerSkill != null)
                {
                    _ownerSkill.OnProjectileHit(pet);
                }
                
                DestroyProjectile();
            }
            else
            {
                // 撞击到了其他物体（墙壁、障碍物） - 销毁投射物
                DestroyProjectile();
            }
        }

        private void DestroyProjectile()
        {
            // 在销毁前可以添加视觉效果
            Destroy(gameObject);
        }
        #endregion
    }
}
