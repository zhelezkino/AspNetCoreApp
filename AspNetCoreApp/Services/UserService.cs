using AspNetCoreApp.Models;
using AspNetCoreApp.Services;

namespace AspNetCoreApp.Services
{
    public class UserService : IUserService
    {
        private readonly List<UserFunc8> _users = new();
        private int _nextId = 1;

        public UserService()
        {
            if (_users.Count == 0)
            {
                _users.Add(new UserFunc8(_nextId++, "Alice"));
                _users.Add(new UserFunc8(_nextId++, "Bob"));
                _users.Add(new UserFunc8(_nextId++, "Tom"));
                _users.Add(new UserFunc8(_nextId++, "Jerry"));
            }
        }

        public IEnumerable<UserFunc8> GetAll()
        {
            Console.WriteLine($"GetAll: returning {_users.Count} users");
            return _users;
        }

        public UserFunc8? GetById(int id) => _users.FirstOrDefault(u => u.Id == id);

        public UserFunc8 Create(string name)
        {
            Console.WriteLine($"Current count: {_users.Count}, creating user: {name}, ");
            var user = new UserFunc8(_nextId++, name);
            _users.Add(user);
            Console.WriteLine($"After add: {_users.Count} users");
            return user;
        }
    }
}
