using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#nullable enable

public class LevelManager : MonoBehaviour {
    [Tooltip("All scene level names")]
    public string[]? AllLevels;

    private int currentLevelIndex = 0;
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
        if (GetLoadedLevel() is Scene currScene) {
            currentLevelScene = currScene;
            SceneManager.SetActiveScene(currentLevelScene.Value);
        } else {
            yield return LoadLevel(AllLevels[0]);
        }
    }
    
    public IEnumerator LoadNextLevel() {
        yield return SceneManager.UnloadSceneAsync(currentLevelScene!.Value.name);

        currentLevelIndex = (currentLevelIndex + 1) % AllLevels!.Length; // Rotate for now
        string nextLevelName = AllLevels[currentLevelIndex];
        yield return LoadLevel(nextLevelName);
    }

    public IEnumerator ReloadLevel() {
        string currentLevelSceneName = currentLevelScene!.Value.name;
        // unload it & reload it
        yield return SceneManager.UnloadSceneAsync(currentLevelSceneName);
        yield return LoadLevel(currentLevelSceneName);
    }

    private IEnumerator LoadLevel(string levelName) {
        yield return SceneManager.LoadSceneAsync(levelName, LoadSceneMode.Additive);
        currentLevelScene = SceneManager.GetSceneByName(levelName);
        SceneManager.SetActiveScene(currentLevelScene.Value);
    }

    // This also sets currentLevelIndex (when it returns a value)
    private Scene? GetLoadedLevel() {
        HashSet<string> allSceneNames = new(AllLevels!);
        
        for (int i = 0; i < SceneManager.sceneCount; i++) {
            Scene scene = SceneManager.GetSceneAt(i);
            if (allSceneNames.Contains(scene.name)) {
                currentLevelIndex = System.Array.IndexOf(AllLevels!, scene.name);
                return scene;
            }
        }

        return null;
    }
}
