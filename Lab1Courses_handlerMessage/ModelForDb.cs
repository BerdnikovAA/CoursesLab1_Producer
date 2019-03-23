using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;


namespace Lab1Courses_handlerMessage
{
    public class person
    {
        public int Id { get; set; }

        public string Surname { get; set; }

        public string Name { get; set; }

        public string Patronymic { get; set; }

        public DateTime BirthDay { get; set; }

        public int RequestType { get; set; }

        public string Sex { get; set; }
        public string XMLString { get; set; }
    }

    class ModelForDb: DbContext
    {
        public DbSet<person> Persons { get; set; }

        public ModelForDb()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=DESKTOP-7U3L5EC\\SQLEXPRESS;Database=Courses_DB;Trusted_Connection=True;");
        }
    }
}
