using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PetGrooming.Core;
using PetGrooming.AI;

namespace PetGrooming.Systems
{
    /// <summary>
    /// 第二阶段中负责多只宠物的生成与管理。
    /// 需求：1.1, 1.2
    /// </summary>
    public class PetSpawnManager : MonoBehaviour
    {
        #region Singleton
        
        private static PetSpawnManager _instance;
        
        public static PetSpawnManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<PetSpawnManager>();
                    if (_instance == null)
                    {
                        Debug.LogError("[PetSpawnManager] No PetSpawnManager instance found in scene!");
                    }
                }
                return _instance;
            }
        }
        
        #endregion

        #region Enums
        
        /// <summary>
        /// 用于决定要生成多少只宠物的游戏模式。
        /// 需求：1.1, 1.2
        /// </summary>
        public enum GameMode
        {
            TwoPets = 2,
            ThreePets = 3
        }
        
        #endregion

        #region Serialized Fields
        
        [Header("Configuration")]
        [SerializeField] private Phase2GameConfig _phase2Config;
        
        [Header("Spawn Settings")]
        [Tooltip("Current game mode determining pet count")]
        [SerializeField] private GameMode _currentMode = GameMode.TwoPets;
        
        [Tooltip("Spawn points for pets")]
        [SerializeField] private Transform[] _spawnPoints;
        
        [Header("Pet Prefabs")]
        [Tooltip("Prefab for Cat pets")]
        [SerializeField] private GameObject _catPrefab;
        
        [Tooltip("Prefab for Dog pets")]
        [SerializeField] private GameObject _dogPrefab;
        
        [Header("Play Area Bounds")]
        [SerializeField] private Vector3 _playAreaMin = new Vector3(-20f, 0f, -20f);
        [SerializeField] private Vector3 _playAreaMax = new Vector3(20f, 0f, 20f);
        
        #endregion

        #region Private Fields
        
        private List<PetAI> _activePets = new List<PetAI>();
        
        #endregion

        #region Properties
        
        /// <summary>
        /// 当前游戏模式。
        /// </summary>
        public GameMode CurrentMode => _currentMode;
        
        /// <summary>
        /// 场景中所有激活宠物的列表。
        /// </summary>
        public List<PetAI> ActivePets => _activePets;
        
        /// <summary>
        /// 尚未被美容的宠物数量。
        /// </summary>
        public int RemainingPets => _activePets.Count(p => p != null && !p.IsGroomed);
        
        /// <summary>
        /// 已生成宠物的总数。
        /// </summary>
        public int TotalPets => _activePets.Count;
        
        /// <summary>
        /// 已经被美容过的宠物数量。
        /// </summary>
        public int GroomedPets => _activePets.Count(p => p != null && p.IsGroomed);
        
        /// <summary>
        /// 是否所有宠物都已经被美容。
        /// 需求 1.7：胜利条件检查。
        /// </summary>
        public bool AllPetsGroomed => _activePets.Count > 0 && _activePets.All(p => p != null && p.IsGroomed);
        
        /// <summary>
        /// 第二阶段配置引用。
        /// </summary>
        public Phase2GameConfig Phase2Config => _phase2Config;
        
        /// <summary>
        /// 游玩区域的最小边界。
        /// </summary>
        public Vector3 PlayAreaMin => _playAreaMin;
        
        /// <summary>
        /// 游玩区域的最大边界。
        /// </summary>
        public Vector3 PlayAreaMax => _playAreaMax;
        
        #endregion

        #region Events
        
        /// <summary>
        /// 当一只宠物被生成时触发。
        /// </summary>
        public event Action<PetAI> OnPetSpawned;
        
        /// <summary>
        /// 当一只宠物完成美容时触发。
        /// </summary>
        public event Action<PetAI> OnPetGroomed;
        
        /// <summary>
        /// 当所有宠物都被美容时触发。
        /// 需求 1.7：胜利条件。
        /// </summary>
        public event Action OnAllPetsGroomed;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            // 单例初始化
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[PetSpawnManager] Duplicate PetSpawnManager detected, destroying this instance.");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            
            if (_phase2Config == null)
            {
                Debug.LogWarning("[PetSpawnManager] Phase2GameConfig is not assigned.");
            }
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// 根据当前游戏模式生成宠物。
        /// 需求：1.1, 1.2
        /// </summary>
        public void SpawnPets()
        {
            // 清除场景中已有的宠物
            ClearPets();
            
            int petCount = GetPetCountForMode(_currentMode);
            
            Debug.Log($"[PetSpawnManager] Spawning {petCount} pets for {_currentMode} mode");
            
            // 根据游戏模式设置动态恶作剧阈值
            // 属性 15：恶作剧阈值与游戏模式匹配
            // 需求 6.1, 6.2：2 宠模式 800，3 宠模式 1000
            if (MischiefSystem.Instance != null)
            {
                MischiefSystem.Instance.SetDynamicThreshold(_currentMode);
            }
            
            for (int i = 0; i < petCount; i++)
            {
                SpawnPet(i);
            }
            
            // 在 GameManager 中记录总宠物数量
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetTotalPetCount(petCount);
            }
        }
        
        /// <summary>
        /// 设置游戏模式，并可选择是否立即重新生成宠物。
        /// </summary>
        /// <param name="mode">新的游戏模式。</param>
        /// <param name="respawn">是否立刻重新生成宠物。</param>
        public void SetGameMode(GameMode mode, bool respawn = false)
        {
            _currentMode = mode;
            
            if (respawn)
            {
                SpawnPets();
            }
            
            Debug.Log($"[PetSpawnManager] Game mode set to {mode}");
        }
        
        /// <summary>
        /// 当一只宠物完成美容时调用。
        /// 需求 1.7：检查“所有宠物已被美容”胜利条件。
        /// </summary>
        /// <param name="pet">完成美容的那只宠物。</param>
        public void OnPetGroomingComplete(PetAI pet)
        {
            if (pet == null) return;
            
            pet.MarkAsGroomed();
            OnPetGroomed?.Invoke(pet);
            
            Debug.Log($"[PetSpawnManager] Pet groomed. Remaining: {RemainingPets}/{TotalPets}");
            
            // 检查胜利条件
            if (AllPetsGroomed)
            {
                Debug.Log("[PetSpawnManager] All pets groomed! Triggering victory.");
                OnAllPetsGroomed?.Invoke();
            }
        }
        
        /// <summary>
        /// 获取距离指定位置最近且未被美容、未被捕获的宠物。
        /// </summary>
        /// <param name="position">用来计算距离的参考位置。</param>
        /// <returns>最近的可用宠物，如没有则返回 null。</returns>
        public PetAI GetNearestPet(Vector3 position)
        {
            PetAI nearest = null;
            float nearestDistance = float.MaxValue;
            
            foreach (var pet in _activePets)
            {
                if (pet == null || pet.IsGroomed) continue;
                if (pet.CurrentState == PetAI.PetState.Captured || 
                    pet.CurrentState == PetAI.PetState.BeingGroomed) continue;
                
                float distance = Vector3.Distance(position, pet.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = pet;
                }
            }
            
            return nearest;
        }
        
        /// <summary>
        /// 获取所有尚未被美容的宠物。
        /// </summary>
        /// <returns>未美容宠物的列表。</returns>
        public List<PetAI> GetUngroomedPets()
        {
            return _activePets.Where(p => p != null && !p.IsGroomed).ToList();
        }
        
        /// <summary>
        /// 清除所有已生成的宠物。
        /// </summary>
        public void ClearPets()
        {
            foreach (var pet in _activePets)
            {
                if (pet != null)
                {
                    Destroy(pet.gameObject);
                }
            }
            _activePets.Clear();
        }
        
        #endregion

        #region Private Methods
        
        /// <summary>
        /// 在指定索引位置生成一只宠物。
        /// </summary>
        private void SpawnPet(int index)
        {
            // 决定宠物类型（猫狗交替生成）
            bool isCat = index % 2 == 0;
            GameObject prefab = isCat ? _catPrefab : _dogPrefab;
            
            if (prefab == null)
            {
                Debug.LogWarning($"[PetSpawnManager] {(isCat ? "Cat" : "Dog")} prefab is not assigned!");
                return;
            }
            
            // 获取生成位置
            Vector3 spawnPosition = GetSpawnPosition(index);
            
            // 实例化宠物对象
            GameObject petObj = Instantiate(prefab, spawnPosition, Quaternion.identity);
            petObj.name = $"Pet_{(isCat ? "Cat" : "Dog")}_{index}";
            
            PetAI petAI = petObj.GetComponent<PetAI>();
            if (petAI != null)
            {
                _activePets.Add(petAI);
                
                // 订阅宠物完成美容事件
                petAI.OnGroomingComplete += () => OnPetGroomingComplete(petAI);
                
                OnPetSpawned?.Invoke(petAI);
                
                Debug.Log($"[PetSpawnManager] Spawned {petObj.name} at {spawnPosition}");
            }
            else
            {
                Debug.LogError($"[PetSpawnManager] Spawned pet prefab does not have PetAI component!");
                Destroy(petObj);
            }
        }
        
        /// <summary>
        /// 获取指定索引宠物的生成位置。
        /// </summary>
        private Vector3 GetSpawnPosition(int index)
        {
            // 如果有预设出生点则优先使用
            if (_spawnPoints != null && _spawnPoints.Length > 0)
            {
                int spawnIndex = index % _spawnPoints.Length;
                if (_spawnPoints[spawnIndex] != null)
                {
                    return _spawnPoints[spawnIndex].position;
                }
            }
            
            // 否则在边界内随机生成一个位置
            return GenerateRandomSpawnPosition();
        }
        
        /// <summary>
        /// 在游玩区域边界内生成一个随机出生位置。
        /// </summary>
        private Vector3 GenerateRandomSpawnPosition()
        {
            return GenerateRandomPositionInBounds(_playAreaMin, _playAreaMax);
        }
        
        #endregion

        #region Static Methods (Testable)
        
        /// <summary>
        /// 获取指定游戏模式下需要生成的宠物数量。
        /// 属性 1：生成宠物数量与游戏模式匹配。
        /// 需求：1.1, 1.2
        /// </summary>
        /// <param name="mode">游戏模式。</param>
        /// <returns>要生成的宠物数量。</returns>
        public static int GetPetCountForMode(GameMode mode)
        {
            return (int)mode;
        }
        
        /// <summary>
        /// 在指定的边界范围内生成一个随机位置。
        /// </summary>
        /// <param name="min">最小边界。</param>
        /// <param name="max">最大边界。</param>
        /// <returns>边界内的随机位置。</returns>
        public static Vector3 GenerateRandomPositionInBounds(Vector3 min, Vector3 max)
        {
            return new Vector3(
                UnityEngine.Random.Range(min.x, max.x),
                (min.y + max.y) / 2f,
                UnityEngine.Random.Range(min.z, max.z)
            );
        }
        
        /// <summary>
        /// 检查一个位置是否在指定边界内。
        /// </summary>
        /// <param name="position">要检查的位置。</param>
        /// <param name="min">最小边界。</param>
        /// <param name="max">最大边界。</param>
        /// <returns>在边界内返回 true。</returns>
        public static bool IsPositionInBounds(Vector3 position, Vector3 min, Vector3 max)
        {
            return position.x >= min.x && position.x <= max.x &&
                   position.z >= min.z && position.z <= max.z;
        }
        
        /// <summary>
        /// 检查列表中的所有宠物是否都已被美容。
        /// 属性 4：所有宠物被美容的胜利条件。
        /// 需求 1.7。
        /// </summary>
        /// <param name="pets">要检查的宠物列表。</param>
        /// <returns>如果全部已被美容则返回 true。</returns>
        public static bool AreAllPetsGroomed(IEnumerable<PetAI> pets)
        {
            if (pets == null) return false;
            
            var petList = pets.ToList();
            if (petList.Count == 0) return false;
            
            return petList.All(p => p != null && p.IsGroomed);
        }
        
        #endregion

        #region Editor Support
        
