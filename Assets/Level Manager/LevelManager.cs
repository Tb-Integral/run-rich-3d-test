using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ButchersGames
{
    public class LevelManager : MonoBehaviour
    {
        private const string CurrentLevelPrefsKey = "Current Level";
        private const string CompleteLevelCountPrefsKey = "Complete Lvl Count";
        private const string LastLevelIndexPrefsKey = "Last Level Index";
        private const string CurrentAttemptPrefsKey = "Current Attempt";

        private static LevelManager _default;
        public static LevelManager Default => _default;

        public static int CurrentLevel
        {
            get => CompleteLevelCount + 1;
            set
            {
                int number = Mathf.Max(1, value);
                PlayerPrefs.SetInt(CurrentLevelPrefsKey, number);
                CompleteLevelCount = number - 1;
            }
        }

        public static int CompleteLevelCount
        {
            get => PlayerPrefs.GetInt(CompleteLevelCountPrefsKey, 0);
            set => PlayerPrefs.SetInt(CompleteLevelCountPrefsKey, Mathf.Max(0, value));
        }

        public static int LastLevelIndex
        {
            get => PlayerPrefs.GetInt(LastLevelIndexPrefsKey, 0);
            set => PlayerPrefs.SetInt(LastLevelIndexPrefsKey, Mathf.Max(0, value));
        }

        public static int CurrentAttempt
        {
            get => PlayerPrefs.GetInt(CurrentAttemptPrefsKey, 0);
            set => PlayerPrefs.SetInt(CurrentAttemptPrefsKey, Mathf.Max(0, value));
        }

        public int CurrentLevelIndex;
        [SerializeField] private bool editorMode;
        [SerializeField] private LevelsList levels;

        public List<Level> Levels => levels != null ? levels.lvls : null;
        public Level CurrentLevelInstance { get; private set; }
        public event Action OnLevelStarted;

        private void Awake()
        {
            if (_default != null && _default != this)
            {
                Debug.LogError("В сцене уже существует активный LevelManager.", this);
                enabled = false;
                return;
            }

            _default = this;
        }

        public void Init()
        {
#if !UNITY_EDITOR
            editorMode = false;
#endif
            SelectLevel(editorMode ? CurrentLevelIndex : LastLevelIndex);
        }

        private void OnDestroy()
        {
            if (_default == this)
                _default = null;
        }

        private void OnApplicationQuit()
        {
            PlayerPrefs.Save();
        }

        public void StartLevel()
        {
            if (CurrentLevelInstance == null)
            {
                Debug.LogWarning("Уровень не выбран: запуск пропущен.", this);
                return;
            }

            CurrentAttempt++;
            OnLevelStarted?.Invoke();
        }

        public void RestartLevel() => SelectLevel(CurrentLevelIndex, false);

        public void NextLevel()
        {
            if (!HasLevels())
                return;

            int nextIndex = CurrentLevelIndex + 1;
            if (!editorMode && levels.randomizedLvls && nextIndex >= Levels.Count && Levels.Count > 1)
            {
                // Исключён текущий уровень без создания временного списка.
                nextIndex = UnityEngine.Random.Range(0, Levels.Count - 1);
                if (nextIndex >= CurrentLevelIndex)
                    nextIndex++;
            }

            if (TrySelectLevel(nextIndex, true) && !editorMode)
            {
                CompleteLevelCount++;
                CurrentAttempt = 0;
            }
        }

        public void SelectLevel(int levelIndex, bool indexCheck = true) => TrySelectLevel(levelIndex, indexCheck);
        public void PrevLevel() => SelectLevel(CurrentLevelIndex - 1);

        public static void ResetProgress()
        {
            PlayerPrefs.DeleteKey(CurrentLevelPrefsKey);
            PlayerPrefs.DeleteKey(CompleteLevelCountPrefsKey);
            PlayerPrefs.DeleteKey(LastLevelIndexPrefsKey);
            PlayerPrefs.DeleteKey(CurrentAttemptPrefsKey);
            PlayerPrefs.Save();
        }

        private bool HasLevels()
        {
            if (Levels != null && Levels.Count > 0)
                return true;

            Debug.LogWarning("Список уровней не назначен или пуст.", this);
            return false;
        }

        private bool TrySelectLevel(int levelIndex, bool indexCheck)
        {
            if (!HasLevels())
                return false;

            if (indexCheck)
                levelIndex = ((levelIndex % Levels.Count) + Levels.Count) % Levels.Count;

            if (levelIndex < 0 || levelIndex >= Levels.Count || Levels[levelIndex] == null)
            {
                Debug.LogWarning("Не найден префаб для выбранного индекса уровня.", this);
                return false;
            }

            Level prefab = Levels[levelIndex];
            ClearChildren();
#if UNITY_EDITOR
            CurrentLevelInstance = !Application.isPlaying && PrefabUtility.IsPartOfPrefabAsset(prefab)
                ? (Level)PrefabUtility.InstantiatePrefab(prefab, transform)
                : Instantiate(prefab, transform);
#else
            CurrentLevelInstance = Instantiate(prefab, transform);
#endif
            CurrentLevelIndex = levelIndex;
            if (Application.isPlaying)
                LastLevelIndex = levelIndex;
            return true;
        }

        private void ClearChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    // Отключены коллайдеры старого уровня до отложенного уничтожения.
                    child.SetActive(false);
                    child.transform.SetParent(null);
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }

            CurrentLevelInstance = null;
        }
    }
}
