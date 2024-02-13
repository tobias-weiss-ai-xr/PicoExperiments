using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Service;
using UnityEngine;
using static Service.GetResponseRequest.Types;

namespace Convai.Scripts.Utils
{
    /// <summary>
    ///     This class is dedicated to manage all communications between the Convai server and plugin, in addition to
    ///     processing any data transmitted during these interactions. It abstracts the underlying complexities of the plugin,
    ///     providing a seamless interface for users. Modifications to this class are discouraged as they may impact the
    ///     stability and functionality of the system. This class is maintained by the development team to ensure compatibility
    ///     and performance.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ConvaiNPCManager))]
    [AddComponentMenu("Convai/Convai GRPC API")]
    [HelpURL(
        "https://docs.convai.com/api-docs/plugins-and-integrations/unity-plugin/scripts-overview/convaigrpcapi.cs")]
    public class ConvaiGRPCAPI : MonoBehaviour
    {
        public static ConvaiGRPCAPI Instance;
        private readonly List<string> _stringUserText = new();
        private ConvaiNPC _activeConvaiNPC;
        private CancellationTokenSource _cancellationTokenSource;
        private ConvaiChatUIHandler _chatUIHandler;
        private string APIKey { get; set; }

        private void Awake()
        {
            // Singleton pattern: Ensure only one instance of this script is active.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Load API key from a ScriptableObject in Resources folder.
            ConvaiAPIKeySetup apiKeyScriptableObject = Resources.Load<ConvaiAPIKeySetup>("ConvaiAPIKey");

            if (apiKeyScriptableObject != null)
                // If the API key data is found, assign it to the APIKey field.
                APIKey = apiKeyScriptableObject.APIKey;
            else
                // Log an error if API key data is missing.
                Logger.Error(
                    "No API Key data found. Please complete the Convai Setup. In the Menu Bar, click Convai > Setup.",
                    Logger.LogCategory.Character);

            // Find and store a reference to the ConvaiChatUIHandler component in the scene.
            _chatUIHandler = FindObjectOfType<ConvaiChatUIHandler>();
        }

