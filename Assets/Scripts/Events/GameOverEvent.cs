using UnityEngine;
using UnityEngine.Events;

#nullable enable

[CreateAssetMenu(menuName = "ScriptableObjects/GameOverEvent")]
public class GameOverEvent : ScriptableObject {
    public UnityAction? Event;
}
