using System;
using Tea;
using UnityEngine;

namespace AlibabaCloud
{
    /// <summary>
    /// 测试阿里云IOT平台的连接
    /// </summary>
    public class IoTLinkTest
    {
        // 您的AccessKey ID。
        private const string accessKey = "";
        // 您的AccessKey Secret。
        private const string secretKey = "";
        // 您的可用区ID。
        private const string RegionId = "";
        // 您的物联网平台实例ID
        private const string iotInstanceId = "";
        // 您的产品ProductKey
        private const string productKey = "";
        // 您的设备DeviceName
        private const string deviceName = "";


        /**
         * 使用AK&SK初始化Client。
         * @param accessKeyId 
         * @param accessKeySecret
         * @param regionId
         * @return Client
         * @throws Exception
         */
        
        /// <summary>
        /// 创建Client实例。
        /// </summary>
        /// <returns>返回Client实例</returns>
        public static AlibabaCloud.SDK.Iot20180120.Client CreateClient()
        {
            //创建配置文件实例
            AlibabaCloud.OpenApiClient.Models.Config config = new AlibabaCloud.OpenApiClient.Models.Config();
            // 您的AccessKey ID。
            config.AccessKeyId = accessKey;
            // 您的AccessKey Secret。
            config.AccessKeySecret = secretKey;
            // 您的可用区ID。
            config.RegionId = RegionId;

            config.Endpoint = "";
            
            return new AlibabaCloud.SDK.Iot20180120.Client(config);
        }


        /// <summary>
        /// 向阿里IOT平台发送请求，返回消息
        /// </summary>
        /// <returns>返回消息</returns>
        public static string AlibabaCloudIotLink(AlibabaCloud.SDK.Iot20180120.Client client)
        {
            try
            {

                // 创建请求实例。
                AlibabaCloud.SDK.Iot20180120.Models.PubRequest request = new AlibabaCloud.SDK.Iot20180120.Models.PubRequest
                {
                    // 物联网平台实例ID。
                    IotInstanceId = iotInstanceId,
                    // 产品ProductKey。
                    ProductKey = productKey,
                    // 要发送的消息主体，hello world Base64 String。
                    // MessageContent = Convert.ToBase64String(Encoding.Default.GetBytes("Hello World.")),
                    // 要接收消息的设备的自定义Topic。
                    TopicFullName = $"/{productKey}/{deviceName}/user/get",
                    // 指定消息的发送方式，支持QoS0和QoS1。
                    Qos = 0,
                };
                Debug.Log(iotInstanceId);
                Debug.Log(productKey);
                Debug.Log($"/{productKey}/{deviceName}/user/get");

                // 发送消息。
                AlibabaCloud.SDK.Iot20180120.Models.PubResponse response = client.Pub(request);
                // 打印返回消息
                Debug.Log("publish message result: " + response.Body.Success);
                Debug.Log(response.Body.Code);
                Debug.Log(response.Body.ErrorMessage);
                // 返回消息
                return response.Body.Success.ToString();
            }
            catch (TeaException error)
            {
                Debug.Log(error.Code);
                Debug.Log(error.Message);
            }
            catch (Exception _error)
            {
                Debug.Log(_error.Message);
                Debug.Log(_error.StackTrace);
            }
            return "0";
        }
    }
}