        private void Start()
        {
            ConvaiNPCManager.Instance.OnActiveNPCChanged += HandleActiveNPCChanged;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        private void FixedUpdate()
        {
            // Check if there are pending user texts to display
            // If chatUIHandler is available, send the first user text in the list
            if (_stringUserText.Count > 0 && _chatUIHandler != null)
            {
                _chatUIHandler.SendPlayerText(_stringUserText[0]);
                // Remove the displayed user text from the list
                _stringUserText.RemoveAt(0);
            }
        }

        private void OnDestroy()
        {
            ConvaiNPCManager.Instance.OnActiveNPCChanged -= HandleActiveNPCChanged;

            InterruptCharacterSpeech();
            try
            {
                _cancellationTokenSource?.Cancel();
            }
            catch (Exception ex)
            {
                // Handle the Exception, which can occur if the CancellationTokenSource is already disposed. 
                Logger.Warn("Exception in OnDestroy: " + ex.Message, Logger.LogCategory.Character);
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                Logger.Info("The Cancellation Token Source was Disposed in OnDestroy.", Logger.LogCategory.Character);
            }
        }

        /// <summary>
        ///     Asynchronously initializes a session ID by communicating with a gRPC service and returns the session ID if
        ///     successful.
        /// </summary>
        /// <param name="characterName">The name of the character for which the session is being initialized.</param>
        /// <param name="client">The gRPC service client used to make the call to the server.</param>
        /// <param name="characterID">The unique identifier for the character.</param>
        /// <param name="sessionID">The session ID that may be updated during the initialization process.</param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task result contains the initialized session ID if
        ///     successful, or null if the initialization fails.
        /// </returns>
        public static async Task<string> InitializeSessionIDAsync(string characterName,
            ConvaiService.ConvaiServiceClient client,
            string characterID, string sessionID)
        {
            Logger.DebugLog("Initializing SessionID for character: " + characterName, Logger.LogCategory.Character);

            if (client == null)
            {
                Logger.Error("gRPC client is not initialized.", Logger.LogCategory.Character);
                return null;
            }

            using AsyncDuplexStreamingCall<GetResponseRequest, GetResponseResponse> call = client.GetResponse();
            GetResponseRequest getResponseConfigRequest = new()
            {
                GetResponseConfig = new GetResponseConfig
                {
                    CharacterId = characterID,
                    ApiKey = Instance.APIKey,
                    SessionId = sessionID,
                    AudioConfig = new AudioConfig { DisableAudio = true }
                }
            };

            try
            {
                await call.RequestStream.WriteAsync(getResponseConfigRequest);
                await call.RequestStream.WriteAsync(new GetResponseRequest
                {
                    GetResponseData = new GetResponseData
                    {
                        TextData = "Repeat the following exactly as it is: [Hii]"
                    }
                });

                await call.RequestStream.CompleteAsync();

                while (await call.ResponseStream.MoveNext())
                {
                    GetResponseResponse result = call.ResponseStream.Current;

                    if (!string.IsNullOrEmpty(result.SessionId))
                    {
                        Logger.DebugLog("SessionID Initialization SUCCESS for: " + characterName,
                            Logger.LogCategory.Character);
                        sessionID = result.SessionId;
                        return sessionID;
                    }
                }

                Logger.Exception("SessionID Initialization FAILED for: " + characterName, Logger.LogCategory.Character);
            }
            catch (RpcException rpcException)
            {
                switch (rpcException.StatusCode)
                {
                    case StatusCode.Cancelled:
                        Logger.Exception(rpcException, Logger.LogCategory.Character);
                        break;
                    case StatusCode.Unknown:
                        Logger.Error($"Unknown error from server: {rpcException.Status.Detail}",
                            Logger.LogCategory.Character);
                        break;
                    default:
                        throw;
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, Logger.LogCategory.Character);
            }

            return null;
        }


        /// <summary>
        ///     Sends text data to the server and processes the response.
        /// </summary>
        /// <param name="client">The gRPC client used to communicate with the server.</param>
        /// <param name="userText">The text data to send to the server.</param>
        /// <param name="characterID">The ID of the character that is sending the text.</param>
        /// <param name="isActionActive">Indicates whether actions are active.</param>
        /// <param name="isLipSyncActive">Indicates whether lip sync is active.</param>
        /// <param name="actionConfig">The action configuration.</param>
        /// <param name="faceModel">The face model.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task SendTextData(
            ConvaiService.ConvaiServiceClient client,
            string userText,
            string characterID,
            bool isActionActive,
            bool isLipSyncActive,
            ActionConfig actionConfig,
            FaceModel faceModel)
        {
            AsyncDuplexStreamingCall<GetResponseRequest, GetResponseResponse> call =
                GetAsyncDuplexStreamingCallOptions(client);

            GetResponseRequest getResponseConfigRequest = CreateGetResponseRequest(
                isActionActive,
                isLipSyncActive,
                0,
                characterID,
                actionConfig,
                faceModel);

            try
            {
                await call.RequestStream.WriteAsync(getResponseConfigRequest);
                await call.RequestStream.WriteAsync(new GetResponseRequest
                {
                    GetResponseData = new GetResponseData
                    {
                        TextData = userText
                    }
                });
                await call.RequestStream.CompleteAsync();

                // Store the task that receives results from the server.
                Task receiveResultsTask = Task.Run(
                    async () => { await ReceiveResultFromServer(call, _cancellationTokenSource.Token); },
                    _cancellationTokenSource.Token);

                // Await the task if needed to ensure it completes before this method returns [OPTIONAL]
                await receiveResultsTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, Logger.LogCategory.Character);
            }
        }

        // This method will be called whenever the active NPC changes.
        private void HandleActiveNPCChanged(ConvaiNPC newActiveNPC)
        {
            if (newActiveNPC == null)
            {
                // Cancel the ongoing gRPC call
                try
                {
                    _cancellationTokenSource?.Cancel();
                }
                catch (Exception e)
                {
                    // Handle the Exception, which can occur if the CancellationTokenSource is already disposed. 
                    Logger.Warn("Exception in GRPCAPI:HandleActiveNPCChanged: " + e.Message, Logger.LogCategory.Character);
                }
                finally
                {
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = null;
                    Logger.Info("The Cancellation Token Source was Disposed in GRPCAPI:HandleActiveNPCChanged", Logger.LogCategory.Character);
                }

                _cancellationTokenSource = new CancellationTokenSource(); // Create a new token for future calls
            }

            _activeConvaiNPC = newActiveNPC;
        }

