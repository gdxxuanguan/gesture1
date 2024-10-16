using UnityEngine;
using System.IO;
using RestSharp;
using Newtonsoft.Json;
using System;
using Unity.VisualScripting.Antlr3.Runtime;
using System.Numerics;
public class speechRec : MonoBehaviour
{
    private AudioClip recordedClip;
    private string microphoneDevice;
    private bool isRecording = false;
    private string TOKEN;

    //key
    private readonly string API_KEY = "gGxBUXDuT16nrYhh7hPJhuMh";
    private readonly string SECRET_KEY = "R1uaCvD65YaV3x5uUAelzkEhnqF89Mhb";

    private void Start()
    {
        TOKEN=GetAccessToken();
    }
    // 开始录音
    public void StartRecording()
    {
        if (Microphone.devices.Length > 0)
        {
            microphoneDevice = Microphone.devices[0];
            recordedClip = Microphone.Start(microphoneDevice, false, 60, 16000);
            isRecording = true;
            Debug.Log("录音开始");
        }
        else
        {
            Debug.LogWarning("未检测到麦克风设备");
        }
    }

    // 停止录音并保存为 PCM 文件
    public void StopRecordingAndSave()
    {
        if (isRecording)
        {
            int recordedSamples = Microphone.GetPosition(microphoneDevice);  // 获取实际录音样本数量
            Microphone.End(microphoneDevice);  // 停止录音

            string filePath = SavePcmFile(recordedClip, recordedSamples, "RecordedAudio.pcm");
            Debug.Log("录音已结束，并保存为 RecordedAudio.pcm");
            isRecording = false;
            Upload(filePath);
        }
    }

    // 保存实际录音数据为 PCM 文件
    private string SavePcmFile(AudioClip clip, int sampleCount, string filename)
    {
        var filepath = Path.Combine(Application.persistentDataPath, filename);
        using (FileStream fileStream = new FileStream(filepath, FileMode.Create))
        {
            int channels = clip.channels;  // 获取声道数

            float[] floatData = new float[sampleCount * channels];  // 获取实际录音数据
            clip.GetData(floatData, 0);  // 读取样本数据

            byte[] pcmData = new byte[sampleCount * channels * 2];  // 每样本2字节

            for (int i = 0; i < sampleCount * channels; i++)
            {
                short pcmSample = (short)(Mathf.Clamp(floatData[i], -1f, 1f) * short.MaxValue);
                pcmData[i * 2] = (byte)(pcmSample & 0xFF);  // 低字节
                pcmData[i * 2 + 1] = (byte)((pcmSample >> 8) & 0xFF);  // 高字节
            }

            fileStream.Write(pcmData, 0, pcmData.Length);  // 写入文件
        }

        Debug.Log("文件保存路径: " + filepath);
        return filepath;
    }

    private void Upload(string filePath)
    {

        // PCM音频文件路径
        byte[] audioBytes = File.ReadAllBytes(filePath);
        string audioBase64 = Convert.ToBase64String(audioBytes);

        // 创建RestClient和请求
        var client = new RestClient("https://vop.baidu.com/server_api");
        client.Timeout = -1;
        var request = new RestRequest(Method.POST);

        // 设置请求头
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Accept", "application/json");

        // 构建请求体
        var body = $@"{{
            ""format"": ""pcm"",
            ""rate"": 16000,
            ""channel"": 1,
            ""cuid"": ""wX75JNDgiwEl6d3SXium9SN4"",
            ""token"": ""{TOKEN}"", // 替换为你的Access Token
            ""speech"": ""{audioBase64}"",
            ""len"": {audioBytes.Length}
        }}";

        // 将请求体添加到请求中
        request.AddParameter("application/json", body, ParameterType.RequestBody);

        // 执行请求并获取响应
        IRestResponse response = client.Execute(request);
        //处理获得
        string res = response.Content;
        int len = 0;
        for (int i = 0; i < res.Length-6; i++)
        {
            if (res.Substring(i, 6) == "result")
            {
                for(; res[i+10+len] != '"'; len++) { }
                res = res.Substring(i + 10, len);

            }
        }
        Debug.Log(res);


    }

    /**
        * 使用 AK，SK 生成鉴权签名（Access Token）
        * @return 鉴权签名信息（Access Token）
        */
    private string GetAccessToken()
    {   
        var client = new RestClient($"https://aip.baidubce.com/oauth/2.0/token");
        client.Timeout = -1;
        var request = new RestRequest(Method.POST);
        request.AddParameter("grant_type", "client_credentials");
        request.AddParameter("client_id", API_KEY);
        request.AddParameter("client_secret", SECRET_KEY);
        IRestResponse response = client.Execute(request);
        Debug.Log(response.Content);
        var result = JsonConvert.DeserializeObject<dynamic>(response.Content);
        return result.access_token.ToString();
    }
}