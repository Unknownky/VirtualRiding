using Amqp;
using Amqp.Sasl;
using Amqp.Framing;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Text.Json;
using System.Text;
using UnityEngine;


//总结起来，主线程通过阻塞等待的方式，让两个新线程在后台运行，一个用于建立连接，另一个用于接收和处理消息。
namespace AmqpClient
{
    class AmqpLinkTest
    {
        //接入域名，请参见AMQP客户端接入说明文档。
        static string Host = "";
        //对于Java、.NET、Python 2.7、Node.js、Go客户端：端口号为5671
        static int Port = 5671;
        // 工程代码泄露可能会导致 AccessKey 泄露，并威胁账号下所有资源的安全性。以下代码示例使用环境变量获取 AccessKey 的方式进行调用，仅供参考
        static string AccessKey = "";
        static string AccessSecret = "";
        static string consumerGroupId = "";
        //clientId：接入端的唯一标识符，需要保证在接入环境中唯一。
        static string clientId = "";
        //iotInstanceId：实例ID。
        static string iotInstanceId = "";

        static int Count = 0;
        static int IntervalTime = 10000;

        static Address address;

        /// <summary>
        /// 运行AMQP连接, 用于接收消息
        /// </summary>
        /// <param name="args"></param>
        public static void RunAmqpLink(string[] args)
        {
            //获取当前时间戳。
            long timestamp = GetCurrentMilliseconds();
            string param = "authId=" + AccessKey + "&timestamp=" + timestamp;
            //userName组装方法，请参见AMQP客户端接入说明文档。鉴权模式
            string userName = clientId + "|authMode=aksign,signMethod=hmacmd5,consumerGroupId=" + consumerGroupId
               + ",iotInstanceId=" + iotInstanceId + ",authId=" + AccessKey + ",timestamp=" + timestamp + "|";
            //计算签名，password组装方法，请参见AMQP客户端接入说明文档。
            string password = doSign(param, AccessSecret, "HmacMD5");
            //建立连接。
            DoConnectAmqp(userName, password);
            //阻塞主线程。
            // ManualResetEvent resetEvent = new ManualResetEvent(false);
            // resetEvent.WaitOne();
        }

        static void DoConnectAmqp(string userName, string password)
        {
            //创建Address。
            address = new Address(Host, Port, userName, password);
            //创建Connection。
            ConnectionFactory cf = new ConnectionFactory();
            //如果需要，使用本地TLS。
            //cf.SSL.ClientCertificates.Add(GetCert());
            //cf.SSL.RemoteCertificateValidationCallback = ValidateServerCertificate;
            cf.SASL.Profile = SaslProfile.External;
            cf.AMQP.IdleTimeout = 120000;
            //cf.AMQP.ContainerId、cf.AMQP.HostName请自定义。
            cf.AMQP.ContainerId = "client.1.2";
            cf.AMQP.HostName = "contoso.com";
            cf.AMQP.MaxFrameSize = 8 * 1024;
            //开辟新线程，进行连接。
            var connection = cf.CreateAsync(address).Result;

            //Connection Exception已关闭。
            connection.AddClosedCallback(ConnClosed);

            //接收消息。
            DoReceive(connection);
        }
        
        //接收消息。
        static void DoReceive(Connection connection)
        {
            //创建Session。
            var session = new Session(connection);

            //创建ReceiverLink并接收消息。
            var receiver = new ReceiverLink(session, "queueName", null);
            #if UNITY_EDITOR
            Debug.Log("Waiting for messages...");
            #endif
            //接收到消息后，进行消息确认。应该是在此处开辟了一个线程，用于接收消息。
            receiver.Start(20, (link, message) =>
            {
                object messageId = message.ApplicationProperties["messageId"];
                object topic = message.ApplicationProperties["topic"];
                string body = Encoding.UTF8.GetString((Byte[])message.Body);
                //注意：此处不要有耗时的逻辑，如果这里要进行业务处理，请另开线程，否则会堵塞消费。如果消费一直延时，会增加消息重发的概率。
                // Console.WriteLine("receive message, topic=" + topic + ", messageId=" + messageId + ", body=" + body);
                // 在新线程中异步执行AssyFunction函数
                Task.Run(async () =>
                {
                    await AssyFunction(body);
                });

                //ACK消息。
                link.Accept(message);

            });
        }

        /// <summary>
        /// 异步执行的函数, 用于处理消息
        /// </summary>
        /// <returns></returns>
        static async Task AssyFunction(string body)
        {
            // 在此处添加异步执行的逻辑
            // 解析JSON字符串
            var jsonDocument = JsonDocument.Parse(body);
            var items = jsonDocument.RootElement.GetProperty("items");

            // 解析Pitch的value值
            var pitchValue = items.GetProperty("Pitch").GetProperty("value").GetString();

            // 解析led的value值
            var ledValue = items.GetProperty("led").GetProperty("value").GetString();

            // 解析Roll的value值
            var rollValue = items.GetProperty("Roll").GetProperty("value").GetString();

            // 解析Yaw的value值
            var yawValue = items.GetProperty("Yaw").GetProperty("value").GetString();

            #if UNITY_EDITOR
            // 打印解析结果
            Debug.Log("Pitch value: " + pitchValue);
            Debug.Log("led value: " + ledValue);
            Debug.Log("Roll value: " + rollValue);
            Debug.Log("Yaw value: " + yawValue);
            #endif


            // 将解析结果传递给CloudLinkController
            CloudLinkController.SetAttribute(float.Parse(ledValue=="1"?"0.9":"0"), float.Parse(yawValue)/60);

            await Task.Delay((int)Time.fixedDeltaTime*1000);
        }

        //连接发生异常后，进入重连模式。
        //这里只是一个简单重试的示例，您可以采用指数退避方式，来完善异常场景，重连策略。
        static void ConnClosed(IAmqpObject _, Error e)
        {
            Console.WriteLine("ocurr error: " + e);
            if (Count < 3)
            {
                Count += 1;
                Thread.Sleep(IntervalTime * Count);
            }
            else
            {
                Thread.Sleep(120000);
            }

            //重连。
            DoConnectAmqp(address.User, address.Password);
        }

        static X509Certificate GetCert()
        {
            string certPath = Environment.CurrentDirectory + "/root.crt";
            X509Certificate crt = new X509Certificate(certPath);

            return crt;
        }

        static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        static long GetCurrentMilliseconds()
        {
            DateTime dt1970 = new DateTime(1970, 1, 1);
            DateTime current = DateTime.Now;
            return (long)(current - dt1970).TotalMilliseconds;
        }

        //签名方法：支持hmacmd5，hmacsha1和hmacsha256。
        static string doSign(string param, string accessSecret, string signMethod)
        {
            //signMethod = HmacMD5
            byte[] key = Encoding.UTF8.GetBytes(accessSecret);
            byte[] signContent = Encoding.UTF8.GetBytes(param);
            var hmac = new HMACMD5(key);
            byte[] hashBytes = hmac.ComputeHash(signContent);
            return Convert.ToBase64String(hashBytes);
        }
    }
}