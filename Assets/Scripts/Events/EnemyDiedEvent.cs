using UnityEngine;
using UnityEngine.Events;

#nullable enable

[CreateAssetMenu(menuName = "ScriptableObjects/EnemyDiedEvent")]
public class EnemyDiedEvent : ScriptableObject {
    public UnityAction<int>? Event;
}
