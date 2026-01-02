using UnityEngine;
using System.Collections;

public class MusicController : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource audioSource;        // Assign your AudioSource here
    public float maxVolume = 0.20f;         // Maximum volume for the music

    public float fadeInDuration = 2f;      // Duration of fade-in at the start of each loop
    public float fadeOutDuration = 3f;     // Duration of fade-out at the end of each loop

    private void Start()
    {
        if (!audioSource.isPlaying)
            StartCoroutine(LoopWithFade());
    }

    private IEnumerator LoopWithFade()
    {
        while (true)
        {
            // Start the clip
            audioSource.volume = 0f;
            audioSource.Play();

            // Fade in
            yield return StartCoroutine(Fade(0f, maxVolume, fadeInDuration));

            // Wait until it's time to fade out
            float waitTime = Mathf.Max(0f, audioSource.clip.length - fadeInDuration - fadeOutDuration);
            yield return new WaitForSeconds(waitTime);

            // Fade out
            yield return StartCoroutine(Fade(maxVolume, 0f, fadeOutDuration));

            // Stop the clip and start loop again
            audioSource.Stop();
        }
    }

    private IEnumerator Fade(float startVolume, float endVolume, float duration)
    {
        float elapsed = 0f;
        audioSource.volume = startVolume;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, endVolume, elapsed / duration);
            yield return null;
        }

        audioSource.volume = endVolume;
    }
}
