namespace ArcadeLink.Payments.Api.Options;

public interface IApprovalRateProvider
{
    Task<double> GetAsync();
}
