using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private List<AudioClip> pokerChipClips = new List<AudioClip>();
    [SerializeField] private List<AudioClip> playingCardFoldClips = new List<AudioClip>();
    [SerializeField] private List<AudioClip> playingCardDealClips = new List<AudioClip>();
    [SerializeField] private List<AudioClip> playingCardShuffleClips = new List<AudioClip>();
    [SerializeField] private List<AudioClip> playingCardFlipClips = new List<AudioClip>();

    [SerializeField] Vector2 audioVolumeRange;
    [SerializeField] Vector2 audioPitchRange;
    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        audioSource = GetComponent<AudioSource>();
    }


    public void PlayAudioClip(AudioSnippet audioSnippet)
    {
        switch (audioSnippet)
        {
            case AudioSnippet.PokerChip:
                PlaySnippet(pokerChipClips);
                break;
            case AudioSnippet.PlayingCardDeal:
                PlaySnippet(playingCardDealClips);
                break;
            case AudioSnippet.PlayingCardFold:
                PlaySnippet(playingCardFoldClips);
                break;
            case AudioSnippet.PlayingCardShuffle:
                PlaySnippet(playingCardShuffleClips);
                break;
            case AudioSnippet.PlayingCardFlip:
                PlaySnippet(playingCardFlipClips);
                break;

        }
    }

    private void PlaySnippet(List<AudioClip> clipList)
    {
        audioSource.volume = Random.Range(audioVolumeRange.x, audioVolumeRange.y);
        audioSource.pitch = Random.Range(audioPitchRange.x, audioPitchRange.y);
        audioSource.PlayOneShot(clipList[Random.Range(0, clipList.Count)]);
        
    }
}
