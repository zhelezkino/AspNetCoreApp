using AspNetCoreApp.Models;

namespace AspNetCoreApp.Services
{
    public interface IUserService
    {
        IEnumerable<UserModel> GetAll();
        UserModel? GetById(int id);
        UserModel Create(string name);
    }
}
