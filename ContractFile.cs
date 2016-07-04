using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    public class ContractFile
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        [Required]
        [DataType(DataType.Url)]
        public string FileUrl { get; set; }

        public virtual Contract Contract { get; set; }
    }
}