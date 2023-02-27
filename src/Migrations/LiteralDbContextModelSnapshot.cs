﻿// <auto-generated />
using System;
using LiteralCollector.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace LiteralCollector.Migrations
{
    [DbContext(typeof(LiteralDbContext))]
    partial class LiteralDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.3");

            modelBuilder.Entity("LiteralCollector.Database.Literal", b =>
                {
                    b.Property<int>("LiteralId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("TEXT");

                    b.HasKey("LiteralId");

                    b.HasAlternateKey("Value")
                        .HasName("AK_Literal_Value");

                    b.ToTable("Literals");
                });

            modelBuilder.Entity("LiteralCollector.Database.LiteralLocation", b =>
                {
                    b.Property<int>("SourceFileId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("LiteralId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("LineStart")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasDefaultValueSql("0");

                    b.Property<int>("ColumnStart")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasDefaultValueSql("0");

                    b.Property<int>("ColumnEnd")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasDefaultValueSql("0");

                    b.Property<int>("LineEnd")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasDefaultValueSql("0");

                    b.HasKey("SourceFileId", "LiteralId", "LineStart", "ColumnStart");

                    b.HasIndex("LiteralId");

                    b.ToTable("LiteralLocations");
                });

            modelBuilder.Entity("LiteralCollector.Database.Project", b =>
                {
                    b.Property<int>("ProjectId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("BaseFolder")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("TEXT");

                    b.Property<string>("HostName")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("TEXT");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("TEXT");

                    b.HasKey("ProjectId");

                    b.ToTable("Projects");
                });

            modelBuilder.Entity("LiteralCollector.Database.Scan", b =>
                {
                    b.Property<int>("ScanId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("DurationSecs")
                        .HasColumnType("INTEGER");

                    b.Property<int>("LiteralCount")
                        .HasColumnType("INTEGER");

                    b.Property<int>("PreviousLiteralCount")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ProjectId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTimeOffset>("StartTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("TEXT");

                    b.HasKey("ScanId");

                    b.HasIndex("ProjectId");

                    b.ToTable("Scans");
                });

            modelBuilder.Entity("LiteralCollector.Database.SourceFile", b =>
                {
                    b.Property<int>("SourceFileId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("FileName")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("TEXT");

                    b.Property<string>("Path")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("TEXT");

                    b.Property<int>("ScanId")
                        .HasColumnType("INTEGER");

                    b.HasKey("SourceFileId");

                    b.HasIndex("ScanId");

                    b.ToTable("SourceFiles");
                });

            modelBuilder.Entity("LiteralCollector.Database.LiteralLocation", b =>
                {
                    b.HasOne("LiteralCollector.Database.Literal", "Literal")
                        .WithMany("LiteralLocations")
                        .HasForeignKey("LiteralId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("LiteralCollector.Database.SourceFile", "SourceFile")
                        .WithMany("LiteralLocations")
                        .HasForeignKey("SourceFileId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Literal");

                    b.Navigation("SourceFile");
                });

            modelBuilder.Entity("LiteralCollector.Database.Scan", b =>
                {
                    b.HasOne("LiteralCollector.Database.Project", "Project")
                        .WithMany("Scans")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Project");
                });

            modelBuilder.Entity("LiteralCollector.Database.SourceFile", b =>
                {
                    b.HasOne("LiteralCollector.Database.Scan", "Scan")
                        .WithMany("SourceFiles")
                        .HasForeignKey("ScanId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Scan");
                });

            modelBuilder.Entity("LiteralCollector.Database.Literal", b =>
                {
                    b.Navigation("LiteralLocations");
                });

            modelBuilder.Entity("LiteralCollector.Database.Project", b =>
                {
                    b.Navigation("Scans");
                });

            modelBuilder.Entity("LiteralCollector.Database.Scan", b =>
                {
                    b.Navigation("SourceFiles");
                });

            modelBuilder.Entity("LiteralCollector.Database.SourceFile", b =>
                {
                    b.Navigation("LiteralLocations");
                });
#pragma warning restore 612, 618
        }
    }
}
