using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#nullable enable

public class EnemyManager : MonoBehaviour {
    [Header("Events")]
    public EnemyLoadedEvent? EnemyLoadedEvent;
    public EnemyDiedEvent? EnemyDiedEvent;
    
    public UnityAction? AllEnemiesKilledEvent;
    
    private readonly HashSet<int> allEnemies = new();

    private void Awake() {
        EnemyLoadedEvent!.Event += OnEnemyLoaded;
        EnemyDiedEvent!.Event += OnEnemyDied;
    }

    private void Update() {
        if (allEnemies.Count == 0) {
            // Note: this gets ignored by the GameManager when a level is loading
            // otherwise we'd reload scenes a ton
            AllEnemiesKilledEvent!.Invoke();
        }
    }

    private void OnEnemyLoaded(int enemyInstanceID) {
        allEnemies.Add(enemyInstanceID);
    }
    
    private void OnEnemyDied(int enemyInstanceID) {
        allEnemies.Remove(enemyInstanceID);
    }
}