        /// <summary>
        ///     Converts a byte array representing 16-bit audio samples to a float array.
        /// </summary>
        /// <param name="source">Byte array containing 16-bit audio data</param>
        /// <returns>Float array containing audio samples in the range [-1, 1]</returns>
        private static float[] Convert16BitByteArrayToFloatAudioClipData(byte[] source)
        {
            const int x = sizeof(short); // Size of a short in bytes
            int convertedSize = source.Length / x; // Number of short samples
            float[] data = new float[convertedSize]; // Float array to hold the converted data

            int byteIndex = 0; // Index for the byte array
            int dataIndex = 0; // Index for the float array

            // Convert each pair of bytes to a short and then to a float
            while (byteIndex < source.Length)
            {
                byte firstByte = source[byteIndex];
                byte secondByte = source[byteIndex + 1];
                byteIndex += 2;

                // Combine the two bytes to form a short (little endian)
                short s = (short)((secondByte << 8) | firstByte);

                // Convert the short value to a float in the range [-1, 1]
                data[dataIndex] = s / 32768.0F; // Dividing by 32768.0 to normalize the range
                dataIndex++;
            }

            return data;
        }

        /// <summary>
        ///     Converts a byte array containing audio data into an AudioClip.
        /// </summary>
        /// <param name="byteAudio">Byte array containing the audio data</param>
        /// <param name="stringSampleRate">String containing the sample rate of the audio</param>
        /// <returns>AudioClip containing the decoded audio data</returns>
        public AudioClip ProcessByteAudioDataToAudioClip(byte[] byteAudio, string stringSampleRate)
        {
            try
            {
                if (byteAudio.Length <= 44)
                    throw new ArgumentException("Not enough data in byte audio to trim the header.", nameof(byteAudio));

                // Trim the 44 bytes WAV header from the byte array to get the actual audio data
                byte[] trimmedByteAudio = new byte[byteAudio.Length - 44];
                for (int i = 0, j = 44; i < byteAudio.Length - 44; i++, j++) trimmedByteAudio[i] = byteAudio[j];

                // Convert the trimmed byte audio data to a float array of audio samples
                float[] samples = Convert16BitByteArrayToFloatAudioClipData(trimmedByteAudio);
                if (samples.Length <= 0) throw new Exception("No samples created after conversion from byte array.");

                const int channels = 1; // Mono audio
                int sampleRate = int.Parse(stringSampleRate); // Convert the sample rate string to an integer

                // Create an AudioClip using the converted audio samples and other parameters
                AudioClip clip = AudioClip.Create("Audio Response", samples.Length, channels, sampleRate, false);

                // Set the audio data for the AudioClip
                clip.SetData(samples, 0);

                return clip;
            }
            catch (Exception)
            {
                // Log or handle exceptions appropriately
                return null;
            }
        }

        // Events to notify when the player starts or stops speaking
        public event Action<bool> OnPlayerSpeakingChanged;

        // Events to notify when the character starts or stops speaking
        // public event Action<bool> OnCharacterSpeakingChanged;


        /// <summary>
        ///     Starts recording audio and sends it to the server for processing.
        /// </summary>
        /// <param name="client">gRPC service Client object</param>
        /// <param name="isActionActive">Bool specifying whether we are expecting action responses</param>
        /// <param name="isLipSyncActive"></param>
        /// <param name="recordingFrequency">Frequency of the audio being sent</param>
        /// <param name="recordingLength">Length of the recording from the microphone</param>
        /// <param name="characterID">Character ID obtained from the playground</param>
        /// <param name="actionConfig">Object containing the action configuration</param>
        /// <param name="faceModel"></param>
        public async Task StartRecordAudio(
            ConvaiService.ConvaiServiceClient client,
            bool isActionActive,
            bool isLipSyncActive,
            int recordingFrequency,
            int recordingLength,
            string characterID,
            ActionConfig actionConfig,
            FaceModel faceModel)
        {
            AsyncDuplexStreamingCall<GetResponseRequest, GetResponseResponse>
                call = GetAsyncDuplexStreamingCallOptions(client);

            GetResponseRequest getResponseConfigRequest = CreateGetResponseRequest(
                isActionActive,
                isLipSyncActive,
                recordingFrequency,
                characterID,
                actionConfig,
                faceModel);

            Logger.DebugLog(getResponseConfigRequest.ToString(), Logger.LogCategory.Character);

            try
            {
                await call.RequestStream.WriteAsync(getResponseConfigRequest);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, Logger.LogCategory.Character);
                return; // early return on error
            }

