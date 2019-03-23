using System.Configuration;
using System.Collections.Specialized;
using System;
using System.Collections.Generic;
using static System.Console;
using RabbitMQ.Client;
using RabbitMQ.Client.MessagePatterns;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using System.Text;
using System.Threading;
using System.Collections;
using System.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

using System.Linq;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Lab1Courses_handlerMessage
{
   
        public class RootRequest
    {
            [JsonProperty("Surname")]
            public string Surname { get; set; }

            [JsonProperty("Name")]
            public string Name { get; set; }

            [JsonProperty("Patronymic")]
            public string Patronymic { get; set; }

            [JsonProperty("BirthDay")]
            public DateTime BirthDay { get; set; }

            [JsonProperty("RequestType")]
            public int RequestType { get; set; }

            [JsonProperty("Sex")]
            public string Sex { get; set; }
    }



    public class HandlerMessage
    {
        NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();


        
        
        private IConnection GetRabbitConnection()
        {
            
            ConnectionFactory factory = new ConnectionFactory
            {
                UserName = "guest",
                Password = "guest",
                VirtualHost = "/",
                HostName = "localhost"
            };
            var conn = factory.CreateConnection();
            logger.Info("Connection-ok, Guid = ",Guid.NewGuid());
            return conn;
        }

        public string queueName = "test";
        public string exchangeName = "test";
        public string routingKey = "test";
        public ConcurrentDictionary<int,byte> CashDic = new ConcurrentDictionary<int, byte>();



        private IModel GetRabbitChannel(string exchangeName, string queueName, string routingKey)
        {
            HashFromDB(CashDic);
            WriteLine(CashDic.Count());
            logger.Info("Data for HashSet was read from db,Guid = ",Guid.NewGuid());

            IModel model = GetRabbitConnection().CreateModel();            
            model.ExchangeDeclare("test", ExchangeType.Direct);
            model.QueueDeclare(queueName, false, false, false, null);                model.QueueBind(queueName, exchangeName, routingKey, null);            
            return model;            
        }

        public void RabbitListener()
        {

            var model = GetRabbitChannel(exchangeName, queueName, routingKey);
            var subscription = new Subscription(model, queueName, false);             
                    while (true)
                    {
                        BasicDeliverEventArgs basicDeliveryEventArgs = subscription.Next();
                        string messageContent = Encoding.UTF8.GetString(basicDeliveryEventArgs.Body);
                        ThreadPool.QueueUserWorkItem(state =>
                        {
                            MessageWork(messageContent);
                        });

                        subscription.Ack(basicDeliveryEventArgs);
                    }
        }
        public void MessageWork(string messageContent)
        {
            var request = JsonConvert.DeserializeObject<RootRequest>(messageContent);
            logger.Info("Serialization of message from queue, Guid = ", Guid.NewGuid());
            Random rand = new Random();
            WriteLine((request.Name + "|" + request.Surname + "|" + request.Patronymic + "|" + request.Sex + "|" + request.RequestType + "|" + request.BirthDay).ToString());
            int SleepTime = rand.Next(5000, 10000);
            //Task.Delay(SleepTime);
            Thread.Sleep(SleepTime);
            logger.Info("Thread sleep milliseconds {0}, Guid = {1}",SleepTime,Guid.NewGuid());


            if (request.RequestType % 2 != 0)
            {
                SendToDB(request);
                logger.Info("Type %2 !=0, RequestType= {0}, Guid = {1}",request.RequestType,Guid.NewGuid());
            }
            else
            {
                logger.Info("Type %2==0, RequestType = {0, Guid = {1}}", request.RequestType, Guid.NewGuid());
                if (CashDic.ContainsKey(request.RequestType) == false)
                {
                    SendToDB(request);
                    CashDic.GetOrAdd(request.RequestType, default(byte));
                    logger.Info("RequestType isn't at hashset, RequestType= {0}, Guid = {1}", request.RequestType,Guid.NewGuid());
                }
                else
                    logger.Info("Type is repeated, RequestType= {0}, Guid = {1}", request.RequestType,Guid.NewGuid());
            }
        }

        public void SendToDB(RootRequest request)
        {
            using (ModelForDb ModelDb = new ModelForDb())
            {
                string NewXml = XmlString(request);
                person NewPerson = new person { Surname = request.Surname, Name = request.Name, Patronymic = request.Patronymic, BirthDay = request.BirthDay, RequestType = request.RequestType, Sex = request.Sex ,XMLString = NewXml};
                ModelDb.Persons.Add(NewPerson);
                ModelDb.SaveChanges();
                logger.Info("Data sended to DB, Guid = ",Guid.NewGuid());
            }

        }
        public static async void HashFromDB(ConcurrentDictionary<int,byte>  CashDic) 
            //use concurrent dictionary instead of hashset
        {
            using (ModelForDb ModelDb = new ModelForDb())
            {
                var result = await(from person in ModelDb.Persons where (person.RequestType % 2) == 0 select person.RequestType).ToListAsync();
                var dic = result.ToDictionary(x=>x,y=>default(byte));
                CashDic = new ConcurrentDictionary<int, byte>(dic);

                WriteLine(CashDic.Keys);
               
                
                    
                
                
            }

        }
        
        public string XmlString(RootRequest request)
        {
            XmlSerializer xsSubmit = new XmlSerializer(typeof(RootRequest));
            var xml = "";

            using (var sww = new StringWriter())
            {
                using (XmlWriter writer = XmlWriter.Create(sww))
                {
                    xsSubmit.Serialize(writer, request);
                    xml = sww.ToString(); 
                }
            }
            return xml;
        }
    }



    class Program
    {
        static void Main(string[] args)
        {
            
            HandlerMessage hm = new HandlerMessage();
            hm.RabbitListener();
            NLog.LogManager.Shutdown();
        }
    }
}
