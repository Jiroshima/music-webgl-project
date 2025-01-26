using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class SoundSynthesizer : MonoBehaviour
{
    public AudioSource audioSource;

    // managing the audio samples
    public AudioClip[] audioSamples; // an array to store the audio clips 
    public Button[] sampleButtons; // an array to store the buttons for selecting audio samples
    private int currentSampleIndex = -1; // this keeps track of the audio sample that is currently selected (default is none)

    // audio properties 
    public float pitch = 1f; // default to 1 
    public float volume = 1f; // default to 1 (full volume)
    public float playbackRate = 1f; // 1 is normal speed

    // we will only be using one waveform type which is sine 
    private const string waveform = "Sine";  

    // different sliders to manipulate the audio samples
    public Slider pitchSlider;    
    public Slider volumeSlider;     
    public Slider playbackRateSlider; 

    // text UI elements to display different audio values (pitch, volume, playback rate and playback time)
    public TMP_Text pitchValueText;  
    public TMP_Text volumeValueText; 
    public TMP_Text playbackRateValueText;
    public TMP_Text playbackTimeText; 


    // reference for line renderer to visualise the waveform
    public LineRenderer lineRenderer;
    public TMP_Text infoText; // this is to display the information text about the audio sample

    public RectTransform waveformContainer; // parent object for the waveform visualisation called waveform container
 
    // ui elements for pause/play button and the playback slider 
    public Button playPauseButton;
    public Slider playbackSlider;

    private bool isPlaying = false; // keeps track of if the audio is playing
    private const int FFT_SIZE = 1024; // FFT (fast fourier transform ), this will convert the audio signal from time-domain data to frequency domain which we will use to create the waveforms
    // FFT_SIZE determines how many frequency bins are used, so higher tends to be more detail.

    private void Start()
    {
        // get the audio source component attached to this gameobject 
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true; // loops audio 

        // iterate over all the buttons stored in sampleButtons
        for (int i = 0; i < sampleButtons.Length; i++)
        {
            int index = i; // capture the current index for the lambda (lambda function gets the correct index value for each button)
            sampleButtons[i].onClick.AddListener(() => SelectAudioSample(index)); // add button click listener 
        }

        // disable play button initially
        playPauseButton.interactable = false;

        // link slider events to the corresponding functions 
        pitchSlider.onValueChanged.AddListener(UpdatePitch);
        volumeSlider.onValueChanged.AddListener(UpdateVolume);
        playbackRateSlider.onValueChanged.AddListener(UpdatePlaybackRate);

        // set slider ranges
        pitchSlider.minValue = 0f;
        pitchSlider.maxValue = 2f;
        pitchSlider.value = 1f;  // default value

        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 100f;  // volume slider goes from 0 to 100
        volumeSlider.value = 100f;  // with maximum as a default value 

        playbackRateSlider.minValue = 0f; // play back goes from 0-2, with 1 default value 
        playbackRateSlider.maxValue = 2f;
        playbackRateSlider.value = 1f;  

        // update slider values on start
        // added units (Hz for pitch, % for vlume and 'x' for playback rate)
        pitchValueText.text = pitchSlider.value.ToString("F2") + " Hz";
        volumeValueText.text = volumeSlider.value.ToString("F2") + "%"; 
        playbackRateValueText.text = playbackRateSlider.value.ToString("F2") + "x"; 

        // play/pause button 
        playPauseButton.onClick.AddListener(TogglePlayPause);

        // playback slider
        playbackSlider.onValueChanged.AddListener(OnPlaybackSliderChanged);

        // this is to ensure that the line renderer starts within the container and uses local space
        // (when using world space, it draws at 0, 0, 0 automatically)
        lineRenderer.transform.SetParent(waveformContainer, false);
        lineRenderer.transform.localPosition = Vector3.zero; // set to center of the container 
        lineRenderer.useWorldSpace = false; // disable world space

        // this is to set the sorting layer to 1 to ensure that it renders in front of everything else.
        // because line renderer is usually a 3D component, the UI tends to render above it and it can get very fidgity. This is to help make sure that it renders above UI elements. 
        lineRenderer.sortingLayerName = "UI"; 
        lineRenderer.sortingOrder = 1;

        // set material and color for the lineRenderer.
        // again because it is usually a 3D component the visibility can be funny sometimes.
        // lighting/shadow of the material can also affect the visibility. This is to make sure it appears clearly 
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.material.color = Color.red; // set colour to red 

        // ensures the LineRenderer is on the 2D plane
        lineRenderer.transform.position = new Vector3(lineRenderer.transform.position.x, lineRenderer.transform.position.y, 0f);

        // initialize playback time display 
        UpdatePlaybackTimeText();

        // disable the line renderer initially 
        lineRenderer.positionCount = 0; 
    }

    // method to select an audio sample based on button click
    private void SelectAudioSample(int sampleIndex)
    {
        // deselect previous buttons
        for (int i = 0; i < sampleButtons.Length; i++)
        {
            ColorBlock colors = sampleButtons[i].colors;
            colors.normalColor = Color.white;
            sampleButtons[i].colors = colors;
        }

        // highlight selected button
        ColorBlock selectedColors = sampleButtons[sampleIndex].colors;
        selectedColors.normalColor = Color.green;
        sampleButtons[sampleIndex].colors = selectedColors;

        // set the selected audio clip and reset the playback 
        audioSource.clip = audioSamples[sampleIndex];
        currentSampleIndex = sampleIndex;

        // stop any audio that is currently playing 
        audioSource.Stop();
        isPlaying = false;
        playPauseButton.GetComponentInChildren<TextMeshProUGUI>().text = "Play";

        // enable play button
        playPauseButton.interactable = true;

        // update playback time text
        UpdatePlaybackTimeText();

        // visualize the waveform of the selected sample
        VisualizeWaveformFromAudio();

        // enable LineRenderer after selecting the sample (if it was previously disabled)
        lineRenderer.positionCount = 0; // and reset any previous waveform line
    }


    private void Update()
    {
        // if audio is not playing, then generate waveform for custom sound 
        if (audioSource.clip == null || !audioSource.isPlaying)
        {
            if (isPlaying)
            {
                GenerateWaveform();
            }
        }

        // visualise the waveform while playing external audio 
        if (audioSource.isPlaying && audioSource.clip != null)
        {
            VisualizeWaveformFromAudio(); // visualise waveform from external audio clip 
            UpdateAudioInfo(); // update frequency and amplite infiromation 
        }
        else if (!audioSource.isPlaying && isPlaying)
        {
            // visualise the static waveform during pause
            VisualizeWaveformStatic(); 
            // while keeping the last known values of frequency and amplitude.
            // this is so that we can still analyse the waveforms more closely by pausing. 
            UpdateAudioInfoStatic();
        }

        // updates the playblack slider and time text based on the audio playback time 
        if (audioSource.isPlaying)
        {
            playbackSlider.value = audioSource.time / audioSource.clip.length; 
            UpdatePlaybackTimeText();
        }
    }

    // Method to toggle play and pause based on current state
    private void TogglePlayPause()
    {
        if (currentSampleIndex == -1) return; // ensure a sample is selected

        if (!isPlaying) 
        {
            audioSource.Play(); // start playing audio 
            isPlaying = true;
            playPauseButton.GetComponentInChildren<TextMeshProUGUI>().text = "Pause"; // changes the button text to pause
        }
        else
        {
            audioSource.Pause(); 
            isPlaying = false;
            playPauseButton.GetComponentInChildren<TextMeshProUGUI>().text = "Play"; // if not, change button text to play 
        }
    }

    // Update playback time text
    private void UpdatePlaybackTimeText()
    {
        if (audioSource.clip != null) // checks if audio clip is assigned
        {
            string currentTime = FormatTime(audioSource.time); // get the current playback time 
            string totalTime = FormatTime(audioSource.clip.length); // get total length of the audio clip 
            playbackTimeText.text = $"{currentTime} / {totalTime}"; // update text display to show current / total 
        }
        else
        {
            playbackTimeText.text = "00:00 / 00:00"; //if no audio clip is assigned then default to 00:00 / 00:00 
        }
    }

    // method to convert given time to mm:ss 
    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60); // calculate number of minutes by dividing time by 60
        int seconds = Mathf.FloorToInt(time % 60); // calculate number of seconds using modulus 
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // update the pitch of the audio 
    private void UpdatePitch(float value)
    {
        pitch = value;
        audioSource.pitch = pitch; 
        pitchValueText.text = pitch.ToString("F2") + " Hz"; 
    }

    // update the volume 
    private void UpdateVolume(float value)
    {
        volume = value / 100f; // divide slider value by 100 to convert to 0 - 1 (valid range for audioSource.volume)
        audioSource.volume = volume; // update volume of audio 
        volumeValueText.text = value.ToString("F2") + "%"; // displays as a percentage 
    }

    // update the playback rate of the audio 
    private void UpdatePlaybackRate(float value)
    {
        playbackRate = value;
        audioSource.pitch = playbackRate; // the value from the slider is set to audioSource.pitch since playback rate affects pitch 
        playbackRateValueText.text = playbackRate.ToString("F2") + "x"; 
    }

    // method to generate a sine wave based on the pitch and volume values
    // create a float array that holds the waveform samples for each point in time
    // calculate the sine wave's value using Mathf.Sin()
    // then convert waveform to an audioclip using AudioClip.Create() then assign to audioSource for playback
    // then update the playback time display to reflect the new waveform 
    private void GenerateWaveform()
    {
        int sampleRate = AudioSettings.outputSampleRate; // get audio sample rate
        float[] data = new float[sampleRate]; // create an array to hold waveform data 

        for (int i = 0; i < data.Length; i++) // generate the waveform 
        {
            float time = i / (float)sampleRate; // calculate the time at each sample point 
            float sample = Mathf.Sin(2 * Mathf.PI * pitch * time); // generate a sine wave sample 
        
            // apply volume scaling 
            data[i] = sample * volume;
        }

        // create an audio clip from the generated waveform data 
        AudioClip waveformClip = AudioClip.Create("Waveform", data.Length, 1, AudioSettings.outputSampleRate, false);
        waveformClip.SetData(data, 0); // set data for the audioclip 
        audioSource.clip = waveformClip; // assign the generated audioclip to the audiosource 

        // update playback time text after generating waveform
        UpdatePlaybackTimeText();
    }

    // visualize the waveform for the generated wave using line renderer
    // generate an array of vector 3 positions, each to represent a sample of the waveform
    // convert these samples to 3D positions (using scaleX for horizontal positioning and scaleY for vertical scaling)
    // then update linerenderer with the new position to visualise the waveform. 

    private void VisualizeWaveformStatic()
    {
        int sampleRate = AudioSettings.outputSampleRate; // get the audio sample rate from unity settings
        Vector3[] positions = new Vector3[sampleRate]; // create an array to store the positions for the waveform visualisation 

        float scaleX = 0.005f; // horizontal scale of waveform
        float scaleY = 0.5f;   // vertical scale (amplitude)

        // generate waveform data 
        for (int i = 0; i < positions.Length; i++) // loop through each position in the positions array 
        {
            float time = i / (float)sampleRate; // calculate the time of the current sample by dividing the current sample index by the sample rate to give time in seconds for that sample
            float sample = Mathf.Sin(2 * Mathf.PI * pitch * time); // generate the amplitude of the sine wave at the given time.
            // equation inside the brackets represents the phase of the sine wave based on the frequency and time 
            // MathF.PI function is to calculate the sine of this phase which gives the amplitude value for that point in the wave 

            // now calculate the 3d position for the current sample's point on the waveform. 
            // i * scaleX - (sampleRate * scaleX / 2) scales i and shift it so the waveform is centered around the middle.
            // multiple scaleY by sample for the vertical position based on the amplitude of the sine wave
            // z is ofcourse 0 as we are visualising in a 2D space. 
            positions[i] = new Vector3(i * scaleX - (sampleRate * scaleX / 2), sample * scaleY, 0);
            
        }

        lineRenderer.positionCount = positions.Length; // set total number of positions for the line renderer
        lineRenderer.SetPositions(positions);  // update the line renderer with the new positions stored in the positions array. 
        // line renderer will draw a line to connect all the points to create the waveform . 

        // makes sure that the waveform is centered in the container 
        lineRenderer.transform.localPosition = Vector3.zero;
    }

    // method to visualise a waveform using audio data from an audiosource 
    private void VisualizeWaveformFromAudio()
    {
        float[] data = new float[1024]; // create an array of size 1024, (1024 is the number of samples)

        // checks if  there is an audio clip assigned to the audio source. 
        if (audioSource.clip != null)
        {
            audioSource.GetOutputData(data, 0); // retrieve audio data and store it in the data array 

            Vector3[] positions = new Vector3[data.Length]; // create an array of vector3 positions with same length as the data array (each position -> a point in the 3D space for line renderer)

            float scaleX = 0.005f; // adjusts how spread out the waveform is horizontally 
            float scaleY = 0.5f;   // adjust amplitude by scale factor 0.5 

            for (int i = 0; i < data.Length; i++) // iterate through each sample in the data array 
            {
                float sample = data[i]; // get ampitude of audio sample at the current index 

                // calculate the position of each point in the waveform 
                positions[i] = new Vector3(i * scaleX - (data.Length * scaleX / 2), sample * scaleY, 0);
            }

            // sets the total number of points to render for line renderer
            // then update the line renderer with the new positions 
            lineRenderer.positionCount = positions.Length;
            lineRenderer.SetPositions(positions);

            // adjust the position of the line renderer in the container 
            lineRenderer.transform.localPosition = Vector3.zero;
        }
    }

    // method to calculate peak frequency using FFT
    private float CalculatePeakFrequency(float[] spectrum)
    {
        int sampleRate = AudioSettings.outputSampleRate; // get the output sample rate of audio system 
        float maxSpectrum = 0; // keeps track of the highest magnitude in the spectrum 
        int maxIndex = 0; // stores the index where the max value was found 

        // iterate through the first half of the spectrum array (second half of the spectrum is redundant because the frequencies are symmetrical )
        for (int i = 0; i < spectrum.Length / 2; i++)
        {
            if (spectrum[i] > maxSpectrum) // check if the magnitude of the current frequency bin is larger than the previous largest value 
            {
                maxSpectrum = spectrum[i]; // update maximum magnitude
                maxIndex = i; // update the index of max magnitude 
            }
        }

        // maxindex is converted to a frequency value and returns it 
        return maxIndex * sampleRate / spectrum.Length;
    }

    // update the amplitude and frequency
    private void UpdateAudioInfo()
    {
        float[] audioSamples = new float[FFT_SIZE]; // create an array of audio samples and fill it audio data. 
        audioSource.GetOutputData(audioSamples, 0); // get the audio samples from the audio source 

        
        float peakAmplitude = 0f; // will hold the largest amplitude value found 
        for (int i = 0; i < audioSamples.Length; i++) // iterate through the audio samples 
        {
            peakAmplitude = Mathf.Max(peakAmplitude, Mathf.Abs(audioSamples[i])); // compare the absolute value of the current sample with the previous largest 
            // and update peakAmplitude if current sample is bigger 
        }

        
        float[] spectrum = new float[FFT_SIZE]; // array to store the frequency spectrum data 
        AudioListener.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris); // fill it with frequency data from the current the current audio playing. 
        //FFTWINDOW.BlackmanHarris reduces chance of spectral leakage to provide cleaner and more accurate representation

        // find the peak frequency
        float maxFrequency = CalculatePeakFrequency(spectrum);

        // update the info text with peak amplitude (linear value)
        infoText.text = $"Amplitude: {peakAmplitude:F2}\nFrequency: {maxFrequency:F2} Hz";
    }

    // update the frequency and amplitude without changing values during pause
    private void UpdateAudioInfoStatic()
    {
        // just display the last known amplitude and frequency
        infoText.text = $"Amplitude: {volume:F2}\nFrequency: {pitch:F2} Hz";
    }

    // update the playback slider when it is changed
    private void OnPlaybackSliderChanged(float value)
    {
        if (audioSource.clip != null)
        {
            audioSource.time = value * audioSource.clip.length;
            UpdatePlaybackTimeText(); // update time text when slider is changed
        }
    }
}