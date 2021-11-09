namespace dc
{
    public class KafkaConsumerRequest : InterSystems.EnsLib.PEX.Message
    {
        public string text;
        public string topic;
        public string key;
        public string offset;
        public long uxTimesStamp;
    }
}
