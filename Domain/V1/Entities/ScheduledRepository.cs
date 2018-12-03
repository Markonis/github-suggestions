using System.Runtime.Serialization;

namespace Domain.V1.Entities
{

    [DataContract]
    public class ScheduledRepository
    {
        [DataMember]
        public string Name;

        [DataMember]
        public string Owner;
    }
}
