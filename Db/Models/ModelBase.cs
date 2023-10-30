using System.ComponentModel.DataAnnotations;

namespace WebSchoolPlanner.Db.Models;

/// <summary>
/// The base for every DB model
/// </summary>
public abstract class ModelBase : IEquatable<ModelBase>
{
    /// <summary>
    /// The id of the record as a guid
    /// </summary>
    [Key]
    public string Id { get; set; }

    /// <summary>
    /// The default constructor that generate a new id
    /// </summary>
    public ModelBase()
    {
        Id = Guid.NewGuid().ToString();
    }

    public bool Equals(ModelBase? other)
    {
        return Id == other?.Id;
    }
}
