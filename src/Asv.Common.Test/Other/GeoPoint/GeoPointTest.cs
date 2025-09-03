using JetBrains.Annotations;
using Xunit;

namespace Asv.Common.Test.Other;

[TestSubject(typeof(Common.GeoPoint))]
public class GeoPointTest
{
    [Fact]
    public void GeoPoint_ShouldWorkCorrectly()
    {
        // Test constructor and properties
        var point1 = new GeoPoint(45.5, -122.6, 100.0);
        Assert.Equal(45.5, point1.Latitude);
        Assert.Equal(-122.6, point1.Longitude);
        Assert.Equal(100.0, point1.Altitude);

        // Test static properties
        var nanPoint = GeoPoint.NaN;
        Assert.True(double.IsNaN(nanPoint.Latitude));
        Assert.True(double.IsNaN(nanPoint.Longitude));
        Assert.True(double.IsNaN(nanPoint.Altitude));

        var zeroPoint = GeoPoint.Zero;
        Assert.Equal(0.0, zeroPoint.Latitude);
        Assert.Equal(0.0, zeroPoint.Longitude);
        Assert.Equal(0.0, zeroPoint.Altitude);

        var zeroWithAltPoint = GeoPoint.ZeroWithAlt;
        Assert.Equal(0.0, zeroWithAltPoint.Latitude);
        Assert.Equal(0.0, zeroWithAltPoint.Longitude);
        Assert.Equal(0.0, zeroWithAltPoint.Altitude);

        // Test addition operator
        var point2 = new GeoPoint(10.0, 20.0, 30.0);
        var point3 = new GeoPoint(5.0, 10.0, 15.0);
        var sum = point2 + point3;
        Assert.Equal(15.0, sum.Latitude);
        Assert.Equal(30.0, sum.Longitude);
        Assert.Equal(45.0, sum.Altitude);

        // Test subtraction operator
        var diff = point2 - point3;
        Assert.Equal(5.0, diff.Latitude);
        Assert.Equal(10.0, diff.Longitude);
        Assert.Equal(15.0, diff.Altitude);

        // Test basic equality
        var point4 = new GeoPoint(45.5, -122.6, 100.0);
        Assert.True(point1.Equals(point4));
        Assert.True(point1 == point4);
        Assert.False(point1 != point4);
        Assert.False(point1.Equals(point2));

        // Test ToString (basic verification that it returns a string)
        var stringResult = point1.ToString();
        Assert.NotNull(stringResult);
        Assert.NotEmpty(stringResult);

        // Test Random method
        var randomPoint = GeoPoint.Random();    
        Assert.True(randomPoint.Latitude >= -90.0 && randomPoint.Latitude <= 90.0);
        Assert.True(randomPoint.Longitude >= -180.0 && randomPoint.Longitude <= 180.0);
        Assert.True(randomPoint.Altitude >= -10000.0 && randomPoint.Altitude <= 10000.0);

        // Test Random with custom bounds
        var customRandomPoint = GeoPoint.Random(null, 0.0, 45.0, 0.0, 90.0, 0.0, 500.0);
        Assert.True(customRandomPoint.Latitude >= 0.0 && customRandomPoint.Latitude <= 45.0);
        Assert.True(customRandomPoint.Longitude >= 0.0 && customRandomPoint.Longitude <= 90.0);
        Assert.True(customRandomPoint.Altitude >= 0.0 && customRandomPoint.Altitude <= 500.0);
    }
    
    [Theory]
    [InlineData(89.999999995, 179.999999995, 0.0, 89.999999999, 179.999999999, 0.0)]
    [InlineData(-45.123456789, 122.123456789, -10.0, -45.123456784, 122.123456784, -10.0)]
    [InlineData(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN)]
    public void GeoPoint_Equality_ShouldBeEquals(
        double lat1, double lon1, double alt1,
        double lat2, double lon2, double alt2)
    {
        // Arrange
        var point1 = new GeoPoint(lat1, lon1, alt1);
        var point2 = new GeoPoint(lat2, lon2, alt2);
        
        // Act
        var result = point1.Equals(point2);
            
        // Assert
        Assert.True(result);
    }
    
    [Theory]
    [InlineData(-45.0, 122.6, -100.0, -45.00000002, 122.6, -100.0)]
    [InlineData(55.0, 179.99999999, 0.0, 55.0, -179.99999999, 0.0)]
    [InlineData(0.0, 0.0, double.NaN, 0.0, 0.0, 0.0)]
    public void GeoPoint_Equality_ShouldNotBeEquals(
        double lat1, double lon1, double alt1,
        double lat2, double lon2, double alt2)
    {
        // Arrange
        var point1 = new GeoPoint(lat1, lon1, alt1);
        var point2 = new GeoPoint(lat2, lon2, alt2);
        
        // Act
        var result = point1.Equals(point2);
        
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public void GeoPoint_EqualityWithCustomEpsilon_ShouldBeEquals()
    {
        // Arrange
        var point1 = new GeoPoint(0.002, 1.0, 10.2);
        var point2 = new GeoPoint(0.001, 1.0, 10.2);
        const double epsilon = 0.01;
        
        // Act
        var result = point1.Equals(point2, epsilon);
        
        // Assert
        Assert.True(result);
    }
    
    [Fact]
    public void GeoPoint_EqualityWithCustomEpsilon_ShouldNotBeEquals()
    {
        // Arrange
        var point1 = new GeoPoint(0.02, 1.0, 10.2);
        var point2 = new GeoPoint(0.01, 1.0, 10.2);
        const double epsilon = 0.01;
        
        // Act
        var result = point1.Equals(point2, epsilon);
            
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public void GeoPoint_ToStringThenParse_ShouldWorkCorrectly()
    {
        // Arrange
        var originalGeoPoint = new GeoPoint(45.123456, -73.987654, 150.5);
    
        // Act
        var geoPointString = originalGeoPoint.ToString();
        var parsedGeoPoint = GeoPoint.Parse(geoPointString);
    
        // Assert
        Assert.Equal(originalGeoPoint.Latitude, parsedGeoPoint.Latitude,5);
        Assert.Equal(originalGeoPoint.Longitude, parsedGeoPoint.Longitude,5);
        Assert.Equal(originalGeoPoint.Altitude, parsedGeoPoint.Altitude,7);
    
        // Test with different values including edge cases
        var testCases = new[]
        {
            new GeoPoint(51.5074, -0.1278, 35), // London coordinates
            new GeoPoint(-33.8688, 151.2093, 58), // Sydney coordinates
            new GeoPoint(0, 0, 0),
            new GeoPoint(90, 180, 1000),
            new GeoPoint(-90, -180, -100),
        };
    
        foreach (var testCase in testCases)
        {
            var stringRepresentation = testCase.ToString();
            var reparsedGeoPoint = GeoPoint.Parse(stringRepresentation);
            Assert.Equal(testCase.Latitude, reparsedGeoPoint.Latitude,7);
            Assert.Equal(testCase.Longitude, reparsedGeoPoint.Longitude,7);
            Assert.Equal(testCase.Altitude, reparsedGeoPoint.Altitude,7);
        }
    }
}