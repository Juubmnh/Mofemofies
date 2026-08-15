using Mofemofies.Data;

namespace MofemofiesTests;

public class FactoryTests
{
    public class MovieData
    {
        public string? Name { get; set; }
        public TimeSpan Time { get; set; }

        internal MovieData()
        {

        }
    }

    public class MovieDataFactory : MofiFactory<MovieDataFactory, MovieData>
    {
        public MovieDataFactory()
        {

        }

        public MovieDataFactory(Action<MovieData> creator)
        {
            _creator = creator;
        }

        protected override MovieData Instantiate() => new();
    }

    [Fact]
    public void Create_MovieData_Succeed()
    {
        var name = "An Awesome Movie";
        TimeSpan time = TimeSpan.FromHours(1);

        MovieDataFactory factory = new(movieData =>
        {
            movieData.Name = name;
            movieData.Time = time;
        });

        var instance = factory.Create();

        Assert.Equal(name, instance.Name);
        Assert.Equal(time, instance.Time);
    }

    [Fact]
    public void Inherit_MovieDataFactory_Succeed()
    {
        var name1 = "An Awesome Movie";
        var name2 = "A Super Great Movie";
        TimeSpan time = TimeSpan.FromHours(1);

        MovieDataFactory factory1 = new(movieData =>
        {
            movieData.Name = name1;
            movieData.Time = time;
        });

        var factory2 = factory1.Inherit(movieData => movieData.Name = name2);

        var instance1 = factory1.Create();

        Assert.Equal(name1, instance1.Name);
        Assert.Equal(time, instance1.Time);

        var instance2 = factory2.Create();

        Assert.Equal(name2, instance2.Name);
        Assert.Equal(time, instance2.Time);
    }
}
