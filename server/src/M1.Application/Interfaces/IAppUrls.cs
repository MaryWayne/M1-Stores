namespace M1.Application.Interfaces;

/// <summary>Environment-specific public URLs (frontend origin for email links).</summary>
public interface IAppUrls
{
    string FrontendUrl { get; }
}
