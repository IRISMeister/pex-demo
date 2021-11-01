using System;
using InterSystems.Data.IRISClient.ADO;
using InterSystems.Data.IRISClient.Gateway;
using InterSystems.EnsLib.PEX;
using Confluent.Kafka;

namespace dc
{
    public class KafkaConsumer : BusinessService
    {
        /// <summary>
        /// Comma-separated list of Kafka partitions to connect to.
        /// </summary>
        public string SERVERS;

        /// <summary>
        /// Kafka topic, for this service to consume
        /// </summary>
        public string TOPIC;

        /// <summary>
        /// Configuration item(s) to which to send file stream messages
        /// </summary>
        public string TargetConfigNames;

        /// <summary>
        /// Configuration item(s) to which to send file stream messages, parsed into string[]
        /// </summary>
        private string[] targets;

        /// <summary>
        /// Connection to InterSystems IRIS
        /// </summary>
        private IRIS iris;

        /// <summary>
        /// Connection to Kafka
        /// </summary>
        private IConsumer<long, string> consumer;

        private int count=0;

        /// <summary>
        /// Initialize connections 
        ///  - Kafka Consumer
        ///  - Reentrancy to InterSystems IRIS
        /// </summary>
        public override void OnInit()
        {
            LOGINFO("Initialization started");
            var conf = new ConsumerConfig
            {
                GroupId = "test-consumer-group",
                BootstrapServers = SERVERS,
                // Note: The AutoOffsetReset property determines the start offset in the event
                // there are not yet any committed offsets for the consumer group for the
                // topic/partitions of interest. By default, offsets are committed
                // automatically, so in this example, consumption will only start from the
                // earliest message in the topic 'my-topic' the first time you run the program.
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            consumer = new ConsumerBuilder<long, string>(conf).Build();

            consumer.Subscribe(TOPIC);

            if (TargetConfigNames != null)
            {
                targets = TargetConfigNames.Split(",");
            }
            iris = GatewayContext.GetIRIS();

            LOGINFO("Initialized!");
        }

        public override object OnProcessInput(object messageInput)
        {
            bool atEnd = false;
            while (atEnd is false)
            {
                ConsumeResult<long, string> message = consumer.Consume(1000);
                if (message is null)
                {
                    atEnd = true;
                } else {
                    string text = message.Message.Value;
                    long key = message.Message.Key;
                    long uxts = message.Message.Timestamp.UnixTimestampMs;
                    string topic = message.Topic;
                    long offset = message.Offset.Value;

                    foreach (string target in targets)
                    {
                        if(count % 2 == 0) {
                            KafkaConsumerRequest request = new KafkaConsumerRequest();
                            request.text=text;
                            request.topic=topic;
                            request.key=key.ToString();
                            request.offset=offset.ToString();
                            request.uxTimesStamp=uxts;
                            SendRequestAsync(target, request);
                        }
                        else {
                            //IRISObject request = (IRISObject)iris.ClassMethodObject("Ens.StringContainer", "%New", topic+"("+offset+"):"+uxts+":"+key.ToString()+":"+text);
                            IRISObject request = (IRISObject)iris.ClassMethodObject("dc.ConsumerRequest", "%New");
                            request.Set("Topic",topic); //, topic+"("+offset+"):"+uxts+":"+key.ToString()+":"+text);
                            request.Set("Text",text);
                            request.Set("Key",key.ToString());
                            request.Set("Offset",offset.ToString());
                            request.Set("uxTimesStamp",uxts);
                            SendRequestAsync(target, request);
                        }
                        count++;
                    }
                }
            }
            return null;
        }

        public override void OnTearDown()
        {
            iris.Close();
        }
    }
}
