using System;
using System.Threading.Tasks;
using Grpc.Core;
using Service;
using TMPro;
using UnityEngine;
using static Service.GetResponseRequest.Types;

namespace Convai.Scripts.Utils
{
    public class ConvaiTextInOut : MonoBehaviour
    {
        private const string GRPC_API_ENDPOINT = "stream.convai.com";

        // API authentication key
        [HideInInspector] public string APIKey;

        // Character ID defined in tooltip
        [Tooltip("Character ID ")] public string characterID;

        // Session ID defined in tooltip
        [Tooltip("Session ID")] public string sessionID;

        [Header("UI References")] public TMP_Text responseText; // Text area to display responses

        public TMP_InputField userInput; // Text input area for user input

        private AsyncDuplexStreamingCall<GetResponseRequest, GetResponseResponse> _call;
        private ConvaiService.ConvaiServiceClient _client; // gRPC client to interact with the service
        private string _responseString; // The responded string to display on the UI

        /// <summary>
        ///     Runs automatically when the script instance is being loaded.
        /// </summary>
        private void Awake()
        {
            InitializeAPIKey();
            InitializeGrpcConnection();

            userInput.onSubmit.AddListener(delegate
            {
                if (ConvaiInputManager.Instance.WasTextSendKeyPressed())
                {
                    Logger.DebugLog("Sending user text data to the server...", Logger.LogCategory.Character);
                    SendTextData();
                }
            });
        }

        /// <summary>
        ///     Called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        private void Update()
        {
            // Update the response text in the UI.
            responseText.text = _responseString;
        }

        /// <summary>
        ///     Initializes the API key.
        /// </summary>
        private void InitializeAPIKey()
        {
            ConvaiAPIKeySetup apiKeyScriptableObject = Resources.Load<ConvaiAPIKeySetup>("ConvaiAPIKey");
            if (apiKeyScriptableObject != null)
            {
                APIKey = apiKeyScriptableObject.APIKey;
                Logger.Info("API Key initialized successfully.", Logger.LogCategory.Character);
            }
            else
            {
                Logger.Error(
                    "No API Key data found. Please complete the Convai Setup. In the Menu Bar, click Convai > Setup.",
                    Logger.LogCategory.Character);
            }
        }

        /// <summary>
        ///     Initializes the gRPC connection.
        /// </summary>
        private void InitializeGrpcConnection()
        {
            // Setup SSL credentials.
            SslCredentials credentials = new();

            // Create a channel to the API endpoint.
            Channel channel = new(GRPC_API_ENDPOINT, credentials);

            // Create a new Convai service client with the channel.
            _client = new ConvaiService.ConvaiServiceClient(channel);
            Logger.Info("gRPC connection initialized.", Logger.LogCategory.Character);
        }

        /// <summary>
        ///     Sends the user input text data to the server asynchronously.
        /// </summary>
        private async void SendTextData()
        {
            _responseString = "";

            if (_client == null)
            {
                Logger.Error("gRPC client is not initialized.", Logger.LogCategory.Character);
                return;
            }

            _call = _client.GetResponse();

            GetResponseRequest getResponseConfigRequest = new()
            {
                GetResponseConfig = new GetResponseConfig
                {
                    CharacterId = characterID,
                    ApiKey = APIKey,
                    SessionId = sessionID,
                    AudioConfig = new AudioConfig
                    {
                        DisableAudio = true
                    }
                }
            };

            try
            {
                await _call.RequestStream.WriteAsync(getResponseConfigRequest);

                string userText = userInput.text;
                await _call.RequestStream.WriteAsync(new GetResponseRequest
                {
                    GetResponseData = new GetResponseData
                    {
                        TextData = userText
                    }
                });

                if (string.IsNullOrEmpty(userText))
                    Logger.DebugLog("Player connected/initialised", Logger.LogCategory.Character);

                await _call.RequestStream.CompleteAsync();
                await ReceiveResultFromServer(_call);
            }
            catch (RpcException rpcException)
            {
                if (rpcException.StatusCode == StatusCode.Cancelled)
                    Logger.Exception(rpcException, Logger.LogCategory.Character);
                else
                    throw;
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, Logger.LogCategory.Character);
            }
        }

        /// <summary>
        ///     Receives the result from the server in an asynchronous manner.
        /// </summary>
        /// <param name="call">The call made to the server.</param>
        private async Task ReceiveResultFromServer(
            AsyncDuplexStreamingCall<GetResponseRequest, GetResponseResponse> call)
        {
            try
            {
                // Loop through all the results.
                while (await call.ResponseStream.MoveNext())
                {
                    GetResponseResponse result = call.ResponseStream.Current;

                    // Append the new text message to the response string.
                    if (result.AudioResponse != null)
                        _responseString += result.AudioResponse.TextData;

                    // Update session ID.
                    sessionID = result.SessionId;
                }
            }
            catch (RpcException rpcException)
            {
                if (rpcException.StatusCode == StatusCode.Cancelled)
                    Logger.Exception(rpcException, Logger.LogCategory.Character);
                else if (rpcException.StatusCode == StatusCode.Unknown)
                    Logger.Error($"Unknown error from server: {rpcException.Status.Detail}",
                        Logger.LogCategory.Character);
                else
                    throw;
            }
        }
    }
}