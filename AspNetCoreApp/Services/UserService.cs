using AspNetCoreApp.Models;
using AspNetCoreApp.Services;

namespace AspNetCoreApp.Services
{
    public class UserService : IUserService
    {
        private readonly List<UserModel> _users = new();
        private int _nextId = 1;

        public UserService()
        {
            if (_users.Count == 0)
            {
                _users.Add(new UserModel { Id = _nextId++, Name = "Alice" });
                _users.Add(new UserModel { Id = _nextId++, Name = "Bob" });
                _users.Add(new UserModel { Id = _nextId++, Name = "Tom" });
                _users.Add(new UserModel { Id = _nextId++, Name = "Jerry" });
                _users.Add(new UserModel { Id = _nextId++, Name = "Billy" });
                _users.Add(new UserModel { Id = _nextId++, Name = "Nancy" });
            }
        }

        public IEnumerable<UserModel> GetAll()
        {
            Console.WriteLine($"GetAll: returning {_users.Count} users");
            return _users;
        }

        public UserModel? GetById(int id) => _users.FirstOrDefault(u => u.Id == id);

        public UserModel Create(string name)
        {
            Console.WriteLine($"Current count: {_users.Count}, creating user: {name}, ");
            var user = new UserModel {Id = _nextId++, Name = name};
            _users.Add(user);
            Console.WriteLine($"After add: {_users.Count} users");
            return user;
        }
    }
}
