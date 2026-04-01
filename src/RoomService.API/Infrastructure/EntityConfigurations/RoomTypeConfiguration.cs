using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomService.API.Domain.Entities;

namespace RoomService.API.Infrastructure.EntityConfigurations
{
    public class RoomTypeConfiguration : IEntityTypeConfiguration<RoomType>
    {
        public void Configure(EntityTypeBuilder<RoomType> builder)
        {
            builder.ToTable("RoomTypes");

            builder.HasKey(rt => rt.Id);

            builder.Property(rt => rt.Name).IsRequired().HasMaxLength(100);
            builder.Property(rt => rt.Capacity).IsRequired();
            builder.Property(rt => rt.BasePrice).IsRequired().HasColumnType("decimal(18,2)");
        }
    }
}