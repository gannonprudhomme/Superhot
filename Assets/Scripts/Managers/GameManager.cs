using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

#nullable enable

[RequireComponent(typeof(LevelManager))]
public class GameManager : MonoBehaviour {
    [Header("References")]
    [Tooltip("The camera we enable when transitioning between levels")]
    public Camera? LevelTransitionCamera;
    
    [Tooltip("The Fullscreen Material we control to do the level transitions")]
    public Material? FullscreenMaterial;
    
    [Header("Events")]
    public GameOverEvent? GameOverEvent;
    
    private LevelManager? levelManager;
    
    private const string SHADER_START_TIME = "_Start_Time";
    private const string SHADER_UNSCALED_TIME = "_Unscaled_Time";
    private const string SHADER_ENABLED = "_Enabled";
    private const string SHADER_IS_FULLSCREEN_COVERED = "_Is_Fullscreen_Covered";
    
    private void Awake() {
        levelManager = GetComponent<LevelManager>();
        
        GameOverEvent!.Event += OnGameOver;
        
        LevelTransitionCamera!.gameObject.SetActive(false);
    }

    private void Update() {
        // Have to pass this in since ShaderGraph doesn't have an Unscaled Time block
        FullscreenMaterial!.SetFloat(SHADER_UNSCALED_TIME, Time.unscaledTime);
    }

    private void OnGameOver() {
        StartCoroutine(HandleGameOver());
    }

    private IEnumerator HandleGameOver() {
        FullscreenMaterial!.SetInt(SHADER_ENABLED, 1);
        FullscreenMaterial!.SetFloat(SHADER_START_TIME, Time.unscaledTime);

        // Wait for the game over animation to finish
        yield return new WaitForSecondsRealtime(0.5f);
        
        // Switch cameras
        FullscreenMaterial!.SetInt(SHADER_IS_FULLSCREEN_COVERED, 1);
        LevelTransitionCamera!.gameObject.SetActive(true);
        if (FindPlayerCamera() is Camera playerCamera) {
            playerCamera.gameObject.SetActive(false);
        }
        
        // Destroy the current level & reload it
        yield return levelManager!.ReloadLevel();
        
        LevelTransitionCamera!.gameObject.SetActive(false);
        FullscreenMaterial!.SetInt(SHADER_ENABLED, 0);
        FullscreenMaterial!.SetInt(SHADER_IS_FULLSCREEN_COVERED, 0);
    }
    
    private Camera? FindPlayerCamera() {
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        return cameras.FirstOrDefault(cam => cam != LevelTransitionCamera);
    }
}
