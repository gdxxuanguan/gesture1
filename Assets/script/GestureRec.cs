using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.Collections;
using UnityEngine.XR.ARSubsystems;
using System.IO;
using UnityEngine.UI;
using RestSharp;//�����汾106.15.0 https://www.nuget.org/packages/RestSharp/106.15.0
using Newtonsoft.Json; //https://www.nuget.org/packages/Newtonsoft.Json
using TMPro;
using System; // �����ļ�����

public class ARCameraImageCapture : MonoBehaviour
{
    public TextMeshProUGUI captureState;

    public ARCameraManager arCameraManager;
    private string AccessToken;

    private void Start()
    {
        GetAccessToken();
    }

    // ����������ȡAR����ͷ�ĵ�ǰ֡ͼ�񲢱���
    public void CaptureAndSaveImage()
    {
        Texture2D cameraImageTexture = GetCameraImage();
        //if (cameraImageTexture != null)
        //{
        //    SaveTextureAsPNG(cameraImageTexture, Application.persistentDataPath + "/ARCameraImage.png");
        //    Debug.Log("ͼ�񱣴�ɹ�: " + Application.persistentDataPath + "/ARCameraImage.png");
        //    captureState.text = "ͼ�񱣴�ɹ�: " + Application.persistentDataPath + "/ARCameraImage.png";
        //}
        //else
        //{
        //    Debug.LogWarning("�޷���ȡ����ͷͼ��");
        //}
    }

    // ��ȡ��ǰ֡��ͼ��
    private Texture2D GetCameraImage()
    {
        if (arCameraManager == null)
        {
            arCameraManager = GetComponent<ARCameraManager>();
            Debug.Log("ar camera manager��ʼ��");
        }
        captureState.text = "genshin";
        if (arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            SaveCpuImageAsPNG(image,"gesture.png");
            captureState.text = "get picture";
            image.Dispose(); // �ͷ�ͼ����Դ
            return null;
        }
        else
        {
            Debug.LogWarning("�޷���ȡ����ͷͼ��");
            return null;
        }
    }

    // ��XRCpuImageת��ΪTexture2D
    private Texture2D ConvertCpuImageToTexture2D(XRCpuImage image)
    {
        XRCpuImage.ConversionParams conversionParams = new XRCpuImage.ConversionParams
        {
            outputDimensions = new Vector2Int(image.width, image.height),
            outputFormat = TextureFormat.RGBA32,
            transformation = XRCpuImage.Transformation.None
        };

        NativeArray<byte> imageData = new NativeArray<byte>(image.GetConvertedDataSize(conversionParams), Allocator.Temp);
        image.Convert(conversionParams, imageData);

        Texture2D texture = new Texture2D(image.width, image.height, TextureFormat.RGBA32, false);
        texture.LoadRawTextureData(imageData);
        texture.Apply();

        imageData.Dispose();
        return texture;
    }

    // �� XRCpuImage ������ת��Ϊ PNG ��ʽ������
    private void SaveCpuImageAsPNG(XRCpuImage image, string fileName)
    {
        // ����ת������
        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = new Vector2Int(image.width, image.height),
            outputFormat = TextureFormat.RGBA32,
            transformation = XRCpuImage.Transformation.None
        };

        // �����������洢ת�����ͼ������
        NativeArray<byte> rawTextureData = new NativeArray<byte>(
            image.GetConvertedDataSize(conversionParams), Allocator.Temp);

        // �� XRCpuImage ����ת��Ϊ RGBA32 ��ʽ
        image.Convert(conversionParams, rawTextureData);

        // ����һ����ʱ�� Texture2D ���洢ת���������
        Texture2D texture = new Texture2D(
            image.width, image.height, TextureFormat.RGBA32, false);

        texture.LoadRawTextureData(rawTextureData);
        texture.Apply(); // Ӧ����������

        // ���������Ϊ PNG ��ʽ
        byte[] pngBytes = texture.EncodeToPNG();

        // �ͷ� NativeArray �ڴ�
        rawTextureData.Dispose();

        // ��ȡ�ļ�����·��
        string path = Path.Combine(Application.persistentDataPath, fileName);

        // �� PNG ����д���ļ�
        File.WriteAllBytes(path, pngBytes);

        Upload(path);

        Debug.Log($"PNG ͼ���ѱ��浽: {path}");
    }

    const string API_KEY = "IKkPpxRARogntfRcwadoYKRK";  // �滻Ϊ���Լ��� API Key
    const string SECRET_KEY = "nmi5ZLi8XWE2PVZ6vyICXkUWD0xYauC3";  // �滻Ϊ���Լ��� Secret Key

    // ��ȡ Access Token
    public void GetAccessToken()
    {
        var client = new RestClient("https://aip.baidubce.com/oauth/2.0/token");
        client.Timeout = -1;

        var request = new RestRequest(Method.POST);
        request.AddParameter("grant_type", "client_credentials");
        request.AddParameter("client_id", API_KEY);
        request.AddParameter("client_secret", SECRET_KEY);

        IRestResponse response = client.Execute(request);

        var result = JsonConvert.DeserializeObject<dynamic>(response.Content);
        AccessToken = result.access_token.ToString();
        return;
    }

    // �ϴ�ͼƬ�����ðٶ������� API
    public void Upload(string imagePath)
    {
        // ��ȡͼƬ�ļ���ת��Ϊ Base64
        string imageBase64 = Convert.ToBase64String(File.ReadAllBytes(imagePath));

        // ���� API ���õ� URL
        string url = $"https://aip.baidubce.com/rest/2.0/image-classify/v1/gesture?access_token={AccessToken}";

        var client = new RestClient(url);
        client.Timeout = -1;

        var request = new RestRequest(Method.POST);
        request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
        request.AddHeader("Accept", "application/json");
        request.AddParameter("image", imageBase64);  // ʹ�� Base64 ������ͼƬ����
        //request.AddParameter("image_type", "BASE64");  // ָ��ͼƬ����
        //request.AddParameter("quality_control", "NORMAL");  // ͼƬ��������
        //request.AddParameter("liveness_control", "NORMAL");  // ��������ƣ������Ҫ��

        IRestResponse response = client.Execute(request);
        Debug.Log(response.Content);  // ��� API ��Ӧ
    }
}
