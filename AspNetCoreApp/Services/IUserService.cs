using AspNetCoreApp.Models;

namespace AspNetCoreApp.Services
{
    public interface IUserService
    {
        IEnumerable<UserFunc8> GetAll();
        UserFunc8? GetById(int id);
        UserFunc8 Create(string name);
    }
}
