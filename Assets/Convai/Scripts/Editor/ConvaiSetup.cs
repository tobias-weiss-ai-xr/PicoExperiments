#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.VersionControl;
using UnityEditor.VSAttribution;
using Object = UnityEngine.Object;

using Newtonsoft.Json;

using System.Security.Cryptography;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System;
using System.Threading.Tasks;

using UnityEditor;
using UnityEditor.UI;
using Convai.Scripts.Utils;

public class ConvaiSetup : EditorWindow
{
    private const string API_KEY_PATH = "Assets/Resources/ConvaiAPIKey.asset";
    private const string API_URL = "https://api.convai.com/user/referral-source-status";

    [MenuItem("Convai/Convai Setup")]
    public static void ShowConvaiSetupWindow()
    {
        ConvaiSetup wnd = GetWindow<ConvaiSetup>();
    }

    [MenuItem("Convai/Documentation")]
    public static void OpenDocumentation()
    {
        Application.OpenURL("https://docs.convai.com/plugins-and-integrations/unity-plugin");
    }

    public class UpdateSource
    {
        [JsonProperty("referral_source")] public string referral_source;

        public UpdateSource(string referral_source)
        {
            this.referral_source = referral_source;
        }
    }

    public class referralSourceStatus
    {
        [JsonProperty("referral_source_status")] public string referral_source_status;
        [JsonProperty("status")] public string status;
    }

