using ElevatorMaintenanceSystem.Infrastructure;
using Xunit;

namespace ElevatorMaintenanceSystem.Tests.Infrastructure;

public class GpsCoordinateValidatorTests
{
    private readonly GpsCoordinateValidator _validator = new();

    [Fact]
    public void Validate_Throws_WhenLatitudeIsOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _validator.Validate(91, -70));
    }

    [Fact]
    public void Validate_Throws_WhenLongitudeIsOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _validator.Validate(45, -181));
    }

    [Fact]
    public void CreatePoint_ReturnsGeoJsonPoint_WithLongitudeThenLatitude()
    {
        var point = _validator.CreatePoint(43.6532, -79.3832);

        Assert.Equal(43.6532, point.Coordinates.Latitude, 4);
        Assert.Equal(-79.3832, point.Coordinates.Longitude, 4);
    }
}
