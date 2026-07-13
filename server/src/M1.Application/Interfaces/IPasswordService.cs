using M1.Domain.Entities;

namespace M1.Application.Interfaces;

public interface IPasswordService
{
    string Hash(User user, string password);
    bool Verify(User user, string hash, string password);
}
