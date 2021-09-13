using Mapster;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace MapsterMapValue
{
    class Program
    {
        static void Main(string[] args)
        {
            MapsterMapper.IMapper mMapper = new MapsterMapper.Mapper();
            mMapper.Config.Apply(new RequestMapper());

            IMapper localMapper = new Mapper(mMapper);

            var user = new User("Kolya", 23);

            Console.WriteLine("Map Original");
            var originalMap = localMapper.Map<User, UserVm>(user);
            ConsoleObjectToString(originalMap);
            Console.WriteLine();

            Console.WriteLine("Map by value");
            var valueMap = localMapper.Map<User, UserVm>(user, x => x.Description, "I Nicholas a ne Kolya");
            ConsoleObjectToString(valueMap);
            Console.WriteLine();

            Console.WriteLine("Map by collection");
            var users = new List<User>();
            users.Add(new User("Petya", 16));
            users.Add(new User("Slavik", 17));
            var valueMapList = localMapper.Map<User, UserVm>(users, x => x.Description, "I map common description");
            ConsoleObjectToString(valueMapList.ToArray());

            Console.ReadLine();
        }

        private static void ConsoleObjectToString<T>(params T[] source) where T : class, new()
        {
            for (var index = 0; index < source.Length;)
            {
                var o = source[index];
                Console.WriteLine($"Index: {++index}");
                foreach (var prop in o.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    Console.WriteLine($"{prop.Name}: {prop.GetValue(o)}");
                }
            }
        }
    }

    class RequestMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<User, UserVm>()
                .Map(x => x.Age, x => x.Age)
                .Map(x => x.Name, x => x.Name)
                .Map(x => x.Description,
                    x => MapContext.Current == null
                        ? default
                        : MapContext.Current.Parameters[$"{nameof(UserVm)}.{nameof(UserVm.Description)}"]);
        }
    }


    #region Mapper

    public interface IMapper
    {
        TDest Map<TSource, TDest>(TSource obj);
        TDest Map<TSource, TDest>(TSource obj, Expression<Func<TDest, string>> expr, object value);

        List<TDest> Map<TSource, TDest>(List<TSource> obj);
        List<TDest> Map<TSource, TDest>(List<TSource> obj, Expression<Func<TDest, string>> expr, object value);
    }

    public class Mapper : IMapper
    {
        private readonly MapsterMapper.IMapper _mapper;

        public Mapper(MapsterMapper.IMapper mapper)
        {
            _mapper = mapper;
        }

        public TDest Map<TSource, TDest>(TSource obj)
        {
            return obj != null ? _mapper.Map<TDest>(obj) : default;
        }

        public TDest Map<TSource, TDest>(TSource obj, Expression<Func<TDest, string>> expr, object value)
        {
            var propName = ((MemberExpression)expr.Body).Member.Name;

            return obj.BuildAdapter()
                .AddParameters($"{typeof(TDest).Name}.{propName}", value)
                .AdaptToType<TDest>();
        }

        public List<TDest> Map<TSource, TDest>(List<TSource> obj)
        {
            return _mapper.Map<List<TDest>>(obj);
        }

        public List<TDest> Map<TSource, TDest>(List<TSource> obj, Expression<Func<TDest, string>> expr, object value)
        {
            var propName = ((MemberExpression)expr.Body).Member.Name;

            return obj.BuildAdapter()
                .AddParameters($"{typeof(TDest).Name}.{propName}", value)
                .AdaptToType<List<TDest>>();
        }
    }

    #endregion

    #region Models

    class User
    {
        public User(string name, int age)
        {
            Age = age;
            Name = name;
        }

        public string Name { get; set; }
        public int Age { get; set; }
    }

    class UserVm
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string Description { get; set; }
    }

    #endregion
}