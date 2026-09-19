using Mofemofies.Data;

namespace MofemofiesTests;

public class ObjectPoolTests
{
    public class SomeIntegers : IResettable
    {
        public List<int> Values { get; set; } = [];

        void IResettable.Reset() => Values.Clear();
    }

    [Fact]
    public void RentAndReturn_SomeIntegers_CreateLessObjects()
    {
        for (var i = 1; i <= 3; i++)
        {
            ObjectPool.Clear();

            SomeIntegers ints1 = new()
            {
                Values = [1, 2, 3]
            };
            SomeIntegers ints2 = new()
            {
                Values = [4, 5, 6, 7, 8]
            };

            ObjectPool.Return(ints1);
            ObjectPool.Return(ints2);

            Assert.Equal(2, ObjectPool.Count<SomeIntegers>());

            var ints3 = ObjectPool.Rent<SomeIntegers>();

            Assert.Empty(ints3.Values);
            Assert.Equal(1, ObjectPool.Count<SomeIntegers>());

            ObjectPool.Return(ints3);
            Assert.Equal(2, ObjectPool.Count<SomeIntegers>());
        }
    }

    [Fact]
    public void Retain_SomeIntegers_RemoveObjectsCorrectly()
    {
        for (var i = 1; i <= 8; i++)
        {
            ObjectPool.Return(new SomeIntegers());
        }

        ObjectPool.Retain<SomeIntegers>(4);
        Assert.Equal(4, ObjectPool.Count<SomeIntegers>());
    }
}
