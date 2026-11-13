using UnityEngine;

namespace Lego2KDrive
{
    /// <summary>
    /// Procedural sound synthesizer so all LEGO brick clicks, crashes, transformations,
    /// boosts, and countdown beeps work out of the box with zero external audio files.
    /// </summary>
    public class LegoProceduralAudio : MonoBehaviour
    {
        public static LegoProceduralAudio Instance { get; private set; }

        private AudioSource _sfxSource;
        private AudioClip _brickClickClip;
        private AudioClip _transformSnapClip;
        private AudioClip _crashShatterClip;
        private AudioClip _boostWhooshClip;
        private AudioClip _jumpBoingClip;
        private AudioClip _powerUpClip;
        private AudioClip _beepLowClip;
        private AudioClip _beepHighClip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.spatialBlend = 0f;
            _sfxSource.volume = 0.45f;

            GenerateClips();
        }

        private void GenerateClips()
        {
            _brickClickClip = CreateBrickClickClip();
            _transformSnapClip = CreateTransformSnapClip();
            _crashShatterClip = CreateCrashShatterClip();
            _boostWhooshClip = CreateBoostClip();
            _jumpBoingClip = CreateJumpClip();
            _powerUpClip = CreatePowerUpClip();
            _beepLowClip = CreateToneClip(440f, 0.16f);
            _beepHighClip = CreateToneClip(880f, 0.32f);
        }

        public void PlayBrickClick(float volume = 0.5f)
        {
            if (_sfxSource && _brickClickClip)
                _sfxSource.PlayOneShot(_brickClickClip, volume);
        }

        public void PlayTransformSnap()
        {
            if (_sfxSource && _transformSnapClip)
                _sfxSource.PlayOneShot(_transformSnapClip, 0.7f);
        }

        public void PlayBrickCrash()
        {
            if (_sfxSource && _crashShatterClip)
                _sfxSource.PlayOneShot(_crashShatterClip, 0.75f);
        }

        public void PlayBoost()
        {
            if (_sfxSource && _boostWhooshClip)
                _sfxSource.PlayOneShot(_boostWhooshClip, 0.65f);
        }

        public void PlayJump()
        {
            if (_sfxSource && _jumpBoingClip)
                _sfxSource.PlayOneShot(_jumpBoingClip, 0.55f);
        }

        public void PlayPowerUp()
        {
            if (_sfxSource && _powerUpClip)
                _sfxSource.PlayOneShot(_powerUpClip, 0.65f);
        }

        public void PlayCountdownBeep(bool isGo)
        {
            if (!_sfxSource) return;
            _sfxSource.PlayOneShot(isGo ? _beepHighClip : _beepLowClip, 0.6f);
        }

        private AudioClip CreateBrickClickClip()
        {
            int sampleRate = 22050;
            int samples = (int)(sampleRate * 0.08f);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float env = Mathf.Exp(-t * 55f);
                float freq = i < samples / 2 ? 1200f : 1850f;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.7f;
            }
            AudioClip clip = AudioClip.Create("BrickClick", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateTransformSnapClip()
        {
            int sampleRate = 22050;
            int samples = (int)(sampleRate * 0.22f);
            float[] data = new float[samples];
            float[] notes = { 520f, 740f, 980f, 1320f };
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                int step = Mathf.Clamp((int)((i / (float)samples) * notes.Length), 0, notes.Length - 1);
                float localT = (i % (samples / notes.Length)) / (float)sampleRate;
                float env = Mathf.Exp(-localT * 38f);
                data[i] = (Mathf.Sin(2f * Mathf.PI * notes[step] * t) + Random.Range(-0.2f, 0.2f)) * env * 0.6f;
            }
            AudioClip clip = AudioClip.Create("TransformSnap", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateCrashShatterClip()
        {
            int sampleRate = 22050;
            int samples = (int)(sampleRate * 0.30f);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float env = Mathf.Exp(-t * 12f);
                float clatter = Mathf.Sin(2f * Mathf.PI * (900f + (i % 7) * 210f) * t);
                float noise = Random.Range(-0.8f, 0.8f);
                data[i] = (noise * 0.6f + clatter * 0.4f) * env * 0.7f;
            }
            AudioClip clip = AudioClip.Create("BrickShatter", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateBoostClip()
        {
            int sampleRate = 22050;
            int samples = (int)(sampleRate * 0.35f);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float freq = Mathf.Lerp(220f, 880f, t / 0.35f);
                float env = Mathf.Sin((t / 0.35f) * Mathf.PI);
                data[i] = (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.6f + Random.Range(-0.3f, 0.3f)) * env * 0.6f;
            }
            AudioClip clip = AudioClip.Create("BoostWhoosh", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateJumpClip()
        {
            int sampleRate = 22050;
            int samples = (int)(sampleRate * 0.18f);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float freq = Mathf.Lerp(300f, 680f, t / 0.18f);
                float env = 1f - (t / 0.18f);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.6f;
            }
            AudioClip clip = AudioClip.Create("JumpBoing", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreatePowerUpClip()
        {
            int sampleRate = 22050;
            int samples = (int)(sampleRate * 0.28f);
            float[] data = new float[samples];
            float[] arp = { 523.25f, 659.25f, 783.99f, 1046.5f };
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                int idx = Mathf.Clamp((int)((i / (float)samples) * arp.Length), 0, arp.Length - 1);
                data[i] = Mathf.Sin(2f * Mathf.PI * arp[idx] * t) * 0.55f;
            }
            AudioClip clip = AudioClip.Create("PowerUpArp", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateToneClip(float freq, float duration)
        {
            int sampleRate = 22050;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float env = 1f - (t / duration) * 0.5f;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.5f;
            }
            AudioClip clip = AudioClip.Create($"Tone_{freq}", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}

