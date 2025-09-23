using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class DataFormatFault
    {
        [DataMember]
        public string Message { get; set; }
        public DataFormatFault(string message)
        {
            Message = message;
        }
    }

    [DataContract]
    public class ValidationFault
    {
        [DataMember]
        public string Message { get; set; }

        public ValidationFault(string message)
        {
            Message = message;
        }
    }
}
