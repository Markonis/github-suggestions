using System.Runtime.Serialization;

namespace Domain.V1.Messages
{
    [DataContract]
    public class SearchInput
    {
        [DataMember]
        public string AuthToken { get; set; }

        [DataMember]
        public string Query { get; set; }
    }
}
