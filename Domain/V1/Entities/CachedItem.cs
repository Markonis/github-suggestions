using System;

namespace Domain.V1.Entities
{
    public class CachedItem<T>
    {
        public DateTime CachedAt { get; set; } = DateTime.UtcNow;
        public T Data { get; set; }
    }
}
