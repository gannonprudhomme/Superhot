using UnityEngine;
using UnityEngine.Events;

#nullable enable

[CreateAssetMenu(menuName = "ScriptableObjects/EnemyLoadedEvent")]
public class EnemyLoadedEvent : ScriptableObject {
    public UnityAction<int>? Event;
}
