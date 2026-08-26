using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BookStore.Domain.Entities
{
    public class FlashSaleCampaign
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        
        public bool IsActive { get; set; } = true;

        public virtual ICollection<FlashSale> FlashSales { get; set; } = new List<FlashSale>();

        public bool IsValid => IsActive && BookStore.Domain.Common.TimeHelper.GetVnTime() >= StartTime && BookStore.Domain.Common.TimeHelper.GetVnTime() <= EndTime;
    }
}
