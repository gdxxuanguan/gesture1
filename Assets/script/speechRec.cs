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
    // ��ʼ¼��
    public void StartRecording()
    {
        if (Microphone.devices.Length > 0)
        {
            microphoneDevice = Microphone.devices[0];
            recordedClip = Microphone.Start(microphoneDevice, false, 60, 16000);
            isRecording = true;
            Debug.Log("¼����ʼ");
        }
        else
        {
            Debug.LogWarning("δ��⵽��˷��豸");
        }
    }

    // ֹͣ¼��������Ϊ PCM �ļ�
    public void StopRecordingAndSave()
    {
        if (isRecording)
        {
            int recordedSamples = Microphone.GetPosition(microphoneDevice);  // ��ȡʵ��¼����������
            Microphone.End(microphoneDevice);  // ֹͣ¼��

            string filePath = SavePcmFile(recordedClip, recordedSamples, "RecordedAudio.pcm");
            Debug.Log("¼���ѽ�����������Ϊ RecordedAudio.pcm");
            isRecording = false;
            Upload(filePath);
        }
    }

    // ����ʵ��¼������Ϊ PCM �ļ�
    private string SavePcmFile(AudioClip clip, int sampleCount, string filename)
    {
        var filepath = Path.Combine(Application.persistentDataPath, filename);
        using (FileStream fileStream = new FileStream(filepath, FileMode.Create))
        {
            int channels = clip.channels;  // ��ȡ������

            float[] floatData = new float[sampleCount * channels];  // ��ȡʵ��¼������
            clip.GetData(floatData, 0);  // ��ȡ��������

            byte[] pcmData = new byte[sampleCount * channels * 2];  // ÿ����2�ֽ�

            for (int i = 0; i < sampleCount * channels; i++)
            {
                short pcmSample = (short)(Mathf.Clamp(floatData[i], -1f, 1f) * short.MaxValue);
                pcmData[i * 2] = (byte)(pcmSample & 0xFF);  // ���ֽ�
                pcmData[i * 2 + 1] = (byte)((pcmSample >> 8) & 0xFF);  // ���ֽ�
            }

            fileStream.Write(pcmData, 0, pcmData.Length);  // д���ļ�
        }

        Debug.Log("�ļ�����·��: " + filepath);
        return filepath;
    }

    private void Upload(string filePath)
    {

        // PCM��Ƶ�ļ�·��
        byte[] audioBytes = File.ReadAllBytes(filePath);
        string audioBase64 = Convert.ToBase64String(audioBytes);

        // ����RestClient������
        var client = new RestClient("https://vop.baidu.com/server_api");
        client.Timeout = -1;
        var request = new RestRequest(Method.POST);

        // ��������ͷ
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Accept", "application/json");

        // ����������
        var body = $@"{{
            ""format"": ""pcm"",
            ""rate"": 16000,
            ""channel"": 1,
            ""cuid"": ""wX75JNDgiwEl6d3SXium9SN4"",
            ""token"": ""{TOKEN}"", // �滻Ϊ���Access Token
            ""speech"": ""{audioBase64}"",
            ""len"": {audioBytes.Length}
        }}";

        // ����������ӵ�������
        request.AddParameter("application/json", body, ParameterType.RequestBody);

        // ִ�����󲢻�ȡ��Ӧ
        IRestResponse response = client.Execute(request);
        //������
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
        * ʹ�� AK��SK ���ɼ�Ȩǩ����Access Token��
        * @return ��Ȩǩ����Ϣ��Access Token��
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