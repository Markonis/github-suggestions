namespace Domain.Errors
{
    public class Error
    {
        public ErrorCode Code { get; protected set; }
        public string Description { get; protected set; }

        public Error(ErrorCode code, string description = null)
        {
            Code = code;
            Description = description;
        }
    }
}
