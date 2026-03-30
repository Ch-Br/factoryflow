namespace FactoryFlow.SharedKernel.Domain;

public interface ICurrentUserService
{
    string UserId { get; }
    string UserName { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
