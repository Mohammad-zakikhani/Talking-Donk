using UnityEngine;

[RequireComponent(typeof(AudioSource), typeof(Animator))]
public class DonkeyCharacterController : MonoBehaviour
{
    private AudioSource audioSource;
    private Animator animator;

    [Header("Animator Params")]
    public string talkTrigger = "Talk";

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
    }



    #region Audio And Talking
    public void PlayAudio(AudioClip clip)
    {
        if (clip == null) return;

        audioSource.clip = clip;
        audioSource.Play();
    }

    public void StopAudio()
    {
        if (audioSource.isPlaying)
            audioSource.Stop();
    }

    public bool IsTalking()
    {
        return audioSource.isPlaying;
    }
    #endregion
}
