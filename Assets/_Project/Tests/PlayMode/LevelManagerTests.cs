using System.Collections.Generic;
using System.Reflection;
using ButchersGames;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RunRich.Tests
{
    public sealed class LevelManagerTests
    {
        private readonly string[] _keys = { "Current Level", "Complete Lvl Count", "Last Level Index", "Current Attempt" };
        private readonly Dictionary<string, int> _saved = new Dictionary<string, int>();
        private GameObject _host;
        private GameObject _template;
        private LevelManager _manager;
        private LevelsList _list;

        [SetUp]
        public void SetUp()
        {
            foreach (string key in _keys)
                if (PlayerPrefs.HasKey(key))
                    _saved[key] = PlayerPrefs.GetInt(key);
            LevelManager.ResetProgress();
            _host = new GameObject("Manager under test");
            _manager = _host.AddComponent<LevelManager>();
            _template = new GameObject("Level template");
            var level = _template.AddComponent<Level>();
            _list = ScriptableObject.CreateInstance<LevelsList>();
            _list.lvls = new List<Level> { level };
            typeof(LevelManager).GetField("levels", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_manager, _list);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
            Object.DestroyImmediate(_template);
            Object.DestroyImmediate(_list);
            LevelManager.ResetProgress();
            foreach (var entry in _saved)
                PlayerPrefs.SetInt(entry.Key, entry.Value);
            _saved.Clear();
            PlayerPrefs.Save();
        }

        [Test]
        public void InitLoadsFirstLevelWithFreshProgress()
        {
            _manager.Init();
            Assert.That(_manager.CurrentLevelIndex, Is.Zero);
            Assert.That(LevelManager.CurrentLevel, Is.EqualTo(1));
            Assert.That(_manager.CurrentLevelInstance, Is.Not.Null);
        }

        [Test]
        public void OneRandomLevelCanRepeatAndAdvanceDisplayedNumber()
        {
            _list.randomizedLvls = true;
            _manager.Init();
            _manager.NextLevel();
            Assert.That(_manager.CurrentLevelIndex, Is.Zero);
            Assert.That(LevelManager.CurrentLevel, Is.EqualTo(2));
            Assert.That(_host.transform.childCount, Is.EqualTo(1));
        }

        [Test]
        public void EmptyListDoesNotChangeProgressOrThrow()
        {
            _list.lvls.Clear();
            LogAssert.Expect(LogType.Warning, "Список уровней не назначен или пуст.");
            _manager.NextLevel();
            Assert.That(LevelManager.CurrentLevel, Is.EqualTo(1));
        }

        [Test]
        public void MissingPrefabPreservesCurrentLevelAndProgress()
        {
            _manager.Init();
            var original = _manager.CurrentLevelInstance;
            _list.lvls.Add(null);
            LogAssert.Expect(LogType.Warning, "Не найден префаб для выбранного индекса уровня.");
            _manager.NextLevel();
            Assert.That(_manager.CurrentLevelInstance, Is.SameAs(original));
            Assert.That(LevelManager.CurrentLevel, Is.EqualTo(1));
        }

        [Test]
        public void RestartReplacesLevelWithoutAdvancingProgress()
        {
            _manager.Init();
            var original = _manager.CurrentLevelInstance;
            _manager.RestartLevel();
            Assert.That(_manager.CurrentLevelInstance, Is.Not.SameAs(original));
            Assert.That(original.gameObject.activeSelf, Is.False);
            Assert.That(_host.transform.childCount, Is.EqualTo(1));
            Assert.That(LevelManager.CurrentLevel, Is.EqualTo(1));
        }

        [Test]
        public void NegativeIndexWrapsToLastLevel()
        {
            _list.lvls.Add(_template.GetComponent<Level>());
            _manager.SelectLevel(-1);
            Assert.That(_manager.CurrentLevelIndex, Is.EqualTo(1));
        }

        [Test]
        public void InvalidUncheckedIndexPreservesInstance()
        {
            _manager.Init();
            var original = _manager.CurrentLevelInstance;
            LogAssert.Expect(LogType.Warning, "Не найден префаб для выбранного индекса уровня.");
            _manager.SelectLevel(8, false);
            Assert.That(_manager.CurrentLevelInstance, Is.SameAs(original));
        }

        [Test]
        public void CurrentLevelSetterUpdatesProgress()
        {
            LevelManager.CurrentLevel = 4;
            Assert.That(LevelManager.CurrentLevel, Is.EqualTo(4));
            Assert.That(LevelManager.CompleteLevelCount, Is.EqualTo(3));
            LevelManager.CurrentLevel = 0;
            Assert.That(LevelManager.CurrentLevel, Is.EqualTo(1));
        }

        [Test]
        public void ResetPreservesUnrelatedPreferences()
        {
            const string key = "RunRich.Validation.Unrelated";
            bool existed = PlayerPrefs.HasKey(key);
            int oldValue = PlayerPrefs.GetInt(key);
            try
            {
                PlayerPrefs.SetInt(key, 42);
                LevelManager.CurrentLevel = 3;
                LevelManager.ResetProgress();
                Assert.That(PlayerPrefs.GetInt(key), Is.EqualTo(42));
                Assert.That(LevelManager.CurrentLevel, Is.EqualTo(1));
            }
            finally
            {
                if (existed) PlayerPrefs.SetInt(key, oldValue);
                else PlayerPrefs.DeleteKey(key);
            }
        }
    }
}
