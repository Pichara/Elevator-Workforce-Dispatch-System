using ElevatorMaintenanceSystem.Models;

namespace ElevatorMaintenanceSystem.Services;

/// <summary>
/// Computes deterministic worker proximity suggestions for one selected ticket/elevator context.
/// </summary>
public class ProximityRankingService : IProximityRankingService
{
    private const double EarthRadiusKm = 6371.0088;

    public IReadOnlyList<WorkerProximitySuggestion> RankWorkers(ProximityRankRequest request, int maxResults = 10)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (maxResults <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxResults), "maxResults must be greater than zero.");
        }

        if (request.Candidates.Count == 0)
        {
            return [];
        }

        return request.Candidates
            .Select(candidate => new
            {
                Candidate = candidate,
                DistanceKm = CalculateDistanceKm(
                    request.ElevatorLatitude,
                    request.ElevatorLongitude,
                    candidate.WorkerLatitude,
                    candidate.WorkerLongitude)
            })
            .OrderBy(result => result.DistanceKm)
            .ThenBy(result => GetAvailabilityPriority(result.Candidate.Availability))
            .ThenBy(result => result.Candidate.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(result => result.Candidate.WorkerId)
            .Take(maxResults)
            .Select((result, index) => new WorkerProximitySuggestion(
                WorkerId: result.Candidate.WorkerId,
                DisplayName: result.Candidate.DisplayName,
                Availability: result.Candidate.Availability,
                DistanceKm: result.DistanceKm,
                Rank: index + 1))
            .ToList();
    }

    private static int GetAvailabilityPriority(WorkerAvailabilityStatus availability)
    {
        return availability == WorkerAvailabilityStatus.Available ? 0 : 1;
    }

    private static double CalculateDistanceKm(
        double originLatitude,
        double originLongitude,
        double destinationLatitude,
        double destinationLongitude)
    {
        var latitudeDelta = ToRadians(destinationLatitude - originLatitude);
        var longitudeDelta = ToRadians(destinationLongitude - originLongitude);
        var originLatitudeRadians = ToRadians(originLatitude);
        var destinationLatitudeRadians = ToRadians(destinationLatitude);

        var latitudeSin = Math.Sin(latitudeDelta / 2.0);
        var longitudeSin = Math.Sin(longitudeDelta / 2.0);

        var a = (latitudeSin * latitudeSin) +
                (Math.Cos(originLatitudeRadians) * Math.Cos(destinationLatitudeRadians) * longitudeSin * longitudeSin);

        var c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
        return EarthRadiusKm * c;
    }

    private static double ToRadians(double degrees)
    {
        return degrees * (Math.PI / 180.0);
    }
}
