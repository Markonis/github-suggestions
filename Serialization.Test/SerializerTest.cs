using System.Collections.Immutable;
using Xunit;

namespace Serialization.Test
{
    public class SerializerTest
    {
        [Fact]
        public void DeepCopy_ShouldCopyObjectsCorrectly()
        {

            TestItem original = new TestItem(
                name: "Item Name",
                count: 100,
                subItems: ImmutableList.Create(
                    new TestItem.SubItem(
                        label: "Sub Item 1",
                        number: 1,
                        codes: ImmutableList.Create("x", "y", "z")),
                    new TestItem.SubItem(
                        label: "Sub Item 2",
                        number: 2,
                        codes: ImmutableList.Create("a", "b", "c"))));

            TestItem copy = Serializer.DeepCopy<TestItem>(original);

            Assert.Equal(original.Name, copy.Name);
            Assert.Equal(original.Count, copy.Count);

            Assert.Equal(original.SubItems[0].Label, copy.SubItems[0].Label);
            Assert.Equal(original.SubItems[0].Number, copy.SubItems[0].Number);
            Assert.Equal(original.SubItems[0].Codes[0], copy.SubItems[0].Codes[0]);
            Assert.Equal(original.SubItems[0].Codes[1], copy.SubItems[0].Codes[1]);
            Assert.Equal(original.SubItems[0].Codes[2], copy.SubItems[0].Codes[2]);

            Assert.Equal(original.SubItems[1].Label, copy.SubItems[1].Label);
            Assert.Equal(original.SubItems[1].Number, copy.SubItems[1].Number);
            Assert.Equal(original.SubItems[1].Codes[0], copy.SubItems[1].Codes[0]);
            Assert.Equal(original.SubItems[1].Codes[1], copy.SubItems[1].Codes[1]);
            Assert.Equal(original.SubItems[1].Codes[2], copy.SubItems[1].Codes[2]);
        }
    }
}
