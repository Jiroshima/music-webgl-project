using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class SoundSynthesizer : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip externalClip;  // External audio clip field

    public float pitch = 1f;        // Pitch of the audio
    public float volume = 1f;       // Volume (amplitude)
    public float playbackRate = 1f; // Playback speed (rate)

    // Fixed waveform type - let's use sine wave as the default
    private const string waveform = "Sine";  // Only sine waveform is used

    public Slider pitchSlider;       // Slider for pitch control
    public Slider volumeSlider;      // Slider for volume control
    public Slider playbackRateSlider; // Slider for playback rate control

    public TMP_Text pitchValueText;  // Text to display pitch value
    public TMP_Text volumeValueText; // Text to display volume value
    public TMP_Text playbackRateValueText; // Text to display playback rate value

    public LineRenderer lineRenderer;
    public TMP_Text infoText;

    public RectTransform waveformContainer; // This is the parent object (panel) for the waveform

    // UI Elements
    public Button playPauseButton;
    public Slider playbackSlider;

    private bool isPlaying = false;
    private const int FFT_SIZE = 1024;

    private void Start()
    {
        // Get the AudioSource component
        audioSource = GetComponent<AudioSource>();

        // Optionally, assign external clip
        if (externalClip != null)
        {
            audioSource.clip = externalClip;
        }
        audioSource.loop = true;

        // Link sliders and set initial values
        pitchSlider.onValueChanged.AddListener(UpdatePitch);
        volumeSlider.onValueChanged.AddListener(UpdateVolume);
        playbackRateSlider.onValueChanged.AddListener(UpdatePlaybackRate);

        // Set slider ranges
        pitchSlider.minValue = 0f;
        pitchSlider.maxValue = 2f;
        pitchSlider.value = 1f;  // Default value

        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 100f;  // Now from 0% to 100%
        volumeSlider.value = 100f;  // Default value (100%)

        playbackRateSlider.minValue = 0f;
        playbackRateSlider.maxValue = 2f;
        playbackRateSlider.value = 1f;  // Default value

        // Update slider values on start
        pitchValueText.text = pitchSlider.value.ToString("F2") + " Hz";
        volumeValueText.text = volumeSlider.value.ToString("F2") + "%";  // Display as percentage
        playbackRateValueText.text = playbackRateSlider.value.ToString("F2") + "x"; // Now "x" for playback rate

        // UI: Play/Pause button
        playPauseButton.onClick.AddListener(TogglePlayPause);

        // UI: Playback slider
        playbackSlider.onValueChanged.AddListener(OnPlaybackSliderChanged);

        // Ensure the LineRenderer starts inside the waveform container
        lineRenderer.transform.SetParent(waveformContainer, false);
        lineRenderer.transform.localPosition = Vector3.zero; // Ensure it starts at the container's center

        // Disable world space, so the LineRenderer works in local space
        lineRenderer.useWorldSpace = false;

        // Set the sorting layer and order to make sure it's rendered in front
        lineRenderer.sortingLayerName = "UI"; // Ensure it's on the UI layer
        lineRenderer.sortingOrder = 1; // Set it above other UI elements

        // Set material and color for the LineRenderer to ensure visibility
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.material.color = Color.red; // Color for visibility (can be changed)

        // Ensure the LineRenderer is on the 2D plane
        lineRenderer.transform.position = new Vector3(lineRenderer.transform.position.x, lineRenderer.transform.position.y, 0f);

        // Generate the waveform with the sine wave
        SetWaveformToSine();
    }

    private void Update()
    {
        // If no external clip, generate sound wave for custom waveform
        if (audioSource.clip == null || !audioSource.isPlaying)
        {
            if (isPlaying)
            {
                GenerateWaveform();
            }
        }

        // Visualize waveform depending on external audio or generated waveform
        if (audioSource.isPlaying && audioSource.clip != null)
        {
            VisualizeWaveformFromAudio();
            UpdateAudioInfo();  // Continuously update frequency and amplitude
        }
        else if (!audioSource.isPlaying && isPlaying)
        {
            // Keep the waveform static when paused, don't update LineRenderer
            VisualizeWaveformStatic();
            // Keep last known values of frequency and amplitude
            UpdateAudioInfoStatic();
        }

        // Update playback slider based on audio time
        if (audioSource.isPlaying)
        {
            playbackSlider.value = audioSource.time / audioSource.clip.length; // Update playback slider value
        }
    }

    // Update pitch from the slider
    private void UpdatePitch(float value)
    {
        pitch = value;
        audioSource.pitch = pitch; // Update the pitch of the audio
        pitchValueText.text = pitch.ToString("F2") + " Hz";  // Update pitch value text
    }

    // Update volume from the slider (now as percentage)
    private void UpdateVolume(float value)
    {
        volume = value / 100f; // Convert to range from 0 to 1 for AudioSource
        audioSource.volume = volume; // Update the volume of the audio
        volumeValueText.text = value.ToString("F2") + "%"; // Display as percentage
    }

    // Update playback rate from the slider
    private void UpdatePlaybackRate(float value)
    {
        playbackRate = value;
        audioSource.pitch = playbackRate; // Update the playback rate (also affects pitch)
        playbackRateValueText.text = playbackRate.ToString("F2") + "x"; // Now displaying "x" for playback rate
    }

    // Toggle play and pause state
    private void TogglePlayPause()
    {
        if (!isPlaying)
        {
            if (audioSource.clip == null)
            {
                GenerateWaveform(); // If no clip is assigned, generate the waveform first
            }

            audioSource.Play();
            isPlaying = true;  // Set the state to playing
            playPauseButton.GetComponentInChildren<TextMeshProUGUI>().text = "Pause"; // Update button text to "Pause"
        }
        else
        {
            audioSource.Pause();
            isPlaying = false; // Set the state to paused
            playPauseButton.GetComponentInChildren<TextMeshProUGUI>().text = "Play"; // Update button text to "Play"
        }
    }

    // Generate waveform sound based on selected parameters
    private void GenerateWaveform()
    {
        int sampleRate = AudioSettings.outputSampleRate;
        float[] data = new float[sampleRate];

        for (int i = 0; i < data.Length; i++)
        {
            float time = i / (float)sampleRate;
            float sample = 0f;

            // Use the sine wave since that's the only option now
            if (waveform == "Sine")
            {
                sample = Mathf.Sin(2 * Mathf.PI * pitch * time);
            }

            // Apply volume and store the sample
            data[i] = sample * volume;
        }

        // Create an AudioClip from the data
        AudioClip waveformClip = AudioClip.Create("Waveform", data.Length, 1, AudioSettings.outputSampleRate, false);
        waveformClip.SetData(data, 0);
        audioSource.clip = waveformClip;
    }

    // Visualize the waveform for the generated wave (used when no external audio is playing)
    private void VisualizeWaveformStatic()
    {
        int sampleRate = AudioSettings.outputSampleRate;
        Vector3[] positions = new Vector3[sampleRate];

        float scaleX = 0.005f; // Adjust this for the horizontal scale of the waveform
        float scaleY = 0.5f;   // Adjust this for the vertical scale (amplitude)

        for (int i = 0; i < positions.Length; i++)
        {
            float time = i / (float)sampleRate;
            float sample = Mathf.Sin(2 * Mathf.PI * pitch * time);

            // Apply volume scaling and adjust positions for visualization
            positions[i] = new Vector3(i * scaleX - (sampleRate * scaleX / 2), sample * scaleY, 0);
        }

        // Update the LineRenderer's positions to visualize the waveform
        lineRenderer.positionCount = positions.Length;
        lineRenderer.SetPositions(positions);

        // Adjust the local position of the waveform inside the container
        lineRenderer.transform.localPosition = Vector3.zero;
    }

    // Visualize the waveform using a LineRenderer for external audio (reacts to external clip)
    private void VisualizeWaveformFromAudio()
    {
        float[] data = new float[1024];

        // Get the waveform data from the audio source using GetOutputData
        audioSource.GetOutputData(data, 0);

        Vector3[] positions = new Vector3[data.Length];

        float scaleX = 0.005f; // Adjust this for the horizontal scale of the waveform
        float scaleY = 0.5f;   // Adjust this for the vertical scale (amplitude)

        for (int i = 0; i < data.Length; i++)
        {
            float sample = data[i];

            // Scale and offset the positions so it's visible and centered within the UI panel
            positions[i] = new Vector3(i * scaleX - (data.Length * scaleX / 2), sample * scaleY, 0);
        }

        // Update the LineRenderer's positions to visualize the waveform
        lineRenderer.positionCount = positions.Length;
        lineRenderer.SetPositions(positions);

        // Adjust the local position of the waveform inside the container
        lineRenderer.transform.localPosition = Vector3.zero;
    }

    // Method to calculate peak frequency using FFT
    private float CalculatePeakFrequency(float[] spectrum)
    {
        int sampleRate = AudioSettings.outputSampleRate;
        float maxSpectrum = 0;
        int maxIndex = 0;

        // Find the index with the highest magnitude in the spectrum
        for (int i = 0; i < spectrum.Length / 2; i++)
        {
            if (spectrum[i] > maxSpectrum)
            {
                maxSpectrum = spectrum[i];
                maxIndex = i;
            }
        }

        // Convert index to frequency
        return maxIndex * sampleRate / spectrum.Length;
    }

    // Update the amplitude and frequency
    private void UpdateAudioInfo()
    {
        float[] audioSamples = new float[FFT_SIZE];
        audioSource.GetOutputData(audioSamples, 0);

        // Calculate Peak Amplitude
        float peakAmplitude = 0f;
        for (int i = 0; i < audioSamples.Length; i++)
        {
            peakAmplitude = Mathf.Max(peakAmplitude, Mathf.Abs(audioSamples[i]));
        }

        // Perform FFT to get frequency
        float[] spectrum = new float[FFT_SIZE];
        AudioListener.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        // Find the peak frequency
        float maxFrequency = CalculatePeakFrequency(spectrum);

        // Update the info text with peak amplitude (linear value)
        infoText.text = $"Amplitude: {peakAmplitude:F2}\nFrequency: {maxFrequency:F2} Hz";
    }

    // Update the frequency and amplitude without changing values during pause
    private void UpdateAudioInfoStatic()
    {
        // Just display the last known amplitude and frequency
        infoText.text = $"Amplitude: {volume:F2}\nFrequency: {pitch:F2} Hz";
    }

    // Update the playback slider when it is changed
    private void OnPlaybackSliderChanged(float value)
    {
        if (audioSource.clip != null)
        {
            audioSource.time = value * audioSource.clip.length;
        }
    }

    // Set waveform to sine wave (only option)
    private void SetWaveformToSine()
    {
        // No longer necessary to set anything as it's fixed in the script
    }
}