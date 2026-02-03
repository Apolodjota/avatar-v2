using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.InputSystem;

namespace GoogleSpeechToText.Scripts
{
    public class SpeechToTextManager : MonoBehaviour
    {
        // [SerializeField] private string audioUri = "gs://cloud-samples-tests/speech/brooklyn.flac"; // Audio file URI in Google Cloud Storage
        [Header("Google Cloud API Password")]
        [SerializeField] private string apiKey; // Replace with your API key
        [Header("Gemini Manager Prefab")]
        public UnityAndGeminiV3 geminiManager;
                
        private AudioClip clip;
        private byte[] bytes;
        private bool recording = false;

    void Update()
    {
        // Teclado (Nuevo Input System)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame && !recording)
            {
                StartRecording();
            }
            else if (Keyboard.current.spaceKey.wasReleasedThisFrame && recording)
            {
                StopRecording();
            }
        }

        // Controladores VR (Oculus Integration / OpenXR)
        // Usamos OVRInput porque es lo que estaba en el proyecto original
        // Botón A (One) en controlador derecho
        if (OVRInput.GetDown(OVRInput.Button.One) && !recording)
        {
             StartRecording();
        }
        else if (OVRInput.GetUp(OVRInput.Button.One) && recording)
        {
             StopRecording();
        }
    }

    private void StartRecording()
    {
        // Intentar buscar el micrófono correcto (Quest)
        string micDevice = null;
        foreach (var device in Microphone.devices)
        {
            if (device.Contains("Oculus") || device.Contains("Headset"))
            {
                micDevice = device;
                break;
            }
        }

        clip = Microphone.Start(micDevice, false, 30, 44100); // 30s max
        recording = true;
        Debug.Log($"[SpeechToText] 🎤 Grabando... Dispositivo: {micDevice ?? "Default"}");
    }

    private byte[] EncodeAsWAV(float[] samples, int frequency, int channels) {
        using (var memoryStream = new MemoryStream(44 + samples.Length * 2)) {
            using (var writer = new BinaryWriter(memoryStream)) {
                writer.Write("RIFF".ToCharArray());
                writer.Write(36 + samples.Length * 2);
                writer.Write("WAVE".ToCharArray());
                writer.Write("fmt ".ToCharArray());
                writer.Write(16);
                writer.Write((ushort)1);
                writer.Write((ushort)channels);
                writer.Write(frequency);
                writer.Write(frequency * channels * 2);
                writer.Write((ushort)(channels * 2));
                writer.Write((ushort)16);
                writer.Write("data".ToCharArray());
                writer.Write(samples.Length * 2);

                foreach (var sample in samples) {
                    writer.Write((short)(sample * short.MaxValue));
                }
            }
            return memoryStream.ToArray();
        }
    }

    private void StopRecording()
    {
            var position = Microphone.GetPosition(null);
            Microphone.End(null);
            var samples = new float[position * clip.channels];
            clip.GetData(samples, 0);
            bytes = EncodeAsWAV(samples, clip.frequency, clip.channels);
            recording = false;

            GoogleCloudSpeechToText.SendSpeechToTextRequest(bytes, apiKey,
                (response) => {
                    Debug.Log("Speech-to-Text Response: " + response);
                    // Parse the response if needed
                    var speechResponse = JsonUtility.FromJson<SpeechToTextResponse>(response);
                    var transcript = speechResponse.results[0].alternatives[0].transcript;
                    Debug.Log("Transcript: " + transcript);
                    geminiManager.SendChat(transcript);

                },
                (error) => {
                    Debug.LogError("Error: " + error.error.message);
                });
    }

    }
}
