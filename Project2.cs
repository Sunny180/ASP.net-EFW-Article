using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace SMTW_Management.Models
{ 
    public class RequestProject2ListDto
    {
        [Range(1, int.MaxValue)]
        public int? Catalog_Id { get; set; }
        public string Title { get; set; }
        public int? User_Id { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        [Range(0, 1)]
        public int? Archive { get; set; }
        /// <summary>
        /// {"Sort": "Title", "Direction": 0}
        /// </summary>
        /// <value></value>
        public string Order { get; set; }
    }
    public class Project2OverviewDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int User_Id { get; set; }
        public DateTimeOffset PublishTime { get; set; }
        public int Admin_Id { get; set; }
        public DateTimeOffset CreateTime { get; set; }
        public DateTimeOffset UpdateTime { get; set; }
        public int Archive { get; set; }
    }

    public class Project2DetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Content { get; set; }
        public int User_Id { get; set; }
        public List<int> Catalogs { get; set; }
        public DateTimeOffset PublishTime { get; set; }
        public int Admin_Id { get; set; }
        public DateTimeOffset CreateTime { get; set; }
        public DateTimeOffset UpdateTime { get; set; }
        public int Archive { get; set; }
    }

    public class Project2PostDto
    {
        [Required]
        public string Title { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public string Content { get; set; }
        [Required]
        public int User_Id { get; set; }
        [Required]
        public List<int> Catalogs { get; set; }
        public DateTimeOffset PublishTime { get; set; }
    }

    public class Project2PutDto
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public int User_Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Content { get; set; }
        public List<int> Catalogs { get; set; }
        public DateTimeOffset PublishTime { get; set; }
    }
}