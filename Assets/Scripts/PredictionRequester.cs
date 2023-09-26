using System;
using AsyncIO;
using NetMQ;
using NetMQ.Sockets;
using UnityEngine;

public class PredictionRequester : RunAbleThread
{
    private RequestSocket client;

    private Action<byte[]> onOutputReceived;
    private Action<Exception> onFail;
    
    protected override void Run()
    {
        ForceDotNet.Force(); // this line is needed to prevent unity freeze after one use, not sure why yet
        using (RequestSocket client = new RequestSocket())
        {
            this.client = client;
            client.Connect("tcp://localhost:5555");

            while (Running)
            {
                byte[] outputBytes = new byte[0];
                bool gotMessage = false;
                while (Running)
                {
                    try
                    {
                        gotMessage = client.TryReceiveFrameBytes(out outputBytes); // this returns true if it's successful
                        if (gotMessage) break;
                    }
                    catch (Exception e)
                    {

                    }
                }

                if (gotMessage)
                {
                    var output = new float[outputBytes.Length];

                    //foreach (float i in output)
                    //    Debug.Log(i);

                    Buffer.BlockCopy(outputBytes, 0, output, 0, outputBytes.Length);

                    //foreach (float i in outputBytes)
                    //    Debug.Log(i);

                    

                    onOutputReceived?.Invoke(outputBytes);

                }
            }
        }

        NetMQConfig.Cleanup(); // this line is needed to prevent unity freeze after one use, not sure why yet
    }

    public void SendInput(float[] input)
    {
        try
        {
            var byteArray = new byte[input.Length * 4];
            Buffer.BlockCopy(input, 0, byteArray, 0, byteArray.Length);
            client.SendFrame(byteArray);
        }
        catch (Exception e)
        {
            onFail(e);
        }
    }

    public void SetOnTextReceivedListener(Action<byte[]> onOutputReceived, Action<Exception> fallback)
    {
        this.onOutputReceived = onOutputReceived;
        onFail = fallback;
    }
}