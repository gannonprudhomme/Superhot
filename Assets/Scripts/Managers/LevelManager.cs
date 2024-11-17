using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#nullable enable

public class LevelManager : MonoBehaviour {
    [Tooltip("All scene level names")]
    public string[]? AllLevels;

    private Scene? currentLevelScene;

    private void Awake() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private IEnumerator Start() {
        if (AllLevels == null || AllLevels.Length == 0) {
            Debug.LogError("No levels found");
            yield break;
        }
        
        // Don't need to load a level scene if we already have one!
        // This is just for the Editor really - release builds only start out with the Main scene.
        if (IsLevelSceneLoaded()) {
            yield break;
        }
        
        SceneManager.LoadScene(AllLevels[0], LoadSceneMode.Additive);
        currentLevelScene = SceneManager.GetSceneByName(AllLevels[0]);
        SceneManager.SetActiveScene(currentLevelScene.Value);
    }

    private bool IsLevelSceneLoaded() {
        HashSet<string> allSceneNames = new(AllLevels!);
        
        for (int i = 0; i < SceneManager.sceneCount; i++) {
            Scene scene = SceneManager.GetSceneAt(i);
            if (allSceneNames.Contains(scene.name)) {
                return true;
            }
        }

        return false;
    }
}
