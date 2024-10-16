using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.Collections;
using UnityEngine.XR.ARSubsystems;
using System.IO;
using UnityEngine.UI;
using RestSharp;//依赖版本106.15.0 https://www.nuget.org/packages/RestSharp/106.15.0
using Newtonsoft.Json; //https://www.nuget.org/packages/Newtonsoft.Json
using TMPro;
using System; // 用于文件操作

public class ARCameraImageCapture : MonoBehaviour
{
    public TextMeshProUGUI captureState;

    public ARCameraManager arCameraManager;
    private string AccessToken;

    private void Start()
    {
        GetAccessToken();
    }

    // 主方法：获取AR摄像头的当前帧图像并保存
    public void CaptureAndSaveImage()
    {
        Texture2D cameraImageTexture = GetCameraImage();
        //if (cameraImageTexture != null)
        //{
        //    SaveTextureAsPNG(cameraImageTexture, Application.persistentDataPath + "/ARCameraImage.png");
        //    Debug.Log("图像保存成功: " + Application.persistentDataPath + "/ARCameraImage.png");
        //    captureState.text = "图像保存成功: " + Application.persistentDataPath + "/ARCameraImage.png";
        //}
        //else
        //{
        //    Debug.LogWarning("无法获取摄像头图像");
        //}
    }

    // 获取当前帧的图像
    private Texture2D GetCameraImage()
    {
        if (arCameraManager == null)
        {
            arCameraManager = GetComponent<ARCameraManager>();
            Debug.Log("ar camera manager初始化");
        }
        captureState.text = "genshin";
        if (arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            SaveCpuImageAsPNG(image,"gesture.png");
            captureState.text = "get picture";
            image.Dispose(); // 释放图像资源
            return null;
        }
        else
        {
            Debug.LogWarning("无法获取摄像头图像");
            return null;
        }
    }

    // 将XRCpuImage转换为Texture2D
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

    // 将 XRCpuImage 的数据转换为 PNG 格式并保存
    private void SaveCpuImageAsPNG(XRCpuImage image, string fileName)
    {
        // 设置转换参数
        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = new Vector2Int(image.width, image.height),
            outputFormat = TextureFormat.RGBA32,
            transformation = XRCpuImage.Transformation.None
        };

        // 分配数组来存储转换后的图像数据
        NativeArray<byte> rawTextureData = new NativeArray<byte>(
            image.GetConvertedDataSize(conversionParams), Allocator.Temp);

        // 将 XRCpuImage 数据转换为 RGBA32 格式
        image.Convert(conversionParams, rawTextureData);

        // 创建一个临时的 Texture2D 来存储转换后的数据
        Texture2D texture = new Texture2D(
            image.width, image.height, TextureFormat.RGBA32, false);

        texture.LoadRawTextureData(rawTextureData);
        texture.Apply(); // 应用纹理数据

        // 将纹理编码为 PNG 格式
        byte[] pngBytes = texture.EncodeToPNG();

        // 释放 NativeArray 内存
        rawTextureData.Dispose();

        // 获取文件保存路径
        string path = Path.Combine(Application.persistentDataPath, fileName);

        // 将 PNG 数据写入文件
        File.WriteAllBytes(path, pngBytes);

        Upload(path);

        Debug.Log($"PNG 图像已保存到: {path}");
    }

    const string API_KEY = "IKkPpxRARogntfRcwadoYKRK";  // 替换为你自己的 API Key
    const string SECRET_KEY = "nmi5ZLi8XWE2PVZ6vyICXkUWD0xYauC3";  // 替换为你自己的 Secret Key

    // 获取 Access Token
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

    // 上传图片并调用百度智能云 API
    public void Upload(string imagePath)
    {
        // 读取图片文件并转换为 Base64
        string imageBase64 = Convert.ToBase64String(File.ReadAllBytes(imagePath));

        // 设置 API 调用的 URL
        string url = $"https://aip.baidubce.com/rest/2.0/image-classify/v1/gesture?access_token={AccessToken}";

        var client = new RestClient(url);
        client.Timeout = -1;

        var request = new RestRequest(Method.POST);
        request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
        request.AddHeader("Accept", "application/json");
        request.AddParameter("image", imageBase64);  // 使用 Base64 编码后的图片内容
        //request.AddParameter("image_type", "BASE64");  // 指定图片类型
        //request.AddParameter("quality_control", "NORMAL");  // 图片质量控制
        //request.AddParameter("liveness_control", "NORMAL");  // 活体检测控制（如果需要）

        IRestResponse response = client.Execute(request);
        Debug.Log(response.Content);  // 输出 API 响应
    }
}
