namespace WebSchoolPlanner.ApiModels;

/// <summary>
/// Detailed information to crop an image to a rect
/// </summary>
public class CropModel
{
    /// <summary>
    /// The point where the cropping begins
    /// </summary>
    public Point Point { get; set; }

    /// <summary>
    /// The size in the left and right for the cropping
    /// </summary>
    public int Size { get; set; }
}