#if UNITY_EDITOR
        /// <summary>
        /// 设置游戏模式（测试用）。
        /// </summary>
        public void SetGameModeForTesting(GameMode mode)
        {
            _currentMode = mode;
        }
        
        /// <summary>
        /// 设置 Phase2GameConfig（测试用）。
        /// </summary>
        public void SetPhase2ConfigForTesting(Phase2GameConfig config)
        {
            _phase2Config = config;
        }
        
        /// <summary>
        /// 设置游玩区域边界（测试用）。
        /// </summary>
        public void SetPlayAreaBoundsForTesting(Vector3 min, Vector3 max)
        {
            _playAreaMin = min;
            _playAreaMax = max;
        }
        
        /// <summary>
        /// 向激活宠物列表中添加一只宠物（测试用）。
        /// </summary>
        public void AddPetForTesting(PetAI pet)
        {
            if (pet != null && !_activePets.Contains(pet))
            {
                _activePets.Add(pet);
            }
        }
        
        /// <summary>
        /// 清空激活宠物列表（测试用）。
        /// </summary>
        public void ClearPetsForTesting()
        {
            _activePets.Clear();
        }
#endif
        
        private void OnDrawGizmosSelected()
        {
            // 绘制游玩区域边界
            Gizmos.color = Color.cyan;
            Vector3 center = (_playAreaMin + _playAreaMax) / 2f;
            Vector3 size = _playAreaMax - _playAreaMin;
            Gizmos.DrawWireCube(center, size);
            
            // 绘制出生点
            if (_spawnPoints != null)
            {
                Gizmos.color = Color.green;
                foreach (var point in _spawnPoints)
                {
                    if (point != null)
                    {
                        Gizmos.DrawWireSphere(point.position, 0.5f);
                    }
                }
            }
        }
        
        #endregion
    }
}
