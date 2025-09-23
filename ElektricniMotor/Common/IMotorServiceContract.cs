using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

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
