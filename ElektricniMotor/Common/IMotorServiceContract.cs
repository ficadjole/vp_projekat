using System.ServiceModel;

namespace Common
{
    [ServiceContract]
    public interface IMotorServiceContract
    {
        [OperationContract]
        [FaultContract(typeof(DataFormatFault))]
        [FaultContract(typeof(ValidationFault))]
        string StartSession(MetaZaglavlje metaZaglavlje);

        [OperationContract]
        [FaultContract(typeof(DataFormatFault))]
        [FaultContract(typeof(ValidationFault))]
        string PushSample(MetaZaglavlje metaZaglavlje);

        [OperationContract]
        string EndSession();

    }
}
