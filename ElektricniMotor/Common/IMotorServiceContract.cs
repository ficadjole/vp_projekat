using System.ServiceModel;

namespace Common
{
    [ServiceContract]
    public interface IMotorServiceContract
    {
        [OperationContract]
        //[FaultContract(typeof(ValidationFault))]
        string StartSession(MetaZaglavlje metaZaglavlje);

        [OperationContract]
        string PushSample(MetaZaglavlje metaZaglavlje);

        [OperationContract]
        string EndSession();

    }
}