    async Task<string> CheckReferralStatus(string url, string apiKey)
    {
        // Create a new HttpWebRequest object
        var request = WebRequest.Create(url);
        request.Method = "post";

        // Set the request headers
        request.ContentType = "application/json";

        string bodyJsonString = "{}";

        // Convert the json string to bytes
        byte[] jsonBytes = Encoding.UTF8.GetBytes(bodyJsonString);

        referralSourceStatus referralStatus;

        request.Headers.Add("CONVAI-API-KEY", apiKey);

        // Write the data to the request stream
        using (Stream requestStream = await request.GetRequestStreamAsync())
        {
            await requestStream.WriteAsync(jsonBytes, 0, jsonBytes.Length);
        }

        // Get the response from the server
        try
        {
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (Stream streamResponse = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(streamResponse))
                    {
                        string responseContent = reader.ReadToEnd();

                        referralStatus = JsonConvert.DeserializeObject<referralSourceStatus>(responseContent);
                    }
                }
                return referralStatus.referral_source_status;
            }
        }
        catch (WebException e)
        {
            Debug.LogError(e.Message + "\nPlease check if API Key is correct.");
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return null;
        }
    }

    async Task<bool> SendReferralRequest(string url, string bodyJsonString, string apiKey)
    {
        // Create a new HttpWebRequest object
        var request = WebRequest.Create(url);
        request.Method = "post";

        // Set the request headers
        request.ContentType = "application/json";

        // Convert the json string to bytes
        byte[] jsonBytes = Encoding.UTF8.GetBytes(bodyJsonString);

        request.Headers.Add("CONVAI-API-KEY", apiKey);

        // Write the data to the request stream
        using (Stream requestStream = await request.GetRequestStreamAsync())
        {
            await requestStream.WriteAsync(jsonBytes, 0, jsonBytes.Length);
        }

        // Get the response from the server
        try
        {
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (Stream streamResponse = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(streamResponse))
                    {
                        string responseContent = reader.ReadToEnd();
                    }
                }

                if ((int)response.StatusCode == 200)
                {
                    return true;
                }
            }
        }
        catch (WebException e)
        {
            Debug.LogError(e.Message + "\nPlease check if API Key is correct.");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return false;
        }

        return false;
    }

    private async Task<bool> BeginButtonTask(string apiKey)
    {

        ConvaiAPIKeySetup aPIKeySetup = CreateInstance<ConvaiAPIKeySetup>();

        aPIKeySetup.APIKey = apiKey;

        if (!string.IsNullOrEmpty(apiKey))
        {
            string referralStatus =
                await CheckReferralStatus(API_URL, apiKey);

            if (referralStatus != null)
            {
                CreateOrUpdateAPIKeyAsset(aPIKeySetup);

                if (referralStatus.Trim().ToLower() == "undefined" || referralStatus.Trim().ToLower() == "")
                {
                    EditorUtility.DisplayDialog("Warning", "[Step 1/2] API Key loaded successfully!",
                        "OK");
                    return true;
                }

                EditorUtility.DisplayDialog("Success", "API Key loaded successfully!", "OK");

                // if the status is already set, do not show the referral dialog
                return false;
            }

            else
            {
                EditorUtility.DisplayDialog("Error", "Something went wrong. Please check your API Key. Contact support@convai.com for more help. ", "OK");
                return false;
            }

        }

        EditorUtility.DisplayDialog("Error", "Please enter a valid API Key.", "OK");
        return false;
    }

    private void CreateOrUpdateAPIKeyAsset(ConvaiAPIKeySetup aPIKeySetup)
    {
        string assetPath = "Assets/Resources/ConvaiAPIKey.asset";

        if (!File.Exists(assetPath))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            AssetDatabase.CreateAsset(aPIKeySetup, assetPath);
        }
        else
        {
            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(aPIKeySetup, assetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        VisualElement page1 = new ScrollView();

        VisualElement page2 = new ScrollView();

        root.Add(new Label(""));

        Image convaiLogoImage = new Image();
        convaiLogoImage.image = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Convai/Images/color.png");
        convaiLogoImage.style.height = 100;

        convaiLogoImage.style.paddingBottom = 10;
        convaiLogoImage.style.paddingTop = 10;
        convaiLogoImage.style.paddingRight = 10;
        convaiLogoImage.style.paddingLeft = 10;

        root.Add(convaiLogoImage);

        Label convaiSetupLabel = new Label("Enter your API Key:");
        convaiSetupLabel.style.fontSize = 16;

        TextField APIKeyTextField = new TextField("", -1, false, true, '*');

        Button beginButton = new Button(async () =>
        {
            bool isPage2 = await BeginButtonTask(APIKeyTextField.text);

            if (isPage2)
            {
                root.Remove(page1);
                root.Add(page2);
            }
            else
            {
                this.Close();
            }
        });

        beginButton.text = "Begin!";

        beginButton.style.fontSize = 16;
        beginButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        beginButton.style.alignSelf = Align.Center;

        beginButton.style.paddingBottom = 10;
        beginButton.style.paddingLeft = 30;
        beginButton.style.paddingRight = 30;
        beginButton.style.paddingTop = 10;

        Button docsLink = new Button(() =>
        {
            Application.OpenURL("https://docs.convai.com/api-docs/plugins-and-integrations/unity-plugin/setting-up-unity-plugin");
        });

        docsLink.text = "How do I find my API key?";
        docsLink.style.alignSelf = Align.Center;
        docsLink.style.paddingBottom = 5;
        docsLink.style.paddingLeft = 50;
        docsLink.style.paddingRight = 50;
        docsLink.style.paddingTop = 5;

        page1.Add(convaiSetupLabel);
        page1.Add(new Label("\n"));
        page1.Add(APIKeyTextField);
        page1.Add(new Label("\n"));
        page1.Add(beginButton);
        page1.Add(new Label("\n"));
        page1.Add(docsLink);

        page1.style.marginBottom = 20;
        page1.style.marginLeft = 20;
        page1.style.marginRight = 20;
        page1.style.marginTop = 20;

        Label attributionSourceLabel = new Label("[Step 2/2] Where did you discover Convai?");

        attributionSourceLabel.style.fontSize = 14;
        attributionSourceLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

        List<string> attributionSourceOptions = new List<string>
        {
            "Search Engine (Google, Bing, etc.)",
            "Youtube",
            "Social Media (Facebook, Instagram, TikTok, etc.)",
            "Friend Referral",
            "Unity Asset Store",
            "Others"
        };


        TextField otherOptionTextField = new TextField();

        string currentChoice = "";
        int currentChoiceIndex = -1;

        DropdownMenu dropdownMenu = new DropdownMenu();

        ToolbarMenu toolbarMenu = new ToolbarMenu { text = "Click here to select option..." };

        foreach (string choice in attributionSourceOptions)
        {
            toolbarMenu.menu.AppendAction(choice,
                action =>
                {
                    currentChoice = choice;
                    toolbarMenu.text = choice;
                });
        }

        toolbarMenu.style.paddingBottom = 10;
        toolbarMenu.style.paddingLeft = 30;
        toolbarMenu.style.paddingRight = 30;
        toolbarMenu.style.paddingTop = 10;

        Button continueButton = new Button(() =>
        {
            UpdateSource updateSource;

            currentChoiceIndex = attributionSourceOptions.IndexOf(toolbarMenu.text);

            if (currentChoiceIndex < 0)
            {
                EditorUtility.DisplayDialog("Error", "Please select a valid referral source!", "OK");
            }
            else
            {
                updateSource = new UpdateSource(attributionSourceOptions[currentChoiceIndex]);

                if (attributionSourceOptions[currentChoiceIndex] == "Others")
                {
                    updateSource.referral_source = otherOptionTextField.text;
                }

                ConvaiAPIKeySetup apiKeyObject = AssetDatabase.LoadAssetAtPath<ConvaiAPIKeySetup>("Assets/Resources/ConvaiAPIKey.Asset");

                SendReferralRequest("https://api.convai.com/user/update-source", JsonConvert.SerializeObject(updateSource), apiKeyObject.APIKey);

                if (attributionSourceOptions[currentChoiceIndex] == "Unity Asset Store")
                {
                    // VS Attribution
                    VSAttribution.SendAttributionEvent("Initial Setup", "Convai Technologies, Inc.", apiKeyObject.APIKey);
                }

                this.Close();
            }
        });

        continueButton.text = "Continue";

        continueButton.style.fontSize = 16;
        continueButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        continueButton.style.alignSelf = Align.Center;

        continueButton.style.paddingBottom = 5;
        continueButton.style.paddingLeft = 30;
        continueButton.style.paddingRight = 30;
        continueButton.style.paddingTop = 5;

        page2.Add(new Label("\n"));
        page2.Add(attributionSourceLabel);
        page2.Add(new Label("\n"));

        page2.Add(toolbarMenu);
        page2.Add(new Label("\nIf selected Others above, please specifiy from where: "));

        page2.Add(otherOptionTextField);
        page2.Add(new Label("\n"));
        page2.Add(continueButton);

        page2.style.marginBottom = 20;
        page2.style.marginLeft = 20;
        page2.style.marginRight = 20;
        page2.style.marginTop = 20;
        root.Add(page1);
    }
}
#endif