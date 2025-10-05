namespace Tienda.src.Application.Jobs.Interfaces
{
    public interface IUserJob
    {
        Task DeleteUnconfirmedAsync();
    }
}