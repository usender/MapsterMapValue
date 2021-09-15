using Mapster;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MapsterMapValue
{

    class Program
    {
        private static readonly Random _rnd = new Random();

        static void Main(string[] args)
        {
            IMapper localMapper = GetLocalMapper();

            var user = GenerateUser();
            Console.WriteLine("Map Original");
            var originalMap = localMapper.Map<User, UserVm>(user);
            ConsoleWriteJsonObject(originalMap);
            ConsoleWriteEndLine();

            Console.WriteLine("Map by value");
            var valueMap = localMapper.Map<User, UserVm>(user, "Description", "I Nicholas a ne Kolya");
            ConsoleWriteJsonObject(valueMap);
            ConsoleWriteEndLine();

            Console.WriteLine("Map by collection");
            var users = new List<User>();
            users.Add(GenerateUser());
            users.Add(GenerateUser());
            var valueMapList = localMapper.Map<IList<User>, IList<UserVm>>(users, nameof(UserVm.Description), "Common description for user and roles");
            ConsoleWriteJsonObject(valueMapList.ToArray());

            Console.ReadLine();
        }

        private static void ConsoleWriteJsonObject<T>(params T[] source)
        {
            for (int i = 0; i < source.Length;)
            {
                var obj = source[i++];
                if (source.Length > 1)
                    Console.WriteLine($"Index: {i}");
                Console.WriteLine(obj.ToJson());
            }
        }

        private static IMapper GetLocalMapper()
        {
            MapsterMapper.IMapper mMapper = new MapsterMapper.Mapper();
            mMapper.Config.Apply(new RequestMapper());

            IMapper localMapper = new Mapper(mMapper);
            return localMapper;
        }

        private static User GenerateUser() => new User(GenerateString(8), _rnd.Next(18, 99), Range(1).Select((_, i) => new Role(i + 1, GenerateString(4))));
        private static string GenerateString(int len) => string.Join("", Range(len).Select(x => (char)('A' + _rnd.Next(0, 26)))).ToLower();
        private static string Range(int count, char c = '_') => string.Join("", Enumerable.Repeat(c, count));
        private static void ConsoleWriteEndLine() => Console.WriteLine(Range(100, '-'));
    }

    class RequestMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<User, UserVm>()
                .Map(x => x.Age, x => x.Age)
                .Map(x => x.Name, x => x.Name)
                .Map(x => x.Roles, x => x.Roles)
                .Map(x => x.Description, x => MapDescription(MapContext.Current, x));

            config.NewConfig<Role, RoleVm>()
                .Map(x => x.Id, x => x.Id)
                .Map(x => x.Name, x => x.Name)
                .Map(x => x.Description, x => MapDescription(MapContext.Current, x));
        }

        public object MapDescription<T>(MapContext context, T obj)
        {
            if (context != null)
            {
                var value = context.Parameters[nameof(UserVm.Description)];
                return value;
            }

            return null;
        }
    }


    #region Application Mapper

    public interface IMapper
    {
        TDest Map<TSource, TDest>(TSource obj);
        TDest Map<TSource, TDest>(TSource obj, string propName, object value);
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

        public TDest Map<TSource, TDest>(TSource obj, string name, object value)
        {
            return obj.BuildAdapter()
                .AddParameters(name, value)
                .AdaptToType<TDest>();
        }
    }

    #endregion

    #region Models

    #region Domain models

    class User
    {
        public User(string name, int age, IEnumerable<Role> roles)
        {
            Age = age;
            Name = name;
            Roles = roles.ToList();
        }

        public string Name { get; set; }
        public int Age { get; set; }

        public List<Role> Roles { get; set; }
    }

    class Role
    {
        public Role(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public string Name { get; set; }
        public int Id { get; set; }
    }

    #endregion

    #region View models

    class UserVm
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string Description { get; set; }

        public List<RoleVm> Roles { get; set; }
    }

    class RoleVm
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public string Description { get; set; }
    }

    #endregion

    #endregion

    #region Utils

    public static class JsonExtensions
    {
        public static string ToJson(this object value)
        {
            return JsonConvert.SerializeObject(value, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented
            });
        }
    }

    #endregion
}