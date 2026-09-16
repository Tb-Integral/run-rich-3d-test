using System;
using ButchersGames;
using UnityEngine;

namespace RunRich
{
    public sealed class RunLevelSource : MonoBehaviour
    {
        [SerializeField] private LevelManager manager;
        public int LevelNumber => LevelManager.CurrentLevel;

        public TrackPath Load(bool restart)
        {
            if (restart) manager.RestartLevel(); else manager.Init();
            return CurrentPath();
        }

        public TrackPath LoadNext()
        {
            manager.NextLevel();
            PlayerPrefs.Save();
            return CurrentPath();
        }

        private TrackPath CurrentPath()
        {
            var level = manager.CurrentLevelInstance;
            if (level == null || !level.TryGetComponent<TrackPath>(out var path))
                throw new InvalidOperationException("В префабе уровня не найден TrackPath.");
            return path;
        }

        public void StartAttempt() => manager.StartLevel();
    }
}
