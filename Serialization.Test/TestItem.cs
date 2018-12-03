using System.Collections.Immutable;

namespace Serialization.Test
{
    public class TestItem
    {
        public readonly string Name;
        public readonly int Count;
        public readonly ImmutableList<SubItem> SubItems;

        public TestItem(string name, int count, ImmutableList<SubItem> subItems)
        {
            Name = name;
            Count = count;
            SubItems = subItems;
        }

        public class SubItem
        {
            public readonly string Label;
            public readonly int Number;
            public readonly ImmutableList<string> Codes;

            public SubItem(string label, int number, ImmutableList<string> codes)
            {
                Label = label;
                Number = number;
                Codes = codes;
            }
        }
    }
}
