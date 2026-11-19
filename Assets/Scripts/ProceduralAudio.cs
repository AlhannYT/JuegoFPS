using UnityEngine;

namespace JuegoFPS
{
    /// <summary>
    /// Genera efectos de sonido procedurales en tiempo de ejecución para no depender de archivos externos.
    /// </summary>
    public class ProceduralAudio : MonoBehaviour
    {
        public static ProceduralAudio Instance { get; private set; }

        private AudioSource _source;
        private AudioClip _pistolClip;
        private AudioClip _rifleClip;
        private AudioClip _hitTargetClip;
        private AudioClip _hitEnemyClip;
        private AudioClip _playerHurtClip;
        private AudioClip _victoryClip;
        private AudioClip _gameOverClip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;

            GenerateClips();
        }

        private void GenerateClips()
        {
            _pistolClip = CreateNoiseBurst("PistolShot", 0.14f, 950f, 180f, 0.55f);
            _rifleClip = CreateNoiseBurst("RifleShot", 0.11f, 1400f, 240f, 0.45f);
            _hitTargetClip = CreateTone("HitTarget", 0.18f, 880f, 1320f, 0.35f);
            _hitEnemyClip = CreateNoiseBurst("HitEnemy", 0.12f, 350f, 90f, 0.4f);
            _playerHurtClip = CreateTone("PlayerHurt", 0.28f, 220f, 90f, 0.5f);
            _victoryClip = CreateArpeggio("Victory", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.12f, 0.35f);
            _gameOverClip = CreateArpeggio("GameOver", new[] { 392f, 349.23f, 311.13f, 261.63f }, 0.18f, 0.35f);
        }

        public void PlayPistolShot() => Play(_pistolClip, 0.7f);
        public void PlayRifleShot() => Play(_rifleClip, 0.65f);
        public void PlayHitTarget() => Play(_hitTargetClip, 0.75f);
        public void PlayHitEnemy() => Play(_hitEnemyClip, 0.7f);
        public void PlayPlayerHurt() => Play(_playerHurtClip, 0.85f);
        public void PlayVictory() => Play(_victoryClip, 0.8f);
        public void PlayGameOver() => Play(_gameOverClip, 0.8f);

        private void Play(AudioClip clip, float volume)
        {
            if (_source != null && clip != null)
            {
                _source.PlayOneShot(clip, volume);
            }
        }

        private static AudioClip CreateNoiseBurst(string clipName, float duration, float startFreq, float endFreq, float gain)
        {
            int sampleRate = 44100;
            int samples = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
            float[] data = new float[samples];
            System.Random rng = new System.Random(clipName.GetHashCode());

            float phase = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float env = Mathf.Pow(1f - t, 2.2f);
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                phase += 2f * Mathf.PI * freq / sampleRate;
                float tone = Mathf.Sin(phase) * 0.45f;
                float noise = ((float)rng.NextDouble() * 2f - 1f) * 0.55f;
                data[i] = Mathf.Clamp((tone + noise) * env * gain, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(clipName, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateTone(string clipName, float duration, float startFreq, float endFreq, float gain)
        {
            int sampleRate = 44100;
            int samples = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
            float[] data = new float[samples];

            float phase = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float env = Mathf.Sin(Mathf.PI * t) * (1f - t * 0.5f);
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                phase += 2f * Mathf.PI * freq / sampleRate;
                data[i] = Mathf.Clamp(Mathf.Sin(phase) * env * gain, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(clipName, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateArpeggio(string clipName, float[] notes, float noteDuration, float gain)
        {
            int sampleRate = 44100;
            int noteSamples = Mathf.Max(1, Mathf.RoundToInt(sampleRate * noteDuration));
            int totalSamples = noteSamples * notes.Length;
            float[] data = new float[totalSamples];

            for (int n = 0; n < notes.Length; n++)
            {
                float phase = 0f;
                float freq = notes[n];
                for (int i = 0; i < noteSamples; i++)
                {
                    float t = (float)i / noteSamples;
                    float env = Mathf.Sin(Mathf.PI * t);
                    phase += 2f * Mathf.PI * freq / sampleRate;
                    data[n * noteSamples + i] = Mathf.Clamp(Mathf.Sin(phase) * env * gain, -1f, 1f);
                }
            }

            AudioClip clip = AudioClip.Create(clipName, totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
