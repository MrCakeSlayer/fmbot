﻿using System.Diagnostics.CodeAnalysis;
using Bot.Domain.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bot.Persistence.EntityFrameWork.Configurations
{

    /// <summary>
    /// This class contains the configurations for the <see cref="Request"/> table. 
    /// </summary>
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public class RequestsConfiguration : IEntityTypeConfiguration<Request>
    {
        public void Configure(EntityTypeBuilder<Request> builder)
        {
            builder.ToTable("requests");
            builder.HasKey(q => new { q.TimeStamp, q.Id });

            builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
            builder.Property(x => x.ServerId).HasColumnName("serverid");
            builder.Property(x => x.UserId).HasColumnName("userid").IsRequired();
            builder.Property(x => x.Command).HasColumnName("command").IsRequired();
            builder.Property(x => x.ErrorMessage).HasColumnName("errormessage").IsRequired(false);
            builder.Property(x => x.IsSuccessFull).HasColumnName("issuccessfull").IsRequired();
            builder.Property(x => x.RunTime).HasColumnName("runtime").IsRequired();
            builder.Property(x => x.TimeStamp).HasColumnName("timestamp").IsRequired();
        }
    }
}