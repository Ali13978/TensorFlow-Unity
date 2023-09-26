using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using SFB;

public class UIManager : MonoBehaviour
{
    [SerializeField] RawImage displayImage;
    [SerializeField] private TMP_InputField urlInputfield;
    [SerializeField] private Button importBtnForFiles;
    [SerializeField] private Button importBtnForUrl;
    [SerializeField] private Button predictBtn;
    [SerializeField] private TMP_Text predictText;

    private Texture2D importedTexture;
    private string url;
    private string prediction;
    private PredictionClient client;

    public static async Task<Texture2D> GetRemoteTexture(string url)
{
    using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
    {
        // begin request:
        var asyncOp = www.SendWebRequest();

        // await until it's done: 
        while (asyncOp.isDone == false)
            await Task.Delay(1000 / 30);//30 hertz

        // read results:
        //if (www.isNetworkError || www.isHttpError)
        if (www.result != UnityWebRequest.Result.Success)// for Unity >= 2020.1
        {
            // log error:
#if DEBUG
            Debug.Log($"{www.error}, URL:{www.url}");
#endif

            // nothing to return on error:
            return null;
        }
        else
        {
            // return valid results:
            return DownloadHandlerTexture.GetContent(www);
        }
    }
    }


    private void Start()
    {
        client = GetComponent<PredictionClient>();

        importBtnForFiles.onClick.AddListener(()=>
        {
            importedTexture = ImportImage();
            DisplayImage();
        });

        importBtnForUrl.onClick.AddListener(async () =>
        {
            url = urlInputfield.text;
            if (url != null)
            {
                importedTexture = await GetRemoteTexture(url);
                DisplayImage();
            }
        }
        );

        predictBtn.onClick.AddListener(() =>
        {
            Predict(SaveImage());
        });
    }

    private void Update()
    {
        predictText.text = prediction;
    }


    private Texture2D ImportImage()
    {
        //string filePath = EditorUtility.OpenFilePanel("Select Image", "", "jpg");
        
        var extensions = new[] {new ExtensionFilter("Image Files", "png", "jpg", "jpeg" )};

        var filePath = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, true);

        if (!string.IsNullOrEmpty(filePath[0]))
        {
            byte[] fileData = File.ReadAllBytes(filePath[0]);

            Texture2D texture2D = new Texture2D(2, 2);
            texture2D.LoadImage(fileData);

            return texture2D;
        }
        else
            return null;
    }

    private void DisplayImage()
    {
        if (importedTexture == null)
            return;
        displayImage.texture = importedTexture;
        displayImage.SetNativeSize();
        
        float sizeDeltaX = displayImage.rectTransform.sizeDelta.x;
        float sizeDeltaY = displayImage.rectTransform.sizeDelta.y;

        while (sizeDeltaX > 1000f || sizeDeltaY > 950f)
        {
            sizeDeltaX /= 1.2f;
            sizeDeltaY /= 1.2f;
        }

        displayImage.rectTransform.sizeDelta = new Vector2(sizeDeltaX, sizeDeltaY);
    }

    private void Predict(string imgPath)
    {
        float[] floatArray = imgPath.Select(c => (float)c).ToArray();

        float[] ReadPixels() => floatArray;
        var input = ReadPixels();
        client.Predict(input, (outputBytes) =>
        {
            string str = Encoding.UTF8.GetString(outputBytes);
            prediction = str;
        }, error =>
        {
            // TODO: when i am not lazy
        });

    }

    private string SaveImage()
    {
        // Check if a texture has been imported
        if (importedTexture != null)
        {
            // Convert the texture to PNG format
            byte[] pngData = importedTexture.EncodeToPNG();

            // Save the PNG data to a file
            string savePath = Application.persistentDataPath + "/savedImage.png";
            File.WriteAllBytes(savePath, pngData);
            Debug.Log("Image saved to: " + savePath);
            return savePath;
        }
        else
        {
            Debug.Log("No image imported.");
            return null;
        }
    }
}
