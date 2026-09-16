using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace RunRich.Tests
{
    [SetUpFixture]
    public sealed class ProgressTestScope
    {
        private readonly Dictionary<string, int> _saved = new();
        private readonly string[] _keys = { "Current Level", "Complete Lvl Count", "Last Level Index", "Current Attempt" };
        [OneTimeSetUp]
        public void Save()
        {
            foreach (string key in _keys)
                if (PlayerPrefs.HasKey(key)) _saved[key] = PlayerPrefs.GetInt(key);
        }
        [OneTimeTearDown]
        public void Restore()
        {
            foreach (string key in _keys)
            {
                if (_saved.TryGetValue(key, out int value)) PlayerPrefs.SetInt(key, value);
                else PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();
        }
    }
}
