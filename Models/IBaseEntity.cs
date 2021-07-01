using System;

namespace Bitar.Models
{
    public interface IBaseEntity
    {
        DateTime DateCreated { get; set; }
        DateTime DateUpdated { get; set; }
        int CreatedBy { get; set; }
        int UpdatedBy { get; set; }
    }
}