            AudioClip audioClip = Microphone.Start(MicrophoneManager.Instance.GetSelectedMicrophoneDevice(),
                false, recordingLength, recordingFrequency);

            MicrophoneTestController.Instance.CheckMicrophoneDeviceWorkingStatus(audioClip);

            Logger.Info(_activeConvaiNPC.characterName + " is now listening", Logger.LogCategory.Character);
            OnPlayerSpeakingChanged?.Invoke(true);

            await ProcessAudioContinuously(call, recordingFrequency, recordingLength, audioClip);
        }

        private AsyncDuplexStreamingCall<GetResponseRequest, GetResponseResponse> GetAsyncDuplexStreamingCallOptions(
            ConvaiService.ConvaiServiceClient client)
        {
            Metadata headers = new()
            {
                { "source", "Unity" },
                { "version", "2.1.0" }

            };

            CallOptions options = new(headers);
            return client.GetResponse(options);
        }

        /// <summary>
        ///     Creates a GetResponseRequest object configured with the specified parameters for initiating a gRPC call.
        /// </summary>
        /// <param name="isActionActive">Indicates whether actions are enabled for the character.</param>
        /// <param name="isLipSyncActive">Indicates whether lip sync is enabled for the character.</param>
        /// <param name="recordingFrequency">The frequency at which the audio is recorded.</param>
        /// <param name="characterID">The unique identifier for the character.</param>
        /// <param name="actionConfig">The configuration for character actions.</param>
        /// <param name="faceModel">The facial model configuration for the character.</param>
        /// <returns>A GetResponseRequest object configured with the provided settings.</returns>
        private GetResponseRequest CreateGetResponseRequest(
            bool isActionActive,
            bool isLipSyncActive,
            int recordingFrequency,
            string characterID,
            ActionConfig actionConfig,
            FaceModel faceModel)
        {
            GetResponseRequest getResponseConfigRequest = new()
            {
                GetResponseConfig = new GetResponseConfig
                {
                    CharacterId = characterID,
                    ApiKey = APIKey, // Assumes apiKey is available
                    SessionId = _activeConvaiNPC
                        .sessionID, // Assumes _activeConvaiNPC would not be null, else this will throw NullReferenceException

                    AudioConfig = new AudioConfig
                    {
                        SampleRateHertz = recordingFrequency,
                        EnableFacialData = isLipSyncActive,
                        FaceModel = faceModel
                    }
                }
            };

            if (isActionActive || _activeConvaiNPC != null)
                getResponseConfigRequest.GetResponseConfig.ActionConfig = actionConfig;

            return getResponseConfigRequest;
        }

        /// <summary>
        ///     Processes audio data continuously from a microphone input and sends it to the server via a gRPC call.
        /// </summary>
        /// <param name="call">The streaming call to send audio data to the server.</param>
        /// <param name="recordingFrequency">The frequency at which the audio is recorded.</param>
        /// <param name="recordingLength">The length of the audio recording in seconds.</param>
        /// <param name="audioClip">The AudioClip object that contains the audio data from the microphone.</param>
        /// <returns>A task that represents the asynchronous operation of processing and sending audio data.</returns>
        private async Task ProcessAudioContinuously(
            AsyncDuplexStreamingCall<GetResponseRequest, GetResponseResponse> call,
            int recordingFrequency,
            int recordingLength,
            AudioClip audioClip)
        {
            // Run the receiving results from the server in the background without awaiting it here.
            Task receiveResultsTask =
                Task.Run(async () => { await ReceiveResultFromServer(call, _cancellationTokenSource.Token); },
                    _cancellationTokenSource.Token);

            int pos = 0;
            float[] audioData = new float[recordingFrequency * recordingLength];

            while (Microphone.IsRecording(MicrophoneManager.Instance.GetSelectedMicrophoneDevice()))
            {
                await Task.Delay(200);
                int newPos = Microphone.GetPosition(MicrophoneManager.Instance.GetSelectedMicrophoneDevice());
                int diff = newPos - pos;

                if (diff > 0)
                {
                    if (audioClip == null)
                    {
                        try
                        {
                            _cancellationTokenSource?.Cancel();
                        }
                        catch (Exception e)
                        {
                            // Handle the Exception, which can occur if the CancellationTokenSource is already disposed. 
                            Logger.Warn("Exception when Audio Clip is null: " + e.Message, Logger.LogCategory.Character);
                        }
                        finally
                        {
                            _cancellationTokenSource?.Dispose();
                            _cancellationTokenSource = null;
                            Logger.Info("The Cancellation Token Source was Disposed because the Audio Clip was empty.", Logger.LogCategory.Character);
                        }

                        break;
                    }

                    audioClip.GetData(audioData, pos);
                    await ProcessAudioChunk(call, diff, audioData);
                    pos = newPos;
                }
            }

            // Process any remaining audio data.
            await ProcessAudioChunk(call,
                Microphone.GetPosition(MicrophoneManager.Instance.GetSelectedMicrophoneDevice()) - pos,
                audioData).ConfigureAwait(false);

            await call.RequestStream.CompleteAsync();
        }

        /// <summary>
        ///     Stops recording and processing the audio.
        /// </summary>
        public void StopRecordAudio()
        {
            // End microphone recording
            Microphone.End(MicrophoneManager.Instance.GetSelectedMicrophoneDevice());

            try
            {
                Logger.Info(_activeConvaiNPC.characterName + " has stopped listening", Logger.LogCategory.Character);
                OnPlayerSpeakingChanged?.Invoke(false);
            }
            catch (Exception)
            {
                Logger.Error("No active NPC found", Logger.LogCategory.Character);
            }
        }

        /// <summary>
        ///     Processes each audio chunk and sends it to the server.
        /// </summary>
        /// <param name="call">gRPC Streaming call connecting to the getResponse function</param>
        /// <param name="diff">Length of the audio data from the current position to the position of the last sent chunk</param>
        /// <param name="audioData">Chunk of audio data that we want to be processed</param>
        private static async Task ProcessAudioChunk(
            AsyncDuplexStreamingCall<GetResponseRequest, GetResponseResponse> call,
            int diff, IReadOnlyList<float> audioData)
        {
            if (diff > 0)
            {
                // Convert audio data to byte array
                byte[] audioByteArray = new byte[diff * sizeof(short)];

                for (int i = 0; i < diff; i++)
                {
                    float sample = audioData[i];
                    short shortSample = (short)(sample * short.MaxValue);
                    byte[] shortBytes = BitConverter.GetBytes(shortSample);
                    audioByteArray[i * sizeof(short)] = shortBytes[0];
                    audioByteArray[i * sizeof(short) + 1] = shortBytes[1];
                }

                // Send audio data to the gRPC server
                try
                {
                    await call.RequestStream.WriteAsync(new GetResponseRequest
                    {
                        GetResponseData = new GetResponseData
                        {
                            AudioData = ByteString.CopyFrom(audioByteArray)
                        }
                    });
                }
                catch (RpcException rpcException)
                {
                    if (rpcException.StatusCode == StatusCode.Cancelled)
                        Logger.Error(rpcException, Logger.LogCategory.Character);
                    else
                        throw;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, Logger.LogCategory.Character);
                }
            }
        }

        public event Action CharacterInterrupted;

        /// <summary>
        /// </summary>
        public void InterruptCharacterSpeech()
        {
            // If the active NPC is speaking, cancel the ongoing gRPC call,
            // clear the response queue, and reset the character's speaking state, lip-sync, animation, and audio playback
            if (_activeConvaiNPC != null && _activeConvaiNPC.IsCharacterTalking)
            {
                // Cancel the ongoing gRPC call
                try
                {
                    _cancellationTokenSource?.Cancel();
                }
                catch (Exception e)
                {
                    // Handle the Exception, which can occur if the CancellationTokenSource is already disposed. 
                    Logger.Warn("Exception in Interrupt Character Speech: " + e.Message, Logger.LogCategory.Character);
                }
                finally
                {
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = null;
                    Logger.Info("The Cancellation Token Source was Disposed in Interrupt Character Speech.", Logger.LogCategory.Character);
                }

                _cancellationTokenSource = new CancellationTokenSource(); // Create a new token for future calls

                CharacterInterrupted?.Invoke();

                // Clear the response queue
                _activeConvaiNPC.GetResponseResponses.Clear();

                // Reset the character's speaking state
                _activeConvaiNPC.SetCharacterTalking(false);

                // Stop any ongoing audio playback
                _activeConvaiNPC.StopAllAudioPlayback();

                // Stop any ongoing lip sync for active NPC
                _activeConvaiNPC.StopLipSync();

                // Reset the character's animation to idle
                _activeConvaiNPC.ResetCharacterAnimation();
            }
        }

        /// <summary>
        ///     Periodically receives responses from the server and adds it to a static list in streaming NPC
        /// </summary>
        /// <param name="call">gRPC Streaming call connecting to the getResponse function</param>
        /// <param name="cancellationToken"></param>
        private async Task ReceiveResultFromServer(
            AsyncDuplexStreamingCall<GetResponseRequest, GetResponseResponse> call, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested &&
                   await call.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false))
                try
                {
                    // Get the response from the server
                    GetResponseResponse result = call.ResponseStream.Current;

                    // Process different types of responses
                    if (result.UserQuery != null)
                        if (_chatUIHandler != null)
                            // Add user query to the list
                            _stringUserText.Add(result.UserQuery.TextData);

                    if (result.AudioResponse != null)
                    {
                        if (result.AudioResponse.TextData.Length > 0)
                            // Log audio response data
                            Logger.Info(result.AudioResponse.TextData, Logger.LogCategory.Character);

                        if (result.AudioResponse.AudioData != null)
                            // Add response to the list in the active NPC
                            _activeConvaiNPC.GetResponseResponses.Enqueue(call.ResponseStream.Current);

                        if (result.AudioResponse.FaceData != null)
                            Logger.Info(
                                $"Face Data:{result.AudioResponse.FaceData}. Face Data Size: {result.AudioResponse.FaceData.Length}",
                                Logger.LogCategory.LipSync);

                        if (result.AudioResponse.VisemesData != null)
                            if (_activeConvaiNPC.convaiLipSync != null)
                            {
                                Logger.Info(result.AudioResponse.VisemesData, Logger.LogCategory.LipSync);

                                if (result.AudioResponse.VisemesData.Visemes.Sil == -2)
                                    _activeConvaiNPC.convaiLipSync.faceDataList.Add(new List<VisemesData>());
                                else
                                    _activeConvaiNPC.convaiLipSync
                                        .faceDataList[_activeConvaiNPC.convaiLipSync.faceDataList.Count - 1]
                                        .Add(result.AudioResponse.VisemesData);
                            }
                    }

                    if (result.ActionResponse != null)
                        if (_activeConvaiNPC.actionsHandler != null)
                            _activeConvaiNPC.actionsHandler.actionResponseList.Add(result.ActionResponse.Action);

                    if (result.AudioResponse == null && result.DebugLog != null)
                        _activeConvaiNPC.GetResponseResponses.Enqueue(call.ResponseStream.Current);

                    // Update session ID in the active NPC
                    _activeConvaiNPC.sessionID = call.ResponseStream.Current.SessionId;
                }
                catch (RpcException rpcException)
                {
                    // Handle RpcExceptions, log or throw if necessary
                    if (rpcException.StatusCode == StatusCode.Cancelled)
                        Logger.Error(rpcException, Logger.LogCategory.Character);
                    else
                        throw;
                }
                catch (Exception ex)
                {
                    Logger.DebugLog(ex, Logger.LogCategory.Character);
                }

            if (cancellationToken.IsCancellationRequested) await call.RequestStream.CompleteAsync();
        }
    }
}