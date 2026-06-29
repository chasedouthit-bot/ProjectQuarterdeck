using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class CrewShantyController : MonoBehaviour
{
    [Header("Audio Clip")]
    [SerializeField] private AudioClip shantyClip;
    [SerializeField] private float maxShantyVolume = 0.85f;
    [SerializeField] private float fadeDuration = 1.0f;

    private AudioSource _shantySource;
    private bool _isPlaying = false;
    private Coroutine _fadeRoutine;

    public bool IsPlaying => _isPlaying;

    void Start()
    {
        if (shantyClip == null)
        {
            shantyClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SeaShantyCrew.wav");
        }

        _shantySource = gameObject.AddComponent<AudioSource>();
        _shantySource.clip = shantyClip;
        _shantySource.loop = true;
        _shantySource.volume = 0f;
        _shantySource.spatialBlend = 0.5f; // Partially 3D spatialized so it sounds like it comes from the ship deck
        _shantySource.playOnAwake = false;
    }

    public void ToggleShanty()
    {
        SetShantyState(!_isPlaying);
    }

    public void SetShantyState(bool play)
    {
        if (_isPlaying == play) return;
        _isPlaying = play;

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
        }

        _fadeRoutine = StartCoroutine(FadeShanty(play));
    }

    private IEnumerator FadeShanty(bool play)
    {
        if (play)
        {
            if (!_shantySource.isPlaying)
            {
                _shantySource.Play();
            }

            float elapsed = 0f;
            float startVol = _shantySource.volume;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                _shantySource.volume = Mathf.Lerp(startVol, maxShantyVolume, elapsed / fadeDuration);
                yield return null;
            }
            _shantySource.volume = maxShantyVolume;
        }
        else
        {
            float elapsed = 0f;
            float startVol = _shantySource.volume;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                _shantySource.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeDuration);
                yield return null;
            }
            _shantySource.volume = 0f;
            _shantySource.Stop();
        }
    }
}
